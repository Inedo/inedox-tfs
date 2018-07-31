using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.TFS;
using Inedo.Extensions.TFS.Operations;
using Inedo.Extensions.TFS.SuggestionProviders;
using Inedo.TFS.VisualStudioOnline;
using Inedo.Web;

namespace Inedo.BuildMasterExtensions.TFS.Operations
{
    [DisplayName("Import Artifact from TFS2015 or VSO")]
    [Description("Downloads an artifact from the specified TFS server or Visual Studio Online and saves it to the artifact library.")]
    [ScriptAlias("Import-Artifact")]
    [Tag("artifacts")]
    [Tag("tfs")]
    [AppliesTo(InedoProduct.BuildMaster)]
    public sealed class ImportVsoArtifactOperation : TfsOperation
    {
        [ScriptAlias("Credentials")]
        [DisplayName("Credentials")]
        public override string CredentialName { get; set; }

        [ScriptAlias("TeamProject")]
        [DisplayName("Team project")]
        [SuggestableValue(typeof(TeamProjectNameSuggestionProvider))]
        public string TeamProject { get; set; }

        [Required]
        [ScriptAlias("BuildDefinition")]
        [DisplayName("Build definition")]
        [SuggestableValue(typeof(BuildDefinitionNameSuggestionProvider))]
        public string BuildDefinition { get; set; }        

        [ScriptAlias("BuildNumber")]
        [DisplayName("Build number")]
        [PlaceholderText("latest")]
        [SuggestableValue(typeof(BuildNumberSuggestionProvider))]
        public string BuildNumber { get; set; }

        [Required]
        [ScriptAlias("ArtifactName")]
        [DisplayName("Artifact name")]
        [SuggestableValue(typeof(ArtifactNameSuggestionProvider))]
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
                this,
                this.TeamProject,
                this.BuildNumber,
                this.BuildDefinition,
                context,
                this.ArtifactName
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
