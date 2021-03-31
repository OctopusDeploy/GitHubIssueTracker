using System;
using System.Linq;
using System.Text.RegularExpressions;
using Octokit;
using Octopus.Data;
using Octopus.Diagnostics;
using Octopus.Server.Extensibility.Extensions.WorkItems;
using Octopus.Server.Extensibility.IssueTracker.GitHub.Configuration;
using Octopus.Server.Extensibility.Results;
using Octopus.Server.MessageContracts.Features.BuildInformation;
using Octopus.Server.MessageContracts.Features.IssueTrackers;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.WorkItems
{
    class WorkItemLinkMapper : IWorkItemLinkMapper
    {
        private readonly ISystemLog systemLog;
        private readonly IGitHubConfigurationStore store;
        private readonly CommentParser commentParser;
        private readonly Lazy<IGitHubClient> githubClient;
        private readonly Regex ownerRepoRegex = new Regex("(?:https?://)?(?:[^?/\\s]+[?/])(.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public WorkItemLinkMapper(ISystemLog systemLog,
            IGitHubConfigurationStore store,
            CommentParser commentParser,
            Lazy<IGitHubClient> githubClient)
        {
            this.systemLog = systemLog;
            this.store = store;
            this.commentParser = commentParser;
            this.githubClient = githubClient;
        }

        public string CommentParser => GitHubConfigurationStore.CommentParser;
        public bool IsEnabled => store.GetIsEnabled();

        public IResultFromExtension<WorkItemLink[]> Map(OctopusBuildInformation buildInformation)
        {
            if (!IsEnabled)
                return ResultFromExtension<WorkItemLink[]>.ExtensionDisabled();

            var baseUrl = store.GetBaseUrl();
            if (string.IsNullOrWhiteSpace(baseUrl))
                return ResultFromExtension<WorkItemLink[]>.Failed("Base Url is not configured");
            if (buildInformation.VcsRoot == null)
                return ResultFromExtension<WorkItemLink[]>.Failed("No VCS root configured");

            const string pathComponentIndicatingAzureDevOpsVcs = @"/_git/";
            if (buildInformation.VcsRoot.Contains(pathComponentIndicatingAzureDevOpsVcs))
            {
                systemLog.WarnFormat("The VCS Root '{0}' indicates this build information is Azure DevOps related so GitHub comment references will be ignored", buildInformation.VcsRoot);
                return ResultFromExtension<WorkItemLink[]>.Success(new WorkItemLink[0]);
            }

            var releaseNotePrefix = store.GetReleaseNotePrefix();
            var workItemReferences = commentParser.ParseWorkItemReferences(buildInformation);

            return ResultFromExtension<WorkItemLink[]>.Success(workItemReferences.Select(wir => new WorkItemLink
                {
                    Id = wir.IssueNumber,
                    Description = GetReleaseNote(buildInformation.VcsRoot, wir.IssueNumber, wir.LinkData, releaseNotePrefix),
                    LinkUrl = NormalizeLinkData(baseUrl, buildInformation.VcsRoot, wir.LinkData),
                    Source = GitHubConfigurationStore.CommentParser
                })
                .Distinct()
                .ToArray());
        }

        public string GetReleaseNote(string vcsRoot, string issueNumber, string linkData, string? releaseNotePrefix)
        {
            var result = GetGitHubOwnerAndRepo(vcsRoot, linkData);
            if (!(result is ISuccessResult<(string owner, string repo)> successResult))
                return issueNumber;

            try
            {
                var issue = githubClient.Value.Issue.Get(successResult.Value.owner, successResult.Value.repo, int.Parse(issueNumber)).Result;
                // No comments on issue, or no release note prefix has been specified, so return issue title
                if (issue.Comments == 0 || string.IsNullOrWhiteSpace(releaseNotePrefix))
                    return issue.Title;

                var releaseNoteRegex = new Regex($"^{releaseNotePrefix}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                var issueComments = githubClient.Value.Issue.Comment
                    .GetAllForIssue(successResult.Value.owner, successResult.Value.repo, int.Parse(issueNumber)).Result;

                var releaseNote = issueComments?.LastOrDefault(c => releaseNoteRegex.IsMatch(c.Body))?.Body;
                // Return (last, if multiple found) comment that matched release note prefix, or return issue title
                return !string.IsNullOrWhiteSpace(releaseNote)
                    ? releaseNoteRegex.Replace(releaseNote, "")?.Trim() ?? string.Empty
                    : issue.Title;
            }
            catch (Exception)
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

            return NormalizeBaseGitUrl(baseToUse) + "/" + string.Join("/", linkDataComponents);
        }

        static readonly Regex GitSshUrlRegex = new Regex("^git@(?<host>.*):", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static string NormalizeBaseGitUrl(string vcsRoot)
        {
            var match = GitSshUrlRegex.Match(vcsRoot);
            if (match.Success)
            {
                vcsRoot = GitSshUrlRegex.Replace(vcsRoot, $"https://{match.Groups["host"]}/");
            }

            return vcsRoot;
        }

        IResult<(string owner, string repo)> GetGitHubOwnerAndRepo(string gitHubUrl, string linkData)
        {
            IResult<(string, string)> GetOwnerRepoFromVcsRoot(string vcsRoot)
            {
                var ownerRepoParts = ownerRepoRegex.Match(vcsRoot).Groups[1]?.Value.Split('/', '#');
                return ownerRepoParts == null || ownerRepoParts.Count() < 2
                    ? (IResult<(string owner, string repo)>)Result<(string, string)>.Failed("Incorrect number of owner repo parts")
                    : Result<(string, string)>.Success((ownerRepoParts[0], ownerRepoParts[1]));
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
                return Result<(string owner, string repo)>.Failed("Invalid repo owner components");

            return Result<(string owner, string repo)>.Success((ownerRepoComponents[0], ownerRepoComponents[1]));
        }

    }
}