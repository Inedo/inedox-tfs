using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Extensions.TFS.Clients.Rest;

namespace Inedo.Extensions.TFS.Clients
{
    internal sealed class ArtifactDownloader
    {
        private IVsoConnectionInfo connectionInfo;
        private ILogger logger;

        public ArtifactDownloader(IVsoConnectionInfo connectionInfo, ILogger log)
        {
            if (connectionInfo == null)
                throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(connectionInfo.TeamProjectCollectionUrl))
                throw new InvalidOperationException("The base URL property of the TFS credentials or the Url property of the Import/Download VSO Artifact operation must be set.");

            this.connectionInfo = connectionInfo;
            this.logger = log ?? Logger.Null;
        }

        public async Task<TfsArtifact> DownloadAsync(string teamProject, string buildNumber, string buildDefinitionName, string artifactName)
        {
            if (string.IsNullOrEmpty(teamProject))
                throw new ArgumentException("A team project is required to download the artifact.", nameof(teamProject));
            if (string.IsNullOrEmpty(artifactName))
                throw new ArgumentException("An artifact name is required to download the artifact.", nameof(artifactName));

            var api = new TfsRestApi(connectionInfo, logger);

            var buildDefinition = await api.GetBuildDefinitionAsync(teamProject, buildDefinitionName).ConfigureAwait(false);
            if (buildDefinition == null)
                throw new InvalidOperationException($"The build definition {buildDefinitionName} could not be found.");

            logger.LogInformation($"Finding {AH.CoalesceString(buildNumber, "last successful")} build...");

            var builds = await api.GetBuildsAsync(
                project: teamProject,
                buildDefinition: buildDefinition.id,
                buildNumber: AH.NullIf(buildNumber, ""),
                resultFilter: "succeeded",
                statusFilter: "completed",
                top: 2
            ).ConfigureAwait(false);

            if (builds.Length == 0)
                throw new InvalidOperationException($"Could not find build number {buildNumber}. Ensure there is a successful, completed build with this number.");

            var build = builds.FirstOrDefault();
            
            logger.LogInformation($"Downloading {artifactName} artifact from VSO...");

            var stream = await api.DownloadArtifactAsync(teamProject, build.id, artifactName).ConfigureAwait(false);

            return new TfsArtifact(stream, artifactName, buildNumber);
        }
    }

    internal sealed class TfsArtifact : IDisposable
    {
        public TfsArtifact(Stream content, string name, string buildNumber)
        {
            this.Content = content;
            this.Name = name;
            this.BuildNumber = buildNumber;
        }

        public Stream Content { get; }
        public string Name { get; }
        public string BuildNumber { get; }
        public string FileName => this.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? this.Name : (this.Name + ".zip");

        public void Dispose() => this.Content?.Dispose();
    }
}
