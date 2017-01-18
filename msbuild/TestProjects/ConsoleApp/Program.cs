using System;
using System.Diagnostics;

namespace ConsoleApp
{
	public class Program
	{
		public static void Main(string[] args)
		{
			// Reference something in System.dll to make ResolveAssemblies
			// include it in the resolved framework assemblies.
			Console.WriteLine($"Hello from {Process.GetCurrentProcess().MainModule.FileName}!");
			// Likewise we need to reference something in ClassLibrary.
			ClassLibrary.LibClass.SayHello();
		}
	}
}
