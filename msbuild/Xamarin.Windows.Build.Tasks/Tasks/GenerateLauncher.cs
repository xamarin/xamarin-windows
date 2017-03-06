using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Xamarin.Windows.Tasks.Properties;

namespace Xamarin.Windows.Tasks
{
	public class GenerateLauncher : Task
	{

		public ITaskItem[] AotAssemblies { get; set; }

		public ITaskItem[] BundledAssemblies { get; set; }

		[Required]
		public string OutputDirectory { get; set; }

		public string LauncherTemplatePath { get; set; }

		public string LauncherFileName { get; set; }

		public bool UseCustomPlatformImpl { get; set; }

		public string UserSymbolPrefix { get; set; }

		public string CustomDefines { get; set; }

		public string MainAssemblyName { get; set; }

		public bool SkipMain { get; set; }

		[Output]
		public ITaskItem[] GeneratedFiles { get; set; }

		public override bool Execute()
		{
			try {
				return DoExecute();
			} catch (Exception e) {
				Log.LogError("{0}", e);
				return false;
			}
		}

		private bool DoExecute()
		{
			if (string.IsNullOrEmpty(LauncherFileName)) {
				LauncherFileName = "launcher.c";
			}
			AotAssemblies = AotAssemblies ?? new ITaskItem[0];
			BundledAssemblies = BundledAssemblies ?? new ITaskItem[0];

			Log.LogDebugMessage("GenerateLauncher Task");
			Log.LogDebugMessage("  OutputDirectory: {0}", OutputDirectory);
			Log.LogDebugMessage("  MainAssemblyName: {0}", MainAssemblyName);
			Log.LogDebugMessage("  LauncherTemplatePath: {0}", LauncherTemplatePath);
			Log.LogDebugMessage("  LauncherFileName: {0}", LauncherFileName);
			Log.LogDebugMessage("  UseCustomPlatformImpl: {0}", UseCustomPlatformImpl);
			Log.LogDebugTaskItems("  AotAssemblies:", AotAssemblies);
			Log.LogDebugTaskItems("  BundledAssemblies:", BundledAssemblies);

			var mainAssemblyName = MainAssemblyName;
			if (!SkipMain && string.IsNullOrEmpty(mainAssemblyName)) {
				var found = AotAssemblies.FirstOrDefault(a => Path.GetExtension(a.ItemSpec).ToLower() == ".exe");
				if (found == null) {
					throw new InvalidOperationException("Could not determine main assembly. No .exe assembly found.");
				}
				mainAssemblyName = Path.GetFileName(found.ItemSpec);
				Log.LogDebugMessage("  Found main assembly: {0}", mainAssemblyName);
			}

			var aoted = AotAssemblies.Select(a => Symbols.GetAotModuleSymbolName(a.ItemSpec, UserSymbolPrefix)).ToList();
			var bundled = BundledAssemblies.Select(a => {
				var assemblyName = Symbols.GetBundledAssemblyName(a.ItemSpec, Log);
				return new {
					getterSymbol = Symbols.GetBundledAssemblyGetter(assemblyName),
					configGetterSymbol = Symbols.GetBundledAssemblyConfigGetter(assemblyName),
					cleanupSymbol = Symbols.GetBundledAssemblyCleanup(assemblyName)
				};
			}).ToList();

			var nl = Environment.NewLine;
			var launcher = string.IsNullOrEmpty(LauncherTemplatePath) ? Resources.LauncherTemplate : File.ReadAllText(LauncherTemplatePath);

			var aotModules = new StringBuilder()
				.AppendLine("BEGIN_DECLARE_AOT_MODULES")
				.AppendLine(string.Join(nl, aoted.Select(a => $"\tDECLARE_AOT_MODULE ({a})")))
				.AppendLine("END_DECLARE_AOT_MODULES")
				.AppendLine("BEGIN_DEFINE_AOT_MODULES")
				.AppendLine(string.Join(nl, aoted.Select(a => $"\tDEFINE_AOT_MODULE ({a})")))
				.AppendLine("END_DEFINE_AOT_MODULES");
			launcher = Regex.Replace(launcher, @"(//\s*)\$\{AOTModules\}", aotModules.ToString());

			var bundledAssemblies = new StringBuilder()
				.AppendLine("BEGIN_DECLARE_BUNDLED_ASSEMBLIES")
				.AppendLine(string.Join(nl, bundled.Select(a => $"\tDECLARE_BUNDLED_ASSEMBLY ({a.getterSymbol})")))
				.AppendLine("END_DECLARE_BUNDLED_ASSEMBLIES")
				.AppendLine("BEGIN_DEFINE_BUNDLED_ASSEMBLIES")
				.AppendLine(string.Join(nl, bundled.Select(a => $"\tDEFINE_BUNDLED_ASSEMBLY ({a.getterSymbol})")))
				.AppendLine("END_DEFINE_BUNDLED_ASSEMBLIES");
			launcher = Regex.Replace(launcher, @"(//\s*)\$\{BundledAssemblies\}", bundledAssemblies.ToString());

			var bundledAssemblyConfigs = new StringBuilder()
				.AppendLine("BEGIN_DECLARE_BUNDLED_ASSEMBLY_CONFIGS")
				.AppendLine(string.Join(nl, bundled.Select(a => $"\tDECLARE_BUNDLED_ASSEMBLY_CONFIG ({a.configGetterSymbol})")))
				.AppendLine("END_DECLARE_BUNDLED_ASSEMBLY_CONFIGS")
				.AppendLine("BEGIN_DEFINE_BUNDLED_ASSEMBLY_CONFIGS")
				.AppendLine(string.Join(nl, bundled.Select(a => $"\tDEFINE_BUNDLED_ASSEMBLY_CONFIG ({a.configGetterSymbol})")))
				.AppendLine("END_DEFINE_BUNDLED_ASSEMBLY_CONFIGS");
			launcher = Regex.Replace(launcher, @"(//\s*)\$\{BundledAssemblyConfigs\}", bundledAssemblyConfigs.ToString());

			var bundledAssemblyCleanups = new StringBuilder()
				.AppendLine("BEGIN_DECLARE_BUNDLED_ASSEMBLY_CLEANUPS")
				.AppendLine(string.Join(nl, bundled.Select(a => $"\tDECLARE_BUNDLED_ASSEMBLY_CLEANUP ({a.cleanupSymbol})")))
				.AppendLine("END_DECLARE_BUNDLED_ASSEMBLY_CLEANUPS")
				.AppendLine("BEGIN_DEFINE_BUNDLED_ASSEMBLY_CLEANUPS")
				.AppendLine(string.Join(nl, bundled.Select(a => $"\tDEFINE_BUNDLED_ASSEMBLY_CLEANUP ({a.cleanupSymbol})")))
				.AppendLine("END_DEFINE_BUNDLED_ASSEMBLY_CLEANUPS");
			launcher = Regex.Replace(launcher, @"(//\s*)\$\{BundledAssemblyCleanups\}", bundledAssemblyCleanups.ToString());

			var defines = new List<string>();
			if (SkipMain) {
				defines.Add("SKIP_MAIN=1");
			}
			if (!string.IsNullOrEmpty(CustomDefines)) {
				defines.AddRange(CustomDefines.Split(';'));
			}
			defines = defines
				.Select(d => d.Split("=".ToCharArray(), 2))
				.Select(a => "#define " + string.Join(" ", a.ToArray())).ToList();
			launcher = Regex.Replace(launcher, @"(//\s*)\$\{Defines\}", string.Join(nl, defines));

			launcher = Regex.Replace(launcher, @"\$\{MainAssemblyName\}", mainAssemblyName ?? "");

			if (!Directory.Exists(OutputDirectory)) {
				Directory.CreateDirectory(OutputDirectory);
			}
			var outputFiles = new List<string>();
			var outputFile = Path.Combine(OutputDirectory, LauncherFileName);
			var writeOutputFile = true;
			if (File.Exists(outputFile)) {
				var oldContents = File.ReadAllText(outputFile);
				writeOutputFile = oldContents != launcher;
			}
			if (writeOutputFile) {
				File.WriteAllText(outputFile, launcher);
			} else {
				Log.LogDebugMessage("  Output file {0} hasn't changed. Won't overwrite.", outputFile);
			}
			outputFiles.Add(outputFile);

			if (string.IsNullOrEmpty(LauncherTemplatePath)) {
				// Copy the bundled platform.h
				var platformHeaderFile = Path.Combine(OutputDirectory, "platform.h");
				File.WriteAllText(platformHeaderFile, Resources.PlatformHeader);
				outputFiles.Add(platformHeaderFile);

				if (!UseCustomPlatformImpl) {
					// Use the bundled platform.c
					var platformImplFile = Path.Combine(OutputDirectory, "platform.c");
					File.WriteAllText(platformImplFile, Resources.PlatformImpl);
					outputFiles.Add(platformImplFile);
				}
			}

			GeneratedFiles = outputFiles.Select(f => new TaskItem(f)).ToArray();
			Log.LogDebugTaskItems("  [Output] GeneratedFiles:", GeneratedFiles);

			return true;
		}

