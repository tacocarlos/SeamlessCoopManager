using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static SeamlessCoopManager.Manager.Game;

namespace SeamlessCoopManager.Manager
{
	public static class Applicators
	{
		private static readonly Dictionary<string, ModApplicator> _applicators = new Dictionary<string, ModApplicator>()
		{
			{ DSR.ShortName, new GameApplicators.DarkSoulsApplicator() },
			{ DS2.ShortName, new GameApplicators.DarkSoulsTwoApplicator() },
			{ DS3.ShortName, new GameApplicators.DarkSoulsThreeApplicator() },
			{ ELDEN_RING.ShortName, new GameApplicators.EldenRingApplicator() },
			{ NIGHTREIGN.ShortName, new GameApplicators.NightreignApplicator() },
			{ AC6.ShortName, new GameApplicators.ArmoredCoreSixApplicator()  }
		};
		public static ModApplicator GetApplicator(Game game)
		{
			ModApplicator? applicator;
			bool found = _applicators.TryGetValue(game.ShortName, out applicator);

			if (found && applicator != null) { return applicator; }

			throw new InvalidDataException($"No applicator found for game {game.Name}");
		}
	}
}
