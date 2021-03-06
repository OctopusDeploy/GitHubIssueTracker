using System.Linq;
using System.Text.RegularExpressions;
using Octopus.Server.MessageContracts.Features.BuildInformation;

namespace Octopus.Server.Extensibility.IssueTracker.GitHub.WorkItems
{
    class CommentParser
    {
        private static readonly Regex Expression = new Regex("(?:close[d|s]*|fix[ed|es]*|resolve[d|s]*):?\\s((?:[A-Z0-9/_.-]*#|GH-|http[/A-Z:.]*/issues/)(\\d+))", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public WorkItemReference[] ParseWorkItemReferences(OctopusBuildInformation buildInformation)
        {
            return buildInformation.Commits.SelectMany(c => ParseReferences(c.Comment))
                .ToArray();
        }

        internal static WorkItemReference[] ParseReferences(string comment)
        {
            return Expression.Matches(comment)
                .Select(m => new WorkItemReference
                {
                    IssueNumber = m.Groups[2].Value,
                    LinkData = m.Groups[1].Value
                })
                .ToArray();
        }

        public class WorkItemReference
        {
            public string IssueNumber { get; set; } = string.Empty;
            public string LinkData { get; set; } = string.Empty;
        }
    }
}