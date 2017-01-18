// Copyright (C) 2011, Xamarin Inc.
// Copyright (C) 2010, Novell Inc.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using Mono.Linker;
using System.IO;

namespace Xamarin.Windows.Tasks
{
	public class ResolveAssemblies : Task
	{
		// MUST BE SORTED CASE-INSENSITIVE
		internal static readonly string[] FrameworkAssembliesToTreatAsUserAssemblies = {
		};

		// The user's assemblies to package
		[Required]
		public ITaskItem[] Assemblies { get; set; }

		[Required]
		public string ReferenceAssembliesDirectory { get; set; }

		public string I18nAssemblies { get; set; }

		// The user's assemblies, and all referenced assemblies
		[Output]
		public ITaskItem[] ResolvedAssemblies { get; set; }

		[Output]
		public ITaskItem[] ResolvedUserAssemblies { get; set; }

		[Output]
		public ITaskItem[] ResolvedFrameworkAssemblies { get; set; }

		[Output]
		public ITaskItem[] ResolvedPdbFiles { get; set; }

		[Output]
		public ITaskItem[] ResolvedMdbFiles { get; set; }

		public override bool Execute()
		{
			using (var resolver = new DirectoryAssemblyResolver(Log.LogWarning, loadDebugSymbols: false)) {
				return Execute(resolver);
			}
		}

		bool Execute(DirectoryAssemblyResolver resolver)
		{
			Log.LogDebugMessage("ResolveAssemblies Task");
			Log.LogDebugMessage("  ReferenceAssembliesDirectory: {0}", ReferenceAssembliesDirectory);
			Log.LogDebugMessage("  I18nAssemblies: {0}", I18nAssemblies);
			Log.LogDebugTaskItems("  Assemblies:", Assemblies);

			foreach (var dir in ReferenceAssembliesDirectory.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
				resolver.SearchDirectories.Add(dir);

			var assemblies = new HashSet<string>();

			var topAssemblyReferences = new List<AssemblyDefinition>();

			try {
				foreach (var assembly in Assemblies) {
					var assembly_path = Path.GetDirectoryName(assembly.ItemSpec);

					if (!resolver.SearchDirectories.Contains(assembly_path))
						resolver.SearchDirectories.Add(assembly_path);

					// Add each user assembly and all referenced assemblies (recursive)
					var assemblyDef = resolver.Load(assembly.ItemSpec);
					if (assemblyDef == null)
						throw new InvalidOperationException("Failed to load assembly " + assembly.ItemSpec);
					topAssemblyReferences.Add(assemblyDef);
					assemblies.Add(Path.GetFullPath(assemblyDef.MainModule.FullyQualifiedName));
				}
			} catch (Exception ex) {
				Log.LogError("Exception while loading assemblies: {0}", ex);
				return false;
			}
			try {
				foreach (var assembly in topAssemblyReferences)
					AddAssemblyReferences(resolver, assemblies, assembly, true);
			} catch (Exception ex) {
				Log.LogError("Exception while loading assemblies: {0}", ex);
				return false;
			}

			// Add I18N assemblies if needed
			AddI18nAssemblies(resolver, assemblies);

			ResolvedAssemblies = assemblies.Select(a => new TaskItem(a)).ToArray();
			// mdb files retain the .dll/.exe, e.g. mscorlib.dll.mdb
			ResolvedMdbFiles = assemblies.Select(a => $"{a}.mdb").Where(File.Exists).Select(a => new TaskItem(a)).ToArray();
			// pdb files have the .dll/.exe removed, e.g. mscorlib.pdb
			ResolvedPdbFiles = assemblies.Select(a => a.Substring(0, a.LastIndexOf('.'))).Select(a => $"{a}.pdb")
					.Where(File.Exists).Select(a => new TaskItem(a)).ToArray();
			ResolvedFrameworkAssemblies = ResolvedAssemblies.Where(p => IsFrameworkAssembly(p.ItemSpec, true)).ToArray();
			ResolvedUserAssemblies = ResolvedAssemblies.Where(p => !IsFrameworkAssembly(p.ItemSpec, true)).ToArray();

			Log.LogDebugTaskItems("  [Output] ResolvedAssemblies:", ResolvedAssemblies);
			Log.LogDebugTaskItems("  [Output] ResolvedUserAssemblies:", ResolvedUserAssemblies);
			Log.LogDebugTaskItems("  [Output] ResolvedFrameworkAssemblies:", ResolvedFrameworkAssemblies);
			Log.LogDebugTaskItems("  [Output] ResolvedMdbFiles:", ResolvedMdbFiles);
			Log.LogDebugTaskItems("  [Output] ResolvedPdbFiles:", ResolvedPdbFiles);

			return !Log.HasLoggedErrors;
		}

		int indent = 2;

		void AddAssemblyReferences(DirectoryAssemblyResolver resolver, ICollection<string> assemblies, AssemblyDefinition assembly, bool topLevel)
		{
			var fqname = assembly.MainModule.FullyQualifiedName;
			var fullPath = Path.GetFullPath(fqname);

			// Don't repeat assemblies we've already done
			if (!topLevel && assemblies.Contains(fullPath))
				return;

			Log.LogMessage(MessageImportance.Low, "{0}Adding assembly reference for {1}, recursively...", new string(' ', indent), assembly.Name);
			indent += 2;
			// Add this assembly
			if (!topLevel && assemblies.All(a => new AssemblyNameDefinition(a, null).Name != assembly.Name.Name))
				assemblies.Add(fullPath);

			// Recurse into each referenced assembly
			foreach (AssemblyNameReference reference in assembly.MainModule.AssemblyReferences) {
				var reference_assembly = resolver.Resolve(reference);
				AddAssemblyReferences(resolver, assemblies, reference_assembly, false);
			}
			indent -= 2;
		}
		
		public static I18nAssemblies ParseI18nAssemblies(string i18n)
		{
			if (string.IsNullOrWhiteSpace(i18n))
				return Mono.Linker.I18nAssemblies.None;

			var assemblies = Mono.Linker.I18nAssemblies.None;

			foreach (var part in i18n.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)) {
				var assembly = part.Trim();
				if (string.IsNullOrEmpty(assembly))
					continue;

				try {
					assemblies |= (I18nAssemblies)Enum.Parse(typeof(I18nAssemblies), assembly, true);
				} catch {
					throw new FormatException("Unknown value for i18n: " + assembly);
				}
			}

			return assemblies;
		}

