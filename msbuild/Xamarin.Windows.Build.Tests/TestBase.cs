using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using NUnit.Framework;

namespace Xamarin.Windows.Build.Tests
{
	public abstract class TestBase
	{

		public static readonly string TestProjectsRoot;
		public static readonly string MonoDevRoot;
		public static readonly string MonoDevBcl;
		public static readonly string ReferenceAssemblies;

		static TestBase()
		{
			var currentDir = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
			TestProjectsRoot = Canonicalize(Path.Combine(currentDir, @"..\..\..\TestProjects"));
			MonoDevRoot = Canonicalize(Path.Combine(currentDir, @"..\..\..\..\external\mono"));
			MonoDevBcl = Canonicalize(Path.Combine(MonoDevRoot, @"mcs\class\lib\net_4_x"));
			ReferenceAssemblies = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\Xamarin.Windows\v1.0";
		}

		static string Canonicalize(string path)
		{
			return new Uri(path).LocalPath;
		}

		protected static string ReplaceRoots(string path)
		{
			if (path.StartsWith(TestProjectsRoot, StringComparison.CurrentCultureIgnoreCase)) {
				return "$(TestProjectsRoot)\\" + path.Substring(TestProjectsRoot.Length + 1);
			}
			if (path.StartsWith(MonoDevBcl, StringComparison.CurrentCultureIgnoreCase)) {
				return "$(MonoDevBcl)\\" + path.Substring(MonoDevBcl.Length + 1);
			}
			if (path.StartsWith(MonoDevRoot, StringComparison.CurrentCultureIgnoreCase)) {
				return "$(MonoDevRoot)\\" + path.Substring(MonoDevRoot.Length + 1);
			}
			if (path.StartsWith(ReferenceAssemblies, StringComparison.CurrentCultureIgnoreCase)) {
				return "$(ReferenceAssemblies)\\" + path.Substring(ReferenceAssemblies.Length + 1);
			}
			return path;
		}

		protected static string[] GetPathItems(BuildResult result, string itemType)
		{
			return result.ProjectStateAfterBuild.Items
				.Where(i => i.ItemType == itemType)
				.Select(i => ReplaceRoots(i.EvaluatedInclude))
				.OrderBy(s => s)
				.ToArray();
		}

		protected static string GetTestProjectDir(string name)
		{
			return Path.Combine(TestProjectsRoot, name);
		}

		public BuildResult BuildProject(string projectName, string targets = "Build", object properties = null)
		{
			var props = properties?.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(properties).ToString()) ?? new Dictionary<string, string>();
			var projectPath = Path.Combine(GetTestProjectDir(projectName), projectName + ".csproj");
			var loggers = new List<Microsoft.Build.Framework.ILogger> {new ConsoleLogger(LoggerVerbosity.Detailed)};
			var buildParameters = new BuildParameters(new ProjectCollection()) { Loggers = loggers };
			var buildRequest = new BuildRequestData(projectPath, props, null,
					targets.Split(';'), null, BuildRequestDataFlags.ProvideProjectStateAfterBuild);
			var buildResult = BuildManager.DefaultBuildManager.Build(buildParameters, buildRequest);
			Assert.AreEqual(BuildResultCode.Success, buildResult.OverallResult, 
					"Failed to build target(s) {0} in project {1}", targets, projectName);
			return buildResult;
		}
	}
}
