using Octopus.Data.Model;
using Octopus.Data.Storage.Configuration;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.Configuration
{
    class GitHubConfigurationStore : ExtensionConfigurationStore<GitHubConfiguration>, IGitHubConfigurationStore
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
            return GetProperty(doc => doc.ReleaseNoteOptions.Username);
        }

        public void SetUsername(string username)
        {
            SetProperty(doc => doc.ReleaseNoteOptions.Username = username);
        }

        public SensitiveString GetPassword()
        {
            return GetProperty(doc => doc.ReleaseNoteOptions.Password);
        }

        public void SetPassword(SensitiveString password)
        {
            SetProperty(doc => doc.ReleaseNoteOptions.Password = password);
        }

        public string GetReleaseNotePrefix()
        {
            return GetProperty(doc => doc.ReleaseNoteOptions.ReleaseNotePrefix);
        }

        public void SetReleaseNotePrefix(string releaseNotePrefix)
        {
            SetProperty(doc => doc.ReleaseNoteOptions.ReleaseNotePrefix = releaseNotePrefix);
        }
    }
}
