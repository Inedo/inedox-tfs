using System.Linq;

namespace Inedo.TFS.Clients.SourceControl
{
    public sealed class TfsSourcePath
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

        public string DisplayName => this.AbsolutePath.Split('/').LastOrDefault() ?? "";
        public string FullPath => this.AbsolutePath;

        public override string ToString() => this.AbsolutePath;
    }
}
