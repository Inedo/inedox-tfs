using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.TFS.VisualStudioOnline
{
    internal sealed class ImportVsoArtifactActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtTeamProject;
        private ValidatingTextBox txtArtifactName;
        private ValidatingTextBox txtBuildDefinition;
        private ValidatingTextBox txtBuildNumber;

        public override void BindToForm(ActionBase extension)
        {
            var action = (ImportVsoArtifactAction)extension;

            this.txtTeamProject.Text = action.TeamProject;
            this.txtArtifactName.Text = action.ArtifactName;
            this.txtBuildDefinition.Text = action.BuildDefinition;
            this.txtBuildNumber.Text = action.BuildNumber;
        }

        public override ActionBase CreateFromForm()
        {
            return new ImportVsoArtifactAction()
            {
                TeamProject = this.txtTeamProject.Text,
                ArtifactName = this.txtArtifactName.Text,
                BuildDefinition = this.txtBuildDefinition.Text,
                BuildNumber = this.txtBuildNumber.Text
            };
        }

        protected override void CreateChildControls()
        {
            this.txtTeamProject = new ValidatingTextBox { Required = true };
            this.txtBuildDefinition = new ValidatingTextBox { DefaultText = "default" };
            this.txtBuildNumber = new ValidatingTextBox { DefaultText = "last successful" };
            this.txtArtifactName = new ValidatingTextBox { DefaultText = "use name of build definition" };

            this.Controls.Add(
                new SlimFormField("Team project:", this.txtTeamProject),
                new SlimFormField("Build definition:", this.txtBuildDefinition)
                {
                    HelpText = "By default, the single build definition for the team project will be used. If there are multiple build definitions and one is not supplied here, an error will be generated at execution time."
                },
                new SlimFormField("Build number:", this.txtBuildNumber),
                new SlimFormField("Artifact name:", this.txtArtifactName)
                {
                    HelpText = "By default, the BuildMaster build artifact created from the output will use the name of the build definition. To use a different name, specify one here."
                }
            );
        }
    }
}
