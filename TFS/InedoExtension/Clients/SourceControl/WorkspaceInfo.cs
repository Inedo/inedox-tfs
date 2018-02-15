using Inedo.Diagnostics;
using Inedo.IO;

namespace Inedo.Extensions.TFS.Clients.SourceControl
{
    internal sealed class WorkspaceInfo
    {
        public WorkspaceInfo(string name, string overriddenDiskPath, string rootWorkspacesDirectory)
        {
            this.Name = name;
            this.OverriddenDiskPath = overriddenDiskPath;
            this.RootWorkspacesDirectory = rootWorkspacesDirectory;
        }

        public string Name { get; set; }
        public string OverriddenDiskPath { get; }
        public string RootWorkspacesDirectory { get; }

        public string ResolveWorkspaceDiskPath(ILogSink log)
        {
            if (!string.IsNullOrEmpty(this.OverriddenDiskPath))
            {
                log?.LogDebug("Overridden workspace directory specified: " + this.OverriddenDiskPath);
                return this.OverriddenDiskPath;
            }
            else
            {
                string diskPath = PathEx.Combine(this.RootWorkspacesDirectory, this.Name);
                log?.LogDebug("Using workspace path: " + diskPath);
                return diskPath;
            }
        }
    }
}
