using System.ComponentModel;
using Octopus.Client.Extensibility.Attributes;
using Octopus.Client.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Client.Extensibility.IssueTracker.GitHub
{
    public class GitHubConfigurationResource : ExtensionConfigurationResource
    {
        public const string GitHubBaseUrlDescription = "Set the base url for the GitHub repositories.";

        public GitHubConfigurationResource()
        {
            Id = "issuetracker-github";
            BaseUrl = "https://github.com";
        }

        [DisplayName("GitHub Base Url")]
        [Description(GitHubBaseUrlDescription)]
        [Writeable]
        public string BaseUrl { get; set; }
    }
}