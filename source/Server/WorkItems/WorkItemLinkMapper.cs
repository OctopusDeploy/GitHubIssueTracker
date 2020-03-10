using System;
using System.Linq;
using System.Text.RegularExpressions;
using Octokit;
using Octopus.Server.Extensibility.Extensions;
using Octopus.Server.Extensibility.Extensions.WorkItems;
using Octopus.Server.Extensibility.HostServices.Model.BuildInformation;
using Octopus.Server.Extensibility.IssueTracker.GitHub.Configuration;
using Octopus.Server.Extensibility.Resources.IssueTrackers;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.WorkItems
{
    class WorkItemLinkMapper : IWorkItemLinkMapper
    {
        private readonly IGitHubConfigurationStore store;
        private readonly CommentParser commentParser;
        private readonly Lazy<IGitHubClient> githubClient;
        private readonly Regex ownerRepoRegex = new Regex("(?:https?://)?(?:[^?/\\s]+[?/])(.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);


        public WorkItemLinkMapper(IGitHubConfigurationStore store,
            CommentParser commentParser,
            Lazy<IGitHubClient> githubClient)
        {
            this.store = store;
            this.commentParser = commentParser;
            this.githubClient = githubClient;
        }

        public string CommentParser => GitHubConfigurationStore.CommentParser;
        public bool IsEnabled => store.GetIsEnabled();

        public SuccessOrErrorResult<WorkItemLink[]> Map(OctopusBuildInformation buildInformation)
        {
            if (!IsEnabled)
                return null;

            var baseUrl = store.GetBaseUrl();
            if (string.IsNullOrWhiteSpace(baseUrl))
                return null;

            var releaseNotePrefix = store.GetReleaseNotePrefix();
            var workItemReferences = commentParser.ParseWorkItemReferences(buildInformation);

            return workItemReferences.Select(wir => new WorkItemLink
                {
                    Id = wir.IssueNumber,
                    Description = GetReleaseNote(buildInformation.VcsRoot, wir.IssueNumber, wir.LinkData, releaseNotePrefix),
                    LinkUrl = NormalizeLinkData(baseUrl, buildInformation.VcsRoot, wir.LinkData),
                    Source = GitHubConfigurationStore.CommentParser
                })
                .Distinct()
                .ToArray();
        }

        public string GetReleaseNote(string vcsRoot, string issueNumber, string linkData, string releaseNotePrefix)
        {
            var (success, owner, repo) = GetGitHubOwnerAndRepo(vcsRoot, linkData);
            if (!success)
                return issueNumber;
            
            try
            {
                var issue = githubClient.Value.Issue.Get(owner, repo, int.Parse(issueNumber)).Result;
                // No comments on issue, or no release note prefix has been specified, so return issue title
                if (issue.Comments == 0 || string.IsNullOrWhiteSpace(releaseNotePrefix))
                    return issue.Title;

                var releaseNoteRegex = new Regex($"^{releaseNotePrefix}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                var issueComments = githubClient.Value.Issue.Comment
                    .GetAllForIssue(owner, repo, int.Parse(issueNumber)).Result;

                var releaseNote = issueComments?.LastOrDefault(c => releaseNoteRegex.IsMatch(c.Body))?.Body;
                // Return (last, if multiple found) comment that matched release note prefix, or return issue title
                return !string.IsNullOrWhiteSpace(releaseNote)
                    ? releaseNoteRegex.Replace(releaseNote, "")?.Trim()
                    : issue.Title;
            }
            catch (Exception e)
            {
                return issueNumber;
            }
        }

        public static string NormalizeLinkData(string baseUrl, string vcsRoot, string linkData)
        {
            if (string.IsNullOrWhiteSpace(linkData))
                return string.Empty;

            if (linkData.StartsWith("http"))
                return linkData;

            var baseToUse = !string.IsNullOrWhiteSpace(vcsRoot) ? vcsRoot : baseUrl;

            var linkDataComponents = linkData.Split('#').ToList();
            if (!string.IsNullOrEmpty(linkDataComponents[0]))
            {
                // we have org/repo or user/repo formatted, insert "issues" between that and the issueId
                linkDataComponents.Insert(1, "issues");

                // switch to the baseUrl, rather than calc relative to the vscRoot
                baseToUse = baseUrl;
            }
            else
            {
                // we have only issueId, insert "issues" before it
                linkDataComponents[0] = "issues";
            }

            return baseToUse + "/" + string.Join("/", linkDataComponents);
        }
        
        (bool success, string owner, string repo) GetGitHubOwnerAndRepo(string gitHubUrl, string linkData)
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