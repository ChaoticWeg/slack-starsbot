using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NHLAPI;
using NHLAPI.Models.Game;
using StarsBot.Files;

namespace StarsBot
{
    public partial class Watcher
    {
        private const int DelayMs = 5000;

        public string GameId { get; }
        public bool Running { get; private set; }

        private readonly FileHandler _fileHandler;

        private CancellationTokenSource _loopTokenSource;
        private CancellationToken _loopToken;

        private CancellationTokenSource _tickTokenSource;
        private CancellationToken _tickToken;

        public Watcher(string gameId, FileHandler fh)
        {
            GameId = gameId;
            Running = false;
            _fileHandler = fh;
        }

        public Watcher(string gameId) : this(gameId, new FileHandler(gameId)) { }

        public void Start() { Running = true; LoopAsync(); }
        public void Stop() { Running = false; }


        // ReSharper disable once AccessToDisposedClosure
        private async void LoopAsync()
        {
            _loopTokenSource?.Dispose();
            _loopTokenSource = new CancellationTokenSource();

            _loopToken = new CancellationTokenSource().Token;

            try
            {
                await Task.Factory.StartNew(() =>
                {
                    while (Running && !_loopToken.IsCancellationRequested)
                    {
                        _tickTokenSource?.Dispose();
                        _tickTokenSource = new CancellationTokenSource(5000);

                        Tick();

                        Task.Delay(DelayMs, _loopToken)
                            .Wait(_loopToken);
                    }

                }, _loopToken);

            }

            // free up data once loop is done
            finally
            {
                _tickTokenSource.Dispose();
                _loopTokenSource.Dispose();
            }
        }


        public void Tick()
        {
            _tickToken = new CancellationTokenSource(4750).Token;

            if (_tickToken.IsCancellationRequested)
                return;

            Stopwatch sw = new Stopwatch();

            try
            {
                sw.Start();

                Task<NHLData> task = NHL.FetchGame(GameId, _tickToken);
                task.RunSynchronously();
                OnDataReceived(task.Result);

                sw.Stop();
            }

            catch (TaskCanceledException)
            {
                sw.Stop();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] " +
                                  $"WARNING: tick task cancelled after {sw.ElapsedMilliseconds}ms (did it take too long?)");
            }

            catch (AggregateException ex)
            {
                foreach (Exception ie in ex.InnerExceptions)
                    Console.WriteLine(ie);
            }
        }


        private void ProcessPlay(Play play, NHLData context, RunningData runningData)
        {
            List<int> knownPlays = runningData.KnownPlays;

            int playId = play?.About.EventIdx ?? -1;

            if (playId != -1 && !knownPlays.Contains(playId))
            {
                string eventType = play?.Result.EventTypeId;

                if (eventType != null)
                {

                    // shootout try
                    if (play.About.Period == 5)
                    {
                        if (eventType.Equals("GOAL"))
                            ShootoutTry?.Invoke(this, new ShootoutGoalEventArgs(context.Game, play));

                        else if (eventType.Equals("SHOT"))
                            ShootoutTry?.Invoke(this, new ShootoutMissEventArgs(context.Game, play));
                    }


                    // not a shootout try
                    else
                    {
                        if (eventType.Equals("GAME_SCHEDULED"))
                            GameScheduled?.Invoke(this, new GameScheduledEventArgs(context.Game));


                        if (eventType.Equals("PERIOD_START"))
                        {
                            if (play.About.Period == 1)
                                GameStarted?.Invoke(this,
                                    new GameStartedEventArgs(context.Game,
                                        TimeZoneInfo.ConvertTimeFromUtc(DateTime.Parse(play.About.DateTimeStr, null,
                                            System.Globalization.DateTimeStyles.RoundtripKind), TimeZoneInfo.Local)));
                            else
                                PeriodStarted?.Invoke(this, new PeriodStartedEventArgs(context.Game, play));
                        }


                        if (eventType.Equals("PERIOD_END"))
                            PeriodEnded?.Invoke(this, new PeriodEndedEventArgs(context.Game, play));

                        if (eventType.Equals("GOAL"))
                            GoalScored.Invoke(this, new GoalScoredEventArgs(context.Game, play));

                        if (eventType.Equals("GAME_END"))
                            GameEnded?.Invoke(this, new GameEndedEventArgs(context.Game, context.Live, play));

                        if (eventType.Equals("PENALTY"))
                            Penalty?.Invoke(this, new PenaltyEventArgs(context.Game, play));

                    } // end not a shootout try

                } // end if play has ID and is unknown

                knownPlays.Add(playId);
            }
        }


        private void OnDataReceived(NHLData data)
        {
            try
            {
                RunningData runningData = _fileHandler.Load();

                if (data == null)
                {
                    NullData?.Invoke(this, new NullDataEventArgs(GameId));
                    return;
                }

                if (data.Live.LineScore.CurrentPeriod > 0 && data.Live.Plays.AllPlays.Count > 0)
                {
                    List<Play> unknownPlays =
                        data.Live.Plays.AllPlays.Where(p => !runningData.KnownPlays.Contains(p.About.EventIdx)).ToList();

                    if (unknownPlays.Count > 0)
                        unknownPlays.ForEach(p => ProcessPlay(p, data, runningData));

                    else
                    {
                        Play currentPlay = data.Live.Plays.CurrentPlay;

                        if (currentPlay.Result.EventTypeId.Equals("GAME_END"))
                            GameEnded?.Invoke(this, new GameEndedEventArgs(data.Game, data.Live, currentPlay));
                    }
                }

                _fileHandler.Save(runningData);
            }

            catch (AggregateException e)
            {
                foreach (Exception ie in e.InnerExceptions)
                    Console.WriteLine(ie);
            }

            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
