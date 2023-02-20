using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility.ResourceMonitors;
using Inedo.Extensions.Credentials;
using Inedo.Extensions.TFS.Clients.Rest;
using Inedo.Extensions.TFS.Credentials;
using Inedo.Serialization;
using Inedo.Web;
using Newtonsoft.Json;

namespace Inedo.Extensions.TFS.RepositoryMonitors
{
    [DisplayName("TFVC")]
    [Description("Monitors a TFVC repository for new check-ins.")]
    public sealed class TfvcRepositoryMonitor : ResourceMonitor<TfvcRepositoryCommit, TfsSecureResource>
    {

        [Required]
        [Persistent]
        [DisplayName("Project")]
        [Description("The name of the project in TFS or Azure DevOps")]
        public string ProjectName { get; set; }

        [Persistent]
        [DisplayName("Path")]
        [PlaceholderText("$/")]
        [DefaultValue("$/")]
        [Description("A TFVC path (e.g. $/Path/To/Source) to monitor for changes. Leave empty to monitor from the root path $/")]
        public string SourcePath { get; set; }

        public async override Task<IReadOnlyDictionary<string, ResourceMonitorState>> GetCurrentStatesAsync(IResourceMonitorContext context, CancellationToken cancellationToken)
        {
            var dic = new Dictionary<string, ResourceMonitorState>();
            var resource = (TfsSecureResource)context.Resource;
            var credentials = resource.GetCredentials(context) as UsernamePasswordCredentials;
            var conn = new ConnectionInfo(credentials?.UserName, AH.Unprotect(credentials?.Password), resource.TeamProjectCollectionUrl, null);
            var client = new TfsRestApi(conn, this);

            var path = this.SourcePath ?? "$/";

            try
            {
                var checkins = await client.GetChangesetsAsync(this.ProjectName, path, cancellationToken);
                dic.Add(path, new TfvcRepositoryCommit { ChangeSetId = checkins.FirstOrDefault()?.changesetId });
            }
            catch (Exception ex)
            {
                string message = "An error occurred attempting to determine latest change set: " + ex.Message;
                dic.Add(path, new TfvcRepositoryCommit { Error = message });
            }

            this.LogDebug($"Finished executing TfsTiny.exe list");

            return dic;
        }

        public override RichDescription GetDescription()
        {
            return new RichDescription(
                "TFS or Azure DevOps project ",
                new Hilite(this.ProjectName)
            );
        }

        
    }
}
