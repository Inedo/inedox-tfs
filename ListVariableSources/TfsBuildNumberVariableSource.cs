using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Extensibility.ListVariableSources;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMasterExtensions.TFS.Clients.Rest;
using Inedo.BuildMasterExtensions.TFS.Credentials;
using Inedo.BuildMasterExtensions.TFS.SuggestionProviders;
using Inedo.Documentation;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.TFS.ListVariableSources
{
    [DisplayName("TFS Build Number")]
    [Description("Build numbers from a specified build definition in a TFS instance.")]
    public sealed class TfsBuildNumberVariableSource : ListVariableSource, IHasCredentials<TfsCredentials>
    {
        [Persistent]
        [DisplayName("Credentials")]
        [TriggerPostBackOnChange]
        [Required]
        public string CredentialName { get; set; }

        [Persistent]
        [DisplayName("Team project")]
        [SuggestibleValue(typeof(TeamProjectNameSuggestionProvider))]
        [TriggerPostBackOnChange]
        [Required]
        public string TeamProjectName { get; set; }

        [Persistent]
        [DisplayName("Build definition")]
        [SuggestibleValue(typeof(BuildDefinitionNameSuggestionProvider))]
        [Required]
        public string BuildDefinitionName { get; set; }

        public override async Task<IEnumerable<string>> EnumerateValuesAsync(ValueEnumerationContext context)
        {
            var credentials = ResourceCredentials.Create<TfsCredentials>(this.CredentialName);

            var api = new TfsRestApi(credentials);
            var definition = await api.GetBuildDefinitionAsync(this.TeamProjectName, this.BuildDefinitionName).ConfigureAwait(false);
            if (definition == null)
                return Enumerable.Empty<string>();

            var builds = await api.GetBuildsAsync(this.TeamProjectName, definition.id).ConfigureAwait(false);
            return builds.Select(b => b.buildNumber);
        }

        public override RichDescription GetDescription()
        {
            return new RichDescription("TFS (", new Hilite(this.CredentialName), ") ", " builds for ", new Hilite(this.BuildDefinitionName), " in ", new Hilite(this.TeamProjectName), ".");
        }
    }
}
