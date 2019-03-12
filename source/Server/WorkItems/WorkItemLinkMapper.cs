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

        public WorkItemLinkMapper(IGitHubConfigurationStore store)
        {
            this.store = store;
        }

        public string IssueTrackerId => GitHubConfigurationStore.SingletonId;
        public bool IsEnabled => store.GetIsEnabled();

        public WorkItemLink[] Map(OctopusPackageMetadata packageMetadata)
        {
            if (packageMetadata.IssueTrackerId != IssueTrackerId)
                return null;

            var baseUrl = store.GetBaseUrl();
            if (string.IsNullOrWhiteSpace(baseUrl))
                return null;

            var isEnabled = store.GetIsEnabled();

            return packageMetadata.WorkItems.Select(wi => new WorkItemLink
                {
                    Id = wi.Id,
                    LinkText = wi.LinkText,
                    LinkUrl = isEnabled ? NormalizeLinkData(baseUrl, packageMetadata.VcsRoot, wi.LinkData) : wi.LinkData
                })
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
            if (linkDataComponents.Count == 2)
            {
                // we have org/repo or user/repo formatted, insert "issues" between that and the issueId
                linkDataComponents.Insert(1, "issues");

                // switch to the baseUrl, rather than calc relative to the vscRoot
                baseToUse = baseUrl;
            }
            else
            {
                // we have only issueId, insert "issues" before it
                linkDataComponents.Insert(0, "issues");
            }

            return baseToUse + "/" + string.Join("/", linkDataComponents);
        }
    }
}