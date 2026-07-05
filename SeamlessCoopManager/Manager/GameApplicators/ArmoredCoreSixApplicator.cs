using System.IO;

namespace SeamlessCoopManager.Manager.GameApplicators
{
	internal class ArmoredCoreSixApplicator : GenericDefaultApplicator
	{
		private static readonly string INIT_REL_PATH = Path.Join("AC6Coop", "ac6_coop_settings.ini");
		public override string GetRelativeSettingsIniPath()
		{
			return INIT_REL_PATH;
		}
	}
}
