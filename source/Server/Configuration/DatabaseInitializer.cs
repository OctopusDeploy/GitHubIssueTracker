using Octopus.Data.Storage.Configuration;
using Octopus.Diagnostics;
using Octopus.Server.Extensibility.Extensions.Infrastructure;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.Configuration
{
    class DatabaseInitializer : ExecuteWhenDatabaseInitializes
    {
        readonly ISystemLog log;
        readonly IConfigurationStore configurationStore;

        public DatabaseInitializer(ISystemLog log, IConfigurationStore configurationStore)
        {
            this.log = log;
            this.configurationStore = configurationStore;
        }

        public override void Execute()
        {
            var doc = configurationStore.Get<GitHubConfiguration>(GitHubConfigurationStore.SingletonId);
            if (doc != null)
                return;

            log.Info("Initializing GitHub integration settings");
            doc = new GitHubConfiguration();
            configurationStore.Create(doc);
        }
    }
}