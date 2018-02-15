using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Inedo.Diagnostics;
using Inedo.IO;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Inedo.Extensions.TFS.Clients.SourceControl
{
    internal sealed class TfsSourceControlClient : IDisposable
    {
        private TfsTeamProjectCollection collection;
        private ILogSink log;

        public TfsSourceControlClient(string projectCollectionUrl, string userName, string password, string domain, ILogSink log)
        {
            var uri = new Uri(projectCollectionUrl, UriKind.Absolute);

            if (string.IsNullOrEmpty(userName))
            {
                this.collection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(uri);
            }
            else
            {
                TfsClientCredentials credentials;
                if (string.IsNullOrEmpty(domain))
                    credentials = new TfsClientCredentials(new BasicAuthCredential(new NetworkCredential(userName, password)));
                else
                    credentials = new TfsClientCredentials(new WindowsCredential(new NetworkCredential(userName, password, domain)));

                this.collection = new TfsTeamProjectCollection(uri, credentials);
            }

            this.log = log;
        }

        public void GetSource(TfsSourcePath sourcePath, WorkspaceInfo workspaceInfo, string targetDirectory, string label = null)
        {
            this.collection.EnsureAuthenticated();

            var versionControlServer = this.collection.GetService<VersionControlServer>();

            using (var workspace = MappedWorkspace.Create(workspaceInfo, versionControlServer, sourcePath, this.log))
            {
                var versionSpec = label == null
                    ? VersionSpec.Latest
                    : VersionSpec.ParseSingleSpec("L" + label, versionControlServer.AuthorizedUser);

                workspace.Workspace.Get(new GetRequest(new ItemSpec(sourcePath.AbsolutePath, RecursionType.Full), versionSpec), GetOptions.Overwrite);

                CopyNonTfsFiles(workspace.DiskPath, targetDirectory);
            }
        }

        public void ApplyLabel(TfsSourcePath path, string label, string comment)
        {
            this.collection.EnsureAuthenticated();

            var versionControlService = this.collection.GetService<VersionControlServer>();

            var versionControlLabel = new VersionControlLabel(versionControlService, label, versionControlService.AuthorizedUser, path.AbsolutePath, comment);
            versionControlService.CreateLabel(versionControlLabel, new[] { new LabelItemSpec(new ItemSpec(path.AbsolutePath, RecursionType.Full), VersionSpec.Latest, false) }, LabelChildOption.Replace);
        }

        public IEnumerable<TfsSourcePath> EnumerateChildSourcePaths(TfsSourcePath path)
        {
            this.collection.EnsureAuthenticated();

            var sourceControl = this.collection.GetService<VersionControlServer>();
            var itemSet = sourceControl.GetItems(path.AbsolutePath, RecursionType.OneLevel);

            var result = from i in itemSet.Items
                         where i.ServerItem != path.AbsolutePath
                         where i.ItemType == ItemType.Folder
                         select new TfsSourcePath(i.ServerItem, i.ItemType == ItemType.Folder);

            return result;
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
