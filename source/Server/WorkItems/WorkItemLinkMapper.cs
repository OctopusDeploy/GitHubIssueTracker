using System.Linq;
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

        public WorkItemLinkMapper(IGitHubConfigurationStore store,
            CommentParser commentParser)
        {
            this.store = store;
            this.commentParser = commentParser;
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

            var workItemReferences = commentParser.ParseWorkItemReferences(packageMetadata);

            return workItemReferences.Select(wir => new WorkItemLink
                {
                    Id = wir.IssueNumber,
                    Description = wir.IssueNumber,
                    LinkUrl = isEnabled ? NormalizeLinkData(baseUrl, packageMetadata.VcsRoot, wir.LinkData) : wir.LinkData
                })
                .Distinct()
                .ToArray();
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