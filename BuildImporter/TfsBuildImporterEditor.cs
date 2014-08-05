using System;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.BuildImporters;
using Inedo.BuildMaster.Web.Controls.Extensions.BuildImporters;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.TFS.BuildImporter
{
    internal sealed class TfsBuildImporterEditor : BuildImporterEditorBase<TfsBuildImporterTemplate>
    {
        private ValidatingTextBox txtArtifactName;
        private ValidatingTextBox txtTeamProject;
        private ValidatingTextBox txtBuildDefinition;
        private ValidatingTextBox txtBuildNumber;

        public override BuildImporterBase CreateFromForm()
        {
            return new TfsBuildImporter
            {
                ArtifactName = this.txtArtifactName.Text,
                TeamProject = this.txtTeamProject.Text,
                BuildDefinition = this.txtBuildDefinition.Text,
                BuildNumber = (this.txtBuildNumber.Text != "last succeeded build" && this.txtBuildNumber.Text != "last completed build") ? this.txtBuildNumber.Text : null,
                IncludeUnsuccessful = this.txtBuildNumber.Text == "last completed build"
            };
        }

        protected override void CreateChildControls()
        {
            this.txtArtifactName = new ValidatingTextBox
            {
                Text = this.Template.ArtifactName,
                Enabled = !this.Template.ArtifactNameLocked
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

            this.txtBuildNumber = new ValidatingTextBox
            {
                AutoCompleteValues = new[] { "last completed build", "last succeeded build" },
                Text = this.Template.BuildNumberLocked ? (this.Template.IncludeUnsuccessful ? "last completed build" : "last succeeded build") : string.Empty,
                Enabled = !this.Template.BuildNumberLocked
            };

            this.Controls.Add(
                new SlimFormField("Build number:", this.txtBuildNumber),
                new SlimFormField("Artifact name:", this.txtArtifactName),
                new SlimFormField("Team project:", this.txtTeamProject),
                new SlimFormField("Build definition:", this.txtBuildDefinition)
            );
        }
    }
}
