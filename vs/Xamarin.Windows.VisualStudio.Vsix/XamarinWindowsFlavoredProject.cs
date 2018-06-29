// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using Microsoft.VisualStudio.Shell.Flavor;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;
using Xamarin.Windows.Properties;
using System.Linq;

namespace Xamarin.Windows
{
    internal class XamarinWindowsFlavoredProject : FlavoredProjectBase, IVsProjectFlavorCfgProvider
    {
        /// <summary>
        /// A space-delimited list of the project's capabilities. This property is optional.
        /// </summary>
        /// <devdoc>
        /// Value from __VSHPROPID5, which only exists on dev11+ and is used to express support
        /// for the new shared projects.
        /// </devdoc>
        const int VSHPROPID_ProjectCapabilities = -2124;

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

        
        protected override int GetProperty(uint itemId, int propId, out object property) {
            //Use propId to filter configuration-dependent property pages.  
            switch ((__VSHPROPID2)propId)
            {
                case __VSHPROPID2.VSHPROPID_CfgPropertyPagesCLSIDList:
                case __VSHPROPID2.VSHPROPID_PropertyPagesCLSIDList:
                    {
                        // Get a semicolon-delimited list of clsids of property pages.
                        ErrorHandler.ThrowOnFailure(base.GetProperty(itemId, propId, out property));
                        var ids = ((string)property).Split(';').Select(Guid.Parse).ToList();

                        if ((__VSHPROPID2)propId == __VSHPROPID2.VSHPROPID_CfgPropertyPagesCLSIDList)
                        {
                            // Add our property page.
                            ids.Add(typeof(OptionsPage).GUID);
                        }

                        property = string.Join(";", ids.Select(g => g.ToString("B")));
                        return VSConstants.S_OK;
                    }
                case __VSHPROPID2.VSHPROPID_PriorityPropertyPagesCLSIDList:
                    {
                        // Get a semicolon-delimited list of clsids of property pages.
                        ErrorHandler.ThrowOnFailure(base.GetProperty(itemId, propId, out property));
                        var ids = ((string)property).Split(';').Select(Guid.Parse).ToList();
                        // Move our Options page to the top
                        ids.Remove(typeof(OptionsPage).GUID);
                        ids.Insert(0, typeof(OptionsPage).GUID);
                        property = string.Join(";", ids.Select(g => g.ToString("B")));
                        return VSConstants.S_OK;
                    }
            }
            if (propId == VSHPROPID_ProjectCapabilities)
            {
                // First grab whatever capabilities our base project has.
                if (base.GetProperty(itemId, propId, out property) == VSConstants.S_OK && property != null)
                    property = ((string)property) + " SharedProjectReferences";
                else
                    property = "SharedProjectReferences";

                return VSConstants.S_OK;
            }

            return base.GetProperty(itemId, propId, out property);
        }
        
    }
}