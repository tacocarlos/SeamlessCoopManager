using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeamlessCoopManager.Manager
{
	public interface ModApplicator
	{
		public enum ModApplicationResult
		{
			SUCCESS,
			INVALID_MOD,
			UNKNOWN_INSTALL_PATH,
			FAILED_UNKNOWN
		}

		public static IniData MergeIniData(FileIniDataParser parser, string userIniPath, string modIniPath)
		{
			IniData mod = parser.ReadFile(modIniPath);
			if(File.Exists(userIniPath)) 
			{
				IniData user = parser.ReadFile(userIniPath);
				mod.Merge(user);
			}
			return mod;
		}

		public string GetRelativeSettingsIniPath();

		public ModApplicationResult ApplyMod(Game game, string modFolderPath);

		public ModApplicationResult UpdateMod(Game game, string modZipPath)
		{
			string destPath = Path.GetTempPath() + "\\sc_update\\";
			ZipFile.ExtractToDirectory(modZipPath, destPath, true);			
			ModApplicationResult result = ApplyMod(game, destPath);
			Directory.Delete(destPath, true);
			return result;
		}
	}
}
