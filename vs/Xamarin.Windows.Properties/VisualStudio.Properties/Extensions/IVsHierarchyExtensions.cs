// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.
using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.Shell.Interop
{
	public static class IVsHierarchyExtensions
	{
		public static Project AsDteProject(this IVsHierarchy hierarchy)
		{
			if (hierarchy != null)
			{
				object project;
				if (ErrorHandler.Succeeded(
					hierarchy.GetProperty(
						VSConstants.VSITEMID_ROOT,
						(int)__VSHPROPID.VSHPROPID_ExtObject,
						out project)))
					return project as Project;
			}

			return null;
		}
	}
}
