using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Inedo.Documentation;
using Inedo.Extensibility.ResourceMonitors;
using Inedo.Extensions.Credentials;
using Inedo.Extensions.TFS.Credentials;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.Extensions.TFS.RepositoryMonitors
{
    [DisplayName("TFVC")]
    [Description("Monitors a TFVC repository for new check-ins.")]
    public sealed class TfvcRepositoryMonitor : ResourceMonitor<TfvcRepositoryCommit, TfsSecureResource>, IMissingPersistentPropertyHandler
    {

        [Required]
        [Persistent]
        [DisplayName("Project")]
        [Description("The name of the project in TFS or Azure DevOps")]
        public string ProjectName { get; set; }

        [Persistent]
        [DisplayName("Path")]
        [PlaceholderText("$/")]
        [Description("A TFVC path (e.g. $/Path/To/Source) to monitor for changes. Leave empty to monitor from the root path $/")]
        [FieldEditMode(FieldEditMode.Multiline)]
        public string SourcePath { get; set; }

        public async override Task<IReadOnlyDictionary<string, ResourceMonitorState>> GetCurrentStatesAsync(IResourceMonitorContext context, CancellationToken cancellationToken)
        {
            var dic = new Dictionary<string, ResourceMonitorState>();
            var r = (TfsSecureResource)context.Resource;
            var c = r.GetCredentials(context) as UsernamePasswordCredentials;

#warning TfsTine list
            //var conn = new ConnectionInfo(c.UserName, c.Password, r.TeamProjectCollection, null);
            //var client = new TfsRestApi(conn, null);

            //var paths = (this.SourcePaths == null || this.SourcePaths.Length == 0) ? new[] { "$/" } : this.SourcePaths;

            //foreach (string path in paths)
            //{
            //    try
            //    {
            //        var checkins = await client.GetChangesetsAsync(this.ProjectName, path);

            //        dic.Add(path, new TfvcRepositoryCommit { ChangeSetId = checkins.FirstOrDefault()?.changesetId });
            //    }
            //    catch (Exception ex)
            //    {
            //        string message = "An error occurred attempting to determine latest change set: " + ex.Message;
            //        dic.Add(path, new TfvcRepositoryCommit { Error = message });
            //    }
            //}

            return dic;
        }

        public override RichDescription GetDescription()
        {
            return new RichDescription(
                "TFS or Azure DevOps project ",
                new Hilite(this.ProjectName)
            );
        }

        void IMissingPersistentPropertyHandler.OnDeserializedMissingProperties(IReadOnlyDictionary<string, string> missingProperties)
        {
            //if (missingProperties.ContainsKey("SvnExePath"))
            //    _ = missingProperties["SvnExePath"];
            if (missingProperties.ContainsKey("CredentialName"))
                _ = missingProperties["CredentialName"];
            if (missingProperties.ContainsKey("TeamProjectCollectionUrl"))
                _ = missingProperties["TeamProjectCollectionUrl"];
            //
        }

        
    }
}
