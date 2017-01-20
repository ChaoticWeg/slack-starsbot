using System;
using NHLAPI.Models.Game;

namespace StarsBot
{
    public partial class Watcher
    {

#pragma warning disable 67
        public event EventHandler<NullDataEventArgs> NullData;

        public event EventHandler<GameScheduledEventArgs> GameScheduled;
        public event EventHandler<GameStartedEventArgs> GameStarted;
        public event EventHandler<GameEndedEventArgs> GameEnded;

        public event EventHandler<PeriodStartedEventArgs> PeriodStarted;
        public event EventHandler<PeriodEndedEventArgs> PeriodEnded;

        public event EventHandler<PenaltyEventArgs> Penalty;
        public event EventHandler<GoalScoredEventArgs> GoalScored;
#pragma warning restore 67

    }

    public abstract class NHLEventArgs : EventArgs
    {
        public GameData Data { get; }

        protected NHLEventArgs(GameData data)
        {
            Data = data;
        }
    }

    public class NullDataEventArgs : NHLEventArgs
    {
        public string GameId { get; private set; }

        public NullDataEventArgs(string gameId) : base(null)
        {
            GameId = gameId;
        }
    }

    public class GameScheduledEventArgs : NHLEventArgs
    {
        public DateTime ScheduledTime { get; private set; }

        public GameScheduledEventArgs(GameData data) : base(data)
        {
            ScheduledTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.Parse(Data.DateTime.StartTimeStr, null,
                System.Globalization.DateTimeStyles.RoundtripKind), TimeZoneInfo.Local);
        }
    }

    public class GameStartedEventArgs : NHLEventArgs
    {
        public DateTime StartTime { get; private set; }

        public GameStartedEventArgs(GameData data, DateTime dt) : base(data)
        {
            StartTime = dt;
        }
    }

    public class PeriodStartedEventArgs : NHLEventArgs
    {
        public Play CurrentPlay { get; private set; }

        public PeriodStartedEventArgs(GameData data, Play currentPlay) : base(data)
        {
            CurrentPlay = currentPlay;
        }
    }

    public class PenaltyEventArgs : NHLEventArgs
    {
        public Play CurrentPlay { get; private set; }

        public PenaltyEventArgs(GameData data, Play currentPlay) : base(data)
        {
            CurrentPlay = currentPlay;
        }
    }

    public class PeriodEndedEventArgs : NHLEventArgs
    {
        public Play CurrentPlay { get; private set; }

        public PeriodEndedEventArgs(GameData data, Play currentPlay) : base(data)
        {
            CurrentPlay = currentPlay;
        }
    }

    public class GameEndedEventArgs : NHLEventArgs
    {
        public LiveData Live { get; private set; }
        public Play CurrentPlay { get; private set; }

        public GameEndedEventArgs(GameData data, LiveData live, Play currentPlay) : base(data)
        {
            Live = live;
            CurrentPlay = currentPlay;
        }
    }

    public class GoalScoredEventArgs : NHLEventArgs
    {
        public Play CurrentPlay { get; private set; }

        public GoalScoredEventArgs(GameData data, Play currentPlay) : base(data)
        {
            CurrentPlay = currentPlay;
        }
    }
}
