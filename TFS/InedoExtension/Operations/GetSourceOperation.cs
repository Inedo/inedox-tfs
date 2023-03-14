using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.TFS.Credentials;
using Inedo.Web;

namespace Inedo.Extensions.TFS.Operations
{
    [DisplayName("TFS Get Source")]
    [Description("Gets the source code from a TFS repository.")]
    [Tag("source-control")]
    [ScriptAlias("Tfs-GetSource")]
    [Example(@"
# checkout a remote repository locally
Tfs-GetSource(
    From: Hdars-Tfs,
    SourcePath: `$/HdarsApp,
    DiskPath: ~\Sources
);
")]
    [Serializable]
    public sealed class GetSourceOperation : TfsOperation
    {
        [ScriptAlias("From")]
        [ScriptAlias("Credentials")]
        [DisplayName("From TFS Resource")]
        [SuggestableValue(typeof(SecureResourceSuggestionProvider<TfsSecureResource>))]
        public override string ResourceName { get; set; }
        [ScriptAlias("SourcePath")]
        [DisplayName("Source path")]
        public string SourcePath { get; set; }
        [ScriptAlias("DiskPath")]
        [DisplayName("Export to directory")]
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
        [Output]
        [ScriptAlias("ChangeSet")]
        [DisplayName("Change Set")]
        [PlaceholderText("eg. $ChangeSet")]
        public string ChangeSet { get; set; }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
               new RichDescription("Get TFS Source"),
               new RichDescription(
                   "from ", 
                    new Hilite(string.IsNullOrWhiteSpace(config[nameof(this.SourcePath)]) ? "$/" : config[nameof(this.SourcePath)]), 
                    " to ", 
                    new Hilite(string.IsNullOrWhiteSpace(config[nameof(this.DiskPath)]) ? "the Working Directory" : config[nameof(this.DiskPath)])
                )
           );
        }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            this.LogInformation($"Getting source from TFS {(string.IsNullOrEmpty(this.Label) ? "(latest)" : $"labeled '{this.Label}'")}...");

            var args = new List<TfsArg>();
            if (!string.IsNullOrWhiteSpace(this.SourcePath))
                args.Add(new ("--source", this.SourcePath, true, false));

            args.Add(new("--workspace", (!string.IsNullOrWhiteSpace(this.WorkspaceDiskPath) ? this.WorkspaceDiskPath : context.ResolvePath(@"~\TfsWorkspaces"))?.TrimEnd('\\'), true, false));
            if(!string.IsNullOrWhiteSpace(this.WorkspaceName))
                args.Add(new("--workspace-name", this.WorkspaceName, true, false));
            

            args.Add(new("--target", context.ResolvePath(this.DiskPath)?.TrimEnd('\\'), true, false));
            if(!string.IsNullOrWhiteSpace(this.Label))
                args.Add(new ("--label", this.Label, true, false));
            var result = await this.ExecuteCommandAsync(context, "get", args.ToArray());
            this.ChangeSet = result.OutputLines.Where(l => l.StartsWith("ChangeSet: ", StringComparison.OrdinalIgnoreCase)).FirstOrDefault()?.Substring(11).Trim();
            if (result.ExitCode != 0)
                this.LogError("Failed to get source");
            this.LogInformation("Get TFS source complete.");
        }
    }
}
