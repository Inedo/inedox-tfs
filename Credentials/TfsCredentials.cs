using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Web;
using Inedo.Documentation;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.TFS.Credentials
{
    [ScriptAlias("Tfs")]
    [DisplayName("Team Foundation Server")]
    [Description("Credentials for TFS that can be either username/password, or personal access token.")]
    public sealed class TfsCredentials : ResourceCredentials
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
    }
}
