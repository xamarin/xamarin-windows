using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Pdb2Mdb;

namespace Xamarin.Windows.Tasks
{
	public class ConvertDebuggingFiles : Task
	{
		// The .pdb files we need to convert
		[Required]
		public ITaskItem[] Files { get; set; }

		public override bool Execute()
		{
			Log.LogDebugMessage("ConvertDebuggingFiles Task");
			Log.LogDebugMessage("  InputFiles: {0}", Files);

			foreach (var file in Files) {
				var pdb = file.ToString();

				if (!File.Exists(pdb)) {
					Log.LogDebugMessage("  Skipping non-existing file: {0}", pdb);
					continue;
				}

				try {
					var assembly = Path.ChangeExtension(pdb, ".dll");
					if (!File.Exists(assembly))
						assembly = Path.ChangeExtension(pdb, ".exe");
					Log.LogDebugMessage("  Converting file: {0} -> {1}.mdb", pdb, assembly);
					//MonoAndroidHelper.SetWriteable(pdb);
					Converter.Convert(assembly);
				} catch (PortablePdbNotSupportedException) {
					Log.LogDebugMessage("Not converting portable PDB: {0}", pdb);
				} catch (Exception ex) {
					Log.LogWarningFromException(ex, true);
				}
			}

			return true;
		}
	}
}
