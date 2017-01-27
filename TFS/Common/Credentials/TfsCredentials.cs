using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using Inedo.Documentation;
using Inedo.Extensions.TFS;
using Inedo.Serialization;

#if BuildMaster
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Web;
#elif Otter
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.Credentials;
using Inedo.Otter.Extensions;
using Inedo.Otter.Web;
#endif

#if BuildMaster
namespace Inedo.BuildMasterExtensions.TFS.Credentials
#elif Otter
namespace Inedo.OtterExtensions.TFS.Credentials
#endif
{
    [ScriptAlias("Tfs")]
    [DisplayName("Team Foundation Server")]
    [Description("Credentials for TFS that can be either username/password, or personal access token.")]
    public sealed class TfsCredentials : ResourceCredentials, IVsoConnectionInfo
    {
        [Required]
        [Persistent]
        [DisplayName("Team project collection URL")]
        public string TeamProjectCollection { get; set; }

        [Persistent]
        [DisplayName("User name")]
        [PlaceholderText("Use system credentials")]
        public string UserName { get; set; }

        [Persistent(Encrypted = true)]
        [DisplayName("Password/token")]
        [FieldEditMode(FieldEditMode.Password)]
        public SecureString PasswordOrToken { get; set; }

        [Persistent]
        [DisplayName("Domain")]
        public string Domain { get; set; }

        public override RichDescription GetDescription()
        {
            var desc = new RichDescription(this.UserName);
            if (!string.IsNullOrEmpty(this.Domain))
                desc.AppendContent("@", this.Domain);
            return desc;
        }

        string IVsoConnectionInfo.TeamProjectCollectionUrl => this.TeamProjectCollection;

        string IVsoConnectionInfo.PasswordOrToken
        {
            get
            {
                var ptr = Marshal.SecureStringToGlobalAllocUnicode(this.PasswordOrToken);
                try
                {
                    return Marshal.PtrToStringUni(ptr);
                }
                finally
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(ptr);
                }
            }
        }
    }
}
