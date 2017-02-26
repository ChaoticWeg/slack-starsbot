using System;
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

            Console.WriteLine($"About to start watching game {gameId}. Debug: {SlackInfo.Debugging}");
            Console.WriteLine($"Slack message posting delay is {SlackHandler.PostDelayMs / 1000.0:F1} seconds.");

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

        private static void Announce(string message, NHLAPI.Models.Game.Play play = null)
        {
            if (string.IsNullOrEmpty(message) || string.IsNullOrWhiteSpace(message))
            {
                if (play != null)
                {
                    Console.WriteLine($"ERROR: null, empty, or whitespace message built for play: #{play.About.EventIdx}");
                    _fileHandler.DumpPlay(play);
                }

                else
                {
                    Console.WriteLine("ERROR: null, empty, or whitespace message built, but null play passed to Announce()");
                }

                return;
            }

            Slack.PostMessage(message.Trim());
        }

        private static void StartWatching(string gameId)
        {
            Console.WriteLine($"\nWatching game: {gameId}\n");

            _fileHandler = new FileHandler(gameId);

            _watcher = new Watcher(gameId, _fileHandler);

            _watcher.GameScheduled += (_, r) => Announce(MessageBuilder.Greeting(r));
            _watcher.GameStarted += (_, r) => Announce(MessageBuilder.GameStart(r));

            _watcher.PeriodStarted += (_, r) => Announce(MessageBuilder.PeriodStart(r), r.CurrentPlay);
            _watcher.PeriodEnded += (_, r) => Announce(MessageBuilder.PeriodEnd(r), r.CurrentPlay);

            _watcher.Penalty += (_, r) => Announce(MessageBuilder.Penalty(r), r.CurrentPlay);

            _watcher.GoalScored += (_, r) => Announce(MessageBuilder.GoalScored(r), r.CurrentPlay);
            _watcher.ShootoutTry += (_, r) => Announce(MessageBuilder.ShootoutTry(r), r.CurrentPlay);


            _watcher.NullData += (_, r) =>
            {
                Console.WriteLine($"Null data received from server for game ID {r.GameId}");
                Console.WriteLine("Is the game ID number valid?");
                _running = false;
            };

            _watcher.GameEnded += (_, r) =>
            {
                Announce(MessageBuilder.GameEnd(r), r.CurrentPlay);
                _running = false;
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
