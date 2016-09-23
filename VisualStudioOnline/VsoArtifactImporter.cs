using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Inedo.BuildMaster.Artifacts;
using Inedo.Diagnostics;
using Inedo.IO;

namespace Inedo.BuildMasterExtensions.TFS.VisualStudioOnline
{
    internal static class VsoArtifactImporter
    {
        public static async Task<string> DownloadAndImportAsync(IVsoConnectionInfo connectionInfo, ILogger logger, string teamProject, string buildNumber, string buildDefinitionName, ArtifactIdentifier artifactId)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            if (connectionInfo == null)
                throw new ArgumentNullException("A configurer must be configured or selected in order to import a VS online build.");
            if (string.IsNullOrEmpty(connectionInfo.TeamProjectCollectionUrl))
                throw new InvalidOperationException("The base URL property of the TFS configurer or the Url property of the Import VSO Artifact operation must be set to import a VS online build.");

            var api = new TfsRestApi(connectionInfo.TeamProjectCollectionUrl, teamProject)
            {
                UserName = string.IsNullOrEmpty(connectionInfo.Domain) ? connectionInfo.UserName : string.Format("{0}\\{1}", connectionInfo.Domain, connectionInfo.UserName),
                Password = connectionInfo.PasswordOrToken
            };

            logger.LogInformation($"Finding last successful build...");
            var buildDefinitions = await api.GetBuildDefinitionsAsync().ConfigureAwait(false);

            var buildDefinition = buildDefinitions.FirstOrDefault(b => b.name == buildDefinitionName);

            if (buildDefinition == null)
            {
                throw new InvalidOperationException($"The build definition {buildDefinitionName} could not be found.");
            }

            logger.LogInformation($"Finding {AH.CoalesceString(buildNumber, "last successful")} build...");

            var builds = await api.GetBuildsAsync(
                buildDefinition: buildDefinition.id,
                buildNumber: AH.NullIf(buildNumber, ""),
                resultFilter: "succeeded",
                statusFilter: "completed",
                top: 2
            ).ConfigureAwait(false);

            if (builds.Length == 0)
                throw new InvalidOperationException($"Could not find build number {buildNumber}. Ensure there is a successful, completed build with this number.");

            var build = builds.FirstOrDefault();

            string tempFile = Path.GetTempFileName();
            try
            {
                logger.LogInformation($"Downloading {artifactId.ArtifactName} artifact from VSO...");
                logger.LogDebug("Downloading artifact file to: " + tempFile);
                await api.DownloadArtifactAsync(build.id, artifactId.ArtifactName, tempFile).ConfigureAwait(false);
                logger.LogInformation("Artifact file downloaded from VSO, importing into BuildMaster artifact library...");

                using (var stream = FileEx.Open(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    await Artifact.CreateArtifactAsync(
                        applicationId: artifactId.ApplicationId,
                        releaseNumber: artifactId.ReleaseNumber,
                        buildNumber: artifactId.BuildNumber,
                        deployableId: artifactId.DeployableId,
                        executionId: null,
                        artifactName: artifactId.ArtifactName,
                        artifactData: stream,
                        overwrite: true
                    ).ConfigureAwait(false);
                }

                logger.LogInformation($"{artifactId.ArtifactName} artifact imported.");

                return build.buildNumber;
            }
            finally
            {
                if (tempFile != null)
                    FileEx.Delete(tempFile);
            }
        }
    }
}
