using EnvDTE;
using Mono.Debugging.Soft;
using Mono.Debugging.VisualStudio;

namespace Xamarin.Windows
{
    internal class WindowsStartInfo : StartInfo
    {
        public WindowsStartInfo(SoftDebuggerStartArgs args, DebuggingOptions options, Project startupProject) : base(args, options, startupProject)
        {
        }
    }
}