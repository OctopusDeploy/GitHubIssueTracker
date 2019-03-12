using NUnit.Framework;
using Octopus.Server.Extensibility.IssueTracker.GitHub.WorkItems;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.Tests
{
    [TestFixture]
    public class WorkItemLinkMapperScenarios
    {
        [TestCase("https://github.com", "https://github.com/UserX/RepoY", "1234", ExpectedResult = "https://github.com/UserX/RepoY/issues/1234")]
        [TestCase("https://github.com", "https://github.com/UserX/RepoY", "UserX/RepoZ#1234", ExpectedResult = "https://github.com/UserX/RepoZ/issues/1234")]
        [TestCase("https://github.com", "https://github.com/UserX/RepoY", "https://github.com/UserX/RepoY/issues/1234", ExpectedResult = "https://github.com/UserX/RepoY/issues/1234")]
        [TestCase("https://github.com", "", "UserX/RepoZ#1234", ExpectedResult = "https://github.com/UserX/RepoZ/issues/1234")]
        public string NormalizeLinkData(string baseUrl, string vcsRoot, string linkData)
        {
            return WorkItemLinkMapper.NormalizeLinkData(baseUrl, vcsRoot, linkData);
        }
    }
}