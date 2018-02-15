using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.TFS.Clients.SourceControl;
using Inedo.Extensions.TFS.SuggestionProviders;
using Inedo.Web;
using Inedo.Web.Plans.ArgumentEditors;

namespace Inedo.Extensions.TFS.Operations
{
    [DisplayName("TFS Get Source")]
    [Description("Gets the source code from a TFS repository.")]
    [Tag("source-control")]
    [ScriptAlias("Tfs-GetSource")]
    [Example(@"
# checkout a remote repository locally
Tfs-GetSource(
    Credentials: Hdars-Tfs,
    SourcePath: `$/HdarsApp,
    DiskPath: ~\Sources
);
")]
    [Serializable]
    public sealed class GetSourceOperation : RemoteTfsOperation
    {
        [ScriptAlias("Credentials")]
        [DisplayName("Credentials")]
        public override string CredentialName { get; set; }
        [ScriptAlias("SourcePath")]
        [DisplayName("Source path")]
        [BrowsablePath(typeof(TfsPathBrowser))]
        public string SourcePath { get; set; }
        [ScriptAlias("DiskPath")]
        [DisplayName("Export to directory")]
        [FilePathEditor]
        [PlaceholderText("$WorkingDirectory")]
        public string DiskPath { get; set; }
        [ScriptAlias("Label")]
        [DisplayName("Label")]
        [PlaceholderText("Latest source")]
        public string Label { get; set; }
        [Category("Advanced")]
        [ScriptAlias("WorkspaceName")]
        [DisplayName("Workspace name")]
        [PlaceholderText("Auto-generated")]
        public string WorkspaceName { get; set; }
        [Category("Advanced")]
        [ScriptAlias("WorkspaceDiskPath")]
        [DisplayName("Workspace disk path")]
        [PlaceholderText("BuildMaster managed")]
        public string WorkspaceDiskPath { get; set; }

        protected override Task<object> RemoteExecuteAsync(IRemoteOperationExecutionContext context)
        {
            this.LogInformation($"Getting source from TFS {(string.IsNullOrEmpty(this.Label) ? "(latest)" : $"labeled '{this.Label}'")}...");

            using (var client = new TfsSourceControlClient(this.TeamProjectCollectionUrl, this.UserName, this.PasswordOrToken, this.Domain, this))
            {
                client.GetSource(
                    new TfsSourcePath(this.SourcePath),
                    new WorkspaceInfo(this.WorkspaceName, this.WorkspaceDiskPath, context.ResolvePath(@"~\TfsWorkspaces")), 
                    context.ResolvePath(this.DiskPath), 
                    this.Label
                );
            }

            this.LogInformation("Get TFS source complete.");

            return Complete;
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
               new RichDescription("Get TFS Source"),
               new RichDescription("from ", new Hilite(config[nameof(this.SourcePath)]), " to ", new Hilite(config[nameof(this.DiskPath)]))
           );
        }
    }
}
