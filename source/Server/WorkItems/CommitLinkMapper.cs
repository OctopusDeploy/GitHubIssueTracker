using Octopus.Server.Extensibility.Extensions.WorkItems;
using Octopus.Server.Extensibility.IssueTracker.GitHub.Configuration;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.WorkItems
{
    public class CommitLinkMapper : ICommitLinkMapper
    {
        private readonly IGitHubConfigurationStore configurationStore;

        public CommitLinkMapper(IGitHubConfigurationStore configurationStore)
        {
            this.configurationStore = configurationStore;
        }

        public string VcsType => "Git";
        public bool IsEnabled => configurationStore.GetIsEnabled();

        public string Map(string vcsRoot, string commitNumber)
        {
            if (!IsEnabled)
                return null;

            return vcsRoot + "/commit/" + commitNumber;
        }
    }
}