using Npgsql;
using System;
using System.Xml.Linq;

namespace EZ2ScoreShare
{
	internal class Program
	{
		public static List<ScoreData> LocalScores;
		public static List<ScoreData> RemoteScores;
		public static string DefaultName;

		static void Main(string[] args)
		{
			Console.WriteLine("EZ2ScoreShare - R U READY 2 SHARE SUM SCORES?");
			Console.WriteLine();

			if (args.Length < 4)
			{
				Console.WriteLine("USAGE:\n" +
				"\tEZ2ScoreShare.exe {server} {user} {password} {database} {table} {defaultName=\"EZ2AC_FN\"}\n" +
				"\tCLI only.");
				Console.Read();
				return;
			}

			LocalScores = new List<ScoreData>();
			RemoteScores = new List<ScoreData>();

			if (args.Length < 6) DefaultName = "EZ2AC_FN";
			else DefaultName = args[5];

			if(Directory.Exists(AppContext.BaseDirectory + "Sound"))
			{
				Console.WriteLine(AppContext.BaseDirectory + "Sound");

				foreach (string path in Directory.GetFiles(AppContext.BaseDirectory + "Sound", "rank_*.bin"))
				{
					LocalScores.Add(new ScoreData(path));
				}
				Console.WriteLine($"Local rankings: {LocalScores.Count}");
			}
			else
			{
				Console.WriteLine("Can't find Sound folder! Make sure you are starting EZ2ScoreShare from game's root directory.");
				return;
			}
			string connectionString = $"server={args[0]};uid={args[1]};pwd={args[2]};database={args[3]}";

			var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
			var dataSource = dataSourceBuilder.Build();

			Console.WriteLine("Fetching rankings from server....");

			try
			{
				var conn = dataSource.OpenConnection();

				List<string> remoteFilenames = new List<string>();

				using (var cmd = new NpgsqlCommand($"SELECT * FROM \"{args[4]}\";", conn))
				using (var reader =  cmd.ExecuteReader())
				{
					while (reader.Read())
						remoteFilenames.Add(reader.GetString(0));
				}

				List<string> uniqueFilenames = new List<string>();
				List<ScoreData.ScoreEntry> entries = new List<ScoreData.ScoreEntry>();

				foreach (string f in remoteFilenames)
					if (!uniqueFilenames.Contains(f)) uniqueFilenames.Add(f);

				foreach (string f in uniqueFilenames)
				{
					entries.Clear();
					using (var cmd = new NpgsqlCommand($"SELECT * FROM \"{args[4]}\" WHERE filename='{f}';", conn))
					using (var reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							entries.Add(new ScoreData.ScoreEntry(reader.GetString(1), reader.GetInt32(2)));
						}
					}
					RemoteScores.Add(new ScoreData(f, entries.ToArray()));
				}				

				Console.WriteLine("ScoreShare found these rankings on server:");
				foreach(ScoreData data in RemoteScores)
				{
					Console.WriteLine($"\t> {data.Filename} ({data.Scores.Length} score(-s))");
				}

				Console.WriteLine("Uploading local scores....");
				foreach (ScoreData data in LocalScores)
				{
					foreach (ScoreData.ScoreEntry e in data.Scores)
					{
						if (e.Score > 0)
						{
							using (var cmd = new NpgsqlCommand($"INSERT INTO \"{args[4]}\" (filename, player, score) VALUES(@f, @p, @s) ON CONFLICT DO NOTHING;", conn))
							{
								cmd.Parameters.AddWithValue("f", data.Filename.ToLower());
								cmd.Parameters.AddWithValue("p", e.Player);
								cmd.Parameters.AddWithValue("s", e.Score);
								cmd.ExecuteNonQuery();
							}
						}
					}
				}

				Console.WriteLine("Updating local scores....");
				List<ScoreData> mergedScores = new List<ScoreData>();
				foreach(ScoreData remote in RemoteScores)
				{
					ScoreData? local = LocalScores.Find((x)=> x.Filename == remote.Filename.ToLower());
					if (local != null)
						mergedScores.Add(ScoreData.MergeScores(local, remote));
					else
						mergedScores.Add(remote);
				}
				foreach(ScoreData data in mergedScores)
				{
					File.WriteAllBytes(AppContext.BaseDirectory + "Sound\\" + data.Filename + ".bin", data.GetBytes());
				}

				Console.WriteLine($"Successfully written {mergedScores.Count} rankings!");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"An exception has occurred: {ex}");
				Console.Read();
				return;
			}
		}
	}
}