		void AddI18nAssemblies(DirectoryAssemblyResolver resolver, ICollection<string> assemblies)
		{
			var i18n = ParseI18nAssemblies(I18nAssemblies);

			// Check if we should add any I18N assemblies
			if (i18n == Mono.Linker.I18nAssemblies.None)
				return;

			assemblies.Add(ResolveI18nAssembly(resolver, "I18N"));

			if (i18n.HasFlag(Mono.Linker.I18nAssemblies.CJK))
				assemblies.Add(ResolveI18nAssembly(resolver, "I18N.CJK"));

			if (i18n.HasFlag(Mono.Linker.I18nAssemblies.MidEast))
				assemblies.Add(ResolveI18nAssembly(resolver, "I18N.MidEast"));

			if (i18n.HasFlag(Mono.Linker.I18nAssemblies.Other))
				assemblies.Add(ResolveI18nAssembly(resolver, "I18N.Other"));

			if (i18n.HasFlag(Mono.Linker.I18nAssemblies.Rare))
				assemblies.Add(ResolveI18nAssembly(resolver, "I18N.Rare"));

			if (i18n.HasFlag(Mono.Linker.I18nAssemblies.West))
				assemblies.Add(ResolveI18nAssembly(resolver, "I18N.West"));
		}

		string ResolveI18nAssembly(DirectoryAssemblyResolver resolver, string name)
		{
			var assembly = resolver.Resolve(AssemblyNameReference.Parse(name));
			return Path.GetFullPath(assembly.MainModule.FullyQualifiedName);
		}

		public bool IsFrameworkAssembly(string assembly, bool checkSdkPath)
		{
			//var assemblyName = Path.GetFileName(assembly);

			//if (Profile.SharedRuntimeAssemblies.Contains(assemblyName, StringComparer.InvariantCultureIgnoreCase)) {
			//	bool treatAsUser = Array.BinarySearch (FrameworkAssembliesToTreatAsUserAssemblies, assemblyName, StringComparer.OrdinalIgnoreCase) >= 0;
			//	// Framework assemblies don't come from outside the SDK Path;
			//	// user assemblies do
			//	if (checkSdkPath && treatAsUser && ReferenceAssembliesDirectory != null) {
			//		return ExistsInFrameworkPath (assembly);
			//	}
			//	return true;
			//}
			return ReferenceAssembliesDirectory == null || !checkSdkPath ? false : ExistsInFrameworkPath(assembly);
		}

		public bool ExistsInFrameworkPath(string assembly)
		{
			return ReferenceAssembliesDirectory.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
					.Select(p => new Uri(p).LocalPath)
					.Any(p => assembly.StartsWith(p, StringComparison.CurrentCultureIgnoreCase));
		}
	}
}
