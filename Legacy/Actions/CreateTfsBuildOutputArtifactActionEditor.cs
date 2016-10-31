using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.TFS
{
    public sealed class CreateTfsBuildOutputArtifactActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtTeamProject;
        private ValidatingTextBox txtArtifactName;
        private ValidatingTextBox txtBuildDefinition;
        private ValidatingTextBox txtBuildNumber;
        private CheckBox chkIncludeUnsuccessful;

        public override void BindToForm(ActionBase extension)
        {
            var action = (CreateTfsBuildOutputArtifactAction)extension;

            this.txtTeamProject.Text = action.TeamProject;
            this.txtArtifactName.Text = action.ArtifactName;
            this.txtBuildDefinition.Text = action.BuildDefinition;
            this.txtBuildNumber.Text = action.BuildNumber;
            this.chkIncludeUnsuccessful.Checked = action.IncludeUnsuccessful;
        }

        public override ActionBase CreateFromForm()
        {
            return new CreateTfsBuildOutputArtifactAction()
            {
                TeamProject = this.txtTeamProject.Text,
                ArtifactName = this.txtArtifactName.Text,
                BuildDefinition = this.txtBuildDefinition.Text,
                BuildNumber = this.txtBuildNumber.Text,
                IncludeUnsuccessful = this.chkIncludeUnsuccessful.Checked
            };
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            this.txtTeamProject = new ValidatingTextBox { Required = true };
            this.txtArtifactName = new ValidatingTextBox { DefaultText = "use name of build definition" };
            this.txtBuildNumber = new ValidatingTextBox { DefaultText = "last successful" };
            this.txtBuildDefinition = new ValidatingTextBox { DefaultText = "any" };
            this.chkIncludeUnsuccessful = new CheckBox { Text = "Only include builds that are \"succeeded\"", Checked = true };

            this.Controls.Add(
                new SlimFormField("Team project:", this.txtTeamProject),
                new SlimFormField("Build definition:", this.txtBuildDefinition),
                new SlimFormField("Build number:", this.txtBuildNumber),
                new SlimFormField("Artifact name:", this.txtArtifactName)
                {
                    HelpText = "By default, the BuildMaster build artifact created from the output will use the name of the build definition. To use a different name, specify one here."
                },
                new SlimFormField("Options:", this.chkIncludeUnsuccessful)
            );
        }
    }
}
