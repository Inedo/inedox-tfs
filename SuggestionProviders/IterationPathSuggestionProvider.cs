using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMasterExtensions.TFS.Credentials;
using Inedo.BuildMasterExtensions.TFS.VisualStudioOnline;

namespace Inedo.BuildMasterExtensions.TFS.SuggestionProviders
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

            var api = new TfsRestApi(credentials);
            var iterations = await api.GetIterationsAsync(teamProject).ConfigureAwait(false);
            return iterations.Select(i => i.path);
        }
    }
}
