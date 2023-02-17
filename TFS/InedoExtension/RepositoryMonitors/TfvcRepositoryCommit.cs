using System;
using Inedo.Documentation;
using Inedo.Extensibility.ResourceMonitors;
using Inedo.Serialization;

namespace Inedo.Extensions.TFS.RepositoryMonitors
{
    [Serializable]
    public sealed class TfvcRepositoryCommit : ResourceMonitorState
    {
        [Persistent]
        public int? ChangeSetId { get; set; }

        [Persistent]
        public string Error { get; set; }

        public override bool Equals(ResourceMonitorState other)
        {
            if (!(other is TfvcRepositoryCommit tfvcCommit))
                return false;

            return this.ChangeSetId == tfvcCommit.ChangeSetId;
        }
        public override int GetHashCode() => this.ChangeSetId.GetHashCode();

        public override RichDescription GetDescription() => new(this.ChangeSetId?.ToString() ?? string.Empty);

        public override string ToString() => this.ChangeSetId.ToString();
    }
}
