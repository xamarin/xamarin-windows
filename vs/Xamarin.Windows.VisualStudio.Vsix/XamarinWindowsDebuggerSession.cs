using Mono.Debugging.Soft;
using System;
using Mono.Debugging.Client;
using EnvDTE;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.Setup.Configuration;

namespace Xamarin.Windows
{
    class XamarinWindowsDebuggerSession : SoftDebuggerSession
    {
        // The GUID of Microsoft.VisualStudio.Setup.Configuration.SetupConfigurationClass
        private readonly Guid SetupConfigurationClassGuid = new Guid("177F0C4A-1CD3-4DE7-A32C-71DBBB9FA36D");
        private System.Diagnostics.Process process;
        private string installPath = null;

        protected override void OnRun(DebuggerStartInfo startInfo)
        {
            base.OnRun(startInfo);
            UseOperationThread = true;

            var assemblyPath = GetOutputAssembly(((Mono.Debugging.VisualStudio.StartInfo) startInfo).StartupProject);
            var monoDirectory = Path.Combine(GetInstallPath(), @"MSBuild\Xamarin\Windows\x64\Release");
            var monoPath = Path.Combine(GetInstallPath(), @"Common7\IDE\ReferenceAssemblies\Microsoft\Framework\Xamarin.Windows\v1.0");

            var args = ((Mono.Debugging.VisualStudio.StartInfo)startInfo).StartArgs as SoftDebuggerListenArgs;

            process = new System.Diagnostics.Process();
            process.StartInfo = new ProcessStartInfo(Path.Combine(monoDirectory, "mono-sgen.exe"), $"--debug --debugger-agent=transport=dt_socket,address=127.0.0.1:{args.DebugPort} \"{assemblyPath}\"");
            process.StartInfo.WorkingDirectory = Path.GetDirectoryName(assemblyPath);
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.EnvironmentVariables["MONO_PATH"] = monoPath;
            process.Start();
        }

        private string GetInstallPath()
        {
            if (installPath == null) {
                var config = (ISetupConfiguration2) Activator.CreateInstance(Type.GetTypeFromCLSID(SetupConfigurationClassGuid));
                var instance = config.GetInstanceForCurrentProcess();
                installPath = instance.GetInstallationPath();
            }
            return installPath;
        }

        private string GetOutputAssembly(Project startupProject)
        {
            var baseFolder = startupProject.Properties.Item("FullPath").Value.ToString();
            var outFolder = startupProject.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString();
            var assemblyName = startupProject.Properties.Item("OutputFileName").Value.ToString();

            return Path.Combine(baseFolder, outFolder, assemblyName);
        }

        protected override void OnExit()
        {
            if (!process.HasExited)
                process.Kill();
        }
    }
}
