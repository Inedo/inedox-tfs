using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Extensions.TFS;
using Inedo.Extensions.TFS.Clients;

namespace Inedo.TFS.VisualStudioOnline
{
    internal static class VsoArtifactImporter
    {
        public static async Task<string> DownloadAndImportAsync(IVsoConnectionInfo connectionInfo, ILogSink logger, string teamProject, string buildNumber, string buildDefinitionName, dynamic context, string artifactName)
        {
            var downloader = new ArtifactDownloader(connectionInfo, logger);

            using (var artifact = await downloader.DownloadAsync(teamProject, buildNumber, buildDefinitionName, artifactName).ConfigureAwait(false))
            {
                logger.LogInformation("Downloading artifact file from VSO and importing into BuildMaster artifact library...");

                await SDK.CreateArtifactAsync(
                    applicationId: (int)context.ApplicationId,
                    releaseNumber: context.ReleaseNumber,
                    buildNumber: context.BuildNumber,
                    deployableId: context.DeployableId,
                    executionId: null,
                    artifactName: artifact.Name,
                    artifactData: artifact.Content,
                    overwrite: true
                ).ConfigureAwait(false);

                logger.LogInformation($"{artifact.Name} artifact imported.");

                return artifact.BuildNumber;
            }
        }
    }
}
