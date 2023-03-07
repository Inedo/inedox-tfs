using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using Inedo.Diagnostics;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Services.Common;
using WindowsCredential = Microsoft.VisualStudio.Services.Common.WindowsCredential;

namespace Inedo.TFS.Clients.SourceControl
{
    public sealed class TfsSourceControlClient : IDisposable
    {
        private readonly TfsTeamProjectCollection collection;
        private readonly ILogSink log;

        public TfsSourceControlClient(string projectCollectionUrl, string userName, string password, string domain, ILogSink log)
        {
            var uri = new Uri(projectCollectionUrl, UriKind.Absolute);

            if (string.IsNullOrEmpty(userName))
            {
                this.collection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(uri);
            }
            else
            {
                VssCredentials credentials;
                if (string.IsNullOrEmpty(domain))
                    credentials = new VssCredentials(new VssBasicCredential(userName, password));
                else
                    credentials = new VssCredentials(new WindowsCredential(new NetworkCredential(userName, password, domain)));

                this.collection = new TfsTeamProjectCollection(uri, credentials);
            }

            this.log = log;
        }

        public void GetSource(TfsSourcePath sourcePath, WorkspaceInfo workspaceInfo, string targetDirectory, string label = null, bool verbose = false)
        {
            this.collection.EnsureAuthenticated();

            var versionControlServer = this.collection.GetService<VersionControlServer>();
            if (verbose)
            {
                this.log.LogDebug("Creating workspace...");
            }
            using var workspace = MappedWorkspace.Create(workspaceInfo, versionControlServer, sourcePath, this.log);
            var versionSpec = label == null
                ? VersionSpec.Latest
                : VersionSpec.ParseSingleSpec("L" + label, versionControlServer.AuthorizedUser);
            if (verbose)
            {
                this.log.LogDebug("Workspace created");
                this.log.LogDebug("Pulling source...");
            }
            workspace.Workspace.Get(new GetRequest(new ItemSpec(sourcePath.AbsolutePath, RecursionType.Full), versionSpec), GetOptions.Overwrite);
            if (verbose)
                this.log.LogDebug($"Copying source to target directory \"{targetDirectory}\"...");
            CopyNonTfsFiles(workspace.DiskPath, targetDirectory, verbose);
            if (verbose)
                this.log.LogDebug($"Copied to target directory");

        }

        public void ApplyLabel(TfsSourcePath path, string label, string comment)
        {
            this.collection.EnsureAuthenticated();

            var versionControlService = this.collection.GetService<VersionControlServer>();

            var versionControlLabel = new VersionControlLabel(versionControlService, label, versionControlService.AuthorizedUser, path.AbsolutePath, comment);
            var results = versionControlService.CreateLabel(versionControlLabel, new[] { new LabelItemSpec(new ItemSpec(path.AbsolutePath, RecursionType.Full), VersionSpec.Latest, false) }, LabelChildOption.Replace, out Failure[] failures);
            foreach(var failure in failures)
            {
                log.LogError(failure.GetFormattedMessage());
            }
            foreach (var result in results)
            {
                this.log.LogDebug($"{result.Status} \"{result.Label}\" on the scope \"{result.Scope}\".");
            }
        }

        public void Dispose()
        {
            this.collection?.Dispose();
        }

        private void CopyNonTfsFiles(string sourceDir, string targetDir, bool verbose)
        {
            if (!Directory.Exists(sourceDir))
                return;
            if (verbose)
                this.log.LogDebug($"Creating target directory \"{targetDir}\"");
            Directory.CreateDirectory(targetDir);

            var sourceDirInfo = new DirectoryInfo(sourceDir);

            foreach (var file in sourceDirInfo.GetFiles())
            {
                var targetFile = Path.Combine(targetDir, file.Name);
                if (verbose)
                    this.log.LogDebug($"Copying file \"{file.FullName}\" to \"{targetFile}\"");
                file.CopyTo(targetFile, true);
            }

            foreach (var subDir in sourceDirInfo.GetDirectories().Where(d => d.Name != "$tf"))
            {
                var targetSubDir = Path.Combine(targetDir, subDir.Name);
                if (verbose)
                    this.log.LogDebug($"Copying contents of \"{subDir.FullName}\" to \"{targetSubDir}\"");
                CopyNonTfsFiles(subDir.FullName, targetSubDir, verbose);
            }
        }
    }
}
