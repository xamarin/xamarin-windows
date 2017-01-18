using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xamarin.Windows.Tests
{
	[TestFixture]
	public class TargetTests : TestBase
	{
		private static readonly string[] ConsoleAppFrameworkAssemblies = {
			"mscorlib.dll",
			"System.dll",
			"System.Configuration.dll",
			"System.Security.dll",
			"System.Xml.dll",
			"I18N.dll",
			"I18N.West.dll",
			"Mono.Security.dll"
		};

		private static readonly string[] ConsoleAppUserAssemblies = {
			@"$(TestProjectsRoot)\ConsoleApp\bin\Debug\ConsoleApp.exe",
			@"$(TestProjectsRoot)\ClassLibrary\bin\Debug\ClassLibrary.dll"
		};

		private void ResolveAssemblies(string prefix, object properties = null)
		{
			var result = BuildProject("ConsoleApp", targets: "ResolveAssembliesTest", properties: properties);
			var allAssemblies = GetPathItems(result, "ResolvedAssemblies");
			var frameworkAssemblies = GetPathItems(result, "ResolvedFrameworkAssemblies");
			var userAssemblies = GetPathItems(result, "ResolvedUserAssemblies");
			var resolvedMdbFiles = GetPathItems(result, "ResolvedMdbFiles");
			var resolvedPdbFiles = GetPathItems(result, "ResolvedPdbFiles");
			var assembliesWithSymbols = resolvedPdbFiles
				.Select(Path.GetFileNameWithoutExtension)
				.Union(resolvedMdbFiles.Select(Path.GetFileNameWithoutExtension).Select(Path.GetFileNameWithoutExtension))
				.ToArray();
			var expectedFrameworkAssemblies = ConsoleAppFrameworkAssemblies.Select(s => $"{prefix}\\{s}").ToArray();
			var expectedAssembliesWithSymbols = expectedFrameworkAssemblies.Union(ConsoleAppUserAssemblies).Select(Path.GetFileNameWithoutExtension).ToArray();
			CollectionAssert.AreEquivalent(userAssemblies.Union(frameworkAssemblies), allAssemblies);
			CollectionAssert.AreEquivalent(expectedFrameworkAssemblies, frameworkAssemblies);
			CollectionAssert.AreEquivalent(ConsoleAppUserAssemblies, userAssemblies);
			CollectionAssert.AreEquivalent(expectedAssembliesWithSymbols, assembliesWithSymbols);
		}

		[Test]
		public void ResolveAssembliesWithMonoDevBcl()
		{
			ResolveAssemblies("$(MonoDevBcl)", properties: new { MonoDevRoot });
		}

		[Test]
		public void ResolveAssembliesWithReferenceAssemblies()
		{
			ResolveAssemblies("$(ReferenceAssemblies)");
		}

		[Test]
		public void Aot()
		{
			var outputDir = Path.Combine(GetTestProjectDir("ConsoleApp"), "obj", "Debug", "Aot");
			var result = BuildProject("ConsoleApp", targets: "AotTest", properties: new { MonoDevRoot });
			var actualObjectFiles = Directory.GetFiles(outputDir, "*.obj", SearchOption.AllDirectories)
					.Select(Path.GetFileName).OrderBy(s => s);
			var expectedObjectFiles = ConsoleAppFrameworkAssemblies
					.Union(ConsoleAppUserAssemblies.Select(Path.GetFileName))
					.Select(s => s + ".obj").OrderBy(s => s);
			CollectionAssert.AreEquivalent(expectedObjectFiles, actualObjectFiles);
			var actualGeneratedAotFiles = GetPathItems(result, "GeneratedAotFiles");
			var expectedGeneratedAotFiles = expectedObjectFiles.Select(f => $"obj\\Debug\\Aot\\{f}");
			CollectionAssert.AreEquivalent(expectedGeneratedAotFiles, actualGeneratedAotFiles);
		}

		[Test]
		public void AotAsmOnly()
		{
			var outputDir = Path.Combine(GetTestProjectDir("ConsoleApp"), "obj", "Debug", "Aot");
			var result = BuildProject("ConsoleApp", targets: "AotTest", properties: new { MonoDevRoot, AotOutputFileType = "Asm" });
			var actualAsmFiles = Directory.GetFiles(outputDir, "*.s", SearchOption.AllDirectories)
					.Select(Path.GetFileName).OrderBy(s => s);
			var expectedAsmFiles = ConsoleAppFrameworkAssemblies
					.Union(ConsoleAppUserAssemblies.Select(Path.GetFileName))
					.Select(s => s + ".s").OrderBy(s => s);
			CollectionAssert.AreEquivalent(expectedAsmFiles, actualAsmFiles);
			var actualGeneratedAotFiles = GetPathItems(result, "GeneratedAotFiles");
			var expectedGeneratedAotFiles = expectedAsmFiles.Select(f => $"obj\\Debug\\Aot\\{f}");
			CollectionAssert.AreEquivalent(expectedGeneratedAotFiles, actualGeneratedAotFiles);
		}

		[Test]
		public void GenerateLauncher()
		{
			var outputDir = Path.Combine(GetTestProjectDir("ConsoleApp"), "obj", "Debug");
			var result = BuildProject("ConsoleApp", targets: "GenerateLauncherTest", properties: new { MonoDevRoot });
			var expectedGeneratedSourceFiles = new[] {"launcher.c", "platform.c", "platform.h"}
					.Select(f => $"obj\\Debug\\Gen\\{f}");
			var actualGeneratedSourceFiles = GetPathItems(result, "GeneratedSourceFiles");
			CollectionAssert.AreEquivalent(expectedGeneratedSourceFiles, actualGeneratedSourceFiles);
		}

		[Test]
		public void GenerateBundledAssemblies()
		{
			var outputDir = Path.Combine(GetTestProjectDir("ConsoleApp"), "obj", "Debug", "Gen");
			var result = BuildProject("ConsoleApp", targets: "GenerateBundledAssembliesTest", properties: new { MonoDevRoot });
			var actualCFiles = Directory.GetFiles(outputDir, "*.c", SearchOption.AllDirectories)
					.Select(Path.GetFileName).OrderBy(s => s);
			Console.WriteLine(string.Join(" ", actualCFiles));
			var expectedCFiles = ConsoleAppFrameworkAssemblies
					.Union(ConsoleAppUserAssemblies.Select(Path.GetFileName))
					.Select(s => s + ".c").OrderBy(s => s);
			CollectionAssert.AreEquivalent(expectedCFiles, actualCFiles);
			var actualGeneratedSourceFiles = GetPathItems(result, "GeneratedSourceFiles");
			var expectedGeneratedSourceFiles = expectedCFiles.Select(f => $"obj\\Debug\\Gen\\{f}");
			CollectionAssert.AreEquivalent(expectedGeneratedSourceFiles, actualGeneratedSourceFiles);
		}

		[Test]
		public void CreateNativeWindowsExecutable()
		{
			var outputDir = Path.Combine(GetTestProjectDir("ConsoleApp"), "bin", "Debug", "Native");
			var exe = Path.Combine(outputDir, "ConsoleApp.exe");
			var result = BuildProject("ConsoleApp", targets: "CreateNativeWindowsExecutableTest", properties: new { MonoDevRoot });
			Assert.IsTrue(File.Exists(exe));

			var psi = new ProcessStartInfo() {
				FileName = exe, UseShellExecute = false,
				RedirectStandardOutput = true, RedirectStandardError = true,
				CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden
			};

			var output = new StringBuilder();
			DataReceivedEventHandler dataReceived = (sender, e) => {
				if (e.Data != null)
					output.AppendLine(e.Data);
			};

			var proc = new Process() { StartInfo = psi };
			proc.OutputDataReceived += dataReceived;
			proc.ErrorDataReceived += dataReceived;
			Assert.IsTrue(proc.Start());
			proc.BeginOutputReadLine();
			proc.BeginErrorReadLine();
			proc.WaitForExit();
			Assert.AreEqual(0, proc.ExitCode);

			StringAssert.AreEqualIgnoringCase($"Hello from {exe}!\r\nHello from ClassLibrary!\r\n", output.ToString());
		}

	}
}
