using System.ComponentModel;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.TFS.Credentials;

namespace Inedo.Extensions.TFS.Operations
{
    public abstract class TfsOperation : ExecuteOperation, IHasCredentials<TfsCredentials>, IVsoConnectionInfo
    {
        private protected TfsOperation()
        {
        }

        public abstract string CredentialName { get; set; }

        [Category("Connection/Identity")]
        [ScriptAlias("Url")]
        [DisplayName("Project collection URL")]
        [PlaceholderText("Use team project from credentials")]
        [MappedCredential(nameof(TfsCredentials.TeamProjectCollection))]
        public string TeamProjectCollectionUrl { get; set; }

        [Category("Connection/Identity")]
        [ScriptAlias("UserName")]
        [DisplayName("User name")]
        [PlaceholderText("Use user name from credentials")]
        [MappedCredential(nameof(TfsCredentials.UserName))]
        public string UserName { get; set; }

        [Category("Connection/Identity")]
        [ScriptAlias("Password")]
        [DisplayName("Password / token")]
        [PlaceholderText("Use password/token from credentials")]
        [MappedCredential(nameof(TfsCredentials.PasswordOrToken))]
        public string PasswordOrToken { get; set; }

        [Category("Connection/Identity")]
        [ScriptAlias("Domain")]
        [DisplayName("Domain name")]
        [PlaceholderText("Use domain from credentials")]
        [MappedCredential(nameof(TfsCredentials.Domain))]
        public string Domain { get; set; }
    }
}