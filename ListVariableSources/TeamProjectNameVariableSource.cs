using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Extensibility.ListVariableSources;
using Inedo.BuildMasterExtensions.TFS.Credentials;
using Inedo.BuildMasterExtensions.TFS.VisualStudioOnline;
using Inedo.Documentation;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.TFS.ListVariableSources
{
    [DisplayName("TFS Team Project")]
    [Description("Team projects from a specified TFS instance.")]
    public sealed class TeamProjectNameVariableSource : ListVariableSource, IHasCredentials<TfsCredentials>
    {
        [Persistent]
        [DisplayName("Credentials")]
        [Required]
        public string CredentialName { get; set; }

        public override async Task<IEnumerable<string>> EnumerateValuesAsync(ValueEnumerationContext context)
        {
            var credentials = ResourceCredentials.Create<TfsCredentials>(this.CredentialName);

            var api = new TfsRestApi(credentials);
            var projects = await api.GetProjectsAsync().ConfigureAwait(false);

            return projects.Select(p => p.name);
        }

        public override RichDescription GetDescription()
        {
            return new RichDescription("TFS (", new Hilite(this.CredentialName), ") ", " team projects.");
        }
    }
}
