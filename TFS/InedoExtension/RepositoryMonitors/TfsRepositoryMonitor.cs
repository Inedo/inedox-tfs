using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Inedo.Documentation;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.RepositoryMonitors;
using Inedo.Extensions.TFS.Clients.SourceControl;
using Inedo.Extensions.TFS.Credentials;
using Inedo.Extensions.TFS.SuggestionProviders;
using Inedo.IO;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.Extensions.TFS.RepositoryMonitors
{
    [DisplayName("TFS")]
    [Description("Monitors a TFS project for new checkins.")]
    public sealed class TfsRepositoryMonitor : RepositoryMonitor, IHasCredentials<TfsCredentials>
    {
        [Persistent]
        [DisplayName("Team project collection URL")]
        [MappedCredential(nameof(TfsCredentials.TeamProjectCollection))]
        public string TeamProjectCollection { get; set; }
        [Persistent]
        [DisplayName("User name")]
        [MappedCredential(nameof(TfsCredentials.UserName))]
        public string UserName { get; set; }
        [Persistent(Encrypted = true)]
        [DisplayName("Password/token")]
        [FieldEditMode(FieldEditMode.Password)]
        [MappedCredential(nameof(TfsCredentials.PasswordOrToken))]
        public SecureString PasswordOrToken { get; set; }
        [Persistent]
        [DisplayName("Domain")]
        [MappedCredential(nameof(TfsCredentials.Domain))]
        public string Domain { get; set; }
        [Persistent]
        [DisplayName("Credentials")]
        public string CredentialName { get; set; }
        [Persistent]
        [DisplayName("Source path")]
        [BrowsablePath(typeof(TfsPathBrowser))]
        public string SourcePath { get; set; }

        public override Task<IReadOnlyDictionary<string, RepositoryCommit>> GetCurrentCommitsAsync(IRepositoryMonitorContext context)
        {
            using (var client = new TfsSourceControlClient(this.TeamProjectCollection, this.UserName, AH.Unprotect(this.PasswordOrToken), this.Domain, this))
            {
                var folders = client.EnumerateChildSourcePaths(new TfsSourcePath(this.SourcePath));

                var dict = folders.ToDictionary(f => PathEx.GetFileName(f.AbsolutePath), f => (RepositoryCommit)new TfsRepositoryCommit(f));

                return Task.FromResult<IReadOnlyDictionary<string, RepositoryCommit>>(dict);
            }
        }

        public override RichDescription GetDescription()
        {
            return new RichDescription("TFS repository at ", new Hilite(AH.CoalesceString(this.TeamProjectCollection, this.CredentialName)));
        }
    }
}
