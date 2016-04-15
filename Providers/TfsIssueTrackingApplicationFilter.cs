using System;
using Inedo.BuildMaster.Extensibility.IssueTrackerConnections;
using Inedo.BuildMaster.Web;
using Inedo.Documentation;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.TFS.Providers
{
    [Serializable]
    [CustomEditor(typeof(TfsIssueTrackingApplicationFilterEditor))]
    public sealed class TfsIssueTrackingApplicationFilter : IssueTrackerApplicationConfigurationBase
    {
        [Persistent]
        public Guid? CollectionId { get; set; }
        [Persistent]
        public string CollectionName { get; set; }
        [Persistent]
        public string ProjectName { get; set; }
        [Persistent]
        public string AreaPath { get; set; }
        [Persistent]
        public string CustomWiql { get; set; }

        public override RichDescription GetDescription()
        {
            var description = new RichDescription(
                "Collection: ",
                new Hilite(this.CollectionName ?? (this.CollectionId.ToString()))
            );

            if (string.IsNullOrWhiteSpace(this.CustomWiql))
            {
                if (!string.IsNullOrWhiteSpace(this.ProjectName))
                {
                    description.AppendContent(
                        ", Project: ",
                        new Hilite(this.ProjectName)
                    );

                    if (!string.IsNullOrWhiteSpace(this.AreaPath))
                    {
                        description.AppendContent(
                            ", Area: ",
                            new Hilite(this.AreaPath)
                        );
                    }
                }
            }
            else
            {
                description.AppendContent(", custom query");
            }

            return description;
        }
    }
}
