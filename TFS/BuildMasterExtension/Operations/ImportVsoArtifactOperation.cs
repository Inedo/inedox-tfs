using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Artifacts;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMasterExtensions.TFS.SuggestionProviders;
using Inedo.BuildMasterExtensions.TFS.VisualStudioOnline;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensions.TFS;
using Inedo.Extensions.TFS.Operations;
using Inedo.Extensions.TFS.SuggestionProviders;

namespace Inedo.BuildMasterExtensions.TFS.Operations
{
    [DisplayName("Import Artifact from TFS2015 or VSO")]
    [Description("Downloads an artifact from the specified TFS server or Visual Studio Online and saves it to the artifact library.")]
    [ScriptAlias("Import-Artifact")]
    [Tag(Tags.Artifacts)]
    [Tag("tfs")]
    public sealed class ImportVsoArtifactOperation : TfsOperation
    {
        [ScriptAlias("Credentials")]
        [DisplayName("Credentials")]
        public override string CredentialName { get; set; }

        [ScriptAlias("TeamProject")]
        [DisplayName("Team project")]
        [SuggestibleValue(typeof(TeamProjectNameSuggestionProvider))]
        public string TeamProject { get; set; }

        [Required]
        [ScriptAlias("BuildDefinition")]
        [DisplayName("Build definition")]
        [SuggestibleValue(typeof(BuildDefinitionNameSuggestionProvider))]
        public string BuildDefinition { get; set; }        

        [ScriptAlias("BuildNumber")]
        [DisplayName("Build number")]
        [PlaceholderText("latest")]
        [SuggestibleValue(typeof(BuildNumberSuggestionProvider))]
        public string BuildNumber { get; set; }

        [Required]
        [ScriptAlias("ArtifactName")]
        [DisplayName("Artifact name")]
        [SuggestibleValue(typeof(ArtifactNameSuggestionProvider))]
        public string ArtifactName { get; set; }

        [Output]
        [ScriptAlias("TfsBuildNumber")]
        [DisplayName("Set build number to variable")]
        [Description("The TFS build number can be output into a runtime variable.")]
        [PlaceholderText("e.g. $TfsBuildNumber")]
        public string TfsBuildNumber { get; set; }

        public async override Task ExecuteAsync(IOperationExecutionContext context)
        {
            this.LogInformation($"Importing {this.ArtifactName} artifact with build number \"{this.BuildNumber ?? "latest"}\" from TFS...");

            this.TfsBuildNumber = await VsoArtifactImporter.DownloadAndImportAsync(
                (IVsoConnectionInfo)this,
                (ILogger)this,
                this.TeamProject,
                this.BuildNumber,
                this.BuildDefinition,
                new ArtifactIdentifier((int)context.ApplicationId, context.ReleaseNumber, context.BuildNumber, context.DeployableId, this.ArtifactName)
            );

            this.LogInformation("Import complete.");
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            var shortDesc = new RichDescription("Import VSO ", new Hilite(config[nameof(this.ArtifactName)]), " Artifact");

            var longDesc = new RichDescription("from ", new Hilite(config[nameof(this.TeamProject)]), " using ");
            if (string.IsNullOrEmpty(config[nameof(this.BuildNumber)]))
                longDesc.AppendContent("the last successful build");
            else
                longDesc.AppendContent("build ", new Hilite(config[nameof(this.BuildNumber)]));

            longDesc.AppendContent(" of ");

            if (string.IsNullOrEmpty(config[nameof(this.BuildDefinition)]))
                longDesc.AppendContent("any build definition");
            else
                longDesc.AppendContent("build definition ", new Hilite(config[nameof(this.BuildDefinition)]));

            longDesc.AppendContent(".");

            return new ExtendedRichDescription(shortDesc, longDesc);
        }
    }
}
