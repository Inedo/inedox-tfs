using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Extensions.TFS.Clients.Rest;

#if BuildMaster
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMasterExtensions.TFS.Credentials;
#elif Otter
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.Credentials;
using Inedo.Otter.Web.Controls;
using Inedo.OtterExtensions.TFS.Credentials;
#endif

namespace Inedo.Extensions.TFS.SuggestionProviders
{
    internal sealed class TeamProjectNameSuggestionProvider : ISuggestionProvider
    {
        public async Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
        {
            var credentialName = config["CredentialName"];
            if (string.IsNullOrEmpty(credentialName))
                return Enumerable.Empty<string>();

            var credentials = ResourceCredentials.Create<TfsCredentials>(credentialName);

            var api = new TfsRestApi(credentials, null);
            var projects = await api.GetProjectsAsync().ConfigureAwait(false);

            return projects.Select(p => p.name);
        }
    }
}
