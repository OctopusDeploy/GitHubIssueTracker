using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Octokit;
using Octokit.Internal;
using Octopus.Data;
using Octopus.Diagnostics;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Web.Api;
using Octopus.Server.Extensibility.IssueTracker.GitHub.Configuration;
using Octopus.Server.Extensibility.IssueTracker.GitHub.WorkItems;
using Octopus.Server.MessageContracts.Features.BuildInformation;
using Octopus.Server.MessageContracts.Features.IssueTrackers;
using Commit = Octopus.Server.MessageContracts.Features.BuildInformation.Commit;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.Tests
{
    [TestFixture]
    public class WorkItemLinkMapperScenarios
    {
        [TestCase("https://github.com/UserX/RepoY", "1234", "", "Release note:", "Release note: This is the release note", "UserX", "RepoY", ExpectedResult = "This is the release note")]
        [TestCase("https://github.com/UserX/RepoY", "1234", "", "release note:", "Release note: This is the release note", "UserX", "RepoY", ExpectedResult = "This is the release note")]
        [TestCase("https://github.com/UserX/RepoY", "1234", "", "Release note:", "release note: This is the release note", "UserX", "RepoY", ExpectedResult = "This is the release note")]
        [TestCase("https://github.com/UserX/RepoY", "1234", "", "release note:", "release note: This is the release note", "UserX", "RepoY", ExpectedResult = "This is the release note")]
        [TestCase("https://github.com/UserX/RepoY", "1234", "", "Release note:", "This is not a release note", "UserX", "RepoY", ExpectedResult = "Test title")]
        [TestCase("https://github.com/UserX/RepoY", "1234", "", "", "Release note: This is a release note", "UserX", "RepoY", ExpectedResult = "Test title")]
        [TestCase("https://github.com/UserX", "1234", "", "Release note:", "Release note: This is the release note", "UserX", "RepoY", ExpectedResult = "1234")]
        [TestCase("https://github.com/UserX", "1234", "", "release note:", "Release note: This is the release note", "UserX", "RepoY", ExpectedResult = "1234")]
        [TestCase("https://github.com/UserX/RepoY", "1234", "", "Release note:", "Release notes: This is the release note", "UserX", "RepoY", ExpectedResult = "Test title")]
        [TestCase("https://github.com/UserX/RepoY", "1234", "", "Release note:", "release notes: This is the release note", "UserX", "RepoY", ExpectedResult = "Test title")]
        [TestCase("https://github.com/UserX/Repo-Y", "1234", "", "Release note:", "release notes: This is the release note", "UserX", "Repo-Y", ExpectedResult = "Test title")]
        [TestCase("https://github.com/UserX/RepoY", "1234", "#1234", "Release note:", "Release note: This is the release note", "UserX", "RepoY", ExpectedResult = "This is the release note")]
        [TestCase("https://github.com/UserX/RepoY", "1234", "UserX/RepoY#1234", "release note:", "Release note: This is the release note", "UserX", "RepoY", ExpectedResult = "This is the release note")]
        [TestCase("https://github.com/UserX/RepoY", "1234", "https://github.com/UserX/RepoY#1234", "Release note:", "release note: This is the release note", "UserX", "RepoY", ExpectedResult = "This is the release note")]
        [TestCase("https://github.com/UserX/RepoY", "1234", "https://github.com/UserX/RepoY/issues/1234", "Release note:", "release note: This is the release note", "UserX", "RepoY", ExpectedResult = "This is the release note")]
        [TestCase("https://github.com/UserX/RepoY", "1234", "UserX/RepoZ#1234", "release note:", "release note: This is the release note", "UserX", "RepoZ", ExpectedResult = "This is the release note")]
        [TestCase("https://github.com/UserX/RepoY", "1234", "UserX/Repo-Z#1234", "release note:", "release note: This is the release note", "UserX", "Repo-Z", ExpectedResult = "This is the release note")]
        [TestCase("https://github.com/UserX/RepoY", "1234", "https://github.com/UserX/RepoZ#1234", "Release note:", "release note: This is the release note", "UserX", "RepoZ", ExpectedResult = "This is the release note")]
        [TestCase("https://github.com/UserX/RepoY", "1234", "https://github.com/UserX/RepoZ/issues/1234", "Release note:", "release note: This is the release note", "UserX", "RepoZ", ExpectedResult = "This is the release note")]
        [TestCase("https://github.com/UserX/Repo-Y", "1234", "https://github.com/UserX/Repo-Z/issues/1234", "Release note:", "release note: This is the release note", "UserX", "Repo-Z", ExpectedResult = "This is the release note")]
        public async Task<string> GetWorkItemDescription(string vcsRoot, string issueNumber, string linkData, string releaseNotePrefix, string releaseNoteComment, string expectedOwner, string expectedRepo)
        {
            var store = Substitute.For<IGitHubConfigurationStore>();
            var workItemNumber = Convert.ToInt32(issueNumber);
            var githubClient = Substitute.For<IGitHubClient>();
            var githubClientFactory = Substitute.For<IGitHubClientFactory>();

            githubClientFactory.CreateClient(Arg.Any<IHttpClient>(), CancellationToken.None).Returns(githubClient);
            githubClient.Issue.Get(Arg.Is(expectedOwner), Arg.Is(expectedRepo), Arg.Is(workItemNumber))
                .Returns(new Issue("url", "htmlUrl", "commentUrl", "eventsUrl", workItemNumber, ItemState.Open, "Test title", "test body", null, null, new List<Label>(), null, new List<User>(), null, 1, null, null, DateTimeOffset.Now, null, workItemNumber, "node", false, null, null));
            githubClient.Issue.Comment.GetAllForIssue(Arg.Is(expectedOwner), Arg.Is(expectedRepo), Arg.Is(workItemNumber)).Returns(new []
            {
                new IssueComment(0, null, null, null, releaseNoteComment, DateTimeOffset.Now, null, null, null, AuthorAssociation.None)
            });

            return await new WorkItemLinkMapper(Substitute.For<ISystemLog>(), store, githubClientFactory, Substitute.For<IOctopusHttpClientFactory>(), new CommentParser()).GetReleaseNote(githubClient, vcsRoot, issueNumber, linkData, releaseNotePrefix);
        }

        [TestCase("https://github.com", "https://github.com/UserX/RepoY", "#1234", ExpectedResult = "https://github.com/UserX/RepoY/issues/1234")]
        [TestCase("https://github.com", "https://github.com/UserX/RepoY", "UserX/RepoZ#1234", ExpectedResult = "https://github.com/UserX/RepoZ/issues/1234")]
        [TestCase("https://github.com", "https://github.com/UserX/RepoY", "https://github.com/UserX/RepoY/issues/1234", ExpectedResult = "https://github.com/UserX/RepoY/issues/1234")]
        [TestCase("https://github.com", "", "UserX/RepoZ#1234", ExpectedResult = "https://github.com/UserX/RepoZ/issues/1234")]
        [TestCase("https://github.com", "git@github.com:UserX/RepoY", "#1234", ExpectedResult = "https://github.com/UserX/RepoY/issues/1234")]
        public string NormalizeLinkData(string baseUrl, string vcsRoot, string linkData)
        {
            return WorkItemLinkMapper.NormalizeLinkData(baseUrl, vcsRoot, linkData);
        }

        [Test]
        public async Task DuplicatesGetIgnored()
        {
            var workItemNumber = 1234;

            var store = Substitute.For<IGitHubConfigurationStore>();
            store.GetBaseUrl(CancellationToken.None).Returns("https://github.com");
            store.GetIsEnabled(CancellationToken.None).Returns(true);

            var githubClient = Substitute.For<IGitHubClient>();
            var githubClientFactory = Substitute.For<IGitHubClientFactory>();
            githubClientFactory.CreateClient(Arg.Any<IHttpClient>(), CancellationToken.None).Returns(githubClient);
            githubClient.Issue.Get(Arg.Is("UserX"), Arg.Is("RepoY"), Arg.Is(workItemNumber))
                .Returns(new Issue("url", "htmlUrl", "commentUrl", "eventsUrl", workItemNumber, ItemState.Open, "Test title", "test body", null, null, new List<Label>(), null, new List<User>(), null, 0, null, null, DateTimeOffset.Now, null, workItemNumber, "node", false, null, null));

            var octopusHttpClientFactory = Substitute.For<IOctopusHttpClientFactory>();
            octopusHttpClientFactory.HttpClientHandler.Returns(Substitute.For<HttpClientHandler>());

            var mapper = new WorkItemLinkMapper(Substitute.For<ISystemLog>(), store, githubClientFactory, octopusHttpClientFactory, new CommentParser());

            var workItems = await mapper.Map(new OctopusBuildInformation
            {
                VcsRoot = "https://github.com/UserX/RepoY",
                VcsType = "Git",
                Commits = new[]
                {
                    new Commit { Id = "abcd", Comment = "This is a test commit message. Fixes #1234"},
                    new Commit { Id = "defg", Comment = "This is a test commit message with duplicates. Fixes #1234"}
                }
            }, CancellationToken.None);

            Assert.AreEqual(1, ((ISuccessResult<WorkItemLink[]>)workItems).Value.Length);
        }

        [Test]
        public async Task SourceGetsSet()
        {
            var workItemNumber = 1234;

            var store = Substitute.For<IGitHubConfigurationStore>();
            store.GetBaseUrl(CancellationToken.None).Returns("https://github.com");
            store.GetIsEnabled(CancellationToken.None).Returns(true);

            var githubClient = Substitute.For<IGitHubClient>();
            var githubClientFactory = Substitute.For<IGitHubClientFactory>();
            githubClientFactory.CreateClient(Arg.Any<IHttpClient>(), CancellationToken.None).Returns(githubClient);
            githubClient.Issue.Get(Arg.Is("UserX"), Arg.Is("RepoY"), Arg.Is(workItemNumber))
                .Returns(new Issue("url", "htmlUrl", "commentUrl", "eventsUrl", workItemNumber, ItemState.Open, "Test title", "test body", null, null, new List<Label>(), null, new List<User>(), null, 0, null, null, DateTimeOffset.Now, null, workItemNumber, "node", false, null, null));

            var octopusHttpClientFactory = Substitute.For<IOctopusHttpClientFactory>();
            octopusHttpClientFactory.HttpClientHandler.Returns(Substitute.For<HttpClientHandler>());

            var mapper = new WorkItemLinkMapper(Substitute.For<ISystemLog>(), store, githubClientFactory, octopusHttpClientFactory, new CommentParser());

            var workItems = await mapper.Map(new OctopusBuildInformation
            {
                VcsRoot = "https://github.com/UserX/RepoY",
                VcsType = "Git",
                Commits = new[]
                {
                    new Commit { Id = "abcd", Comment = "This is a test commit message. Fixes #1234"}
                }
            }, CancellationToken.None);

            Assert.AreEqual("GitHub", ((ISuccessResult<WorkItemLink[]>)workItems).Value.Single().Source);
        }

        [Test]
        public async Task AzureDevOpsGitCommentsGetIgnored()
        {
            var store = Substitute.For<IGitHubConfigurationStore>();
            store.GetBaseUrl(CancellationToken.None).Returns("https://github.com");
            store.GetIsEnabled(CancellationToken.None).Returns(true);

            var githubClient = Substitute.For<IGitHubClient>();
            var githubClientFactory = Substitute.For<IGitHubClientFactory>();
            githubClientFactory.CreateClient(Arg.Any<IHttpClient>(), CancellationToken.None).Returns(githubClient);

            var log = Substitute.For<ISystemLog>();

            var mapper = new WorkItemLinkMapper(log, store, githubClientFactory, Substitute.For<IOctopusHttpClientFactory>(), new CommentParser());

            var workItems = await mapper.Map(new OctopusBuildInformation
            {
                VcsRoot = "https://something.com/_git/ProjectX",
                VcsType = "Git",
                Commits = new[]
                {
                    new Commit { Id = "abcd", Comment = "This is a test commit message. Fixes #1234"}
                }
            }, CancellationToken.None);
            var success = workItems as ISuccessResult<WorkItemLink[]>;
            Assert.IsNotNull(success, "AzureDevOps VCS root should not be a failure");
            Assert.IsEmpty(success!.Value, "AzureDevOps VCS root should return an empty list of links");
            log.Received(1).WarnFormat("The VCS Root '{0}' indicates this build information is Azure DevOps related so GitHub comment references will be ignored", "https://something.com/_git/ProjectX");
        }
    }
}