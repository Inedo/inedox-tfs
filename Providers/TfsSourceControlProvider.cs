using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Extensibility.Providers.SourceControl;
using Inedo.BuildMaster.Files;
using Inedo.BuildMaster.Web;
using Inedo.IO;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Inedo.BuildMasterExtensions.TFS
{
    [ProviderProperties(
        "Team Foundation Server",
        "Supports TFS 2010-2015; requires that Visual Studio Team System is installed.",
        RequiresTransparentProxy = true)]
    [CustomEditor(typeof(TfsSourceControlProviderEditor))]
    public class TfsSourceControlProvider : SourceControlProviderBase, ILocalWorkspaceProvider, ILabelingProvider, IRevisionProvider
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
        /// Gets the base URI of the Team Foundation Server
        /// </summary>
        protected Uri BaseUri
        {
            get { return new Uri(BaseUrl); }
        }

        /// <summary>
        /// Gets the char that's used by the provider to separate directories/files in a path string
        /// </summary>
        public override char DirectorySeparator
        {
            get { return '/'; }
        }
        /// <summary>
        /// Retrieves the latest version of the source code from the provider's sourcePath into the target path
        /// </summary>
        /// <param name="sourcePath">provider source path</param>
        /// <param name="targetPath">target file path</param>
        public override void GetLatest(string sourcePath, string targetPath)
        {
            var context = (TfsSourceControlContext)this.CreateSourceControlContext(sourcePath);
            this.GetLatest(context, targetPath);
        }

        private void GetLatest(TfsSourceControlContext context, string targetPath)
        {
            this.EnsureLocalWorkspace(context);
            this.UpdateLocalWorkspace(context);
            this.ExportFiles(context, targetPath);
        }

        /// <summary>
        /// Returns a string representation of this provider.
        /// </summary>
        /// <returns>String representation of this provider.</returns>
        public override string ToString()
        {
            return "Provides functionality for getting files and browsing folders in TFS 2010-2015.";
        }

        public override DirectoryEntryInfo GetDirectoryEntryInfo(string sourcePath)
        {
            var context = (TfsSourceControlContext)this.CreateSourceControlContext(sourcePath);
            return this.GetDirectoryEntryInfo(context);
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

        /// <summary>
        /// When implemented in a derived class, returns the contents of the specified file.
        /// </summary>
        /// <param name="filePath">Provider file path.</param>
        /// <returns>
        /// Contents of the file as an array of bytes.
        /// </returns>
        public override byte[] GetFileContents(string filePath)
        {
            var context = (TfsSourceControlContext)this.CreateSourceControlContext(filePath);
            return this.GetFileContents(context);
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

        /// <summary>
        /// When implemented in a derived class, indicates whether the provider
        /// is installed and available for use in the current execution context.
        /// </summary>
        /// <returns>
        /// Value indicating whether the provider is available in the current context.
        /// </returns>
        public override bool IsAvailable()
        {
            return IsAvailable2();
        }

        /// <summary>
        /// When implemented in a derived class, attempts to connect with the
        /// current configuration and throws an exception if unsuccessful.
        /// </summary>
        /// <exception cref="NotAvailableException">
        /// Could not connect to TFS. Verify that Visual Studio 2010-2015 or Team Explorer 2010-2015 is installed on the server.
        /// or
        /// Could not connect to TFS:  + ex.ToString()
        /// </exception>
        public override void ValidateConnection()
        {
            try
            {
                this.ValidateConnection2();
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

        /// <summary>
        /// When implemented in a derived class, applies the specified label to the specified
        /// source path.
        /// </summary>
        /// <param name="label">Label to apply.</param>
        /// <param name="sourcePath">Path to apply label to.</param>
        /// <exception cref="ArgumentNullException">sourcePath</exception>
        public void ApplyLabel(string label, string sourcePath)
        {
            var context = (TfsSourceControlContext)this.CreateSourceControlContext(sourcePath);
            this.ApplyLabel(context, label);
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

        /// <summary>
        /// When implemented in a derived class, retrieves labeled
        /// source code from the provider's source path into the target path.
        /// </summary>
        /// <param name="label">Label of source files to get.</param>
        /// <param name="sourcePath">Provider source path.</param>
        /// <param name="targetPath">Target file path.</param>
        /// <exception cref="ArgumentNullException">
        /// sourcePath
        /// or
        /// targetPath
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">targetPath not found:  + targetPath</exception>
        public void GetLabeled(string label, string sourcePath, string targetPath)
        {
            var context = new TfsSourceControlContext(this, sourcePath, label);
            this.GetLabeled(context, label, targetPath);
        }

        private void GetLabeled(TfsSourceControlContext context, string label, string targetDirectory)
        {
            this.EnsureLocalWorkspace(context);
            this.UpdateLocalWorkspace(context);
            this.ExportFiles(context, targetDirectory);
        }

        /// <summary>
        /// Returns a fingerprint that represents the current revision on the source control repository.
        /// </summary>
        /// <param name="path">The source control path whos revision is returned.</param>
        /// <returns>
        /// A representation of the current revision in source control.
        /// </returns>
        /// <remarks>
        /// <para>Notes to implementers:</para>
        /// <para>
        /// The object returned by this method should implement <see cref="M:System.Object.Equals(System.Object)" />.
        /// </para>
        /// </remarks>
        public object GetCurrentRevision(string path)
        {
            var context = (TfsSourceControlContext)this.CreateSourceControlContext(path);
            return this.GetCurrentRevision(context);
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

        /// <summary>
        /// Gets the appropriate version control server based by connecting to TFS using the persisted credentials
        /// </summary>
        /// <returns></returns>
        protected virtual TfsTeamProjectCollection GetTeamProjectCollection()
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

        /// <summary>
        /// Gets a TFS workspace mapped to the specified target path
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="sourcePath">The source path.</param>
        /// <param name="targetPath">The target path.</param>
        private Workspace GetMappedWorkspace(VersionControlServer server, TfsSourceControlContext context)
        {
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
                this.DeleteWorkspace(context);
                workspace.Map(context.SourcePath, context.WorkspaceDiskPath);
            }

            if (!workspace.HasReadPermission)
                throw new System.Security.SecurityException(string.Format("{0} does not have read permission for {1}", server.AuthorizedUser, context.WorkspaceDiskPath));

            return workspace;
        }

        private static bool IsAvailable2()
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

        private void ValidateConnection2()
        {
            using (var tfs = this.GetTeamProjectCollection())
            {
            }
        }

        public override SourceControlContext CreateSourceControlContext(object contextData)
        {
            return new TfsSourceControlContext(this, (string)contextData);
        }

        public void DeleteWorkspace(SourceControlContext context)
        {
            DirectoryEx.Clear(context.WorkspaceDiskPath);
        }

        public void EnsureLocalWorkspace(SourceControlContext context)
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

        public void ExportFiles(SourceControlContext context, string targetDirectory)
        {
            var tfsContext = (TfsSourceControlContext)context;
            this.LogDebug("Exporting files from \"{0}\" to \"{1}\"...", tfsContext.WorkspaceDiskPath, targetDirectory);
            this.CopyNonTfsFiles(tfsContext.WorkspaceDiskPath, targetDirectory);
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

        public string GetWorkspaceDiskPath(SourceControlContext context)
        {
            return context.WorkspaceDiskPath;
        }

        public void UpdateLocalWorkspace(SourceControlContext context)
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
    }
}
