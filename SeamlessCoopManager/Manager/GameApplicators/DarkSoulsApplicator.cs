using System.IO;

namespace SeamlessCoopManager.Manager.GameApplicators
{
	internal class DarkSoulsApplicator: GenericDefaultApplicator
	{
		private static readonly string INIT_REL_PATH = Path.Join("SeamlessCoop", "ds1sc_settings.ini");
		public override string GetRelativeSettingsIniPath()
		{
			return INIT_REL_PATH;
		}
	}
}
