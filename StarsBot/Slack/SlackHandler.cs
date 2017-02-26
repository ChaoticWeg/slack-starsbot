using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Slack.Webhooks;

namespace StarsBot.Slack
{
    public class SlackHandler
    {
        public static readonly string ChannelName = SlackInfo.ChannelName;
        public static readonly string BotName = "StarsBot";

        public static readonly int DefaultPostDelayMs = 20000;
        public static readonly int PostDelayMs = DefaultPostDelayMs;

        private readonly SlackClient _internalClient = new SlackClient(SlackInfo.WebhookUrl);

        public void PostMessage(string text)
        {
            Console.WriteLine(text);
            Debug.WriteLine($"[{DateTime.Now:HH:mm:ss}] Posting message in {PostDelayMs / 1000} seconds: {text}");

            Task.Delay(PostDelayMs)
                .ContinueWith(a =>
                {
                    _internalClient.Post(BuildSlackMessage(text));
                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss}] Posted message: {text}");
                });
        }

        private static SlackMessage BuildSlackMessage(string text) => new SlackMessage
        {
            Channel = ChannelName,
            Text = text,
            Username = BotName,
            IconEmoji = ":dallasstars:"
        };
    }
}
