using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Artifacts;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMasterExtensions.TFS.VisualStudioOnline;
using Inedo.Diagnostics;
using Inedo.Documentation;

namespace Inedo.BuildMasterExtensions.TFS.Operations
{
    [DisplayName("Import Artifact from TFS2015 or VSO")]
    [Description("Downloads an artifact from the specified TFS server or Visual Studio Online and saves it to the artifact library.")]
    [ScriptAlias("Import-Artifact")]
    [Tag(Tags.Artifacts)]
    public sealed class ImportVsoArtifactOperation : TfsOperation
    {
        [ScriptAlias("BuildNumber")]
        [DisplayName("Build number")]
        [PlaceholderText("latest")]
        public string BuildNumber { get; set; }

        [Required]
        [ScriptAlias("ArtifactName")]
        [DisplayName("Artifact name")]
        public string ArtifactName { get; set; }

        [ScriptAlias("TeamProject")]
        [DisplayName("Team project")]
        public string TeamProject { get; set; }

        [Required]
        [ScriptAlias("BuildDefinition")]
        [DisplayName("Build definition")]
        public string BuildDefinition { get; set; }        

        [Category("Advanced")]
        [ScriptAlias("CreateBuildNumberVariable")]
        [DisplayName("Create $TfsBuildNumber")]
        [DefaultValue(true)]
        public bool CreateBuildNumberVariable { get; set; } = true;

        public async override Task ExecuteAsync(IOperationExecutionContext context)
        {
            this.LogInformation($"Importing {this.ArtifactName} artifact with build number \"{this.BuildNumber ?? "latest"}\" from TFS...");

            string buildNumber = await VsoArtifactImporter.DownloadAndImportAsync(
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
