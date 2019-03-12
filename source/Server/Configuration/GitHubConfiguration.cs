using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.Configuration
{
    public class GitHubConfiguration : ExtensionConfigurationDocument
    {
        public GitHubConfiguration() : base("GitHub", "Octopus Deploy", "1.0")
        {
            Id = GitHubConfigurationStore.SingletonId;
        }

        public string BaseUrl { get; set; }
    }
}
