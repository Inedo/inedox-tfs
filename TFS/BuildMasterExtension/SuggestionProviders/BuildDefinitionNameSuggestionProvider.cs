using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMasterExtensions.TFS.Clients.Rest;
using Inedo.BuildMasterExtensions.TFS.Credentials;

namespace Inedo.BuildMasterExtensions.TFS.SuggestionProviders
{
    internal sealed class BuildDefinitionNameSuggestionProvider : ISuggestionProvider
    {
        public async Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
        {
            var credentialName = config["CredentialName"];
            var projectName = AH.CoalesceString(config["TeamProject"], config["TeamProjectName"]);
            if (string.IsNullOrEmpty(credentialName) || string.IsNullOrEmpty(projectName))
                return Enumerable.Empty<string>();

            var credentials = ResourceCredentials.Create<TfsCredentials>(credentialName);

            var api = new TfsRestApi(credentials, null);
            var definitions = await api.GetBuildDefinitionsAsync(projectName).ConfigureAwait(false);

            return definitions.Select(d => d.name);
        }
    }
}
