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

		[Output]
		public ITaskItem[] MdbFiles { get; set; }

		[Output]
		public ITaskItem[] PPdbFiles { get; set; }

		public override bool Execute()
		{
			Log.LogDebugMessage("ConvertDebuggingFiles Task");
			Log.LogDebugMessage("  Files: {0}", Files);

			var mdbs = new List<string>();
			var ppdbs = new List<string>();
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
					var mdb = assembly + ".mdb";
					if (File.Exists(pdb) && File.Exists(mdb) && File.GetLastWriteTime(pdb) <= File.GetLastWriteTime(mdb)) {
						Log.LogDebugMessage("  Not converting unchanged file: {0}", pdb);
					} else {
						Log.LogDebugMessage("  Trying to convert file: {0} -> {1}", pdb, mdb);
						Converter.Convert(assembly);
					}
					mdbs.Add(mdb);
				} catch (PortablePdbNotSupportedException) {
					Log.LogDebugMessage("Not converting portable PDB: {0}", pdb);
					ppdbs.Add(pdb);
				} catch (Exception ex) {
					Log.LogWarningFromException(ex, true);
				}
			}

			MdbFiles = mdbs.Select(f => new TaskItem(f)).ToArray();
			PPdbFiles = ppdbs.Select(f => new TaskItem(f)).ToArray();
			Log.LogDebugTaskItems("  [Output] MdbFiles:", MdbFiles);
			Log.LogDebugTaskItems("  [Output] PPdbFiles:", PPdbFiles);

			return true;
		}
	}
}
