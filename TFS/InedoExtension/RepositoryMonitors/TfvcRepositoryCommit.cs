using System;
using System.Collections.Generic;
using Inedo.ExecutionEngine;
using Inedo.Extensibility.RepositoryMonitors;
using Inedo.Serialization;

namespace Inedo.Extensions.TFS.RepositoryMonitors
{
    [Serializable]
    internal sealed class TfvcRepositoryCommit : RepositoryCommit
    {
        [Persistent]
        public int? ChangeSetId { get; set; }

        [Persistent]
        public string Error { get; set; }

        public override bool Equals(RepositoryCommit other)
        {
            if (!(other is TfvcRepositoryCommit tfvcCommit))
                return false;

            return this.ChangeSetId == tfvcCommit.ChangeSetId;
        }
        public override int GetHashCode() => this.ChangeSetId.GetHashCode();

        public override string GetFriendlyDescription() => this.Error ?? this.ToString();

        public override string ToString() => this.ChangeSetId.ToString();

        public override IReadOnlyDictionary<RuntimeVariableName, RuntimeValue> GetRuntimeVariables()
        {
            return new Dictionary<RuntimeVariableName, RuntimeValue>()
            {
                [new RuntimeVariableName("ChangeSetId", RuntimeValueType.Scalar)] = this.ChangeSetId.ToString()
            };
        }
    }
}
