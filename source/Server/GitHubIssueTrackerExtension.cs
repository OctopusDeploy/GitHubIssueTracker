using System;
using Autofac;
using Octopus.Server.Extensibility.Extensions;
using Octopus.Server.Extensibility.Extensions.Infrastructure;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Configuration;
using Octopus.Server.Extensibility.Extensions.Mappings;
using Octopus.Server.Extensibility.Extensions.WorkItems;
using Octopus.Server.Extensibility.IssueTracker.GitHub.Configuration;
using Octopus.Server.Extensibility.IssueTracker.GitHub.WorkItems;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub
{
    [OctopusPlugin("GitHub Issue Tracker", "Octopus Deploy")]
    public class GitHubIssueTrackerExtension : IOctopusExtension
    {
        public void Load(ContainerBuilder builder)
        {
            builder.RegisterType<GitHubConfigurationMapping>()
                .As<IConfigurationDocumentMapper>()
                .InstancePerLifetimeScope();

            builder.RegisterType<DatabaseInitializer>().As<IExecuteWhenDatabaseInitializes>().InstancePerLifetimeScope();

            builder.RegisterType<GitHubConfigurationStore>()
                .As<IGitHubConfigurationStore>()
                .InstancePerLifetimeScope();

            builder.RegisterType<GitHubConfigurationSettings>()
                .As<IGitHubConfigurationSettings>()
                .As<IHasConfigurationSettings>()
                .As<IHasConfigurationSettingsResource>()
                .As<IContributeMappings>()
                .InstancePerLifetimeScope();

            builder.RegisterType<GitHubIssueTracker>()
                .As<IIssueTracker>()
                .InstancePerLifetimeScope();

            builder.RegisterType<GitHubConfigureCommands>()
                .As<IContributeToConfigureCommand>()
                .InstancePerDependency();

            builder.RegisterType<CommentParser>().AsSelf().InstancePerDependency();

            builder.RegisterType<WorkItemLinkMapper>().As<IWorkItemLinkMapper>().InstancePerDependency();
        }
    }
}
