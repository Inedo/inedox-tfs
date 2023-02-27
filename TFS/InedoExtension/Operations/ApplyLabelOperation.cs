using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.TFS.Credentials;
using Inedo.Web;

namespace Inedo.Extensions.TFS.Operations
{
    [DisplayName("TFS Label")]
    [Description("Labels a specified source path in TFS.")]
    [Tag("source-control")]
    [ScriptAlias("Tfs-Label")]
    [Example(@"
# labels the current application's path with the current package number
Tfs-ApplyLabel(
    From: Hdars-Tfs,
    SourcePath: `$/$ApplicationName,
    Label: BM-$ReleaseName-$PackageNumber
);
")]
    [Serializable]
    public sealed class ApplyLabelOperation : TfsOperation
    {
        [ScriptAlias("From")]
        [ScriptAlias("Credentials")]
        [DisplayName("From TFS Resource")]
        [SuggestableValue(typeof(SecureResourceSuggestionProvider<TfsSecureResource>))]
        public override string ResourceName { get; set; }
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

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            this.LogInformation($"Apply label '{this.Label}' to '{AH.NullIf(this.SourcePath, string.Empty) ?? "$/"}'...");
            var args = new List<TfsArg>();
            if (!string.IsNullOrWhiteSpace(this.SourcePath))
                args.Add(new ("--source", this.SourcePath, true, false));
            
            if (!string.IsNullOrWhiteSpace(this.Label))
                args.Add(new ("--label", this.Label, true, false));

            args.Add(new("--comment", AH.CoalesceString(this.Comment, "Label applied by BuildMaster"), true, false));

            var result = await this.ExecuteCommandAsync(context, "label", args.ToArray());
            if (result.ExitCode != 0)
                this.LogError("Failed apply label");
            this.LogInformation("Label applied.");
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription("Apply Label ", new Hilite(config[nameof(this.Label)])),
                new RichDescription("to ", new Hilite(string.IsNullOrWhiteSpace(config[nameof(this.SourcePath)]) ? "$/" : config[nameof(this.SourcePath)]))
            );
        }
    }
}
