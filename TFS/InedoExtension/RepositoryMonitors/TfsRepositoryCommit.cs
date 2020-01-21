using System;
using System.Collections.Generic;
using Inedo.ExecutionEngine;
using Inedo.Extensibility.RepositoryMonitors;
using Inedo.Extensions.TFS.Clients.SourceControl;
using Inedo.Serialization;

namespace Inedo.Extensions.TFS.RepositoryMonitors
{
    [Serializable]
    internal sealed class TfsRepositoryCommit : RepositoryCommit
    {
        [Persistent]
        public int ChangesetId { get; set; }
        [Persistent]
        public DateTime CheckinDate { get; set; }

        public TfsRepositoryCommit(TfsSourcePath sourcePath)
        {
            this.ChangesetId = sourcePath.ChangesetId.Value;
            this.CheckinDate = sourcePath.CheckinDate.Value;
        }

        public override bool Equals(RepositoryCommit other)
        {
            return other is TfsRepositoryCommit c
                && this.ChangesetId == c.ChangesetId;
        }

        public override int GetHashCode()
        {
            return this.ChangesetId;
        }

        public override string GetFriendlyDescription()
        {
            return $"{this.ChangesetId} ({this.CheckinDate})";
        }

        public override string ToString()
        {
            return this.ChangesetId.ToString();
        }

        public override IReadOnlyDictionary<RuntimeVariableName, RuntimeValue> GetRuntimeVariables()
        {
            return new Dictionary<RuntimeVariableName, RuntimeValue>
            {
                [new RuntimeVariableName("ChangesetId", RuntimeValueType.Scalar)] = this.ChangesetId.ToString(),
                [new RuntimeVariableName("CheckinDate", RuntimeValueType.Scalar)] = this.CheckinDate.ToString("yyyy-MM-ddThh:mm:ss")
            };
        }
    }
}