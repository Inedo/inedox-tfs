using System;
using System.Linq;
using Inedo.Web;

namespace Inedo.Extensions.TFS.Clients.SourceControl
{
    internal sealed class TfsSourcePath : IPathInfo
    {
        public TfsSourcePath(string path)
        {
            if (path?.StartsWith("$/") == true)
                this.AbsolutePath = path;
            else
                this.AbsolutePath = "$/" + path;
        }

        public TfsSourcePath(string path, bool isDirectory)
            : this(path)
        {
            this.IsDirectory = isDirectory;
        }

        public TfsSourcePath(string path, bool isDirectory, int changesetId, DateTime checkinDate)
            : this(path, isDirectory)
        {
            this.ChangesetId = changesetId;
            this.CheckinDate = checkinDate;
        }

        public string AbsolutePath { get; }
        public bool? IsDirectory { get; }

        string IPathInfo.DisplayName => this.AbsolutePath.Split('/').LastOrDefault() ?? "";
        string IPathInfo.FullPath => this.AbsolutePath;

        public int? ChangesetId { get; }
        public DateTime? CheckinDate { get; }

        public override string ToString() => this.AbsolutePath;
    }
}
