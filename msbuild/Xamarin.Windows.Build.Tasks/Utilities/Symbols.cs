using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Build.Utilities;

namespace Xamarin.Windows.Tasks
{
	public class Symbols
	{

		public static string GetAotModuleSymbolName(string assemblyPath, string userSymbolPrefix = "")
		{
			var assemblyNameNoExt = Path.GetFileNameWithoutExtension(assemblyPath);
			return $"{userSymbolPrefix}mono_aot_module_{MangleAssemblyName(assemblyNameNoExt)}_info";
		}

		public static string GetBundledAssemblyGetter(string assemblyName)
		{
			return $"mono_launcher_get_bundled_assembly_{MangleAssemblyName(assemblyName)}";
		}

		public static string GetBundledAssemblyConfigGetter(string assemblyName)
		{
			return $"mono_launcher_get_bundled_assembly_config_{MangleAssemblyName(assemblyName)}";
		}

		public static string GetBundledAssemblyCleanup(string assemblyName)
		{
			return $"mono_launcher_cleanup_bundled_assembly_{MangleAssemblyName(assemblyName)}";
		}

		public static string GetBundledAssemblyName(string path, TaskLoggingHelper log)
		{
			string name = Path.GetFileName(path);

			// A bit of a hack to support satellite assemblies. They all share the same name but
			// are placed in subdirectories named after the locale they implement. Also, all of
			// them end in .resources.dll, therefore we can use that to detect the circumstances.
			if (name.EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase)) {
				string dir = Path.GetDirectoryName(path);
				int idx = dir.LastIndexOf(Path.DirectorySeparatorChar);
				if (idx >= 0) {
					name = dir.Substring(idx + 1) + Path.DirectorySeparatorChar + name;
					log.LogDebugMessage($"Storing satellite assembly '{path}' with name '{name}'");
				} else {
					log.LogWarning($"Warning: satellite assembly {path} doesn't have locale path prefix, name conflicts possible");
				}
			}

			return name;
		}

		private readonly static Regex AssemblyNameEscapeRe = new Regex("[^\\w_]");
		public static string MangleAssemblyName(string assemblyName)
		{
			return AssemblyNameEscapeRe.Replace(assemblyName, "_");
		}
	}
}
