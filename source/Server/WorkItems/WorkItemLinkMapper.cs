using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using Octokit.Internal;
using Octopus.Data;
using Octopus.Diagnostics;
using Octopus.Server.Extensibility.Extensions.Infrastructure.Web.Api;
using Octopus.Server.Extensibility.Extensions.WorkItems;
using Octopus.Server.Extensibility.IssueTracker.GitHub.Configuration;
using Octopus.Server.Extensibility.Results;
using Octopus.Server.MessageContracts.Features.BuildInformation;
using Octopus.Server.MessageContracts.Features.IssueTrackers;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.WorkItems
{
    interface IGitHubClientFactory
    {
        Task<IGitHubClient> CreateClient(IHttpClient httpClient, CancellationToken cancellationToken);
    }

    class GitHubClientFactory: IGitHubClientFactory
    {
        private readonly IGitHubConfigurationStore store;

        public GitHubClientFactory(IGitHubConfigurationStore store)
        {
            this.store = store;
        }

        public async Task<IGitHubClient> CreateClient(IHttpClient httpClient, CancellationToken cancellationToken)
        {
            var productHeaderValue = "octopus-github-issue-tracker";
            var productInformation = new ProductHeaderValue(productHeaderValue);
            var username = await store.GetUsername(cancellationToken);
            var password = await store.GetPassword(cancellationToken);
            var connection = new Connection(productInformation, httpClient);

            var client = new GitHubClient(connection);
            if (string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(password?.Value))
                return client;

            // Username/Password authentication used
            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password?.Value))
            {
                client.Credentials = new Credentials(username, password?.Value);
            }

            // Personal Access Token authentication used
            if(string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password?.Value))
            {
                client.Credentials = new Credentials(password?.Value);
            }

            return client;
        }
    }

    class WorkItemLinkMapper : IWorkItemLinkMapper
    {
        private readonly ISystemLog systemLog;
        private readonly IGitHubConfigurationStore store;
        private readonly IGitHubClientFactory githubClientFactory;
        private readonly IOctopusHttpClientFactory httpClientFactory;
        private readonly CommentParser commentParser;
        private readonly Regex ownerRepoRegex = new Regex("(?:https?://)?(?:[^?/\\s]+[?/])(.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public WorkItemLinkMapper(ISystemLog systemLog,
            IGitHubConfigurationStore store,
            IGitHubClientFactory githubClientFactory,
            IOctopusHttpClientFactory httpClientFactory,
            CommentParser commentParser)
        {
            this.systemLog = systemLog;
            this.store = store;
            this.githubClientFactory = githubClientFactory;
            this.httpClientFactory = httpClientFactory;
            this.commentParser = commentParser;
        }

        public async Task<bool> IsEnabled(CancellationToken cancellationToken)
        {
            return await store.GetIsEnabled(cancellationToken);
        }

        public string CommentParser => GitHubConfigurationStore.CommentParser;

        public async Task<IResultFromExtension<WorkItemLink[]>> Map(OctopusBuildInformation buildInformation, CancellationToken cancellationToken)
        {
            if (!await IsEnabled(cancellationToken))
                return ResultFromExtension<WorkItemLink[]>.ExtensionDisabled();

            var baseUrl = await store.GetBaseUrl(cancellationToken);
            if (string.IsNullOrWhiteSpace(baseUrl))
                return ResultFromExtension<WorkItemLink[]>.Failed("Base Url is not configured");
            if (buildInformation.VcsRoot == null)
                return ResultFromExtension<WorkItemLink[]>.Failed("No VCS root configured");

            const string pathComponentIndicatingAzureDevOpsVcs = @"/_git/";
            if (buildInformation.VcsRoot.Contains(pathComponentIndicatingAzureDevOpsVcs))
            {
                systemLog.WarnFormat("The VCS Root '{0}' indicates this build information is Azure DevOps related so GitHub comment references will be ignored", buildInformation.VcsRoot);
                return ResultFromExtension<WorkItemLink[]>.Success(Array.Empty<WorkItemLink>());
            }

            var releaseNotePrefix = await store.GetReleaseNotePrefix(cancellationToken);
            var workItemReferences = commentParser.ParseWorkItemReferences(buildInformation);

            List<WorkItemLink> list = new List<WorkItemLink>();
            using var httpClientAdapter = new HttpClientAdapter(() => httpClientFactory.HttpClientHandler);
            var client = await githubClientFactory.CreateClient(httpClientAdapter, cancellationToken);

            foreach (CommentParser.WorkItemReference wir in workItemReferences)
            {
                var link = new WorkItemLink
                {
                    Id = wir.IssueNumber,
                    Description = await GetReleaseNote(client, buildInformation.VcsRoot, wir.IssueNumber, wir.LinkData, releaseNotePrefix),
                    LinkUrl = NormalizeLinkData(baseUrl, buildInformation.VcsRoot, wir.LinkData), Source = GitHubConfigurationStore.CommentParser
                };
                if (!list.Contains(link))
                {
                    list.Add(link);
                }
            }

            return ResultFromExtension<WorkItemLink[]>.Success(list.ToArray());
        }

        public async Task<string> GetReleaseNote(IGitHubClient githubClient, string vcsRoot, string issueNumber, string linkData, string? releaseNotePrefix)
        {
            var result = GetGitHubOwnerAndRepo(vcsRoot, linkData);
            if (!(result is ISuccessResult<(string owner, string repo)> successResult))
                return issueNumber;

            try
            {
                var issue = await githubClient.Issue.Get(successResult.Value.owner, successResult.Value.repo, int.Parse(issueNumber));
                // No comments on issue, or no release note prefix has been specified, so return issue title
                if (issue.Comments == 0 || string.IsNullOrWhiteSpace(releaseNotePrefix))
                    return issue.Title;

                var releaseNoteRegex = new Regex($"^{releaseNotePrefix}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                var issueComments = await githubClient.Issue.Comment
                    .GetAllForIssue(successResult.Value.owner, successResult.Value.repo, int.Parse(issueNumber));

                var releaseNote = issueComments?.LastOrDefault(c => releaseNoteRegex.IsMatch(c.Body))?.Body;
                // Return (last, if multiple found) comment that matched release note prefix, or return issue title
                return !string.IsNullOrWhiteSpace(releaseNote)
                    ? releaseNoteRegex.Replace(releaseNote, String.Empty).Trim()
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