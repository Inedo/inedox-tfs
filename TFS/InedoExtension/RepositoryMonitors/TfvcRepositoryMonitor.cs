using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Inedo.Documentation;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.RepositoryMonitors;
using Inedo.Extensions.TFS.Clients.Rest;
using Inedo.Extensions.TFS.Credentials;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.Extensions.TFS.RepositoryMonitors
{
    [DisplayName("TFVC")]
    [Description("Monitors a TFVC repository for new check-ins.")]
    public sealed class TfvcRepositoryMonitor : RepositoryMonitor
    {
        [Persistent]
        [DisplayName("Credentials")]
        public string CredentialName { get; set; }

        [Required]
        [Persistent]
        [DisplayName("Project")]
        [Description("The name of the project in TFS or Azure DevOps")]
        public string ProjectName { get; set; }

        [Persistent]
        [DisplayName("Paths")]
        [PlaceholderText("$/")]
        [Description("A newline-separated list of paths (e.g. $/Path/To/Source) to monitor for changes. Leave empty to monitor from the root path $/")]
        [FieldEditMode(FieldEditMode.Multiline)]
        public string[] SourcePaths { get; set; }

        [Persistent]
        [Category("Connection")]
        [DisplayName("Team project collection URL")]
        [PlaceholderText("Use project collection URL from credentials")]
        public string TeamProjectCollectionUrl { get; set; }

        [Persistent]
        [DisplayName("User name")]
        [Category("Connection")]
        [PlaceholderText("Use user name from credentials")]
        public string UserName { get; set; }

        [Persistent(Encrypted = true)]
        [Category("Connection")]
        [DisplayName("Password/token")]
        [FieldEditMode(FieldEditMode.Password)]
        [PlaceholderText("Use password from credentials")]
        public SecureString PasswordOrToken { get; set; }

        [Persistent]
        [Category("Connection")]
        [DisplayName("Domain")]
        [PlaceholderText("Use domain from credentials")]
        public string Domain { get; set; }

        public override async Task<IReadOnlyDictionary<string, RepositoryCommit>> GetCurrentCommitsAsync(IRepositoryMonitorContext context)
        {
            var conn = new ConnectionInfo(this.CredentialName, this.UserName, this.PasswordOrToken, this.TeamProjectCollectionUrl, this.Domain);
            var client = new TfsRestApi(conn, null);

            var dic = new Dictionary<string, RepositoryCommit>();

            var paths = (this.SourcePaths == null || this.SourcePaths.Length == 0) ? new[] { "$/" } : this.SourcePaths;

            foreach (string path in paths) 
            {
                try
                {
                    var checkins = await client.GetChangesetsAsync(this.ProjectName, path);

                    dic.Add(path, new TfvcRepositoryCommit { ChangeSetId = checkins.FirstOrDefault()?.changesetId });
                }
                catch (Exception ex)
                {
                    string message = "An error occurred attempting to determine latest change set: " + ex.Message;
                    dic.Add(path, new TfvcRepositoryCommit { Error = message });
                }
            }

            return dic;            
        }

        public override RichDescription GetDescription()
        {
            return new RichDescription(
                "TFS or Azure DevOps project ",
                new Hilite(this.ProjectName)
            );
        }

        private sealed class ConnectionInfo : IVsoConnectionInfo
        {
            public ConnectionInfo(string credentialName, string username, SecureString password, string projectCollectionUrl, string domain)
            {
                var creds = ResourceCredentials.TryCreate<TfsCredentials>(credentialName);

                this.UserName = username ?? creds?.UserName;
                this.PasswordOrToken = AH.Unprotect(password ?? creds?.PasswordOrToken);
                this.Domain = domain ?? creds?.Domain;
                this.TeamProjectCollectionUrl = projectCollectionUrl ?? creds?.TeamProjectCollection;
            }

            public string UserName { get; }
            public string PasswordOrToken { get; }
            public string Domain { get; }
            public string TeamProjectCollectionUrl { get; }
        }
    }
}
