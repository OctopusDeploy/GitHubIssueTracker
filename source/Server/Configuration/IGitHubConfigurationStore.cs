using System.Threading;
using System.Threading.Tasks;
using Octopus.Data.Model;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.Configuration
{
    interface IGitHubConfigurationStore : IExtensionConfigurationStoreAsync<GitHubConfiguration>
    {
        Task<string?> GetBaseUrl(CancellationToken cancellationToken);
        Task SetBaseUrl(string? baseUrl, CancellationToken cancellationToken);

        Task<string?> GetUsername(CancellationToken cancellationToken);
        Task SetUsername(string? username, CancellationToken cancellationToken);

        Task<SensitiveString?> GetPassword(CancellationToken cancellationToken);
        Task SetPassword(SensitiveString? password, CancellationToken cancellationToken);

        Task<string?> GetReleaseNotePrefix(CancellationToken cancellationToken);
        Task SetReleaseNotePrefix(string? releaseNotePrefix, CancellationToken cancellationToken);
    }
}
