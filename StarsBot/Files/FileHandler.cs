using System;
using System.IO;

namespace StarsBot.Files
{
    public class FileHandler
    {
        private readonly Uri _uri;
        public string GameId { get; }

        public FileHandler(string gameId)
        {
            GameId = gameId;
            _uri = new Uri(Uri.UnescapeDataString(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{GameId}.json")));
        }

        public void Save(RunningData data)
            => File.WriteAllText(_uri.AbsolutePath, data.ToString());

        public RunningData Load()
        {
            RunningData result = null;

            try
            {
                result = RunningData.FromString(File.ReadAllText(_uri.AbsolutePath));
            }

            catch (Exception e)
            {
                if (e is FileNotFoundException || e is DirectoryNotFoundException)
                    Console.WriteLine($"File not found: {_uri.AbsolutePath}.\nUsing default RunningData.\n");

                else
                    throw;
            }

            return result ?? new RunningData();
        }
    }
}
