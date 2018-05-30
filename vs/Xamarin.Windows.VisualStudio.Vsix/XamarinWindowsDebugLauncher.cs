using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.VisualStudio.Shell.Interop;
using Mono.Debugging.Soft;
using Microsoft.VisualStudio.Shell;
using Mono.Debugging.VisualStudio;

namespace Xamarin.Windows
{
    internal class XamarinWindowsDebugLauncher : IDebugLauncher
    {
        private static readonly ITracer tracer = Tracer.Get<XamarinWindowsDebugLauncher>();

        public bool StartDebugger(SoftDebuggerSession session, StartInfo startInfo)
        {
            tracer.Verbose("Entering Launch for: {0}", this);
            var debugger = ServiceProvider.GlobalProvider.GetService<SVsShellDebugger, IVsDebugger4>();

            var sessionMarshalling = new SessionMarshalling(session, startInfo);

            VsDebugTargetInfo4 info = new VsDebugTargetInfo4();
            info.dlo = (uint)Microsoft.VisualStudio.Shell.Interop.DEBUG_LAUNCH_OPERATION.DLO_CreateProcess;

            var startArgs = startInfo.StartArgs;
            var appName = "Mono";
            if (startArgs is SoftDebuggerRemoteArgs)
            {
                appName = ((SoftDebuggerRemoteArgs)startArgs).AppName;
            }
            else if (startArgs is SoftDebuggerLaunchArgs)
            {
                appName = Path.GetFileNameWithoutExtension(startInfo.Command);
            }

            info.bstrExe = appName;
            info.bstrCurDir = "";
            info.bstrArg = null; // no command line parameters
            info.bstrRemoteMachine = null;
            info.fSendToOutputWindow = 0; // Let stdout stay with the application.
            info.guidPortSupplier = Guids.PortSupplierGuid;
            info.guidLaunchDebugEngine = Guids.EngineGuid;
            info.bstrPortName = appName;

            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                ObjRef oref = RemotingServices.Marshal(sessionMarshalling);
                bf.Serialize(ms, oref);
                info.bstrOptions = Convert.ToBase64String(ms.ToArray());
            }

            try
            {
                var results = new VsDebugTargetProcessInfo[1];
                debugger.LaunchDebugTargets4(1, new[] { info }, results);
                return true;
            }
            catch (Exception ex)
            {
                tracer.Error("Controller.Launch ()", ex);
                throw;
            }
        }
    }
}