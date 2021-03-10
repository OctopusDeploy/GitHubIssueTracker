using System;
using System.Collections.Generic;
using Octopus.Diagnostics;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.Configuration
{
    class GitHubConfigureCommands : IContributeToConfigureCommand
    {
        readonly ISystemLog systemLog;
        readonly Lazy<IGitHubConfigurationStore> gitHubConfiguration;

        public GitHubConfigureCommands(
            ISystemLog systemLog,
            Lazy<IGitHubConfigurationStore> gitHubConfiguration)
        {
            this.systemLog = systemLog;
            this.gitHubConfiguration = gitHubConfiguration;
        }

        public IEnumerable<ConfigureCommandOption> GetOptions()
        {
            yield return new ConfigureCommandOption("GitHubIsEnabled=", "Set whether GitHub issue tracker integration is enabled.", v =>
            {
                var isEnabled = bool.Parse(v);
                gitHubConfiguration.Value.SetIsEnabled(isEnabled);
                systemLog.Info($"GitHub Issue Tracker integration IsEnabled set to: {isEnabled}");
            });
            yield return new ConfigureCommandOption("GitHubBaseUrl=", GitHubConfigurationResource.GitHubBaseUrlDescription, v =>
            {
                gitHubConfiguration.Value.SetBaseUrl(v);
                systemLog.Info($"GitHub Issue Tracker integration base Url set to: {v}");
            });
        }
    }
}