using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using StarsBot.Files;
using StarsBot.Slack;
using NHLAPI;
using NHLAPI.Models.Schedule;

namespace StarsBot
{
    internal class Program
    {
        private const int StarsTeamId = 25;

        private static bool _running = false;
        private static readonly SlackHandler Slack = new SlackHandler();

        private static FileHandler _fileHandler;
        private static Watcher _watcher;

        private static readonly CancellationTokenSource FetchScheduleCTS = new CancellationTokenSource();

        private static void Main()
        {
            Task<NHLScheduleData> schedTask = NHL.FetchSchedule(FetchScheduleCTS.Token);
            schedTask.RunSynchronously();

            string gameId = GetCurrentGameId(schedTask.Result);

            if (string.IsNullOrEmpty(gameId))
            {
                Console.WriteLine("The Stars do not play today.");
                Console.ReadLine();
                return;
            }

            StartWatching(gameId);

            // keep main thread open while running
            while (_running)
                Task.Delay(1000).Wait();

            StopWatching();

            Console.WriteLine("\nDone.");
            Console.ReadLine();
        }

        private static string GetCurrentGameId(NHLScheduleData sched)
        {
            foreach (Schedule date in sched.Dates)
            {
                foreach (ScheduledGame game in date.Games)
                {
                    if (game.HasTeamWithId(StarsTeamId))
                        return $"{game.Id}";
                }
            }

            return "";
        }

        private static void Announce(string message)
        {
            Slack.PostMessage(message.Trim());
        }

        private static void StartWatching(string _gameId)
        {
            Console.Clear();
            Console.WriteLine($"Watching game: {_gameId}\n");

            _fileHandler = new FileHandler(_gameId);

            _watcher = new Watcher(_gameId, _fileHandler);

            _watcher.GameScheduled += (_, r) => Announce(MessageBuilder.Greeting(r));
            _watcher.GameStarted += (_, r) => Announce(MessageBuilder.GameStart(r));

            _watcher.PeriodStarted += (_, r) => Announce(MessageBuilder.PeriodStart(r));
            _watcher.PeriodEnded += (_, r) => Announce(MessageBuilder.PeriodEnd(r));

            _watcher.Penalty += (_, r) => Announce(MessageBuilder.Penalty(r));

            _watcher.NullData += (_, r) =>
            {
                Console.WriteLine($"Null data received from server for game ID {r.GameId}");
                Console.WriteLine("Is the game ID number valid?");
                _running = false;
            };

            _watcher.GameEnded += (_, r) =>
            {
                Announce(MessageBuilder.GameEnd(r));
                _running = false;
            };

            _watcher.GoalScored += (_, r) =>
            {
                string message = MessageBuilder.GoalScored(r);

                if (!string.IsNullOrEmpty(message))
                    Announce(message);

                else
                {
                    RunningData data = _fileHandler.Load();
                    if (data.KnownPlays.Contains(r.CurrentPlay.About.EventIdx))
                    {
                        data.KnownPlays.Remove(r.CurrentPlay.About.EventIdx);
                        _fileHandler.Save(data);
                    }
                }
            };

            _watcher.Start();
            _running = true;
        }

        private static void StopWatching()
        {
            _watcher.Stop();
        }
    }
}
