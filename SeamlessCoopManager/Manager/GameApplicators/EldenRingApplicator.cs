using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeamlessCoopManager.Manager.GameApplicators
{
	internal class EldenRingApplicator : GenericDefaultApplicator
	{
		private static readonly string INIT_REL_PATH = Path.Join("SeamlessCoop", "ersc_settings.ini");
		public override string GetRelativeSettingsIniPath()
		{
			return INIT_REL_PATH;
		}
	}
}
