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

        public string GetUsername()
        {
            return GetProperty(doc => doc.Username);
        }

        public void SetUsername(string username)
        {
            SetProperty(doc => doc.Username = username);
        }

        public string GetPassword()
        {
            return GetProperty(doc => doc.Password);
        }

        public void SetPassword(string password)
        {
            SetProperty(doc => doc.Password = password);
        }

        public string GetReleaseNotePrefix()
        {
            return GetProperty(doc => doc.ReleaseNotePrefix);
        }

        public void SetReleaseNotePrefix(string releaseNotePrefix)
        {
            SetProperty(doc => doc.ReleaseNotePrefix = releaseNotePrefix);
        }
    }
}
