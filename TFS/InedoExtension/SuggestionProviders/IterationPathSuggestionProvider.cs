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
    internal sealed class IterationPathSuggestionProvider : ISuggestionProvider
    {
        public async Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
        {
            var credentialName = config["CredentialName"];
            var teamProject = config["TeamProject"];
            if (string.IsNullOrEmpty(credentialName) || string.IsNullOrEmpty(teamProject))
                return Enumerable.Empty<string>();

            var credentials = ResourceCredentials.Create<TfsCredentials>(credentialName);

            var api = new TfsRestApi(credentials, null);
            var iterations = await api.GetIterationsAsync(teamProject).ConfigureAwait(false);
            return iterations.Select(i => i.path);
        }
    }
}
