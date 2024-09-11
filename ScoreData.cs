using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EZ2ScoreShare
{
	internal class ScoreData
	{
		public string Filename { get; set; }

		public ScoreEntry[] Scores { get; set; }

		public class ScoreEntry
		{
			public string Player { get; set; }

			public int Score { get; set; }

			public ScoreEntry(string player, int score)
			{
				Player = player;
				Score = score;
			}
		}

		public ScoreData(string filename)
		{
			Filename = Path.GetFileNameWithoutExtension(filename);

			byte[] data = File.ReadAllBytes(filename);

			Scores = new ScoreEntry[5]
			{
				new ScoreEntry(Encoding.ASCII.GetString(data, 0x00, 0x08), BitConverter.ToInt32(data, 0x28)),
				new ScoreEntry(Encoding.ASCII.GetString(data, 0x08, 0x08), BitConverter.ToInt32(data, 0x2C)),
				new ScoreEntry(Encoding.ASCII.GetString(data, 0x10, 0x08), BitConverter.ToInt32(data, 0x30)),
				new ScoreEntry(Encoding.ASCII.GetString(data, 0x18, 0x08), BitConverter.ToInt32(data, 0x34)),
				new ScoreEntry(Encoding.ASCII.GetString(data, 0x20, 0x08), BitConverter.ToInt32(data, 0x38)),
			};
		}

		public ScoreData(string filename, ScoreEntry[] entries)
		{
			Filename = filename;

			List<ScoreEntry> sortedEntries = entries.OrderByDescending((x) => x.Score).ToList();

			Scores = sortedEntries.ToArray();
		}

		public static ScoreData MergeScores(ScoreData local, ScoreData remote)
		{
			List<ScoreEntry> entries = new List<ScoreEntry>();

			foreach (ScoreEntry e in local.Scores) if(e.Score != 0) entries.Add(e);
			foreach (ScoreEntry e in remote.Scores) if (!entries.Exists((x) => x.Player == e.Player && x.Score == e.Score) && e.Score != 0) entries.Add(e);

			List<string> uniqueNames = new List<string>();
			foreach (ScoreEntry e in entries) if (!uniqueNames.Contains(e.Player)) uniqueNames.Add(e.Player);

			List<ScoreEntry> onePlayer = new List<ScoreEntry>();
			List<ScoreEntry> filteredEntries = new List<ScoreEntry>();
			foreach (string s in uniqueNames)
			{
				onePlayer.Clear();

				onePlayer = entries.FindAll((x) => x.Player == s).OrderByDescending((x) => x.Score).ToList();
				filteredEntries.Add(onePlayer[0]);
			}
			
			ScoreData result = new ScoreData(local.Filename, filteredEntries.OrderByDescending((x) => x.Score).ToArray());

			return result;
		}

		public byte[] GetBytes()
		{
			byte[] result = new byte[0x3C];

			for(int p = 0; p < 5; p++)
			{
				if (p < Scores.Length)
					Encoding.ASCII.GetBytes(Scores[p].Player).CopyTo(result, p * 8);
				else
					Encoding.ASCII.GetBytes(Program.DefaultName).CopyTo(result, p * 8);
			}
			for(int s = 0; s < 5; s++)
			{
				if (s < Scores.Length)
					BitConverter.GetBytes(Scores[s].Score).CopyTo(result, 0x28 + s * 4);
			}

			return result;
		}
	}
}
