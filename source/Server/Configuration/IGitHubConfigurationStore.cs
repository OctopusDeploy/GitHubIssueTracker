using System;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.Configuration
{
    public interface IGitHubConfigurationStore : IExtensionConfigurationStore<GitHubConfiguration>
    {
        string GetBaseUrl();
        void SetBaseUrl(string baseUrl);
    }
}
