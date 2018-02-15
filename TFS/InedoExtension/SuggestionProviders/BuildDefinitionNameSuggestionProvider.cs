using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensions.TFS.Clients.Rest;
using Inedo.Extensions.TFS.Credentials;
using Inedo.Web;

namespace Inedo.Extensions.TFS.SuggestionProviders
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
