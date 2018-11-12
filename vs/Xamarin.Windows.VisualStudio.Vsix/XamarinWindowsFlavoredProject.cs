// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using Microsoft.VisualStudio.Shell.Flavor;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;

namespace Xamarin.Windows
{
    internal class XamarinWindowsFlavoredProject : FlavoredProjectBase, IVsProjectFlavorCfgProvider
    {
        private IVsProjectFlavorCfgProvider innerFlavorConfig;
        private XamarinWindowsPackage package;

        public XamarinWindowsFlavoredProject(XamarinWindowsPackage package)
        {
            this.package = package;
        }

        public int CreateProjectFlavorCfg(IVsCfg pBaseProjectCfg, out IVsProjectFlavorCfg ppFlavorCfg)
        {
            Console.WriteLine("CreateProjectFlavorcfg");

            IVsProjectFlavorCfg cfg = null;
            ppFlavorCfg = null;

            if (innerFlavorConfig != null)
            {
                object project;
                GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out project);

                this.innerFlavorConfig.CreateProjectFlavorCfg(pBaseProjectCfg, out cfg);
                ppFlavorCfg = new XamarinWindowsDebuggableConfig(cfg, project as EnvDTE.Project);
            }

            if (ppFlavorCfg != null)
                return VSConstants.S_OK;
            Console.WriteLine("Failing CreateProjectFlavorcfg");
            return VSConstants.E_FAIL;
        }

        protected override void SetInnerProject(IntPtr innerIUnknown)
        {
            object inner = null;

            inner = Marshal.GetObjectForIUnknown(innerIUnknown);
            innerFlavorConfig = inner as IVsProjectFlavorCfgProvider;

            if (base.serviceProvider == null)
                base.serviceProvider = this.package;

            base.SetInnerProject(innerIUnknown);
        }

    }
}