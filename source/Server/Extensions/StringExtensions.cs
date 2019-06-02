using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.Extensions
{
    public static class StringExtensions
    {
        private static readonly Regex ownerRepoRegex = new Regex("(?:https?://)?(?:[^?/\\s]+[?/])(.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static (bool success, string owner, string repo) ParseGitHubOwnerAndRepo(this string gitHubUrl, string linkData = null)
        {
            (bool, string, string) GetOwnerRepoFromVcsRoot(string vcsRoot)
            {
                var ownerRepoParts = ownerRepoRegex.Match(vcsRoot).Groups[1]?.Value.Split('/', '#');
                return ownerRepoParts.Count() < 2
                    ? (false, null, null)
                    : (true, ownerRepoParts[0], ownerRepoParts[1]);
            }

            if (string.IsNullOrWhiteSpace(linkData))
            {
                return GetOwnerRepoFromVcsRoot(gitHubUrl);
            }
            else if (linkData.StartsWith("http"))
            {
                return GetOwnerRepoFromVcsRoot(linkData);
            }

            var linkDataComponents = linkData.Split('#');
            if (string.IsNullOrWhiteSpace(linkDataComponents[0]) || linkDataComponents[0].Split('/').Length != 2)
            {
                return GetOwnerRepoFromVcsRoot(gitHubUrl);
            }

            var ownerRepoComponents = linkDataComponents[0].Split('/');
            if (string.IsNullOrWhiteSpace(ownerRepoComponents[0]) || string.IsNullOrWhiteSpace(ownerRepoComponents[1]))
                return (false, null, null);

            return (true, ownerRepoComponents[0], ownerRepoComponents[1]);
        }
    }
}
