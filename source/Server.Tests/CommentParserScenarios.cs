using System.Linq;
using NUnit.Framework;
using Octopus.Server.Extensibility.Extensions.Model.BuildInformation;
using Octopus.Server.Extensibility.IssueTracker.GitHub.WorkItems;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.Tests
{
    [TestFixture]
    public class CommentParserScenarios
    {
        [Test]
        public void StandardIssueNumberReferenceGetsParsedCorrectly()
        {
            var workItemReferences = new CommentParser().ParseWorkItemReferences(Create("Fixes #1234"));
            Assert.IsNotEmpty(workItemReferences);

            var reference = workItemReferences.First();
            Assert.AreEqual("1234", reference.IssueNumber);
            Assert.AreEqual("#1234", reference.LinkData);
        }

        [Test]
        public void IssueReferenceToAnotherRepoGetsParsedCorrectly()
        {
            var workItemReferences = new CommentParser().ParseWorkItemReferences(Create("Fixes OrgA/RepoB#1234"));
            Assert.IsNotEmpty(workItemReferences);

            var reference = workItemReferences.First();
            Assert.AreEqual("1234", reference.IssueNumber);
            Assert.AreEqual("OrgA/RepoB#1234", reference.LinkData);
        }

        [Test]
        public void IssueReferenceToAnotherRepoWithDashGetsParsedCorrectly()
        {
            var workItemReferences = new CommentParser().ParseWorkItemReferences(Create("Fixes OrgA/Repo-B#1234"));
            Assert.IsNotEmpty(workItemReferences);

            var reference = workItemReferences.First();
            Assert.AreEqual("1234", reference.IssueNumber);
            Assert.AreEqual("OrgA/Repo-B#1234", reference.LinkData);
        }

        [Test]
        public void GHReferenceGetsParsedCorrectly()
        {
            var workItemReferences = new CommentParser().ParseWorkItemReferences(Create("Fixes GH-1234"));
            Assert.IsNotEmpty(workItemReferences);

            var reference = workItemReferences.First();
            Assert.AreEqual("1234", reference.IssueNumber);
            Assert.AreEqual("GH-1234", reference.LinkData);
        }

        [Test]
        public void AbsoluteUrlReferenceGetsParsedCorrectly()
        {
            var workItemReferences = new CommentParser().ParseWorkItemReferences(Create("Fixes https://github.com/OrgA/RepoB/issues/1234"));
            Assert.IsNotEmpty(workItemReferences);

            var reference = workItemReferences.First();
            Assert.AreEqual("1234", reference.IssueNumber);
            Assert.AreEqual("https://github.com/OrgA/RepoB/issues/1234", reference.LinkData);
        }

        [TestCase("Fix")]
        [TestCase("Fixes")]
        [TestCase("Fixed")]
        [TestCase("Resolve")]
        [TestCase("Resolves")]
        [TestCase("Resolved")]
        [TestCase("Close")]
        [TestCase("Closes")]
        [TestCase("Closed")]
        public void TheKeywordsAllWork(string keyword)
        {
            var workItemReferences = new CommentParser().ParseWorkItemReferences(Create($"{keyword} #1234"));
            Assert.IsNotEmpty(workItemReferences);

            var reference = workItemReferences.First();
            Assert.AreEqual("1234", reference.IssueNumber);
            Assert.AreEqual("#1234", reference.LinkData);
        }

        private OctopusBuildInformation Create(string comment)
        {
            return new OctopusBuildInformation
            {
                Commits = new[] { new Commit{ Id = "a", Comment = comment }}
            };
        }
    }
}