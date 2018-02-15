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
    internal sealed class BuildNumberSuggestionProvider : ISuggestionProvider
    {
        public async Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
        {
            var credentialName = config["CredentialName"];
            var projectName = AH.CoalesceString(config["TeamProject"], config["TeamProjectName"]);
            var definitionName = config["BuildDefinition"];
            if (string.IsNullOrEmpty(credentialName) || string.IsNullOrEmpty(projectName) || string.IsNullOrEmpty(definitionName))
                return Enumerable.Empty<string>();

            var credentials = ResourceCredentials.Create<TfsCredentials>(credentialName);

            var api = new TfsRestApi(credentials, null);
            var definition = await api.GetBuildDefinitionAsync(projectName, definitionName).ConfigureAwait(false);
            if (definition == null)
                return Enumerable.Empty<string>();

            var builds = await api.GetBuildsAsync(projectName, definition.id).ConfigureAwait(false);
            return builds.Select(b => b.buildNumber);
        }
    }
}
