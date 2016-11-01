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
        private TeamProjectPicker txtTeamProject;
        private BuildDefinitionPicker txtBuildDefinition;
        private DropDownList ddlBuildNumber;
        private ValidatingTextBox txtBuildNumberPattern;
        private CheckBox chkCreateBuildNumberVariable;

        public override void BindToForm(BuildImporterTemplateBase extension)
        {
            var template = (TfsBuildImporterTemplate)extension;
            this.txtArtifactName.Text = template.ArtifactName;
            this.txtTeamProject.Text = template.TeamProject;
            this.txtBuildDefinition.InitialSelection = template.BuildDefinition;
            this.txtBuildDefinition.TeamProject = template.TeamProject;
            this.ddlBuildNumber.SelectedValue = template.BuildNumberLocked ? (template.IncludeUnsuccessful ? "last" : "success") : string.Empty;
            this.txtBuildNumberPattern.Text = template.BuildNumberPattern;
            this.chkCreateBuildNumberVariable.Checked = template.CreateBuildNumberVariable;
        }
        public override BuildImporterTemplateBase CreateFromForm()
        {
            var config = (TfsConfigurer)this.GetExtensionConfigurer();

            return new TfsBuildImporterTemplate
            {
                ArtifactName = this.txtArtifactName.Text,
                TeamProject = this.txtTeamProject.Text,
                BuildDefinition = this.txtBuildDefinition.Text,
                IncludeUnsuccessful = this.ddlBuildNumber.SelectedValue == "last",
                BuildNumberLocked = !string.IsNullOrEmpty(this.ddlBuildNumber.SelectedValue),
                BuildNumberPattern = this.txtBuildNumberPattern.Text,
                ServerId = config.ServerId.GetValueOrDefault(),
                CreateBuildNumberVariable = this.chkCreateBuildNumberVariable.Checked
            };
        }

        protected override void CreateChildControls()
        {
            var config = (TfsConfigurer)this.GetExtensionConfigurer();

            this.txtArtifactName = new ValidatingTextBox { DefaultText = "Same as project name" };
            this.txtTeamProject = new TeamProjectPicker(config);
            this.txtBuildDefinition = new BuildDefinitionPicker(config);
            this.txtTeamProject.SelectedIndexChanged += (s, e) => { this.txtBuildDefinition.TeamProject = this.txtTeamProject.SelectedValue; };
            this.ddlBuildNumber = new DropDownList
            {
                Items =
                {
                    new ListItem("allow selection at build time", string.Empty),
                    new ListItem("last succeeded build", "success"),
                    new ListItem("last completed build", "last")
                }
            };
            this.txtBuildNumberPattern = new ValidatingTextBox { Text = "_(?<num>[^_]+)$" };
            this.chkCreateBuildNumberVariable = new CheckBox() { Text = "Store the TFS build number as $TfsBuildNumber", Checked = true };

            this.Controls.Add(
                new SlimFormField("Artifact name:", this.txtArtifactName),
                new SlimFormField("Team project:", this.txtTeamProject),
                new SlimFormField("Build definition:", txtBuildDefinition),
                new SlimFormField("Build number:",
                    new Div(this.ddlBuildNumber),
                    new Div(this.chkCreateBuildNumberVariable)
                ),
                new SlimFormField("Capture pattern:", this.txtBuildNumberPattern)
                {
                    HelpText = "When importing a build, you can opt to use the TFS build number; however, because TFS build numbers "
                    + "can be 1,000 characters (or more), up to 10 characters must be extracted to fit the BuildMaster build number "
                    + "using a Regex capture group named \"num\". The default TFS Build Number Format is $(BuildDefinitionName)_$(Date:yyyyMMdd)$(Rev:.r); "
                    + " and thus the pattern _(?<num>[^_]+)$ will extract the date and revision."
                }
            );
        }
    }
}
