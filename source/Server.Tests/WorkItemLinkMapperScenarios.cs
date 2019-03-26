using NSubstitute;
using NUnit.Framework;
using Octopus.Server.Extensibility.HostServices.Model.IssueTrackers;
using Octopus.Server.Extensibility.HostServices.Model.PackageMetadata;
using Octopus.Server.Extensibility.IssueTracker.GitHub.Configuration;
using Octopus.Server.Extensibility.IssueTracker.GitHub.WorkItems;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.Tests
{
    [TestFixture]
    public class WorkItemLinkMapperScenarios
    {
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
            store.GetBaseUrl().Returns("https://github.com");
            store.GetIsEnabled().Returns(true);
            
            var mapper = new WorkItemLinkMapper(store, new CommentParser());

            var workItems = mapper.Map(new OctopusPackageMetadata
            {
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