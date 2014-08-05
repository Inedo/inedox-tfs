using System;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.BuildImporters;
using Inedo.BuildMaster.Web.Controls.Extensions.BuildImporters;
using Inedo.Web.Controls;
using Inedo.Web.Controls.SimpleHtml;

namespace Inedo.BuildMasterExtensions.TFS.BuildImporter
{
    internal sealed class TfsBuildImporterTemplateEditor : BuildImporterTemplateEditorBase
    {
        private ValidatingTextBox txtFileMasks;
        private CheckBox chkFileMasksLocked;
        private ValidatingTextBox txtArtifactName;
        private CheckBox chkArtifactNameLocked;
        private ValidatingTextBox txtTeamProject;
        private CheckBox chkTeamProjectLocked;
        private ValidatingTextBox txtBuildDefinition;
        private CheckBox chkBuildDefinitionLocked;

        public override void BindToForm(BuildImporterTemplateBase extension)
        {
            var template = (TfsBuildImporterTemplate)extension;
            this.txtFileMasks.Text = string.Join(Environment.NewLine, template.FileMasks);
            this.chkFileMasksLocked.Checked = template.FileMasksLocked;
            this.txtArtifactName.Text = template.ArtifactName;
            this.chkArtifactNameLocked.Checked = template.ArtifactNameLocked;
            this.txtTeamProject.Text = template.TeamProject;
            this.chkTeamProjectLocked.Checked = template.TeamProjectLocked;
            this.txtBuildDefinition.Text = template.BuildDefinition;
            this.chkBuildDefinitionLocked.Checked = template.BuildDefinitionLocked;
        }
        public override BuildImporterTemplateBase CreateFromForm()
        {
            return new TfsBuildImporterTemplate
            {
                FileMasks = this.txtFileMasks.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                FileMasksLocked = this.chkFileMasksLocked.Checked,
                ArtifactName = this.txtArtifactName.Text,
                ArtifactNameLocked = this.chkArtifactNameLocked.Checked,
                TeamProject = this.txtTeamProject.Text,
                TeamProjectLocked = this.chkTeamProjectLocked.Checked,
                BuildDefinition = this.txtBuildDefinition.Text,
                BuildDefinitionLocked = this.chkBuildDefinitionLocked.Checked
            };
        }

        protected override void CreateChildControls()
        {
            this.txtArtifactName = new ValidatingTextBox();

            this.chkArtifactNameLocked = new CheckBox { Text = "Allow selection at build time" };

            this.txtFileMasks = new ValidatingTextBox
            {
                TextMode = TextBoxMode.MultiLine,
                Rows = 3,
                Text = "*"
            };

            this.chkFileMasksLocked = new CheckBox { Text = "Allow selection at build time" };

            this.txtTeamProject = new ValidatingTextBox();

            this.chkTeamProjectLocked = new CheckBox { Text = "Allow selection at build time" };

            this.txtBuildDefinition = new ValidatingTextBox();

            this.chkBuildDefinitionLocked = new CheckBox { Text = "Allow selection at build time" };

            var fldSingleFileArtifactName = new SlimFormField("Artifact name:", new Div(new Div(this.txtArtifactName), new Div(this.chkArtifactNameLocked)));

            this.Controls.Add(
                fldSingleFileArtifactName,
                new SlimFormField("File masks:", new Div(new Div(this.txtFileMasks), new Div(this.chkFileMasksLocked))),
                new SlimFormField("Team project:", new Div(new Div(this.txtTeamProject), new Div(this.chkTeamProjectLocked))),
                new SlimFormField("Build definition:", new Div(new Div(this.txtBuildDefinition), new Div(this.chkBuildDefinitionLocked)))
            );
        }
    }
}
