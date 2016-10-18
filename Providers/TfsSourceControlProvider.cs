using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Inedo.Agents;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Extensibility.Providers.SourceControl;
using Inedo.BuildMaster.Files;
using Inedo.BuildMaster.Web;
using Inedo.IO;
using Inedo.Serialization;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Inedo.BuildMasterExtensions.TFS
{
    [DisplayName("Team Foundation Server")]
    [Description("Supports TFS 2010-2015.")]
    [CustomEditor(typeof(TfsSourceControlProviderEditor))]
    public sealed class TfsSourceControlProvider : SourceControlProviderBase, ILocalWorkspaceProvider, ILabelingProvider, IRevisionProvider
    {
        /// <summary>
        /// The base url of the TFS store, e.g. "http://server:port/tfs"
        /// </summary>
        [Persistent]
        public string BaseUrl { get; set; }
        /// <summary>
        /// The username used to connect to the server
        /// </summary>
        [Persistent]
        public string UserName { get; set; }
        /// <summary>
        /// The password used to connect to the server
        /// </summary>
        [Persistent]
        public string Password { get; set; }
        /// <summary>
        /// The domain of the server
        /// </summary>
        [Persistent]
        public string Domain { get; set; }
        /// <summary>
        /// Returns true if BuildMaster should connect to TFS using its own account, false if the credentials are specified
        /// </summary>
        [Persistent]
        public bool UseSystemCredentials { get; set; }
        /// <summary>
        /// Gets or sets the custom workspace path. If null or empty, it will be generated based on the path for the Get Latest action.
        /// </summary>
        [Persistent]
        public string CustomWorkspacePath { get; set; }
        /// <summary>
        /// Gets or sets the name of the custom workspace. If null or empty, it will be generated based on the <see cref="CustomWorkspacePath"/>.
        /// </summary>
        [Persistent]
        public string CustomWorkspaceName { get; set; }

        /// <summary>
        /// Gets the base URI of the Team Foundation Server
        /// </summary>
        private Uri BaseUri => new Uri(this.BaseUrl);

        public override char DirectorySeparator => '/';

        private IRemoteMethodExecuter Remote
        {
            get
            {
                var remote = this.Agent.TryGetService<IRemoteMethodExecuter>();
                if (remote == null)
                    throw new InvalidOperationException($"The specified agent {this.Agent.GetDescription().ToString()} does not support remote method execution.");
                return remote;
            }
        }

        public override void GetLatest(string sourcePath, string targetPath)
        {
            this.Remote.InvokeAction(() =>
            {
                var context = (TfsSourceControlContext)this.CreateSourceControlContext(sourcePath);
                this.GetLatest(context, targetPath);
            });
        }
        private void GetLatest(TfsSourceControlContext context, string targetPath)
        {
            this.EnsureLocalWorkspaceInternal(context);
            this.UpdateLocalWorkspaceInternal(context);
            this.ExportFilesInternal(context, targetPath);
        }

        public override string ToString() => "Provides functionality for getting files and browsing folders in TFS 2010-2015.";

        public override DirectoryEntryInfo GetDirectoryEntryInfo(string sourcePath)
        {
            return this.Remote.InvokeFunc(() =>
            {
                var context = (TfsSourceControlContext)this.CreateSourceControlContext(sourcePath);
                return this.GetDirectoryEntryInfo(context);
            });
        }
        private DirectoryEntryInfo GetDirectoryEntryInfo(TfsSourceControlContext context)
        {
            using (var tfs = this.GetTeamProjectCollection())
            {
                var sourceControl = tfs.GetService<VersionControlServer>();
                var itemSet = sourceControl.GetItems(context.SourcePath, RecursionType.OneLevel);
                return new DirectoryEntryInfo(
                    context.LastSubDirectoryName,
                    context.SourcePath,
                    itemSet.Items.Where(i => i.ServerItem != context.SourcePath).Select(i => context.CreateSystemEntryInfo(i))
            );
            }
        }

        public override byte[] GetFileContents(string filePath)
        {
            return this.Remote.InvokeFunc(() =>
            {
                var context = (TfsSourceControlContext)this.CreateSourceControlContext(filePath);
                return this.GetFileContents(context);
            });
        }
        private byte[] GetFileContents(TfsSourceControlContext context)
        {
            var tempFile = Path.GetTempFileName();
            using (var tfs = this.GetTeamProjectCollection())
            {
                var versionControlServer = tfs.GetService<VersionControlServer>();
                var item = versionControlServer.GetItem(context.SourcePath);
                item.DownloadFile(tempFile);

                return File.ReadAllBytes(tempFile);
            }
        }

        public override bool IsAvailable()
        {
            return this.Remote.InvokeFunc(IsAvailableInternal);            
        }
        private static bool IsAvailableInternal()
        {
            try
            {
                typeof(TfsTeamProjectCollection).GetType();
                return true;
            }
            catch
            {
                return false;
            }
        }
        public override void ValidateConnection()
        {
            try
            {
                this.Remote.InvokeAction(this.ValidateConnectionInternal);
            }
            catch (TypeLoadException)
            {
                throw new NotAvailableException("Could not connect to TFS. Verify that Visual Studio 2010-2015 or Team Explorer 2010-2015 is installed on the server.");
            }
            catch (Exception ex)
            {
                throw new NotAvailableException("Could not connect to TFS: " + ex.ToString());
            }
        }
        private void ValidateConnectionInternal()
        {
            using (var tfs = this.GetTeamProjectCollection())
            {
            }
        }

        public void ApplyLabel(string label, string sourcePath)
        {
            this.Remote.InvokeAction(() =>
            {
                var context = (TfsSourceControlContext)this.CreateSourceControlContext(sourcePath);
                this.ApplyLabel(context, label);
            });
        }
        private void ApplyLabel(TfsSourceControlContext context, string label)
        {
            using (var tfs = this.GetTeamProjectCollection())
            {
                var versionControlService = tfs.GetService<VersionControlServer>();

                var versionControlLabel = new VersionControlLabel(versionControlService, label, versionControlService.AuthorizedUser, context.SourcePath, "Label applied by BuildMaster");
                versionControlService.CreateLabel(versionControlLabel, new[] { new LabelItemSpec(new ItemSpec(context.SourcePath, RecursionType.Full), VersionSpec.Latest, false) }, LabelChildOption.Replace);
            }
        }

        public void GetLabeled(string label, string sourcePath, string targetPath)
        {
            this.Remote.InvokeAction(() =>
            {
                var context = new TfsSourceControlContext(this, sourcePath, label);
                this.GetLabeled(context, label, targetPath);
            });
        }
        private void GetLabeled(TfsSourceControlContext context, string label, string targetDirectory)
        {
            this.EnsureLocalWorkspaceInternal(context);
            this.UpdateLocalWorkspaceInternal(context);
            this.ExportFilesInternal(context, targetDirectory);
        }

        public object GetCurrentRevision(string path)
        {
            return this.Remote.InvokeFunc(() =>
            {
                var context = (TfsSourceControlContext)this.CreateSourceControlContext(path);
                return this.GetCurrentRevision(context);
            });
        }
        private object GetCurrentRevision(TfsSourceControlContext context)
        {
            using (var tfs = this.GetTeamProjectCollection())
            {
                var sourceControl = tfs.GetService<VersionControlServer>();

                string sourcePath = sourceControl.GetItem(context.SourcePath).ServerItem; // matches the sourcePath with the base path returned by TFS

                var itemSet = sourceControl.GetItems(sourcePath, VersionSpec.Latest, RecursionType.Full, DeletedState.Any, ItemType.Any);
                if (itemSet == null || itemSet.Items == null || itemSet.Items.Length == 0)
                    return new byte[0];

                return itemSet.Items.Max(i => i.ChangesetId);
            }
        }

        private TfsTeamProjectCollection GetTeamProjectCollection()
        {
            if (this.UseSystemCredentials)
            {
                var projectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(this.BaseUri);
                projectCollection.EnsureAuthenticated();
                return projectCollection;
            }
            else
            {
                TfsClientCredentials credentials;

                if (string.IsNullOrEmpty(this.Domain))
                    credentials = new TfsClientCredentials(new BasicAuthCredential(new NetworkCredential(this.UserName, this.Password)));
                else
                    credentials = new TfsClientCredentials(new WindowsCredential(new NetworkCredential(this.UserName, this.Password, this.Domain)));

                var projectColleciton = new TfsTeamProjectCollection(this.BaseUri, credentials);
                projectColleciton.EnsureAuthenticated();
                return projectColleciton;
            }
        }
        private Workspace GetMappedWorkspace(VersionControlServer server, TfsSourceControlContext context)
        {
            if (!string.IsNullOrEmpty(this.CustomWorkspacePath))
                this.LogDebug("Using custom workspace path: " + this.CustomWorkspacePath);
            if (!string.IsNullOrEmpty(this.CustomWorkspaceName))
                this.LogDebug("Using custom workspace name: " + this.CustomWorkspaceName);

            var workspaces = server.QueryWorkspaces(context.WorkspaceName, server.AuthorizedUser, Environment.MachineName);
            var workspace = workspaces.FirstOrDefault();
            if (workspace == null)
            {
                this.LogDebug("Existing workspace not found, creating workspace \"{0}\"...", context.WorkspaceName);
                workspace = server.CreateWorkspace(context.WorkspaceName);
            }
            else
            {
                this.LogDebug("Workspace found: " + workspace.Name);
            }

            this.LogDebug("Workspace mappings: \r\n" + string.Join(Environment.NewLine, workspace.Folders.Select(m => m.LocalItem + "\t->\t" + m.ServerItem)));

            if (!workspace.IsLocalPathMapped(context.WorkspaceDiskPath))
            {
                this.LogDebug("Local path is not mapped, creating mapping to \"{0}\"...", context.WorkspaceDiskPath);
                this.DeleteWorkspaceInternal(context);
                workspace.Map(context.SourcePath, context.WorkspaceDiskPath);
            }

            if (!workspace.HasReadPermission)
                throw new System.Security.SecurityException($"{server.AuthorizedUser} does not have read permission for {context.WorkspaceDiskPath}");

            return workspace;
        }
        private void CopyNonTfsFiles(string sourceDir, string targetDir)
        {
            if (!Directory.Exists(sourceDir))
                return;
            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            var sourceDirInfo = new DirectoryInfo(sourceDir);

            foreach (var file in sourceDirInfo.GetFiles())
            {
                file.CopyTo(Path.Combine(targetDir, file.Name), true);
            }

            foreach (var subDir in sourceDirInfo.GetDirectories().Where(d => d.Name != "$tf"))
            {
                this.CopyNonTfsFiles(subDir.FullName, Path.Combine(targetDir, subDir.Name));
            }
        }

        public override SourceControlContext CreateSourceControlContext(object contextData)
            => new TfsSourceControlContext(this, (string)contextData);

        private void DeleteWorkspaceInternal(SourceControlContext context)
        {
            DirectoryEx.Clear(context.WorkspaceDiskPath);
        }

        private void EnsureLocalWorkspaceInternal(SourceControlContext context)
        {
            this.LogDebug("Ensuring local workspace disk path: " + context.WorkspaceDiskPath);
            if (!Directory.Exists(context.WorkspaceDiskPath))
            {
                this.LogDebug("Creating workspace disk path...");
                Directory.CreateDirectory(context.WorkspaceDiskPath);
            }
            else
            {
                this.LogDebug("Workspace disk path exists.");
            }
        }

        private void ExportFilesInternal(SourceControlContext context, string targetDirectory)
        {
            var tfsContext = (TfsSourceControlContext)context;
            this.LogDebug("Exporting files from \"{0}\" to \"{1}\"...", tfsContext.WorkspaceDiskPath, targetDirectory);
            this.CopyNonTfsFiles(tfsContext.WorkspaceDiskPath, targetDirectory);
        }

        private string GetWorkspaceDiskPathInternal(SourceControlContext context)
        {
            return context.WorkspaceDiskPath;
        }

        private void UpdateLocalWorkspaceInternal(SourceControlContext context)
        {
            using (var tfs = this.GetTeamProjectCollection())
            {
                var versionControlServer = tfs.GetService<VersionControlServer>();

                var workspace = this.GetMappedWorkspace(versionControlServer, (TfsSourceControlContext)context);
                if (context.Label != null)
                {
                    string sourcePath = ((TfsSourceControlContext)context).SourcePath;
                    var getRequest = new GetRequest(new ItemSpec(sourcePath, RecursionType.Full), VersionSpec.ParseSingleSpec("L" + context.Label, versionControlServer.AuthorizedUser));
                    workspace.Get(getRequest, GetOptions.Overwrite);
                }
                else
                {
                    workspace.Get(VersionSpec.Latest, GetOptions.Overwrite);
                }
            }
        }

        string ILocalWorkspaceProvider.GetWorkspaceDiskPath(SourceControlContext context) => this.GetWorkspaceDiskPathInternal(context);
        void ILocalWorkspaceProvider.EnsureLocalWorkspace(SourceControlContext context) => this.EnsureLocalWorkspaceInternal(context);
        void ILocalWorkspaceProvider.UpdateLocalWorkspace(SourceControlContext context) => this.UpdateLocalWorkspaceInternal(context);
        void ILocalWorkspaceProvider.ExportFiles(SourceControlContext context, string targetDirectory) => this.ExportFilesInternal(context, targetDirectory);
        void ILocalWorkspaceProvider.DeleteWorkspace(SourceControlContext context) => this.DeleteWorkspaceInternal(context);
    }
}
