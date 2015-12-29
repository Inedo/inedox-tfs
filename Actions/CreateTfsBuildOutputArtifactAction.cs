using System;
using System.IO;
using System.Linq;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Artifacts;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Files;
using Inedo.BuildMaster.Web;
using Microsoft.TeamFoundation.Build.Client;

namespace Inedo.BuildMasterExtensions.TFS
{
    [ActionProperties(
        "Capture Artifact from TFS Build Output",
        "Creates a BuildMaster build artifact from a TFS build server drop location.",
        DefaultToLocalServer = true)]
    [RequiresInterface(typeof(IFileOperationsExecuter))]
    [RequiresInterface(typeof(IRemoteZip))]
    [CustomEditor(typeof(CreateTfsBuildOutputArtifactActionEditor))]
    [Tag(Tags.Artifacts)]
    [Tag(Tags.Builds)]
    [Tag("tfs")]
    public sealed class CreateTfsBuildOutputArtifactAction : TfsActionBase
    {
        /// <summary>
        /// Gets or sets the build number if not empty, or includes all builds in the search.
        /// </summary>
        [Persistent]
        public string BuildNumber { get; set; }

        /// <summary>
        /// Gets or sets the name of the artifact if not empty, otherwise use the build definition name.
        /// </summary>
        [Persistent]
        public string ArtifactName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the build spec should include unsuccessful builds.
        /// </summary>
        [Persistent]
        public bool IncludeUnsuccessful { get; set; }

        public override ActionDescription GetActionDescription()
        {
            var shortDesc = new ShortActionDescription("Capture TFS Build Artifact from ", new Hilite(this.TeamProject));

            var longDesc = new LongActionDescription("using ");
            if (string.IsNullOrEmpty(this.BuildNumber))
                longDesc.AppendContent("the last successful build");
            else
                longDesc.AppendContent("build ", new Hilite(this.BuildNumber));

            longDesc.AppendContent(" of ");

            if (string.IsNullOrEmpty(this.BuildDefinition))
                longDesc.AppendContent("any build definition");
            else
                longDesc.AppendContent("build definition ", new Hilite(this.BuildDefinition));

            longDesc.AppendContent(".");

            return new ActionDescription(shortDesc, longDesc);
        }

        protected override void Execute()
        {
            var collection = this.GetTeamProjectCollection();

            var buildService = collection.GetService<IBuildServer>();            

            var spec = buildService.CreateBuildDetailSpec(this.TeamProject, string.IsNullOrEmpty(this.BuildDefinition) ? "*" : this.BuildDefinition);
            if (!string.IsNullOrEmpty(this.BuildNumber))
                spec.BuildNumber = this.BuildNumber;
            spec.MaxBuildsPerDefinition = 1;
            spec.QueryOrder = BuildQueryOrder.FinishTimeDescending;
            spec.Status = BuildStatus.Succeeded;

            if (this.IncludeUnsuccessful)
                spec.Status |= (BuildStatus.Failed | BuildStatus.PartiallySucceeded);

            var result = buildService.QueryBuilds(spec);
            var build = result.Builds.FirstOrDefault();
            if (build == null)
                throw new InvalidOperationException($"Build {this.BuildNumber} for team project {this.TeamProject} definition {this.BuildDefinition} did not return any builds.");

            this.LogDebug($"Build number {build.BuildNumber} drop location: {build.DropLocation}");

            CreateArtifact(string.IsNullOrEmpty(this.ArtifactName) ? build.BuildDefinition.Name : this.ArtifactName, build.DropLocation);
        }

        private void CreateArtifact(string artifactName, string path)
        {
            if (string.IsNullOrEmpty(artifactName) || artifactName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new InvalidOperationException("Artifact Name cannot contain invalid file name characters: " + new string(Path.GetInvalidFileNameChars()));

            if (StoredProcs.Releases_GetRelease(this.Context.ApplicationId, this.Context.ReleaseNumber)
                .Execute().ReleaseDeployables_Extended
                .Any(rd => rd.Deployable_Id == this.Context.DeployableId && rd.InclusionType_Code == Domains.DeployableInclusionTypes.Referenced))
            {
                this.LogError(
                    "An Artifact cannot be created for this Deployable because the Deployable is Referenced (as opposed to Included) by this Release. " +
                    "To prevent this error, either include this Deployable in the Release or use a Predicate to prevent this action group from being executed.");
                return;
            }

            var fileOps = this.Context.Agent.GetService<IFileOperationsExecuter>();
            var zipPath = fileOps.CombinePath(this.Context.TempDirectory, artifactName + ".zip");

            this.LogDebug("Preparing directories...");
            fileOps.DeleteFiles(new[] { zipPath });

            this.ThrowIfCanceledOrTimeoutExpired();

            var rootEntry = fileOps.GetDirectoryEntry(
                new GetDirectoryEntryCommand
                {
                    Path = path,
                    Recurse = false,
                    IncludeRootPath = false
                }
            ).Entry;

            if ((rootEntry.Files == null || rootEntry.Files.Length == 0) && (rootEntry.SubDirectories == null || rootEntry.SubDirectories.Length == 0))
                this.LogWarning("There are no files to capture in this artifact.");

            this.LogDebug("Zipping output...");
            this.Context.Agent.GetService<IRemoteZip>().CreateZipFile(path, zipPath);

            var zipFileEntry = fileOps.GetFileEntry(zipPath);

            this.ThrowIfCanceledOrTimeoutExpired();

            this.LogDebug("Transferring file to artifact library...");

            var artifactId = new ArtifactIdentifier(this.Context.ApplicationId, this.Context.ReleaseNumber, this.Context.BuildNumber, this.Context.DeployableId, artifactName);

            ArtifactBuilder.ImportZip(artifactId, fileOps, zipFileEntry);

            this.LogDebug("Cleaning up...");
            fileOps.DeleteFiles(new[] { zipPath });

            this.LogInformation("Artfact captured from TFS.");
        }
    }
}
