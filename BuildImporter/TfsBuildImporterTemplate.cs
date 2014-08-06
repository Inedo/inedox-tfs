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
        public bool ArtifactNameLocked { get; set; }
        [Persistent]
        public string TeamProject { get; set; }
        [Persistent]
        public bool TeamProjectLocked { get; set; }
        [Persistent]
        public string BuildDefinition { get; set; }
        [Persistent]
        public bool BuildDefinitionLocked { get; set; }
        [Persistent]
        public bool BuildNumberLocked { get; set; }
        [Persistent]
        public bool IncludeUnsuccessful { get; set; }

        public override ExtensionComponentDescription GetDescription()
        {
            return new ExtensionComponentDescription(
                "Import an artifact named ",
                new Hilite(this.ArtifactName),
                " from TFS"
            );
        }
    }
}
