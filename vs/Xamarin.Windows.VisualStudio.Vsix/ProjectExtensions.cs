using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using Microsoft.VisualStudio;

namespace EnvDTE
{
    public static class ProjectExtensions
    {

		public static T GetPropertyValue<T>(this Project project, int propertyId) where T : class {
			var hierarchy = project.ToHierarchy();

			object propertyVal;
			if ((hierarchy.GetProperty(0, propertyId, out propertyVal) == VSConstants.S_OK))
				return propertyVal as T;

			return null;
		}

		public static IVsHierarchy ToHierarchy(this Project project) {
			if (project == null) throw new ArgumentNullException("project");

			try
			{
				IVsHierarchy hierarchy;
				var solutionService = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution2;
				if (solutionService.GetProjectOfUniqueName(project.UniqueName, out hierarchy) == VSConstants.S_OK)
					return hierarchy;
			}
			catch (NotImplementedException)
			{
				// ignore - apparently some Project implementations in Visual Studio do not implement the FileName property
				// and we'll get a NotImplemented exception thrown here.
				return null;
			}

			return null;
		}


		public static string GetMSBuildPropertyValue(this IVsBuildPropertyStorage storage, string property, string defaultValue) {
            string value = null;

            if (storage.GetPropertyValue(property, null, (uint)_PersistStorageType.PST_PROJECT_FILE, out value) == VSConstants.S_OK)
                return value;

            return defaultValue;
        }
    }
}
