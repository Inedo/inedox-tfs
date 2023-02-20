using System.IO;
using Inedo.Diagnostics;

namespace Inedo.TFS.Clients.SourceControl
{
    public sealed class WorkspaceInfo
    {
        public WorkspaceInfo(string workspacePath, string name)
        {
            this.WorkspacePath = workspacePath;
            this.Name = name;
        }

        public string Name { get; set; }

        public string WorkspacePath { get; }

        public string ResolveWorkspaceDiskPath(ILogSink log)
        {
            log?.LogDebug("Workspace directory: " + this.WorkspacePath);
            return string.IsNullOrWhiteSpace(this.Name) ? this.WorkspacePath : Path.Combine(this.WorkspacePath, this.Name);
        }
    }
}
