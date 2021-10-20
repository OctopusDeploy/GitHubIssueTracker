using System.Threading;
using System.Threading.Tasks;
using Octopus.Server.Extensibility.Extensions.WorkItems;
using Octopus.Server.Extensibility.IssueTracker.GitHub.Configuration;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub
{
    class GitHubIssueTracker : IIssueTracker
    {
        internal static string Name = "GitHub";

        readonly IGitHubConfigurationStore configurationStore;

        public GitHubIssueTracker(IGitHubConfigurationStore configurationStore)
        {
            this.configurationStore = configurationStore;
        }

        public string CommentParser => GitHubConfigurationStore.CommentParser;
        public async Task<bool> IsEnabled(CancellationToken cancellationToken)
        {
            return await configurationStore.GetIsEnabled(cancellationToken);
        }

        public async Task<string?> BaseUrl(CancellationToken cancellationToken)
        {
            if (await configurationStore.GetIsEnabled(cancellationToken))
            {
                return await configurationStore.GetBaseUrl(cancellationToken);
            }

            return null;
        }

        public string IssueTrackerName => Name;
    }
}
