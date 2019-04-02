using System.Collections.Generic;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;
using Octopus.Server.Extensibility.HostServices.Licensing;
using Octopus.Server.Extensibility.HostServices.Mapping;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.Configuration
{
    public class GitHubConfigurationSettings : ExtensionConfigurationSettings<GitHubConfiguration, GitHubConfigurationResource, IGitHubConfigurationStore>, IGitHubConfigurationSettings
    {
        private readonly IInstallationIdProvider installationIdProvider;

        public GitHubConfigurationSettings(IGitHubConfigurationStore configurationDocumentStore, IInstallationIdProvider installationIdProvider) : base(configurationDocumentStore)
        {
            this.installationIdProvider = installationIdProvider;
        }

        public override string Id => GitHubConfigurationStore.SingletonId;

        public override string ConfigurationSetName => "GitHub Issue Tracker";

        public override string Description => "GitHub Issue Tracker settings";

        public override IEnumerable<IConfigurationValue> GetConfigurationValues()
        {
            var isEnabled = ConfigurationDocumentStore.GetIsEnabled();

            yield return new ConfigurationValue<bool>("Octopus.IssueTracker.GitHubIssueTracker", isEnabled, isEnabled, "Is Enabled");
            yield return new ConfigurationValue<string>("Octopus.IssueTracker.GitHubBaseUrl", ConfigurationDocumentStore.GetBaseUrl(), isEnabled && !string.IsNullOrWhiteSpace(ConfigurationDocumentStore.GetBaseUrl()), "GitHub Base Url");
        }

        public override void BuildMappings(IResourceMappingsBuilder builder)
        {
            builder.Map<GitHubConfigurationResource, GitHubConfiguration>();
        }
    }
}