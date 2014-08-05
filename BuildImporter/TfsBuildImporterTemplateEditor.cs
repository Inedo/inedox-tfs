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
        private ValidatingTextBox txtArtifactName;
        private CheckBox chkArtifactNameLocked;
        private ValidatingTextBox txtTeamProject;
        private CheckBox chkTeamProjectLocked;
        private ValidatingTextBox txtBuildDefinition;
        private CheckBox chkBuildDefinitionLocked;
        private DropDownList ddlBuildNumber;

        public override void BindToForm(BuildImporterTemplateBase extension)
        {
            var template = (TfsBuildImporterTemplate)extension;
            this.txtArtifactName.Text = template.ArtifactName;
            this.chkArtifactNameLocked.Checked = template.ArtifactNameLocked;
            this.txtTeamProject.Text = template.TeamProject;
            this.chkTeamProjectLocked.Checked = template.TeamProjectLocked;
            this.txtBuildDefinition.Text = template.BuildDefinition;
            this.chkBuildDefinitionLocked.Checked = template.BuildDefinitionLocked;
            this.ddlBuildNumber.SelectedValue = template.BuildNumberLocked ? (template.IncludeUnsuccessful ? "last" : "success") : string.Empty;
        }
        public override BuildImporterTemplateBase CreateFromForm()
        {
            return new TfsBuildImporterTemplate
            {
                ArtifactName = this.txtArtifactName.Text,
                ArtifactNameLocked = this.chkArtifactNameLocked.Checked,
                TeamProject = this.txtTeamProject.Text,
                TeamProjectLocked = this.chkTeamProjectLocked.Checked,
                BuildDefinition = this.txtBuildDefinition.Text,
                BuildDefinitionLocked = this.chkBuildDefinitionLocked.Checked,
                IncludeUnsuccessful = this.ddlBuildNumber.SelectedValue == "last",
                BuildNumberLocked = !string.IsNullOrEmpty(this.ddlBuildNumber.SelectedValue)
            };
        }

        protected override void CreateChildControls()
        {
            this.txtArtifactName = new ValidatingTextBox();

            this.chkArtifactNameLocked = new CheckBox { Text = "Allow selection at build time" };

            this.txtTeamProject = new ValidatingTextBox();

            this.chkTeamProjectLocked = new CheckBox { Text = "Allow selection at build time" };

            this.txtBuildDefinition = new ValidatingTextBox();

            this.chkBuildDefinitionLocked = new CheckBox { Text = "Allow selection at build time" };

            var fldSingleFileArtifactName = new SlimFormField("Artifact name:", new Div(new Div(this.txtArtifactName), new Div(this.chkArtifactNameLocked)));

            this.ddlBuildNumber = new DropDownList
            {
                Items =
                {
                    new ListItem("allow selection at build time", string.Empty),
                    new ListItem("last succeeeded build", "success"),
                    new ListItem("last completed build", "last")
                }
            };

            this.Controls.Add(
                fldSingleFileArtifactName,
                new SlimFormField("Team project:", new Div(new Div(this.txtTeamProject), new Div(this.chkTeamProjectLocked))),
                new SlimFormField("Build definition:", new Div(new Div(this.txtBuildDefinition), new Div(this.chkBuildDefinitionLocked))),
                new SlimFormField("Build number:", this.ddlBuildNumber)
            );
        }
    }
}
