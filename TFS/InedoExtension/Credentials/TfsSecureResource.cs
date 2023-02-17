using System.ComponentModel;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.SecureResources;
using Inedo.Serialization;

namespace Inedo.Extensions.TFS.Credentials
{
    [ScriptAlias("Tfs")]
    [DisplayName("Team Foundation Server")]
    [Description("Connection for Team FoundationServer Version Control.")]
    [PersistFrom("Inedo.BuildMasterExtensions.TFS.Credentials.TfsCredentials,TFS")]
    [PersistFrom("Inedo.OtterExtensions.TFS.Credentials.TfsCredentials,TFS")]
    [PersistFrom("Inedo.Extensions.TFS.Credentials.TfsCredentials,TFS")]
    public sealed class TfsSecureResource : SecureResource<Extensions.Credentials.UsernamePasswordCredentials>
    {
        [Required]
        [Persistent]
        [DisplayName("Team project collection URL")]
        public string TeamProjectCollectionUrl { get; set; }

        public override RichDescription GetDescription()
        {
            var desc = new RichDescription(TeamProjectCollectionUrl);
            return desc;
        }

    }
}
