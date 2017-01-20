namespace StarsBot.Slack
{
    internal static class SlackInfo
    {
        private static bool _debugging = false;

        // TESTING CHANNEL - TEST HERE ONLY, NOWHERE ELSE
        private const string DebugWebhookUrl =
          "https://hooks.slack.com/services/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";

        // PUBLIC CHANNEL - FOR RELEASE RUNS ONLY
        private const string ReleaseWebhookUrl =
            "https://hooks.slack.com/services/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";

        public static string ChannelName => _debugging ? "#testing" : "#dallasstars";
        public static string WebhookUrl => _debugging ? DebugWebhookUrl : ReleaseWebhookUrl;
    }
}
