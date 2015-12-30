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
        [Persistent]
        public bool CreateBuildNumberVariable { get; set; } = true;

        public override ExtensionComponentDescription GetDescription()
        {
            var desc = new ExtensionComponentDescription("Import ");
            if (this.BuildNumberLocked)
                desc.AppendContent(this.IncludeUnsuccessful ? "last completed build" : "last succeeded build");
            else
                desc.AppendContent("a specific build number");
            desc.AppendContent(" using build definition ", new Hilite(this.BuildDefinition));
            desc.AppendContent(" from the ", new Hilite(this.TeamProject), " team project");
            
            return desc;
        }
    }
}
