using System;
using System.Linq;
using Inedo.Diagnostics;
using Inedo.IO;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Inedo.Extensions.TFS.Clients.SourceControl
{
    internal sealed class MappedWorkspace : IDisposable
    {
        private ILogSink log;
        private bool disposed;

        private MappedWorkspace(Workspace workspace, string diskPath, bool deleteOnDispose, ILogSink log)
        {
            this.Workspace = workspace;
            this.DiskPath = diskPath;
            this.DeleteOnDispose = deleteOnDispose;
            this.log = log;
        }

        public Workspace Workspace { get; }
        public string DiskPath { get; }
        public bool DeleteOnDispose { get; }

        public void Dispose()
        {
            if (this.disposed)
                return;

            this.disposed = true;

            if (this.DeleteOnDispose)
            {
                this.log?.LogDebug("Deleting contents of: " + this.DiskPath);
                DirectoryEx.Delete(this.DiskPath);

                try
                {
                    this.log?.LogDebug("Deleting workspace: " + this.Workspace.Name);
                    this.Workspace.Delete();
                }
                catch (Exception ex)
                {
                    this.log?.LogWarning("Error deleting workspace: " + ex.Message);
                }
            }
            else
            {
                this.log?.LogDebug($"Workspace {this.Workspace.Name} will be persisted.");
            }
        }

        public static MappedWorkspace Create(WorkspaceInfo info, VersionControlServer versionControlServer, TfsSourcePath sourcePath, ILogSink log)
        {
            if (string.IsNullOrEmpty(info.Name))
            {
                return CreateTemporary(info, versionControlServer, sourcePath, log);
            }
            else
            {
                return GetOrCreate(info, versionControlServer, sourcePath, log);
            }
        }

        private static MappedWorkspace GetOrCreate(WorkspaceInfo workspaceInfo, VersionControlServer versionControlServer, TfsSourcePath sourcePath, ILogSink log)
        {
            var workspaces = versionControlServer.QueryWorkspaces(workspaceInfo.Name, versionControlServer.AuthorizedUser, Environment.MachineName);
            var workspace = workspaces.FirstOrDefault();
            if (workspace == null)
            {
                log?.LogDebug($"Workspace '{workspaceInfo.Name}' was not found for user '{versionControlServer.AuthorizedUser}' on machine '{Environment.MachineName}', creating...");
                workspace = versionControlServer.CreateWorkspace(workspaceInfo.Name);
            }

            log?.LogDebug("Workspace mappings: \r\n" + string.Join(Environment.NewLine, workspace.Folders.Select(m => m.LocalItem + "\t->\t" + m.ServerItem)));

            string diskPath = workspaceInfo.ResolveWorkspaceDiskPath(log);

            if (!workspace.IsLocalPathMapped(diskPath))
            {
                log?.LogDebug($"Local path is not mapped, creating mapping to \"{diskPath}\"...");
                DirectoryEx.Delete(diskPath);
                workspace.Map(sourcePath.AbsolutePath, diskPath);
            }

            if (!workspace.HasReadPermission)
                throw new System.Security.SecurityException($"{versionControlServer.AuthorizedUser} does not have read permission for workspace '{workspaceInfo.Name}' at '{diskPath}'");

            return new MappedWorkspace(workspace, diskPath, false, log);
        }

        private static MappedWorkspace CreateTemporary(WorkspaceInfo workspaceInfo, VersionControlServer versionControlServer, TfsSourcePath sourcePath, ILogSink log)
        {
            string uniqueName = "BM-" + DateTime.UtcNow.ToString("yyMMdd-HHmmss-ffff");

            log?.LogDebug($"Creating workspace '{uniqueName}'...");
            var workspace = versionControlServer.CreateWorkspace(uniqueName);
            workspaceInfo.Name = uniqueName;

            string diskPath = workspaceInfo.ResolveWorkspaceDiskPath(log);

            log?.LogDebug($"Creating disk path '{diskPath}'...");

            DirectoryEx.Create(diskPath);

            log?.LogDebug("Mapping workspace to disk path...");
            workspace.Map(sourcePath.AbsolutePath, diskPath);

            return new MappedWorkspace(workspace, diskPath, true, log);
        }
    }
}
