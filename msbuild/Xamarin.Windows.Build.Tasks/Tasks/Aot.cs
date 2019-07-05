// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
		public bool EnableLLVM {get; set; } = false;

		public int MaxParallelBuildTasks { get; set; } = 1;

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
			bool failureDetected = false;

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
			Log.LogDebugMessage("  EnableLLVM: {0}", EnableLLVM);
			Log.LogDebugMessage("  OnlyRecompileIfChanged: {0}", OnlyRecompileIfChanged);
			Log.LogDebugMessage("  GenerateNativeDebugInfo: {0}", GenerateNativeDebugInfo);
			Log.LogDebugMessage("  OutputFileType: {0}", OutputFileType);
			Log.LogDebugMessage("  OutputFileExtension: {0}", OutputFileExtension);
			Log.LogDebugTaskItems("  ResolvedFrameworkAssemblies:", ResolvedFrameworkAssemblies);
			Log.LogDebugTaskItems("  ResolvedUserAssemblies:", ResolvedUserAssemblies);

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

			int parallelBuildTasks = MaxParallelBuildTasks <= 0 ? Environment.ProcessorCount : MaxParallelBuildTasks;
			if (parallelBuildTasks > 1)
				Log.LogMessage(MessageImportance.High, "Using {0} parallel build tasks.", parallelBuildTasks);

			using (SemaphoreSlim parallellCompileTasks = new SemaphoreSlim(parallelBuildTasks))
			{
				var compileTasks = new List<System.Threading.Tasks.Task<bool>>();
				var currentCompileTaskId = 0;
				foreach (var assembly in ResolvedFrameworkAssemblies.Union(ResolvedUserAssemblies))
				{
					if (cancellationToken.IsCancellationRequested)
					{
						failureDetected = true;
						break;
					}

					var outputFile = Path.Combine(AotOutputDirectory, Path.GetFileName(assembly.ItemSpec) + "." + OutputFileExtension);
					var outputFileLLVM = Path.Combine(AotOutputDirectory, Path.GetFileName(assembly.ItemSpec) + "-llvm." + OutputFileExtension);

					outputFiles.Add(outputFile);
					if (EnableLLVM)
					{
						outputFiles.Add(outputFileLLVM);
					}

					var assemblyPath = Path.GetFullPath(assembly.ItemSpec);
					if (OnlyRecompileIfChanged && File.Exists(outputFile) && File.Exists(assemblyPath)
							&& File.GetLastWriteTime(outputFile) >= File.GetLastWriteTime(assemblyPath))
					{
						if (!EnableLLVM && File.Exists(outputFileLLVM))
						{
							Log.LogMessage(MessageImportance.High, "  Found LLVM object file for assembly: {0} during none LLVM build, forcing recompile.", assemblyPath);
						}
						else
						{
							Log.LogMessage(MessageImportance.High, "  Not recompiling unchanged assembly: {0}", assemblyPath);
							continue;
						}
					}

					if (File.Exists(outputFile))
						File.Delete(outputFile);
					if (File.Exists(outputFileLLVM))
						File.Delete(outputFileLLVM);

					var tempDir = Path.Combine(AotOutputDirectory, Path.GetFileName(assembly.ItemSpec) + ".tmp");
					if (!Directory.Exists(tempDir))
						Directory.CreateDirectory(tempDir);

					var aotOptions = new List<string>();

					if (!string.IsNullOrEmpty(PreAdditionalAotArguments))
					{
						aotOptions.Add(PreAdditionalAotArguments);
					}
					aotOptions.Add("full");
					if (EnableLLVM)
					{
						aotOptions.Add("llvm");
						aotOptions.Add("llvm-outfile=" + QuoteFileName(outputFileLLVM));
					}
					aotOptions.Add("outfile=" + QuoteFileName(outputFile));
					if (IsDebug && ResolvedUserAssemblies.Contains(assembly))
					{
						aotOptions.Add("soft-debug");
					}
					if (GenerateNativeDebugInfo)
					{
						aotOptions.Add("dwarfdebug");
					}
					if (outputFileType == AotFileType.Asm)
					{
						aotOptions.Add("asmonly");
					}
					aotOptions.Add("print-skipped");
					if (outputFileType != AotFileType.Dll)
					{
						aotOptions.Add("static");
					}
					aotOptions.Add("tool-prefix=" + QuoteFileName(NativeToolsPrefix));
					aotOptions.Add("ld-flags=" + NativeLinkerFlags);
					aotOptions.Add("temp-path=" + QuoteFileName(tempDir));
					if (!string.IsNullOrEmpty(PostAdditionalAotArguments))
					{
						aotOptions.Add(PostAdditionalAotArguments);
					}

					var args = new List<string>();
					if (IsDebug)
					{
						args.Add("--debug");
					}
					if (!string.IsNullOrWhiteSpace(Runtime))
					{
						args.Add("--runtime=" + Runtime);
					}
					args.Add("--aot=" + string.Join(",", aotOptions));
					args.Add('"' + assemblyPath + '"');

					parallellCompileTasks.Wait();
					if (updateAndCheckTasksResult(compileTasks))
					{
						failureDetected = true;
						break;
					}

					int capturedCompileTaskId = ++currentCompileTaskId;
					var compileTask = System.Threading.Tasks.Task<bool>.Factory.StartNew(() => {
						bool result = false;
						try
						{
							string compileTaskName = String.Format("{0}", "[AOT-" + capturedCompileTaskId + "]");
							Log.LogMessage(MessageImportance.High, "  {0} compiling assembly: {1}", compileTaskName, assemblyPath);
							result = RunAotCompiler(AotCompilerPath, args, assembliesPath, compileTaskName);
							if (!result)
							{
								Log.LogCodedError("XW3001", "Could not AOT compile the assembly: {0}", assemblyPath);
							}
							else
							{
								Log.LogMessage(MessageImportance.High, "  {0} compilation of {1} finished", compileTaskName, Path.GetFileName(assemblyPath));
							}
						}
						finally
						{
							parallellCompileTasks.Release();
						}
						return result;
					});

					compileTasks.Add(compileTask);
				}

				System.Threading.Tasks.Task.WaitAll(compileTasks.ToArray());
				if (updateAndCheckTasksResult(compileTasks)) {
					failureDetected = true;
				}
			}

			if (!failureDetected) {
				GeneratedFiles = outputFiles.Select(f => new TaskItem(f)).ToArray();
				Log.LogDebugTaskItems("  [Output] GeneratedFiles:", GeneratedFiles);
			}

			return !failureDetected;
		}

		bool updateAndCheckTasksResult (List<System.Threading.Tasks.Task<bool>> tasks)
		{
			bool hasFailure = false;

			for (var i = tasks.Count - 1; i >= 0; i--) {
				var currentTask = tasks[i];
				if (currentTask.IsCompleted && currentTask.Result == true) {
					tasks.RemoveAt(i);
				} else if (currentTask.IsCompleted && currentTask.Result == false) {
					hasFailure = true;
				}
			}

			return hasFailure;
		}

		bool RunAotCompiler(string aotCompiler, List<string> args, string assembliesPath, string compileTaskName)
		{
			if (!File.Exists(Path.Combine(NativeToolchainPaths, "clang.exe"))) {
				Log.LogError($"    {compileTaskName}: Could not find clang.exe in {NativeToolchainPaths}. "
							  + "Make sure to select the feature \"Clang with Microsoft Codegen\" in the Visual Studio Installer "
							  + "under \"Cross Platform Mobile Development\"->\"Visual C++ Mobile Development\".");
				return false;
			}

			var crossCompilerPath = Path.GetDirectoryName(aotCompiler);
			if (EnableLLVM) {
				if (!File.Exists(Path.Combine(crossCompilerPath, "opt.exe"))) {
					Log.LogError($"    {compileTaskName}: Could not find opt.exe in {crossCompilerPath}.");
					return false;
				}

				if (!File.Exists(Path.Combine(crossCompilerPath, "llc.exe"))) {
					Log.LogError($"    {compileTaskName}: Could not find llc.exe in {crossCompilerPath}.");
					return false;
				}
			}

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
			psi.EnvironmentVariables["PATH"] = NativeToolchainPaths + ";" + crossCompilerPath;

			Log.LogMessage(MessageImportance.High, "    {0}: PATH=\"{1}\" MONO_PATH=\"{2}\" MONO_ENV_OPTIONS=\"{3}\" {4} {5}",
				compileTaskName,
				psi.EnvironmentVariables["PATH"],
				psi.EnvironmentVariables["MONO_PATH"],
				psi.EnvironmentVariables["MONO_ENV_OPTIONS"],
				psi.FileName,
				psi.Arguments);

			var proc = new CustomProcess();
			proc.OutputDataReceived += OnAotOutputData;
			proc.ErrorDataReceived += OnAotErrorData;
			proc.StartInfo = psi;
			proc.Start();
			proc.BeginOutputReadLine();
			proc.BeginErrorReadLine();
			proc.CompileTaskName = compileTaskName;

			lock (cancellationToken) {
				cancellationToken.Token.Register(() => {
					try {
						var tkStartInfo = new ProcessStartInfo {
							FileName = "taskkill",
							Arguments = $"/T /F /PID {proc.Id}",
							RedirectStandardError = false,
							RedirectStandardOutput = false,
							UseShellExecute = false
						};

						Log.LogMessage(MessageImportance.High, $"    {compileTaskName}: Canceling aot-compiler, PID={proc.Id}");
						var tkProcess = Process.Start(tkStartInfo);
						if (tkProcess.WaitForExit(30000)) {
							Log.LogMessage(MessageImportance.High, $"    {compileTaskName}: aot-compiler, PID={proc.Id}, canceled");
						} else {
							Log.LogError($"    {compileTaskName}: Canceling aot-compiler, PID={proc.Id}, timed out");
							tkProcess.Kill();
						}
					} catch (Exception) { }
				});
			}
			Log.LogMessage(MessageImportance.Normal, $"    {compileTaskName}: Executing aot-compiler, PID={proc.Id}");
			proc.WaitForExit();
			Log.LogMessage(MessageImportance.Normal, $"    {compileTaskName}: aot-compiler, PID={proc.Id}, completed with exit code={proc.ExitCode}");

			return proc.ExitCode == 0;
		}
		void OnAotOutputData(object sender, DataReceivedEventArgs e)
		{
			string compileTaskName = "";
			if (sender is CustomProcess)
				compileTaskName = ((CustomProcess)sender).CompileTaskName;

			if (e.Data != null)
				Log.LogMessage(MessageImportance.High, "    {0}[aot-compiler stdout]: {1}", compileTaskName, e.Data);
		}

		void OnAotErrorData(object sender, DataReceivedEventArgs e)
		{
			string compileTaskName = "";
			if (sender is CustomProcess)
				compileTaskName = ((CustomProcess)sender).CompileTaskName;

			if (e.Data != null)
				Log.LogMessage(MessageImportance.High, "    {0}[aot-compiler stderr]: {1}", compileTaskName, e.Data);
		}

	}

	class CustomProcess : Process
	{
		public string CompileTaskName { get; set; }
	}
}
