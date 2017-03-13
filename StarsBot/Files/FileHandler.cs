using System;
using System.IO;
using NHLAPI.Models.Game;

namespace StarsBot.Files
{
    public class FileHandler
    {
        private readonly Uri _dataFileUri;
        private readonly Uri _dumpsDirUri;

        public string GameId { get; }

        public FileHandler(string gameId)
        {
            GameId = gameId;

            // create URI and validate directory for data
            Uri dataDirUri = new Uri(Uri.UnescapeDataString(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StarsBotData")));
            Directory.CreateDirectory(dataDirUri.AbsolutePath);

            // create URI and validate directory for data dumps
            _dumpsDirUri = new Uri(Uri.UnescapeDataString(Path.Combine(dataDirUri.AbsolutePath, "dump", $"{GameId}")));
            Directory.CreateDirectory(_dumpsDirUri.AbsolutePath);

            // create URI for data file
            _dataFileUri = new Uri(Uri.UnescapeDataString(Path.Combine(dataDirUri.AbsolutePath, $"{GameId}.json")));
        }

        public void Save(RunningData data)
            => File.WriteAllText(_dataFileUri.AbsolutePath, data.ToString());

        public RunningData Load()
        {
            RunningData result = null;

            try
            {
                result = RunningData.FromString(File.ReadAllText(_dataFileUri.AbsolutePath));
            }

            catch (Exception e)
            {
                if (e is FileNotFoundException || e is DirectoryNotFoundException)
                    Console.WriteLine($"File not found: {_dataFileUri.AbsolutePath}.\nUsing default RunningData.\n");

                else
                    throw;
            }

            return result ?? new RunningData();
        }

        public void DumpPlay(Play play)
        {
            string outPath = Path.Combine(_dumpsDirUri.AbsolutePath, $"{play.About.EventIdx}.json");
            Console.Error.WriteLine($"Dumping contents of play #{play.About.EventIdx} to file: {outPath}");

            File.WriteAllText(outPath, play.ToString());
        }
    }
}
