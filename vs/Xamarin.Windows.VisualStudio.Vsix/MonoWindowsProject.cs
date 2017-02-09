using Microsoft.VisualStudio.Shell.Interop;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.OLE.Interop;
using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Flavor;

namespace Xamarin.Windows
{
    [ComVisible(false)]
    [Guid(XamarinWindowsPackage.MonoWindowsProjectGuid)]
    public class MonoWindowsProjectFactory : FlavoredProjectFactoryBase
    {
        private XamarinWindowsPackage package;

        public MonoWindowsProjectFactory(XamarinWindowsPackage package)
        {
            this.package = package;
        }

        protected override object PreCreateForOuter(IntPtr outerProjectIUnknown)
        {
            return new MonoWindowsFlavoredProject(package);
        }
    }

}