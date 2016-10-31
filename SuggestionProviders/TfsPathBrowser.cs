using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Plans.ArgumentEditors;
using Inedo.BuildMasterExtensions.TFS.Clients;
using Inedo.BuildMasterExtensions.TFS.Credentials;
using Inedo.Diagnostics;

namespace Inedo.BuildMasterExtensions.TFS.SuggestionProviders
{
    public sealed class TfsPathBrowser : IPathBrowser
    {
        public Task<IEnumerable<IPathInfo>> GetPathInfosAsync(string path, IComponentConfiguration config)
        {
            var info = new PathBrowserInfo(config);

            if (string.IsNullOrEmpty(info.TeamProjectCollection))
                throw new InvalidOperationException("The TFS Team Project Collection URL could not be determined.");
            
            using (var client = new TfsSourceControlClient(info.TeamProjectCollection, info.UserName, info.Password, info.Domain, Logger.Null))
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
            public string UserName => AH.CoalesceString(config[nameof(this.UserName)], this.getCredentials.Value?.UserName);
            public string Password => AH.CoalesceString(config[nameof(this.Password)], this.getCredentials.Value?.PasswordOrToken.ToUnsecureString());
            public string Domain => AH.CoalesceString(config[nameof(this.Domain)], this.getCredentials.Value?.Domain);
            public int? ApplicationId => ((IBrowsablePathEditorContext)config).ApplicationId;

            public BuildMasterAgent CreateAgent()
            {
                var vars = DB.Variables_GetVariablesAccessibleFromScope(
                    Variable_Name: "TfsDefaultServerName",
                    Application_Id: this.ApplicationId,
                    IncludeSystemVariables_Indicator: true
                );

                var variable = (from v in vars
                                orderby v.Application_Id != null ? 0 : 1
                                select v).FirstOrDefault();

                if (variable == null)
                    return BuildMasterAgent.CreateLocalAgent();
                else
                    return BuildMasterAgent.Create(InedoLib.UTF8Encoding.GetString(variable.Variable_Value));
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
