using System;
using System.Linq;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Artifacts;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Extensibility.BuildImporters;
using Inedo.BuildMaster.Files;
using Inedo.BuildMaster.Web;
using Inedo.Data;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;

namespace Inedo.BuildMasterExtensions.TFS.BuildImporter
{
    [BuildImporterProperties(
        "TFS",
        "Imports artifacts from TFS.",
        typeof(TfsBuildImporterTemplate))]
    [CustomEditor(typeof(TfsBuildImporterEditor))]
    public sealed class TfsBuildImporter : BuildImporterBase, ICustomBuildNumberProvider 
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

        public string BuildNumber { get; set; }

        public override void Import(IBuildImporterContext context)
        {
            var helper = new TfsHelper((TfsConfigurer)this.GetExtensionConfigurer());

            using (var collection = helper.GetTeamProjectCollection())
            {
                this.LogDebug("Searching for Build {0} for team project {1} definition {2}...", this.TfsBuildNumber, this.TeamProject, this.BuildDefinition);
                var build = helper.GetBuild(collection, this.TeamProject, this.BuildDefinition , this.TfsBuildNumber, this.IncludeUnsuccessful);
                if (build == null)
                {
                    this.LogError("Query did not return any builds.");
                    return;
                }

                this.LogInformation("Tfs Build Number: {0}", build.BuildNumber);
                this.LogInformation("Drop location: {0}", build.DropLocation);

                using (var agent = Util.Agents.CreateAgentFromId(context.ServerId))
                {
                    var fileOps = agent.GetService<IFileOperationsExecuter>();

                    this.LogDebug("Querying drop location...");
                    var directoryResult = Util.Files.GetDirectoryEntry(
                        new GetDirectoryEntryCommand
                        {
                            Path = build.DropLocation,
                            Recurse = true,
                            IncludeRootPath = true
                        }
                    ).Entry;

                    var matches = Util.Files.Comparison.GetMatches(build.DropLocation, directoryResult, new[] { "*" });
                    if (!matches.Any())
                    {
                        this.LogWarning("No files were found in the drop folder.");
                        return;
                    }

                    this.LogDebug("Creating {0} artifact...", this.ArtifactName);
                    var artifactId = new ArtifactIdentifier(context.ApplicationId, context.ReleaseNumber, context.BuildNumber, context.DeployableId, this.ArtifactName);
                    using (var artifact = new ArtifactBuilder(artifactId))
                    {
                        artifact.RootPath = build.DropLocation;

                        foreach (var match in matches)
                            artifact.Add(match, fileOps);

                        artifact.Commit();
                    }
                }

                this.LogDebug("Creating $TfsBuildNumber variable...");
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
                    Value_Text: build.BuildNumber,
                    Sensitive_Indicator: YNIndicator.No).Execute();
            }
        }

    }
}
