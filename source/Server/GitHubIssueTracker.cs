using System;
using Octopus.Server.Extensibility.Extensions.WorkItems;
using Octopus.Server.Extensibility.IssueTracker.GitHub.Configuration;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub
{
    public class GitHubIssueTracker : IIssueTracker
    {
        internal static string Name = "GitHub";

        readonly IGitHubConfigurationStore configurationStore;

        public GitHubIssueTracker(IGitHubConfigurationStore configurationStore)
        {
            this.configurationStore = configurationStore;
        }

        public string CommentParser => GitHubConfigurationStore.CommentParser;
        public string IssueTrackerName => Name;

        public bool IsEnabled => configurationStore.GetIsEnabled();

        public string BaseUrl => configurationStore.GetIsEnabled() ? configurationStore.GetBaseUrl() : null;
    }
}
