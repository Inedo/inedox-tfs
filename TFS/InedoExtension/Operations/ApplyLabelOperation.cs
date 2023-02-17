using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;

namespace Inedo.Extensions.TFS.Operations
{
    [DisplayName("TFS Label")]
    [Description("Labels a specified source path in TFS.")]
    [Tag("source-control")]
    [ScriptAlias("Tfs-Label")]
    [Example(@"
# labels the current application's path with the current package number
Tfs-ApplyLabel(
    Credentials: Hdars-Tfs,
    SourcePath: `$/$ApplicationName,
    Label: BM-$ReleaseName-$PackageNumber
);
")]
    [Serializable]
    public sealed class ApplyLabelOperation : TfsOperation
    {
        [ScriptAlias("Credentials")]
        [DisplayName("Credentials")]
        public override string CredentialName { get; set; }
        [ScriptAlias("SourcePath")]
        [DisplayName("Source path")]
        public string SourcePath { get; set; }
        [Required]
        [ScriptAlias("Label")]
        [DisplayName("Label")]
        public string Label { get; set; }
        [ScriptAlias("Comment")]
        [DisplayName("Comment")]
        [PlaceholderText("Label applied by BuildMaster")]
        public string Comment { get; set; }

        public override Task ExecuteAsync(IOperationExecutionContext context)
        {
            throw new NotImplementedException();
        }

        //        protected override Task<object> RemoteExecuteAsync(IRemoteOperationExecutionContext context)
        //        {
        //            this.LogInformation($"Apply label '{this.Label}' to '{this.SourcePath}'...");
        //#warning TfsTiny label
        //            //using (var client = new TfsSourceControlClient(this.TeamProjectCollectionUrl, this.UserName, this.PasswordOrToken, this.Domain, null))
        //            //{
        //            //    client.ApplyLabel(new TfsSourcePath(this.SourcePath), this.Label, AH.CoalesceString(this.Comment, "Label applied by BuildMaster"));
        //            //}

        //            this.LogInformation("Label applied.");

        //            return Complete;
        //        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription("Apply Label ", new Hilite(config[nameof(this.Label)])),
                new RichDescription("to ", new Hilite(config[nameof(this.SourcePath)]))
            );
        }
    }
}
