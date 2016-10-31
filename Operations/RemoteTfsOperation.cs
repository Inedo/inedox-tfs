using System;
using System.ComponentModel;
using Inedo.Agents;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMasterExtensions.TFS.Credentials;
using Inedo.Documentation;
using Inedo.IO;

namespace Inedo.BuildMasterExtensions.TFS.Operations
{
    [Serializable]
    public abstract class RemoteTfsOperation : RemoteExecuteOperation, IHasCredentials<TfsCredentials>
    {
        public abstract string CredentialName { get; set; }

        [Category("Connection/Identity")]
        [ScriptAlias("Url")]
        [DisplayName("Project collection URL")]
        [PlaceholderText("Use team project from credentials")]
        [MappedCredential(nameof(TfsCredentials.TeamProjectCollection))]
        public string TeamProjectCollectionUrl { get; set; }

        [Category("Connection/Identity")]
        [ScriptAlias("UserName")]
        [DisplayName("User name")]
        [PlaceholderText("Use user name from credentials")]
        [MappedCredential(nameof(TfsCredentials.UserName))]
        public string UserName { get; set; }

        [Category("Connection/Identity")]
        [ScriptAlias("Password")]
        [DisplayName("Password / token")]
        [PlaceholderText("Use password/token from credentials")]
        [MappedCredential(nameof(TfsCredentials.PasswordOrToken))]
        public string PasswordOrToken { get; set; }

        [Category("Connection/Identity")]
        [ScriptAlias("Domain")]
        [DisplayName("Domain name")]
        [PlaceholderText("Use domain from credentials")]
        [MappedCredential(nameof(TfsCredentials.Domain))]
        public string Domain { get; set; }

        protected string GetRootWorkspaceDiskPath()
        {
            using (var agent = BuildMasterAgent.CreateLocalAgent())
            {
                return PathEx.Combine(agent.GetService<IFileOperationsExecuter>().GetBaseWorkingDirectory(), "TfsWorkspaces");
            }
        }
    }
}
