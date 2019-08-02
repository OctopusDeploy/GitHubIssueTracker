using System.ComponentModel.DataAnnotations;
using Octopus.Data.Model;
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
        public ReleaseNoteOptions ReleaseNoteOptions { get; set; } = new ReleaseNoteOptions();
    }

    public class ReleaseNoteOptions
    {
        public string Username { get; set; }
        [Encrypted]
        public string Password { get; set; }
        public string ReleaseNotePrefix { get; set; }
    }
}
