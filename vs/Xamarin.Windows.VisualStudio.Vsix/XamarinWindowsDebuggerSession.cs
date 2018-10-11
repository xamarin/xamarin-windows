// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Mono.Debugging.Soft;
using System;
using Mono.Debugging.Client;
using EnvDTE;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.Setup.Configuration;
using Microsoft.VisualStudio.Shell.Interop;
using Clide;
using Xamarin.Windows.Properties;
using Microsoft.VisualStudio;

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
            Project startupProject = ((Mono.Debugging.VisualStudio.StartInfo)startInfo).StartupProject;

            IVsHierarchy hierarchy = startupProject.ToHierarchy();
            IProjectNode node = startupProject.AsProjectNode();
            BuildPropertyStorage storage = new BuildPropertyStorage(hierarchy, node.Configuration.ActiveConfigurationName);

            var enableAotMode = storage.GetPropertyValue<bool>(XamarinWindowsConstants.EnableAotModeProperty);
            var bundleAsemblies = storage.GetPropertyValue<bool>(XamarinWindowsConstants.BundleAssembliesProperty);
            var generateDebuggableAotModules = storage.GetPropertyValue<bool>(XamarinWindowsConstants.GenerateDebuggableAotModulesProperty);
            var additionalMonoOptions = storage.GetPropertyValue<string>(XamarinWindowsConstants.AdditionalMonoOptionsProperty)?.Trim();
            var monoLogLevel = storage.GetPropertyValue<string>(XamarinWindowsConstants.MonoLogLevelProperty);
            var monoLogMask = storage.GetPropertyValue<string>(XamarinWindowsConstants.MonoLogMaskProperty);

            var startArguments = startupProject.ConfigurationManager.ActiveConfiguration.Properties.Item("StartArguments").Value.ToString();
            var assemblyPath = GetOutputAssembly(startupProject, enableAotMode, generateDebuggableAotModules);
            var monoDirectory = Path.Combine(GetInstallPath(), @"MSBuild\Xamarin\Windows\x64\Release");
            var monoPath = Path.Combine(GetInstallPath(), @"Common7\IDE\ReferenceAssemblies\Microsoft\Framework\Xamarin.Windows\v1.0");
            var args = ((Mono.Debugging.VisualStudio.StartInfo)startInfo).StartArgs as SoftDebuggerListenArgs;

            process = new System.Diagnostics.Process();
            var workingDirectory = Path.GetDirectoryName(assemblyPath);
            var monoOptions = $"--debug --debugger-agent=transport=dt_socket,address=127.0.0.1:{args.DebugPort}";
            if (!string.IsNullOrEmpty(additionalMonoOptions))
                monoOptions += " " + additionalMonoOptions;

            if (!enableAotMode)
            {
                process.StartInfo = new ProcessStartInfo(Path.Combine(monoDirectory, "mono-sgen.exe"), monoOptions + $" \"{assemblyPath}\" {startArguments}".TrimEnd());
            }
            else
            {
                IVsBuildPropertyStorage buildPropertyStorage = (IVsBuildPropertyStorage)hierarchy;
                string nativeProjectName = buildPropertyStorage.GetMSBuildPropertyValue("_XWNativeProjectName", null);
                var launcherExe = Path.Combine(workingDirectory, nativeProjectName + ".exe");
                process.StartInfo = new ProcessStartInfo(launcherExe, startArguments);
                process.StartInfo.EnvironmentVariables["MONO_BUNDLED_OPTIONS"] = monoOptions;
            }
            process.StartInfo.WorkingDirectory = workingDirectory;
            process.StartInfo.UseShellExecute = false;
            if (!string.IsNullOrEmpty(monoLogLevel))
                process.StartInfo.EnvironmentVariables["MONO_LOG_LEVEL"] = monoLogLevel;
            if (!string.IsNullOrEmpty(monoLogMask))
                process.StartInfo.EnvironmentVariables["MONO_LOG_MASK"] = monoLogMask;

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

        private string GetOutputAssembly(Project startupProject, bool enableAotMode, bool generateDebuggableAotModules)
        {
            var baseFolder = startupProject.Properties.Item("FullPath").Value.ToString();
            var outFolder = startupProject.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString();
            var assemblyName = startupProject.Properties.Item("OutputFileName").Value.ToString();

            if (enableAotMode && generateDebuggableAotModules)
                outFolder = Path.Combine(outFolder, "Aotd");
            else if (enableAotMode)
                outFolder = Path.Combine(outFolder, "Aot");

            return Path.Combine(baseFolder, outFolder, assemblyName);
        }

        protected override void OnExit()
        {
            if (!process.HasExited)
                process.Kill();
        }
    }
}
