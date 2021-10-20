using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Octopus.Data.Model;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;
using Octopus.Server.Extensibility.HostServices.Mapping;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.Configuration
{
    class GitHubConfigurationSettings : ExtensionConfigurationSettingsAsync<GitHubConfiguration, GitHubConfigurationResource, IGitHubConfigurationStore>, IGitHubConfigurationSettings
    {
        public GitHubConfigurationSettings(IGitHubConfigurationStore configurationDocumentStore) : base(configurationDocumentStore)
        {
        }

        public override string Id => GitHubConfigurationStore.SingletonId;

        public override string ConfigurationSetName => "GitHub Issue Tracker";

        public override string Description => "GitHub Issue Tracker settings";

        public override async IAsyncEnumerable<IConfigurationValue> GetConfigurationValues([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var isEnabled = await ConfigurationDocumentStore.GetIsEnabled(cancellationToken);

            yield return new ConfigurationValue<bool>("Octopus.IssueTracker.GitHubIssueTracker", isEnabled, isEnabled, "Is Enabled");
            var baseUrl = await ConfigurationDocumentStore.GetBaseUrl(cancellationToken);
            yield return new ConfigurationValue<string?>("Octopus.IssueTracker.GitHubBaseUrl", baseUrl, isEnabled && !string.IsNullOrWhiteSpace(baseUrl), "GitHub Base Url");
            var username = await ConfigurationDocumentStore.GetUsername(cancellationToken);
            yield return new ConfigurationValue<string?>("Octopus.IssueTracker.GitHubUsername", username, isEnabled && !string.IsNullOrWhiteSpace(username), "GitHub Username");
            var password = await ConfigurationDocumentStore.GetPassword(cancellationToken);
            yield return new ConfigurationValue<SensitiveString?>("Octopus.IssueTracker.GitHubPassword", password, isEnabled && !string.IsNullOrWhiteSpace(password?.Value), "GitHub Password");
            var releaseNotePrefix = await ConfigurationDocumentStore.GetReleaseNotePrefix(cancellationToken);
            yield return new ConfigurationValue<string?>("Octopus.IssueTracker.GitHubReleaseNotePrefix", releaseNotePrefix, isEnabled && !string.IsNullOrWhiteSpace(releaseNotePrefix), "GitHub Release Note Prefix");
        }

        public override void BuildMappings(IResourceMappingsBuilder builder)
        {
            builder.Map<GitHubConfigurationResource, GitHubConfiguration>();
            builder.Map<ReleaseNoteOptionsResource, ReleaseNoteOptions>();
        }
    }
}