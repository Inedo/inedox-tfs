using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.IssueSources;
using Inedo.Extensions.TFS.Clients.Rest;
using Inedo.Extensions.TFS.Credentials;
using Inedo.Extensions.TFS.SuggestionProviders;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.Extensions.TFS.IssueSources
{
    [DisplayName("TFS Issue Source")]
    [Description("Issue source for TFS.")]
    public sealed class TfsIssueSource : IssueSource, IHasCredentials<TfsCredentials>
    {
        [Persistent]
        [DisplayName("Credentials")]
        public string CredentialName { get; set; }
        [Persistent]
        [DisplayName("Team project")]
        [SuggestableValue(typeof(TeamProjectNameSuggestionProvider))]
        public string TeamProject { get; set; }
        [Persistent]
        [DisplayName("Iteration path")]
        [SuggestableValue(typeof(IterationPathSuggestionProvider))]
        public string IterationPath { get; set; }
        [Persistent]
        [DisplayName("Custom WIQL")]
        [PlaceholderText("Use above fields")]
        [FieldEditMode(FieldEditMode.Multiline)]
        [Description("Custom WIQL will ignore the project name and iteration path if supplied. "
            + "See the <a href=\"https://msdn.microsoft.com/library/bb130306.aspx#Anchor_0\">TFS Query Language documentation</a> "
            + "for more information.")]
        public string CustomWiql { get; set; }

        public override async Task<IEnumerable<IIssueTrackerIssue>> EnumerateIssuesAsync(IIssueSourceEnumerationContext context)
        {
            context.Log.LogDebug("Enumerating TFS issue source...");

            var credentials = this.TryGetCredentials<TfsCredentials>();
            var client = new TfsRestApi(credentials, context.Log);
            string wiql = this.GetWiql(context.Log);

            var workItems = await client.GetWorkItemsAsync(wiql).ConfigureAwait(false);

            return from w in workItems
                   select new TfsRestIssue(w);
        }

        private string GetWiql(ILogSink log)
        {
            if (!string.IsNullOrEmpty(this.CustomWiql))
            {
                log.LogDebug("Using custom WIQL query to filter issues...");
                return this.CustomWiql;
            }

            log.LogDebug($"Constructing WIQL query for project '{this.TeamProject}' and iteration path '{this.IterationPath}'...");

            var buffer = new StringBuilder();
            buffer.Append("SELECT [System.Id] FROM WorkItems ");

            bool projectSpecified = !string.IsNullOrEmpty(this.TeamProject);
            bool iterationPathSpecified = !string.IsNullOrEmpty(this.IterationPath);

            if (!projectSpecified && !iterationPathSpecified)
                return buffer.ToString();

            buffer.Append("WHERE ");

            if (projectSpecified)
                buffer.AppendFormat("[System.TeamProject] = '{0}' ", this.TeamProject.Replace("'", "''"));

            if (projectSpecified && iterationPathSpecified)
                buffer.Append("AND ");

            if (iterationPathSpecified)
                buffer.AppendFormat("[System.IterationPath] UNDER '{0}' ", this.IterationPath.Replace("'", "''"));

            return buffer.ToString();
        }

        public override RichDescription GetDescription()
        {
            if (!string.IsNullOrEmpty(this.CustomWiql))
                return new RichDescription("Get Issues from TFS Using Custom WIQL");
            else
                return new RichDescription(
                    "Get Issues from ", new Hilite(this.TeamProject), " in TFS for iteration path ", new Hilite(this.IterationPath)
                );
        }
    }
}
