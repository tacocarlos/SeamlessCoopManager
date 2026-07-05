using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeamlessCoopManager.Manager
{
	public static class IOUtils
	{
		public static void CopyDirectory(string sourceDir, string destinationDir)
		{
			if (!Directory.Exists(destinationDir))
			{
				Directory.CreateDirectory(destinationDir);
			}
			foreach (string file in Directory.GetFiles(sourceDir))
			{
				string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
				File.Copy(file, destFile, true);
			}
			foreach (string dir in Directory.GetDirectories(sourceDir))
			{
				string destDir = Path.Combine(destinationDir, Path.GetFileName(dir));
				CopyDirectory(dir, destDir);
			}
		}
	}
}
