using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Octopus.Server.Extensibility.Extensions.Model.Projects;
using Octopus.Server.Extensibility.IssueTracker.GitHub.Configuration;
using Octopus.Server.Extensibility.Metadata;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.Projects
{
    public class ProjectSettingsMetadataProvider : IContributeProjectSettingsMetadata
    {
        private readonly IGitHubConfigurationStore store;

        public ProjectSettingsMetadataProvider(IGitHubConfigurationStore store)
        {
            this.store = store;
        }
        
        public string ExtensionId => GitHubConfigurationStore.SingletonId;
        public string ExtensionName => GitHubIssueTracker.Name;

        public List<PropertyMetadata> Properties => store.GetIsEnabled()
            ? new MetadataGenerator().GetMetadata<GitHubProjectSettings>().Types.First().Properties
            : null;

        internal class GitHubProjectSettings
        {
            [DisplayName("Push GitHub Status Updates")]
            [Description("")]
            [ReadOnly(false)]
            public bool PushUpdates { get; set; }
        }
    }
}