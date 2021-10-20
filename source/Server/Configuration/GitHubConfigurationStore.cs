using System.Threading;
using System.Threading.Tasks;
using Octopus.Data.Model;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.Configuration
{
    class GitHubConfigurationStore : ExtensionConfigurationStoreAsync<GitHubConfiguration>, IGitHubConfigurationStore
    {
        public static string CommentParser = "GitHub";
        public static string SingletonId = "issuetracker-github";

        public GitHubConfigurationStore(IConfigurationStoreAsync configurationStore) : base(configurationStore)
        {
        }

        public override string Id => SingletonId;

        public async Task<string?> GetBaseUrl(CancellationToken cancellationToken)
        {
            return await GetProperty(doc => doc.BaseUrl?.Trim('/'), cancellationToken);
        }

        public async Task SetBaseUrl(string? baseUrl, CancellationToken cancellationToken)
        {
            await SetProperty(doc => doc.BaseUrl = baseUrl?.Trim('/'), cancellationToken);
        }

        public async Task<string?> GetUsername(CancellationToken cancellationToken)
        {
            return await GetProperty(doc => doc.ReleaseNoteOptions.Username, cancellationToken);
        }

        public async Task SetUsername(string? username, CancellationToken cancellationToken)
        {
            await SetProperty(doc => doc.ReleaseNoteOptions.Username = username, cancellationToken);
        }

        public async Task<SensitiveString?> GetPassword(CancellationToken cancellationToken)
        {
            return await GetProperty(doc => doc.ReleaseNoteOptions.Password, cancellationToken);
        }

        public async Task SetPassword(SensitiveString? password, CancellationToken cancellationToken)
        {
            await SetProperty(doc => doc.ReleaseNoteOptions.Password = password, cancellationToken);
        }

        public async Task<string?> GetReleaseNotePrefix(CancellationToken cancellationToken)
        {
            return await GetProperty(doc => doc.ReleaseNoteOptions.ReleaseNotePrefix, cancellationToken);
        }

        public async Task SetReleaseNotePrefix(string? releaseNotePrefix, CancellationToken cancellationToken)
        {
            await SetProperty(doc => doc.ReleaseNoteOptions.ReleaseNotePrefix = releaseNotePrefix, cancellationToken);
        }
    }
}
