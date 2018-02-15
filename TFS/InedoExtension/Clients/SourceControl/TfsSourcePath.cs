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

        public string AbsolutePath { get; }
        public bool? IsDirectory { get; }

        string IPathInfo.DisplayName => this.AbsolutePath.Split('/').LastOrDefault() ?? "";
        string IPathInfo.FullPath => this.AbsolutePath;

        public override string ToString() => this.AbsolutePath;
    }
}
