using System;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.BuildImporters;
using Inedo.BuildMaster.Web.Controls.Extensions.BuildImporters;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.TFS.BuildImporter
{
    internal sealed class TfsBuildImporterEditor : BuildImporterEditorBase<TfsBuildImporterTemplate>
    {
        private ValidatingTextBox txtFileMasks;
        private ValidatingTextBox txtArtifactName;
        private ValidatingTextBox txtTeamProject;
        private ValidatingTextBox txtBuildDefinition;

        public override BuildImporterBase CreateFromForm()
        {
            return new TfsBuildImporter
            {
                ArtifactName = this.txtArtifactName.Text,
                FileMasks = this.txtFileMasks.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                TeamProject = this.txtTeamProject.Text,
                BuildDefinition = this.txtBuildDefinition.Text
            };
        }

        protected override void CreateChildControls()
        {
            this.txtArtifactName = new ValidatingTextBox
            {
                Text = this.Template.ArtifactName,
                Enabled = !this.Template.ArtifactNameLocked
            };

            this.txtFileMasks = new ValidatingTextBox
            {
                TextMode = TextBoxMode.MultiLine,
                Rows = 3,
                Text = "*",
                Enabled = !this.Template.FileMasksLocked
            };

            this.txtTeamProject = new ValidatingTextBox
            {
                Text = this.Template.TeamProject,
                Enabled = !this.Template.TeamProjectLocked
            };

            this.txtBuildDefinition = new ValidatingTextBox
            {
                Text = this.Template.BuildDefinition,
                Enabled = !this.Template.BuildDefinitionLocked
            };

            this.Controls.Add(
                new SlimFormField("Artifact name:", this.txtArtifactName),
                new SlimFormField("File masks:", this.txtFileMasks),
                new SlimFormField("Team project:", this.txtTeamProject),
                new SlimFormField("Build definition:", this.txtBuildDefinition)
            );
        }
    }
}
