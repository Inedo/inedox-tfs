using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Extensibility.IssueSources;
using Inedo.BuildMaster.Extensibility.IssueTrackerConnections;
using Inedo.BuildMaster.Web;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMasterExtensions.TFS.Clients.Rest;
using Inedo.BuildMasterExtensions.TFS.Credentials;
using Inedo.BuildMasterExtensions.TFS.SuggestionProviders;
using Inedo.Documentation;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.TFS.IssueSources
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
        [SuggestibleValue(typeof(TeamProjectNameSuggestionProvider))]
        public string TeamProject { get; set; }
        [Persistent]
        [DisplayName("Iteration path")]
        [SuggestibleValue(typeof(IterationPathSuggestionProvider))]
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
            var credentials = this.TryGetCredentials<TfsCredentials>();
            var client = new TfsRestApi(credentials);
            string wiql = this.GetWiql();

            var workItems = await client.GetWorkItemsAsync(wiql).ConfigureAwait(false);

            return from w in workItems
                   select new TfsRestIssue(w);
        }

        private string GetWiql()
        {
            if (!string.IsNullOrEmpty(this.CustomWiql))
                return this.CustomWiql;
            
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
