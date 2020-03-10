using System;
using System.Collections.Generic;
using Octopus.Diagnostics;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.Configuration
{
    class GitHubConfigureCommands : IContributeToConfigureCommand
    {
        readonly ILog log;
        readonly Lazy<IGitHubConfigurationStore> GitHubConfiguration;

        public GitHubConfigureCommands(
            ILog log,
            Lazy<IGitHubConfigurationStore> gitHubConfiguration)
        {
            this.log = log;
            this.GitHubConfiguration = gitHubConfiguration;
        }

        public IEnumerable<ConfigureCommandOption> GetOptions()
        {
            yield return new ConfigureCommandOption("GitHubIsEnabled=", "Set whether GitHub issue tracker integration is enabled.", v =>
            {
                var isEnabled = bool.Parse(v);
                GitHubConfiguration.Value.SetIsEnabled(isEnabled);
                log.Info($"GitHub Issue Tracker integration IsEnabled set to: {isEnabled}");
            });
            yield return new ConfigureCommandOption("GitHubBaseUrl=", GitHubConfigurationResource.GitHubBaseUrlDescription, v =>
            {
                GitHubConfiguration.Value.SetBaseUrl(v);
                log.Info($"GitHub Issue Tracker integration base Url set to: {v}");
            });
        }
    }
}