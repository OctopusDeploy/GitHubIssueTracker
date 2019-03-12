using System.ComponentModel;
using Octopus.Data.Resources.Attributes;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.Configuration
{
    public class GitHubConfigurationResource : ExtensionConfigurationResource
    {
        public const string GitHubBaseUrlDescription = "Set the base url for the Git repositories.";

        [DisplayName("GitHub Base Url")]
        [Description(GitHubBaseUrlDescription)]
        [Writeable]
        public string BaseUrl { get; set; }
    }
}