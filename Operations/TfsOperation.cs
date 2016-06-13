using System.ComponentModel;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMasterExtensions.TFS.Credentials;

namespace Inedo.BuildMasterExtensions.TFS.Operations
{
    public abstract class TfsOperation : ExecuteOperation, IHasCredentials<TfsCredentials>, IVsoConnectionInfo
    {
        [Category("Connection/Identity")]
        [ScriptAlias("Credentials")]
        [DisplayName("Credentials")]
        public string CredentialName { get; set; }

        [Category("Connection/Identity")]
        [ScriptAlias("Url")]
        [DisplayName("Project collection URL")]
        [MappedCredential(nameof(TfsCredentials.TeamProjectCollection))]
        public string TeamProjectCollectionUrl { get; set; }

        [Category("Connection/Identity")]
        [ScriptAlias("UserName")]
        [DisplayName("User name")]
        [MappedCredential(nameof(TfsCredentials.UserName))]
        public string UserName { get; set; }

        [Category("Connection/Identity")]
        [ScriptAlias("Password")]
        [DisplayName("Password / token")]
        [MappedCredential(nameof(TfsCredentials.PasswordOrToken))]
        public string PasswordOrToken { get; set; }

        [Category("Connection/Identity")]
        [ScriptAlias("Domain")]
        [DisplayName("Domain name")]
        [MappedCredential(nameof(TfsCredentials.Domain))]
        public string Domain { get; set; }
    }
}