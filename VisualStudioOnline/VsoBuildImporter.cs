using Inedo.BuildMaster;
using Inedo.BuildMaster.Artifacts;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.BuildImporters;
using Inedo.BuildMaster.Web;
using Inedo.Data;
using Inedo.Diagnostics;

namespace Inedo.BuildMasterExtensions.TFS.VisualStudioOnline
{
    [BuildImporterProperties(
        "Visual Studio Online",
        "Downloads and imports artifacts from Visual Studio Online.",
        typeof(VsoBuildImporterTemplate))]
    [CustomEditor(typeof(VsoBuildImporterEditor))]
    public sealed class VsoBuildImporter : BuildImporterBase, ICustomBuildNumberProvider
    {
        [Persistent]
        public string ArtifactName { get; set; }
        [Persistent]
        public string BuildDefinition { get; set; }
        [Persistent]
        public string TeamProject { get; set; }
        [Persistent]
        public string TfsBuildNumber { get; set; }
        [Persistent]
        public bool IncludeUnsuccessful { get; set; }
        [Persistent]
        public bool CreateBuildNumberVariable { get; set; }

        public string BuildNumber { get; set; }

        public override void Import(IBuildImporterContext context)
        {
            var configurer = (TfsConfigurer)this.GetExtensionConfigurer();

            string buildNumber = VsoArtifactImporter.DownloadAndImport(
                configurer,
                this,
                this.TeamProject,
                this.TfsBuildNumber,
                new ArtifactIdentifier(context.ApplicationId, context.ReleaseNumber, context.BuildNumber, context.DeployableId, this.ArtifactName)
            );

            if (this.CreateBuildNumberVariable)
            {
                this.LogDebug($"Setting $TfsBuildNumber build variable to {buildNumber}...");
                StoredProcs.Variables_CreateOrUpdateVariableDefinition(
                    Variable_Name: "TfsBuildNumber",
                    Environment_Id: null,
                    Server_Id: null,
                    ApplicationGroup_Id: null,
                    Application_Id: context.ApplicationId,
                    Deployable_Id: null,
                    Release_Number: context.ReleaseNumber,
                    Build_Number: context.BuildNumber,
                    Execution_Id: null,
                    Value_Text: buildNumber,
                    Sensitive_Indicator: YNIndicator.No
                ).Execute();

                this.LogInformation("$TfsBuildNumber build variable set to: " + buildNumber);
            }
        }
    }
}
