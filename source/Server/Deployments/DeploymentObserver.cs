using System;
using System.Linq;
using Octokit;
using Octopus.Diagnostics;
using Octopus.Server.Extensibility.Domain.Deployments;
using Octopus.Server.Extensibility.Extensions.Domain;
using Octopus.Server.Extensibility.HostServices.Configuration;
using Octopus.Server.Extensibility.HostServices.Domain.Environments;
using Octopus.Server.Extensibility.HostServices.Domain.Projects;
using Octopus.Server.Extensibility.HostServices.Domain.ServerTasks;
using Octopus.Server.Extensibility.HostServices.Model.Projects;
using Octopus.Server.Extensibility.IssueTracker.GitHub.Configuration;
using Octopus.Server.Extensibility.IssueTracker.GitHub.Extensions;
using Octopus.Server.Extensibility.IssueTracker.GitHub.Projects;
using Octopus.Time;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.Deployments
{
    public class DeploymentObserver : IObserveDomainEvent<DeploymentEvent>
    {
        private readonly ILogWithContext log;
        private readonly IGitHubConfigurationStore store;
        private readonly IProvideProjectSettingsValues projectSettingsProvider;
        private readonly IGitHubClient gitHubClient;
        private readonly IClock clock;
        private readonly IServerConfigurationStore serverConfigurationStore;
        private readonly IProjectStore projectStore;
        private readonly IDeploymentEnvironmentStore deploymentEnvironmentStore;
        private readonly IReleaseStore releaseStore;
        private readonly IServerTaskStore serverTaskStore;

        public DeploymentObserver(ILogWithContext log,
            IGitHubConfigurationStore store,
            IProvideProjectSettingsValues projectSettingsProvider,
            IGitHubClient gitHubClient,
            IClock clock,
            IServerConfigurationStore serverConfigurationStore,
            IProjectStore projectStore,
            IDeploymentEnvironmentStore deploymentEnvironmentStore,
            IReleaseStore releaseStore,
            IServerTaskStore serverTaskStore)
        {
            this.log = log;
            this.store = store;
            this.projectSettingsProvider = projectSettingsProvider;
            this.gitHubClient = gitHubClient;
            this.clock = clock;
            this.serverConfigurationStore = serverConfigurationStore;
            this.projectStore = projectStore;
            this.deploymentEnvironmentStore = deploymentEnvironmentStore;
            this.releaseStore = releaseStore;
            this.serverTaskStore = serverTaskStore;
        }
        
        public void Handle(DeploymentEvent deploymentEvent)
        {
            var projSettings =
                projectSettingsProvider
                    .GetSettings<ProjectSettingsMetadataProvider.GitHubProjectSettings>(
                        GitHubConfigurationStore.SingletonId, deploymentEvent.Deployment.ProjectId) ?? new ProjectSettingsMetadataProvider.GitHubProjectSettings();

            if (!store.GetIsEnabled() || !projSettings.PushUpdates)
                return;

            using (log.OpenBlock($"Sending GitHub status update - {StateFromEventType(deploymentEvent.EventType)}"))
            {
                if (string.IsNullOrEmpty(store.GetUsername()) && string.IsNullOrEmpty(store.GetPassword()))
                {
                    log.Warn($"GitHub integration is enabled but settings are incomplete, ignoring deployment events");
                    log.Finish();
                    return;
                }
                PublishToGitHub(deploymentEvent.EventType, deploymentEvent.Deployment);
                log.Finish();
            }
        }

        private void PublishToGitHub(DeploymentEventType eventType, IDeployment deployment)
        {
            var serverUri = serverConfigurationStore.GetServerUri();
            if (string.IsNullOrEmpty(serverUri))
            {
                log.Warn($"To use GitHub Status Updates integration you must have the Octopus server's external url configured (see the Configuration/Nodes page)");
                return;
            }

            var project = projectStore.Get(deployment.ProjectId);
            var release = releaseStore.Get(deployment.ReleaseId);
            var serverTask = serverTaskStore.Get(deployment.TaskId);

            var commitStatus = new NewCommitStatus
            {
                Context = "continuous-deployment/octopusdeploy",
                Description = serverTask.Description,
                State = Enum.TryParse(StateFromEventType(eventType), true, out CommitState commitState) ? commitState : CommitState.Error,
                TargetUrl = $"{serverUri}/app#/{project.SpaceId}/projects/{project.Slug}/releases/{release.Version}/deployments/{deployment.Id}"
            };

            var commits = deployment.Changes.SelectMany(c => c.VersionMetadata.Select(vm =>
            {
                var (success, owner, repo) = vm.VcsRoot.ParseGitHubOwnerAndRepo();
                if (!success) return (null, null, null);

                return (owner, repo, vm.VcsCommitNumber);
            })).Where(x => x.owner != null);

            foreach(var (owner, repo, reference) in commits)
                gitHubClient.Repository.Status.Create(owner, repo, reference, commitStatus);
        }

        string StateFromEventType(DeploymentEventType eventType)
        {
            switch (eventType)
            {
                case DeploymentEventType.DeploymentStarted:
                case DeploymentEventType.DeploymentResumed:
                    return "pending";
                case DeploymentEventType.DeploymentFailed:
                    return "failure";
                case DeploymentEventType.DeploymentSucceeded:
                    return "success";
                default:
                    return "error";
            }
        }

    }
}