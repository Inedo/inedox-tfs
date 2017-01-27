using System.Threading.Tasks;
using Inedo.BuildMaster.Artifacts;
using Inedo.Diagnostics;
using Inedo.Extensions.TFS;
using Inedo.Extensions.TFS.Clients;

namespace Inedo.BuildMasterExtensions.TFS.VisualStudioOnline
{
    internal static class VsoArtifactImporter
    {
        public static async Task<string> DownloadAndImportAsync(IVsoConnectionInfo connectionInfo, ILogger logger, string teamProject, string buildNumber, string buildDefinitionName, ArtifactIdentifier artifactId)
        {
            var downloader = new ArtifactDownloader(connectionInfo, logger);

            using (var artifact = await downloader.DownloadAsync(teamProject, buildNumber, buildDefinitionName, artifactId.ArtifactName).ConfigureAwait(false))
            {
                logger.LogInformation("Downloading artifact file from VSO and importing into BuildMaster artifact library...");

                await Artifact.CreateArtifactAsync(
                    applicationId: artifactId.ApplicationId,
                    releaseNumber: artifactId.ReleaseNumber,
                    buildNumber: artifactId.BuildNumber,
                    deployableId: artifactId.DeployableId,
                    executionId: null,
                    artifactName: artifact.Name,
                    artifactData: artifact.Content,
                    overwrite: true
                ).ConfigureAwait(false);

                logger.LogInformation($"{artifactId.ArtifactName} artifact imported.");

                return artifact.BuildNumber;
            }
        }
    }
}
