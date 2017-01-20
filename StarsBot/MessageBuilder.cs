using System;
using System.Collections.Generic;
using System.Linq;
using NHLAPI.Models.Game;

namespace StarsBot
{
    internal static class MessageBuilder
    {
        public static string Greeting(GameScheduledEventArgs args)
        {
            return "StarsBot online! I'll be watching and providing updates for " +
                   $"{args.Data.Teams.Away.TeamName} at {args.Data.Teams.Home.TeamName}" +
                   (args.Data.Venue.Name == null ? "." : ", live from " + $"{args.Data.Venue.Name}" +
                   (args.Data.Venue.City == null ? "." : $" in {args.Data.Venue.City}."));
        }

        public static string GameStart(GameStartedEventArgs args)
        {
            return $"{args.Data.Teams.Away.TeamName} at {args.Data.Teams.Home.TeamName} has started. "
                   + $"Start time: {args.StartTime:h:mm tt} CT";
        }

        public static string GameEnd(GameEndedEventArgs r)
        {
            string finalPeriod = r.CurrentPlay.About.Period > 3 ? $"/{r.CurrentPlay.About.OrdinalNumber}" : "";
            return $"{r.Data.Teams.Away.TeamName} at {r.Data.Teams.Home.TeamName} has ended. Final{finalPeriod}: " +
                   $"{r.Data.Teams.Away.TriCode} {r.Live.LineScore.Teams.Away.Goals} - " +
                   $"{r.Data.Teams.Home.TriCode} {r.Live.LineScore.Teams.Home.Goals}";
        }

        public static string PeriodStart(PeriodStartedEventArgs args)
        {
            return $"The {args.CurrentPlay.About.OrdinalNumber} period has started. " +
                   $"{args.Data.Teams.Away.TriCode} {args.CurrentPlay.About.Score.Away} - " +
                   $"{args.Data.Teams.Home.TriCode} {args.CurrentPlay.About.Score.Home}";
        }

        public static string PeriodEnd(PeriodEndedEventArgs args)
        {
            return $"The {args.CurrentPlay.About.OrdinalNumber} period has ended. " +
                   $"{args.Data.Teams.Away.TriCode} {args.CurrentPlay.About.Score.Away} - " +
                   $"{args.Data.Teams.Home.TriCode} {args.CurrentPlay.About.Score.Home}";
        }

        public static string Penalty(PenaltyEventArgs args)
        {
            Play play = args.CurrentPlay;

            PlayParticipant ppPenaltyOn = play.Players.FirstOrDefault(p => p.Role.Equals("PenaltyOn"));
            PlayParticipant ppDrewBy = play.Players.FirstOrDefault(p => p.Role.Equals("DrewBy"));

            if (ppPenaltyOn == null || ppDrewBy == null)
                return "";

            Player playerPenaltyOn = args.Data.Players.FirstOrDefault(p => p.Value.Id == ppPenaltyOn.Id).Value;
            Player playerDrewBy = args.Data.Players.FirstOrDefault(p => p.Value.Id == ppDrewBy.Id).Value;

            string message = $"{args.CurrentPlay.Team?.TriCode} penalty ({playerPenaltyOn?.FullName}), " +
                             $"{play.Result.PenaltyMinutes}-minute {play.Result.PenaltySeverity?.ToLower()} " +
                             $"for {play.Result.SecondaryType?.ToLower()}";

            if (playerDrewBy != null)
                message += $" (drawn by {playerDrewBy?.FullName})";


            message += $". {args.Data.Teams.Away.TriCode} {play.About.Score.Away} - " +
                       $"{args.Data.Teams.Home.TriCode} {play.About.Score.Home}, " +
                       $"{play.About.PeriodTimeRemaining} left in {play.About.OrdinalNumber}.";

            return message;
        }

        public static string GoalScored(GoalScoredEventArgs r)
        {
            PlayParticipant ppScorer = r.CurrentPlay.Players.FirstOrDefault(p => p.Role.Equals("Scorer"));
            int scorerId = ppScorer?.Id ?? 0;

            if (scorerId == 0)
            {
                Console.WriteLine("WARNING: could not parse scorer from goal event");
                return null;
            }

            Player scorer = r.Data.Players.FirstOrDefault(kv => kv.Value.Id == scorerId).Value;

            string message = $"{scorer.CurrentTeam?.TriCode} " +
                             (r.CurrentPlay.Result.Strength.Code.Equals("EVEN")
                                 ? "goal"
                                 : r.CurrentPlay.Result.Strength.Code) +
                             $" scored by #{scorer.PrimaryNumber} {scorer.FullName} " +
                             $"({ppScorer?.GoalsTotal})";

            List<PlayParticipant> ppAssisters = new List<PlayParticipant>(r.CurrentPlay.Players.Where(p => p.Role.Equals("Assist")));

            if (ppAssisters.Count > 0)
            {
                message += ". Assists: ";

                List<Player> assisters =
                    new List<Player>(
                        ppAssisters.Select(p => r.Data.Players.FirstOrDefault(kv => kv.Value.Id == p.Id).Value));

                if (assisters.Count > 0)
                {
                    message = assisters.Aggregate(message, (str, p) => str + $"{p.FullName}, ");
                    message = message.Substring(0, message.Length - 2);
                }

                else
                    message += "unassisted";
            }

            return message + $". {r.Data.Teams.Away.TriCode} {r.CurrentPlay.About.Score.Away} - " +
                              $"{r.Data.Teams.Home.TriCode} {r.CurrentPlay.About.Score.Home}, " +
                              $"{r.CurrentPlay.About.PeriodTimeRemaining} left in {r.CurrentPlay.About.OrdinalNumber}.";
        }
    }
}
