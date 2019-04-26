using System;
using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using Octokit;
using Octopus.Server.Extensibility.HostServices.Model.PackageMetadata;
using Octopus.Server.Extensibility.IssueTracker.GitHub.Configuration;
using Octopus.Server.Extensibility.IssueTracker.GitHub.WorkItems;
using Commit = Octopus.Server.Extensibility.HostServices.Model.IssueTrackers.Commit;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.Tests
{
    [TestFixture]
    public class WorkItemLinkMapperScenarios
    {
        [TestCase("https://github.com/UserX/RepoY", "1234", "Release note:", "Release note: This is the release note", ExpectedResult = "This is the release note")]
        [TestCase("https://github.com/UserX/RepoY", "1234", "release note:", "Release note: This is the release note", ExpectedResult = "This is the release note")]
        [TestCase("https://github.com/UserX/RepoY", "1234", "Release note:", "release note: This is the release note", ExpectedResult = "This is the release note")]
        [TestCase("https://github.com/UserX/RepoY", "1234", "release note:", "release note: This is the release note", ExpectedResult = "This is the release note")]
        [TestCase("https://github.com/UserX/RepoY/Issues", "1234", "Release note:", "Release note: This is the release note", ExpectedResult = "This is the release note")]
        [TestCase("https://github.com/UserX/RepoY/Issues", "1234", "release note:", "Release note: This is the release note", ExpectedResult = "This is the release note")]
        [TestCase("https://github.com/UserX/RepoY/Issues", "1234", "Release note:", "release note: This is the release note", ExpectedResult = "This is the release note")]
        [TestCase("https://github.com/UserX/RepoY/Issues", "1234", "release note:", "release note: This is the release note", ExpectedResult = "This is the release note")]
        [TestCase("https://github.com/UserX/RepoY", "1234", "Release note:", "This is not a release note", ExpectedResult = "Test title")]
        [TestCase("https://github.com/UserX/RepoY", "1234", "", "Release note: This is a release note", ExpectedResult = "Test title")]
        [TestCase("https://github.com/UserX", "1234", "Release note:", "Release note: This is the release note", ExpectedResult = "1234")]
        [TestCase("https://github.com/UserX", "1234", "release note:", "Release note: This is the release note", ExpectedResult = "1234")]
        [TestCase("https://github.com/UserX/RepoY", "1234", "Release note:", "Release notes: This is the release note", ExpectedResult = "Test title")]
        [TestCase("https://github.com/UserX/RepoY", "1234", "Release note:", "release notes: This is the release note", ExpectedResult = "Test title")]
        public string GetWorkItemDescription(string vcsRoot, string linkData, string releaseNotePrefix, string releaseNote)
        {
            var store = Substitute.For<IGitHubConfigurationStore>();
            var githubClient = Substitute.For<IGitHubClient>();
            var githubClientLazy = new Lazy<IGitHubClient>(() => githubClient);
            var workItemNumber = Convert.ToInt32(linkData);
            
            githubClient.Issue.Get(Arg.Is("UserX"), Arg.Is("RepoY"), Arg.Is(workItemNumber))
                .Returns(new Issue("url", "htmlUrl", "commentUrl", "eventsUrl", workItemNumber, ItemState.Open, "Test title", "test body", null, null, new List<Octokit.Label>(), null, new List<Octokit.User>(), null, 1, null, null, DateTimeOffset.Now, null, workItemNumber, "node", false, null, null));
            githubClient.Issue.Comment.GetAllForIssue(Arg.Is("UserX"), Arg.Is("RepoY"), Arg.Is(workItemNumber)).Returns(new []
            {
                new IssueComment(0, null, null, null, releaseNote, DateTimeOffset.Now, null, null, null)
            });

            return new WorkItemLinkMapper(store, new CommentParser(), githubClientLazy).GetReleaseNote(vcsRoot, linkData, releaseNotePrefix);
        }

        [TestCase("https://github.com", "https://github.com/UserX/RepoY", "#1234", ExpectedResult = "https://github.com/UserX/RepoY/issues/1234")]
        [TestCase("https://github.com", "https://github.com/UserX/RepoY", "UserX/RepoZ#1234", ExpectedResult = "https://github.com/UserX/RepoZ/issues/1234")]
        [TestCase("https://github.com", "https://github.com/UserX/RepoY", "https://github.com/UserX/RepoY/issues/1234", ExpectedResult = "https://github.com/UserX/RepoY/issues/1234")]
        [TestCase("https://github.com", "", "UserX/RepoZ#1234", ExpectedResult = "https://github.com/UserX/RepoZ/issues/1234")]
        public string NormalizeLinkData(string baseUrl, string vcsRoot, string linkData)
        {
            return WorkItemLinkMapper.NormalizeLinkData(baseUrl, vcsRoot, linkData);
        }

        [Test]
        public void DuplicatesGetIgnored()
        {
            var store = Substitute.For<IGitHubConfigurationStore>();
            var githubClient = Substitute.For<IGitHubClient>();
            var githubClientLazy = new Lazy<IGitHubClient>(() => githubClient);
            store.GetBaseUrl().Returns("https://github.com");
            store.GetIsEnabled().Returns(true);

            var workItemNumber = 1234;
            
            githubClient.Issue.Get(Arg.Is("UserX"), Arg.Is("RepoY"), Arg.Is(workItemNumber))
                .Returns(new Issue("url", "htmlUrl", "commentUrl", "eventsUrl", workItemNumber, ItemState.Open, "Test title", "test body", null, null, new List<Octokit.Label>(), null, new List<Octokit.User>(), null, 0, null, null, DateTimeOffset.Now, null, workItemNumber, "node", false, null, null));

            var mapper = new WorkItemLinkMapper(store, new CommentParser(), githubClientLazy);

            var workItems = mapper.Map(new OctopusPackageMetadata
            {
                VcsRoot = "https://github.com/UserX/RepoY",
                VcsType = "Git",
                CommentParser = "GitHub",
                Commits = new Commit[]
                {
                    new Commit { Id = "abcd", Comment = "This is a test commit message. Fixes #1234"},
                    new Commit { Id = "defg", Comment = "This is a test commit message with duplicates. Fixes #1234"}
                }
            });

            Assert.AreEqual(1, workItems.Length);
        }
    }
}