		/*
		public static string GenerateLauncher(string build_dir, string device_deploy_dir, params string[] files)
		{
			files = files.Select(Path.GetFileName).ToArray();
			var mainAssembly = files.First(f => f.EndsWith(".exe"));

			var header = new List<string>();
			header.Add("extern void mono_aot_register_module (void *aot_info);");
			header.Add("extern void mono_jit_set_aot_only (int aot_only);\n");
			header.Add("#define g_setenv monoeg_g_setenv");
			header.Add($"static char *main_assembly_name = \"{device_deploy_dir}{(device_deploy_dir == "" ? "" : "/")}{mainAssembly}\";");
			header.Add($"static char *assembly_path = \"{device_deploy_dir}\";");

			header.Add("typedef struct _MonoAssembly MonoAssembly;\n");
			header.Add("typedef struct _MonoDomain MonoDomain;\n");
			header.Add("typedef int MonoImageOpenStatus;\n\n");

			header.Add("extern MonoDomain* mono_jit_init_version (const char *root_domain_name, const char *runtime_version);\n");
			header.Add("extern int mono_jit_exec (MonoDomain *domain, MonoAssembly *assembly, int argc, char *argv[]);\n");
			header.Add("extern MonoAssembly* mono_assembly_open (const char *filename, MonoImageOpenStatus *status);\n");
			header.Add("extern MonoDomain *mono_domain_get (void);\n");

			foreach (var f in files) {
				header.Add($"extern void *mono_aot_module_{Mangle(f)}_info;");
			}
			header.Add("static void register_modules ()");
			header.Add("{");
			foreach (var f in files) {
				header.Add($"    mono_aot_register_module (mono_aot_module_{Mangle(f)}_info);");
			}
			header.Add("}");

			return string.Join("\n", header);
		}*/
	}

}
