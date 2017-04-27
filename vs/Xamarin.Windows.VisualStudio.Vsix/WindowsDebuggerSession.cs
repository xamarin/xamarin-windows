using Mono.Debugging.Soft;
using System;
using Mono.Debugging.Client;
using EnvDTE;
using System.IO;

namespace Xamarin.Windows
{
    class WindowsDebuggerSession : SoftDebuggerSession
    {
        private System.Diagnostics.Process process;

        protected override void OnRun(DebuggerStartInfo startInfo)
        {
            base.OnRun(startInfo);
            UseOperationThread = true;

            var exeName = GetOutputAssembly(((Mono.Debugging.VisualStudio.StartInfo)startInfo).StartupProject);
            var monoDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"MSBuild\Xamarin\Windows\x64\Release");
            var monoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Reference Assemblies\Microsoft\Framework\Xamarin.Windows\v1.0");

            var args = ((Mono.Debugging.VisualStudio.StartInfo)startInfo).StartArgs as SoftDebuggerListenArgs;

            process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo(Path.Combine(monoDirectory, "mono-sgen.exe"), string.Format("--debug --debugger-agent=transport=dt_socket,address=127.0.0.1:{0} {1}", args.DebugPort, exeName));
            process.StartInfo.WorkingDirectory = monoDirectory;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.EnvironmentVariables["MONO_PATH"] = monoPath;
            process.Start();
        }

        private string GetOutputAssembly(Project startupProject)
        {
            var baseFolder = startupProject.Properties.Item("FullPath").Value.ToString();
            var outFolder = startupProject.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString();
            var assemblyName = startupProject.Properties.Item("OutputFileName").Value.ToString();

            return string.Format("\"{0}\"", Path.Combine(baseFolder, outFolder, assemblyName));
        }

        protected override void OnExit()
        {
            if (!process.HasExited)
                process.Kill();
        }
    }
}
