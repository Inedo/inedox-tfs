using System;
using System.IO;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Artifacts;
using Inedo.Diagnostics;
using Inedo.IO;
using System.Linq;

namespace Inedo.BuildMasterExtensions.TFS.VisualStudioOnline
{
    internal static class VsoArtifactImporter
    {
        /// <summary>
        /// Downloads and imports and artifact from Visual Studio Online.
        /// </summary>
        /// <param name="configurer">The configurer.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="teamProject">The team project.</param>
        /// <param name="buildNumber">The build number.</param>
        /// <param name="artifactId">The artifact identifier.</param>
        public static string DownloadAndImport(TfsConfigurer configurer, ILogger logger, string teamProject, string buildNumber, string buildDefinitionName, ArtifactIdentifier artifactId)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            if (configurer == null)
                throw new ArgumentNullException("A configurer must be configured or selected in order to import a VS online build.");
            if (string.IsNullOrEmpty(configurer.BaseUrl))
                throw new InvalidOperationException("The base URL property of the TFS configurer must be set to import a VS online build.");

            var api = new TfsRestApi(configurer.BaseUrl, teamProject)
            {
                UserName = string.IsNullOrEmpty(configurer.Domain) ? configurer.UserName : string.Format("{0}\\{1}", configurer.Domain, configurer.UserName),
                Password = configurer.Password
            };

            logger.LogInformation($"Finding last successful build...");
            var buildDefinitions = api.GetBuildDefinitions();
            
            var buildDefinition = buildDefinitions.FirstOrDefault(b => b.name == buildDefinitionName);
            
            if (buildDefinition == null)
               {
                throw new InvalidOperationException($"The build definition {buildDefinitionName} could not be found.");
               }
            
            logger.LogInformation($"Finding {Util.CoalesceStr(buildNumber, "last successful")} build...");

            var builds = api.GetBuilds(
                buildNumber: InedoLib.Util.NullIf(buildNumber, ""),
                resultFilter: "succeeded",
                statusFilter: "completed"
                //,top: 2
            );

            if (builds.Length == 0)
                throw new InvalidOperationException($"Could not find build number {buildNumber}. Ensure there is a successful, completed build with this number.");
            

            var build = builds.FirstOrDefault(b => b.definition.id == buildDefinition.id);

            string tempFile = Path.GetTempFileName();
            try
            {
                logger.LogInformation($"Downloading {artifactId.ArtifactName} artifact from VSO...");
                logger.LogDebug("Downloading artifact file to: " + tempFile);
                api.DownloadArtifact(build.id, artifactId.ArtifactName, tempFile);
                logger.LogInformation("Artifact file downloaded from VSO, importing into BuildMaster artifact library...");

                using (var stream = FileEx.Open(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    ArtifactBuilder.ImportZip(artifactId, stream);
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
