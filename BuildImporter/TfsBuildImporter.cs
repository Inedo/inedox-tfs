using System;
using System.Linq;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Artifacts;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Extensibility.BuildImporters;
using Inedo.BuildMaster.Files;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;

namespace Inedo.BuildMasterExtensions.TFS.BuildImporter
{
    [BuildImporterProperties(
        "TFS",
        "Imports artifacts from TFS.",
        typeof(TfsBuildImporterTemplate))]
    public sealed class TfsBuildImporter : BuildImporterBase
    {
        [Persistent]
        public string ArtifactName { get; set; }
        [Persistent]
        public string BuildDefinition { get; set; }
        [Persistent]
        public string TeamProject { get; set; }
        [Persistent]
        public string[] FileMasks { get; set; }

        public override void Import(IBuildImporterContext context)
        {
            using (var collection = this.GetTeamProjectCollection())
            {
                var buildService = collection.GetService<IBuildServer>();

                var spec = buildService.CreateBuildDetailSpec(this.TeamProject, string.IsNullOrEmpty(this.BuildDefinition) ? "*" : this.BuildDefinition);
                spec.BuildNumber = context.BuildNumber;
                spec.MaxBuildsPerDefinition = 1;
                spec.QueryOrder = BuildQueryOrder.FinishTimeDescending;
                spec.Status = BuildStatus.All;

                var result = buildService.QueryBuilds(spec);
                var build = result.Builds.FirstOrDefault();
                if (build == null)
                    throw new InvalidOperationException(string.Format("Build {0} for team project {1} definition {2} did not return any builds.", context.BuildNumber, this.TeamProject, this.BuildDefinition));

                this.LogDebug("Build number {0} drop location: {1}", build.BuildNumber, build.DropLocation);

                using (var agent = Util.Agents.CreateAgentFromId(context.ServerId))
                {
                    var fileOps = agent.GetService<IFileOperationsExecuter>();

                    var directoryResult = Util.Files.GetDirectoryEntry(
                        new GetDirectoryEntryCommand
                        {
                            Path = build.DropLocation,
                            Recurse = true,
                            IncludeRootPath = true
                        }
                    ).Entry;

                    var matches = Util.Files.Comparison.GetMatches(build.DropLocation, directoryResult, this.FileMasks);

                    var artifactId = new ArtifactIdentifier(context.ApplicationId, context.ReleaseNumber, context.BuildNumber, context.DeployableId, this.ArtifactName);
                    using (var artifact = new ArtifactBuilder(artifactId))
                    {
                        artifact.RootPath = build.DropLocation;

                        foreach (var match in matches)
                            artifact.Add(match, fileOps);

                        artifact.Commit();
                    }
                }
            }
        }

        private TfsTeamProjectCollection GetTeamProjectCollection()
        {
            return TfsActionBase.GetTeamProjectCollection((TfsConfigurer)this.GetExtensionConfigurer());
        }
    }
}
