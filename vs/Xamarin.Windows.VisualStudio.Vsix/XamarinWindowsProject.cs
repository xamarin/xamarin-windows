// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Microsoft.VisualStudio.Shell.Interop;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.OLE.Interop;
using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Flavor;

namespace Xamarin.Windows
{
    [ComVisible(false)]
    [Guid(XamarinWindowsPackage.XamarinWindowsProjectGuid)]
    public class XamarinWindowsProjectFactory : FlavoredProjectFactoryBase
    {
        private XamarinWindowsPackage package;

        public XamarinWindowsProjectFactory(XamarinWindowsPackage package)
        {
            this.package = package;
        }

        protected override object PreCreateForOuter(IntPtr outerProjectIUnknown)
        {
            return new XamarinWindowsFlavoredProject(package);
        }
    }

}