using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Documentation;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.ListVariableSources;
using Inedo.Extensions.TFS.Clients.Rest;
using Inedo.Extensions.TFS.Credentials;
using Inedo.Extensions.TFS.SuggestionProviders;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.Extensions.TFS.ListVariableSources
{
    [DisplayName("TFS Build Definition")]
    [Description("Build configurations from a specified team project in a TFS instance.")]
    public sealed class BuildDefinitionNameVariableSource : ListVariableSource, IHasCredentials<TfsCredentials>
    {
        [Persistent]
        [DisplayName("Credentials")]
        [TriggerPostBackOnChange]
        [Required]
        public string CredentialName { get; set; }

        [Persistent]
        [DisplayName("Team project")]
        [SuggestableValue(typeof(TeamProjectNameSuggestionProvider))]
        [Required]
        public string TeamProjectName { get; set; }

        public override async Task<IEnumerable<string>> EnumerateValuesAsync(ValueEnumerationContext context)
        {
            var credentials = ResourceCredentials.Create<TfsCredentials>(this.CredentialName);

            var api = new TfsRestApi(credentials, null);
            var definitions = await api.GetBuildDefinitionsAsync(this.TeamProjectName).ConfigureAwait(false);

            return definitions.Select(d => d.name);
        }

        public override RichDescription GetDescription()
        {
            return new RichDescription("TFS (", new Hilite(this.CredentialName), ") ", " build definitions in ", new Hilite(this.TeamProjectName), ".");
        }
    }
}
