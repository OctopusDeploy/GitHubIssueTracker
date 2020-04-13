using System;
using Octopus.Data.Model;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.Configuration
{
    interface IGitHubConfigurationStore : IExtensionConfigurationStore<GitHubConfiguration>
    {
        string GetBaseUrl();
        void SetBaseUrl(string baseUrl);

        string GetUsername();
        void SetUsername(string username);

        SensitiveString GetPassword();
        void SetPassword(SensitiveString password);

        string GetReleaseNotePrefix();
        void SetReleaseNotePrefix(string releaseNotePrefix);
    }
}
