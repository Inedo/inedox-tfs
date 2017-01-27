using System.ComponentModel;
using Inedo.Documentation;

#if BuildMaster
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMaster.Web;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMasterExtensions.TFS.Credentials;
using Inedo.BuildMasterExtensions.TFS.SuggestionProviders;
#elif Otter
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.Credentials;
using Inedo.Otter.Extensibility.Operations;
using Inedo.Otter.Extensions;
using Inedo.Otter.Web.Controls;
using Inedo.OtterExtensions.TFS.Credentials;
#endif

namespace Inedo.Extensions.TFS.Operations
{
    public abstract class TfsOperation : ExecuteOperation, IHasCredentials<TfsCredentials>, IVsoConnectionInfo
    {
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