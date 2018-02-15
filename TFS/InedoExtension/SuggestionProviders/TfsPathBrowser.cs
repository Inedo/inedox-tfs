using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Extensibility;
using Inedo.Extensibility.Agents;
using Inedo.Extensibility.Credentials;
using Inedo.Extensions.TFS.Clients.SourceControl;
using Inedo.Extensions.TFS.Credentials;
using Inedo.Web;
using Inedo.Web.Plans.ArgumentEditors;

namespace Inedo.Extensions.TFS.SuggestionProviders
{
    public sealed class TfsPathBrowser : IPathBrowser
    {
        public Task<IEnumerable<IPathInfo>> GetPathInfosAsync(string path, IComponentConfiguration config)
        {
            var info = new PathBrowserInfo(config);

            if (string.IsNullOrEmpty(info.TeamProjectCollection))
                throw new InvalidOperationException("The TFS Team Project Collection URL could not be determined.");
            
            using (var client = new TfsSourceControlClient(info.TeamProjectCollection, info.UserName, info.Password, info.Domain, null))
            {
                var paths = client.EnumerateChildSourcePaths(new TfsSourcePath(path));

                return Task.FromResult(paths.Where(p => p.IsDirectory == true).Cast<IPathInfo>());
            }
        }

        private sealed class PathBrowserInfo
        {
            private IComponentConfiguration config;
            private Lazy<TfsCredentials> getCredentials;

            public PathBrowserInfo(IComponentConfiguration config)
            {
                this.config = config;
                this.getCredentials = new Lazy<TfsCredentials>(GetCredentials);
            }

            public string SourcePath => config[nameof(this.SourcePath)];
            public string TeamProjectCollection => AH.CoalesceString(config[nameof(this.TeamProjectCollection)], this.getCredentials.Value?.TeamProjectCollection);
            public string UserName => AH.CoalesceString(this.config[nameof(this.UserName)], this.getCredentials.Value?.UserName);
            public string Password => AH.CoalesceString(this.config[nameof(this.Password)], AH.Unprotect(this.getCredentials.Value?.PasswordOrToken));
            public string Domain => AH.CoalesceString(this.config[nameof(this.Domain)], this.getCredentials.Value?.Domain);
            public int? ApplicationId => ((IBrowsablePathEditorContext)this.config).ProjectId;

            public Agent CreateAgent()
            {
#warning FIX
                throw new NotImplementedException();
                //var vars = DB.Variables_GetVariablesAccessibleFromScope(
                //    Variable_Name: "TfsDefaultServerName",
                //    Application_Id: this.ApplicationId,
                //    IncludeSystemVariables_Indicator: true
                //);

                //var variable = (from v in vars
                //                orderby v.Application_Id != null ? 0 : 1
                //                select v).FirstOrDefault();

                //if (variable == null)
                //    return BuildMasterAgent.CreateLocalAgent();
                //else
                //    return BuildMasterAgent.Create(InedoLib.UTF8Encoding.GetString(variable.Variable_Value));
            }

            private TfsCredentials GetCredentials()
            {
                string credentialName = this.config["CredentialName"];
                if (string.IsNullOrEmpty(credentialName))
                    return null;

                return ResourceCredentials.Create<TfsCredentials>(credentialName);
            }
        }
    }
}
