using System;
using Octopus.Data.Storage.Configuration;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.Configuration
{
    public class GitHubConfigurationStore : ExtensionConfigurationStore<GitHubConfiguration>, IGitHubConfigurationStore
    {
        public static string CommentParser = "GitHub";
        public static string SingletonId = "issuetracker-github";
        
        public GitHubConfigurationStore(IConfigurationStore configurationStore) : base(configurationStore)
        {
        }

        public override string Id => SingletonId;

        public string GetBaseUrl()
        {
            return GetProperty(doc => doc.BaseUrl?.Trim('/'));
        }

        public void SetBaseUrl(string baseUrl)
        {
            SetProperty(doc => doc.BaseUrl = baseUrl?.Trim('/'));
        }
    }
}
