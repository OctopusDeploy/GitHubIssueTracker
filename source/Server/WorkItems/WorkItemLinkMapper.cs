using System;
using System.Linq;
using System.Text.RegularExpressions;
using Octokit;
using Octopus.Server.Extensibility.Extensions.WorkItems;
using Octopus.Server.Extensibility.HostServices.Model.PackageMetadata;
using Octopus.Server.Extensibility.IssueTracker.GitHub.Configuration;
using Octopus.Server.Extensibility.Resources.IssueTrackers;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.WorkItems
{
    public class WorkItemLinkMapper : IWorkItemLinkMapper
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

        public WorkItemLink[] Map(OctopusPackageMetadata packageMetadata)
        {
            if (packageMetadata.CommentParser != CommentParser)
                return null;

            var baseUrl = store.GetBaseUrl();
            if (string.IsNullOrWhiteSpace(baseUrl))
                return null;

            var isEnabled = store.GetIsEnabled();

            var releaseNotePrefix = store.GetReleaseNotePrefix();
            var workItemReferences = commentParser.ParseWorkItemReferences(packageMetadata);

            return workItemReferences.Select(wir => new WorkItemLink
                {
                    Id = wir.IssueNumber,
                    Description = isEnabled
                        ? GetReleaseNote(packageMetadata.VcsRoot, wir.IssueNumber, releaseNotePrefix)
                        : wir.IssueNumber,
                    LinkUrl = isEnabled
                        ? NormalizeLinkData(baseUrl, packageMetadata.VcsRoot, wir.LinkData)
                        : wir.LinkData
                })
                .Distinct()
                .ToArray();
        }

        public string GetReleaseNote(string vcsRoot, string issueNumber, string releaseNotePrefix)
        {
            var ownerRepoParts = ownerRepoRegex.Match(vcsRoot).Groups[1]?.Value.Split('/');
            // We couldn't figure out owner/repo from vcsRoot so return issue number
            if (ownerRepoParts.Count() < 2)
                return issueNumber;

            var owner = ownerRepoParts[0];
            var repo = ownerRepoParts[1];
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
    }
}