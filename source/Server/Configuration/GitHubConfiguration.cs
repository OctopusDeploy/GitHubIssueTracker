using System.ComponentModel.DataAnnotations;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.Configuration
{
    public class GitHubConfiguration : ExtensionConfigurationDocument
    {
        public GitHubConfiguration() : base("GitHub", "Octopus Deploy", "1.0")
        {
            Id = GitHubConfigurationStore.SingletonId;
            BaseUrl = "https://github.com";
        }

        [Required]
        public string BaseUrl { get; set; }
    }
}
