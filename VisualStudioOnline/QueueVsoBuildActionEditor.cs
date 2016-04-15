using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;
using Inedo.Web.Controls.SimpleHtml;

namespace Inedo.BuildMasterExtensions.TFS.VisualStudioOnline
{
    internal sealed class QueueVsoBuildActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtTeamProject;
        private ValidatingTextBox txtBuildDefinition;
        private CheckBox chkWaitForCompletion;
        private CheckBox chkValidateBuild;
        private CheckBox chkCreateBuildVariable;

        public override void BindToForm(ActionBase extension)
        {
            var action = (QueueVsoBuildAction)extension;

            this.txtTeamProject.Text = action.TeamProject;
            this.txtBuildDefinition.Text = action.BuildDefinition;
            this.chkWaitForCompletion.Checked = action.WaitForCompletion;
            this.chkValidateBuild.Checked = action.ValidateBuild;
            this.chkCreateBuildVariable.Checked = action.CreateBuildNumberVariable;
        }

        public override ActionBase CreateFromForm()
        {
            return new QueueVsoBuildAction()
            {
                TeamProject = this.txtTeamProject.Text,
                BuildDefinition = this.txtBuildDefinition.Text,
                WaitForCompletion = this.chkWaitForCompletion.Checked,
                ValidateBuild = this.chkValidateBuild.Checked,
                CreateBuildNumberVariable = this.chkCreateBuildVariable.Checked
            };
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            this.txtTeamProject = new ValidatingTextBox { Required = true };
            this.txtBuildDefinition = new ValidatingTextBox { Required = true };

            this.chkWaitForCompletion = new CheckBox { Text = "Wait until the TFS build completes", Checked = true };
            this.chkValidateBuild = new CheckBox { Text = "Fail if the TFS build does not succeed", Checked = true };
            this.chkCreateBuildVariable = new CheckBox { Text = "Store the TFS build number as $TfsBuildNumber", Checked = true };

            this.Controls.Add(
                new SlimFormField(
                    "Team project:",
                    this.txtTeamProject
                ),
                new SlimFormField(
                    "Build definition:",
                    this.txtBuildDefinition
                ),
                new SlimFormField(
                    "Options:",
                    new Div(this.chkCreateBuildVariable),
                    new Div(this.chkWaitForCompletion),
                    new Div(this.chkValidateBuild)
                )
            );
        }
    }
}
