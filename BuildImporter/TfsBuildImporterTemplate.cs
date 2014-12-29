using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.BuildImporters;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.TFS.BuildImporter
{
    [CustomEditor(typeof(TfsBuildImporterTemplateEditor))]
    internal sealed class TfsBuildImporterTemplate : BuildImporterTemplateBase<TfsBuildImporter>
    {
        [Persistent]
        public string ArtifactName { get; set; }
        [Persistent]
        public string TeamProject { get; set; }
        [Persistent]
        public string BuildDefinition { get; set; }
        [Persistent]
        public bool BuildNumberLocked { get; set; }
        [Persistent]
        public bool IncludeUnsuccessful { get; set; }
        [Persistent]
        public string BuildNumberPattern { get; set; }

        public override ExtensionComponentDescription GetDescription()
        {
            var desc = new ExtensionComponentDescription("Import ");
            if (this.BuildNumberLocked)
                desc.AppendContent(this.IncludeUnsuccessful ? "last completed build" : "last succeeded build");
            desc.AppendContent(" from ", new Hilite(this.TeamProject));
            
            return desc;
        }
    }
}
