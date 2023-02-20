using System;
using System.IO;
using System.Linq;
using System.Net;
using Inedo.Diagnostics;
using Inedo.IO;
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

        public void GetSource(TfsSourcePath sourcePath, WorkspaceInfo workspaceInfo, string targetDirectory, string label = null)
        {
            this.collection.EnsureAuthenticated();

            var versionControlServer = this.collection.GetService<VersionControlServer>();

            using var workspace = MappedWorkspace.Create(workspaceInfo, versionControlServer, sourcePath, this.log);
            var versionSpec = label == null
                ? VersionSpec.Latest
                : VersionSpec.ParseSingleSpec("L" + label, versionControlServer.AuthorizedUser);

            workspace.Workspace.Get(new GetRequest(new ItemSpec(sourcePath.AbsolutePath, RecursionType.Full), versionSpec), GetOptions.Overwrite);

            CopyNonTfsFiles(workspace.DiskPath, targetDirectory);
        }

        public void ApplyLabel(TfsSourcePath path, string label, string comment)
        {
            this.collection.EnsureAuthenticated();

            var versionControlService = this.collection.GetService<VersionControlServer>();

            var versionControlLabel = new VersionControlLabel(versionControlService, label, versionControlService.AuthorizedUser, path.AbsolutePath, comment);
            var results = versionControlService.CreateLabel(versionControlLabel, new[] { new LabelItemSpec(new ItemSpec(path.AbsolutePath, RecursionType.Full), VersionSpec.Latest, false) }, LabelChildOption.Replace);
        }

        public void Dispose()
        {
            this.collection?.Dispose();
        }

        private static void CopyNonTfsFiles(string sourceDir, string targetDir)
        {
            if (!DirectoryEx.Exists(sourceDir))
                return;

            DirectoryEx.Create(targetDir);

            var sourceDirInfo = new DirectoryInfo(sourceDir);

            foreach (var file in sourceDirInfo.GetFiles())
            {
                file.CopyTo(PathEx.Combine(targetDir, file.Name), true);
            }

            foreach (var subDir in sourceDirInfo.GetDirectories().Where(d => d.Name != "$tf"))
            {
                CopyNonTfsFiles(subDir.FullName, PathEx.Combine(targetDir, subDir.Name));
            }
        }
    }
}
