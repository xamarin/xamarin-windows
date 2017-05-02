using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Xamarin.Windows.Tasks
{
	public class Aot : Task, ICancelableTask
	{
		public enum AotFileType
		{
			Obj, Asm, Dll
		}

		/* These have to match with the AotFileType enum */
		private readonly static string[] DEFAULT_FILE_EXTENSIONS = {"obj", "s", "dll"};

		private CancellationTokenSource cancellationToken = new CancellationTokenSource();

		[Required]
		public string AotCompilerPath { get; set; }

		public string NativeToolchainPaths { get; set; }
		public string NativeToolsPrefix { get; set; }
		public string NativeLinkerFlags { get; set; }
		public bool IsDebug { get; set; }
		public bool OnlyRecompileIfChanged { get; set; }
		public bool GenerateNativeDebugInfo { get; set; }
		private AotFileType outputFileType { get; set; } = AotFileType.Obj;
		public string OutputFileType {
			get { return outputFileType.ToString(); }
			set { outputFileType = (AotFileType) Enum.Parse(typeof(AotFileType), value, true); }
		}
		public string OutputFileExtension { get; set; }
		public string Runtime { get; set; }

		[Required]
		public ITaskItem[] ResolvedFrameworkAssemblies { get; set; }

		[Required]
		public ITaskItem[] ResolvedUserAssemblies { get; set; }

		public string AotCompilerBclPath { get; set; }

		[Required]
		public string AotOutputDirectory { get; set; }

		[Required]
		public string IntermediateAssemblyDirectory { get; set; }

		public string PreAdditionalAotArguments { get; set; }
		public string PostAdditionalAotArguments { get; set; }

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

		public void Cancel()
		{
			cancellationToken.Cancel();
		}

		static string QuoteFileName(string fileName)
		{
			var builder = new CommandLineBuilder();
			builder.AppendFileNameIfNotNull(fileName);
			return builder.ToString();
		}

		private bool DoExecute()
		{
			if (string.IsNullOrEmpty(OutputFileExtension)) {
				OutputFileExtension = DEFAULT_FILE_EXTENSIONS[(int) outputFileType];
			}

			Log.LogDebugMessage("Aot Task");
			Log.LogDebugMessage("  Environment.CurrentDirectory: {0}", Environment.CurrentDirectory);
			Log.LogDebugMessage("  AotCompilerBclPath: {0}", AotCompilerBclPath);
			Log.LogDebugMessage("  AotOutputDirectory: {0}", AotOutputDirectory);
			Log.LogDebugMessage("  IntermediateAssemblyDirectory: {0}", IntermediateAssemblyDirectory);
			Log.LogDebugMessage("  AotCompilerPath: {0}", AotCompilerPath);
			Log.LogDebugMessage("  NativeToolchainPaths: {0}", NativeToolchainPaths);
			Log.LogDebugMessage("  NativeToolsPrefix: {0}", NativeToolsPrefix);
			Log.LogDebugMessage("  NativeLinkerFlags: {0}", NativeLinkerFlags);
			Log.LogDebugMessage("  PreAdditionalAotArguments: {0}", PreAdditionalAotArguments);
			Log.LogDebugMessage("  PostAdditionalAotArguments: {0}", PostAdditionalAotArguments);
			Log.LogDebugMessage("  IsDebug: {0}", IsDebug);
			Log.LogDebugMessage("  Runtime: {0}", Runtime);
			Log.LogDebugMessage("  OnlyRecompileIfChanged: {0}", OnlyRecompileIfChanged);
			Log.LogDebugMessage("  GenerateNativeDebugInfo: {0}", GenerateNativeDebugInfo);
			Log.LogDebugMessage("  OutputFileType: {0}", OutputFileType);
			Log.LogDebugMessage("  OutputFileExtension: {0}", OutputFileExtension);
			Log.LogDebugTaskItems("  ResolvedFrameworkAssemblies:", ResolvedFrameworkAssemblies);
			Log.LogDebugTaskItems("  ResolvedUserAssemblies:", ResolvedUserAssemblies);

			if (!File.Exists(Path.Combine(NativeToolchainPaths, "clang.exe"))) {
				Log.LogError ($"Could not find clang.exe in {NativeToolchainPaths}. "
							  + "Make sure to select the feature \"Clang with Microsoft Codegen\" in the Visual Studio Installer "
							  + "under \"Cross Platform Mobile Development\"->\"Visual C++ Mobile Development\".");
				return false;
			}

			var outputFiles = new List<string>();

			// Calculate the MONO_PATH we'll use when invoking the AOT compiler. This is the concatenation
			// of AotCompilerBclPath and the dir of each assembly we will compile.
			var assembliesPath =
				string.Join(";",
					(AotCompilerBclPath ?? "")
						.Split(';').Where(p => !string.IsNullOrEmpty(p)).Select(p => Path.GetFullPath(p))
						.Union(
							ResolvedFrameworkAssemblies
								.Union(ResolvedUserAssemblies)
								.Select(a => Path.GetDirectoryName(Path.GetFullPath(a.ItemSpec))))
						.Select(p => p.TrimEnd('\\', '/'))
						.Distinct(StringComparer.CurrentCultureIgnoreCase));

			foreach (var assembly in ResolvedFrameworkAssemblies.Union(ResolvedUserAssemblies)) {
				if (cancellationToken.IsCancellationRequested) {
					return false;
				}

				var outputFile = Path.Combine(AotOutputDirectory, Path.GetFileName(assembly.ItemSpec) + "." + OutputFileExtension);
				outputFiles.Add(outputFile);
				var assemblyPath = Path.GetFullPath(assembly.ItemSpec);
				if (OnlyRecompileIfChanged && File.Exists(outputFile) && File.Exists(assemblyPath) 
						&& File.GetLastWriteTime(outputFile) >= File.GetLastWriteTime(assemblyPath)) {
					Log.LogMessage(MessageImportance.High, "  Not recompiling unchanged assembly: {0}", assemblyPath);
					continue;
				}

				var tempDir = Path.Combine(AotOutputDirectory, Path.GetFileName(assembly.ItemSpec) + ".tmp");
				if (!Directory.Exists(tempDir))
					Directory.CreateDirectory(tempDir);

				var aotOptions = new List<string>();

				if (!string.IsNullOrEmpty(PreAdditionalAotArguments)) {
					aotOptions.Add(PreAdditionalAotArguments);
				}
				aotOptions.Add("full");
				aotOptions.Add("outfile=" + QuoteFileName(outputFile));
				if (IsDebug && ResolvedUserAssemblies.Contains(assembly)) {
					aotOptions.Add("soft-debug");
				}
				if (GenerateNativeDebugInfo) {
					aotOptions.Add("dwarfdebug");
				}
				if (outputFileType == AotFileType.Asm) {
					aotOptions.Add("asmonly");
				}
				aotOptions.Add("print-skipped");
				if (outputFileType != AotFileType.Dll) {
					aotOptions.Add("static");
				}
				aotOptions.Add("tool-prefix=" + QuoteFileName(NativeToolsPrefix));
				aotOptions.Add("ld-flags=" + NativeLinkerFlags);
				aotOptions.Add("temp-path=" + QuoteFileName(tempDir));
				if (!string.IsNullOrEmpty(PostAdditionalAotArguments)) {
					aotOptions.Add(PostAdditionalAotArguments);
				}

				var args = new List<string>();
				if (IsDebug) {
					args.Add("--debug");
				}
				if (!string.IsNullOrWhiteSpace(Runtime)) {
					args.Add("--runtime=" + Runtime);
				}
				args.Add("--aot=" + string.Join(",", aotOptions));
				args.Add('"' + assemblyPath + '"');

				Log.LogMessage(MessageImportance.High, "  AOT compiling assembly: {0}", assemblyPath);
				if (!RunAotCompiler(AotCompilerPath, args, assembliesPath)) {
					Log.LogCodedError("XW3001", "Could not AOT compile the assembly: {0}", assemblyPath);
					return false;
				}
				Log.LogMessage(MessageImportance.High, "  AOT compilation of {0} finished", Path.GetFileName(assemblyPath));
			}

			GeneratedFiles = outputFiles.Select(f => new TaskItem(f)).ToArray();
			Log.LogDebugTaskItems("  [Output] GeneratedFiles:", GeneratedFiles);

			return true;
		}

		bool RunAotCompiler(string aotCompiler, List<string> args, string assembliesPath)
		{
			var psi = new ProcessStartInfo() {
				FileName = aotCompiler,
				Arguments = string.Join(" ", args),
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden,
			};

			// we do not want options to be provided out of band to the cross compilers
			psi.EnvironmentVariables["MONO_ENV_OPTIONS"] = String.Empty;
			psi.EnvironmentVariables["MONO_PATH"] = assembliesPath;
			psi.EnvironmentVariables["PATH"] = NativeToolchainPaths;

			Log.LogMessage(MessageImportance.High, "    [AOT] PATH=\"{0}\" MONO_PATH=\"{1}\" MONO_ENV_OPTIONS=\"{2}\" {3} {4}",
				psi.EnvironmentVariables["PATH"],
				psi.EnvironmentVariables["MONO_PATH"],
				psi.EnvironmentVariables["MONO_ENV_OPTIONS"],
				psi.FileName,
				psi.Arguments);

			var proc = new Process();
			proc.OutputDataReceived += OnAotOutputData;
			proc.ErrorDataReceived += OnAotErrorData;
			proc.StartInfo = psi;
			proc.Start();
			proc.BeginOutputReadLine();
			proc.BeginErrorReadLine();
			cancellationToken.Token.Register(() => { try { proc.Kill(); } catch (Exception) { } });
			proc.WaitForExit();
			return proc.ExitCode == 0;
		}

		void OnAotOutputData(object sender, DataReceivedEventArgs e)
		{
			if (e.Data != null)
				Log.LogMessage(MessageImportance.High, "    [aot-compiler stdout] {0}", e.Data);
		}

		void OnAotErrorData(object sender, DataReceivedEventArgs e)
		{
			if (e.Data != null)
				Log.LogMessage(MessageImportance.High, "    [aot-compiler stderr] {0}", e.Data);
		}

	}
}
