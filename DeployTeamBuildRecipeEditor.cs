using System.Linq;
using System.Web.UI;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Recipes;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;
using Inedo.Web.Controls.SimpleHtml;

namespace Inedo.BuildMasterExtensions.TFS
{
    internal sealed class DeployTeamBuildRecipeEditor : RecipeEditorBase
    {
        private sealed class DeployTeamBuildRecipeWizardSteps : RecipeWizardSteps
        {
            public RecipeWizardStep About = new RecipeWizardStep("About");
            public RecipeWizardStep TfsConnection = new RecipeWizardStep("TFS Connection");
            public RecipeWizardStep TfsBuildDefinition = new RecipeWizardStep("Build Definition");
            public RecipeWizardStep SelectDeploymentPath = new RecipeWizardStep("Deployment");
            public RecipeWizardStep Summary = new RecipeWizardStep("Summary");

            public override RecipeWizardStep[] WizardStepOrder
            {
                get
                {
                    return new[] { this.About, this.TfsConnection, this.TfsBuildDefinition, base.SpecifyApplicationProperties, base.SpecifyWorkflowOrder, this.SelectDeploymentPath, this.Summary };
                }
            }
        }

        private DeployTeamBuildRecipeWizardSteps wizardSteps = new DeployTeamBuildRecipeWizardSteps();

        public override bool DisplayAsWizard { get { return true; } }

        private string TargetDeploymentPath
        {
            get { return (string)this.ViewState["TargetDeploymentPath"]; }
            set { this.ViewState["TargetDeploymentPath"] = value; }
        }

        private string TeamProject
        {
            get { return (string)this.ViewState["TeamProject"]; }
            set { this.ViewState["TeamProject"] = value; }
        }

        private string BuildDefinition
        {
            get { return (string)this.ViewState["BuildDefinition"]; }
            set { this.ViewState["BuildDefinition"] = value; }
        }

        public override RecipeWizardSteps GetWizardStepsControl()
        {
            return this.wizardSteps;
        }

        public override RecipeBase CreateFromForm()
        {
            return new DeployTeamBuildRecipe()
            {
                TargetDeploymentPath = this.TargetDeploymentPath,
                TeamProject = this.TeamProject,
                BuildDefinition = this.BuildDefinition
            };
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            this.CreateAboutControls();
            this.CreateTfsConnectionControls();
            this.CreateSelectProjectControls();
            this.CreateSelectDeploymentPathControls();
            this.CreateSummaryControls();
        }

        private void CreateAboutControls()
        {
            this.wizardSteps.About.Controls.Add(
                new H2("About the ", new I("Deploy TFS/TeamBuild"), " Wizard"),
                new P(
                    "This wizard will create a basic application that imports build artifact from a TeamBuild's drop folder then deploys that artifact to a target folder. ",
                    "It's meant to be a starting point and, once the wizard completes, you can add additional actions to the deployment plan that can ",
                    "do all sorts of things, such as deploying to multiple servers, stopping/starting service, etc."
                ),
                new P(
                    "To learn more about BuildMaster integration, see the ",
                    new A("TFS Extension") { Href = "http://inedo.com/buildmaster/extensions/tfs", Target = "_blank" },
                    " for more details."
                )
            );
        }

        private void CreateTfsConnectionControls()
        {
            var defaultCfg = (TfsConfigurer)this.GetExtensionConfigurer();
            var ctlError = new InfoBox { BoxType = InfoBox.InfoBoxTypes.Error, Visible = false };

            var txtBaseUrl = new ValidatingTextBox
            {
                Required = true,
                Text = defaultCfg.BaseUrl,
                Width = 350
            };

            var txtUserName = new ValidatingTextBox
            {
                DefaultText = "System credentials",
                Text = defaultCfg.UserName,
                Width = 350
            };
            var txtPassword = new PasswordTextBox
            {
                Text = defaultCfg.Password,
                Width = 350
            };
            var txtDomain = new ValidatingTextBox
            {
                Text = defaultCfg.Domain,
                Width = 350
            };

            txtBaseUrl.ServerValidate +=
                (s, e) =>
                {
                    var configurer = new TfsConfigurer
                    {
                        BaseUrl = txtBaseUrl.Text,
                        UserName = txtUserName.Text,
                        Password = txtPassword.Text,
                        UseSystemCredentials = string.IsNullOrWhiteSpace(txtUserName.Text)
                    };

                    var errorMessage = configurer.TestConnection();
                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        e.IsValid = false;
                        ctlError.Visible = true;
                        ctlError.Controls.Add(new P("An error occurred while attempting to connect: " + errorMessage));
                    }
                };

            this.wizardSteps.TfsConnection.Controls.Add(
                ctlError,
                new FormFieldGroup("TFS Server Name",
                    "The name of the Team Foundation Server to connect to, e.g. http://tfsserver:8080/tfs",
                    false,
                    new StandardFormField("Server Name:", txtBaseUrl)
                ),
                new FormFieldGroup("Credentials",
                    "Specify the credentials of the account you would like to use to connect to Team Foundation Server",
                    false,
                    new StandardFormField("Username:", txtUserName),
                    new StandardFormField("Password:", txtPassword),
                    new StandardFormField("Domain:", txtDomain)
                )
            );

            this.WizardStepChange += (s, e) =>
            {
                if (e.CurrentStep != this.wizardSteps.TfsConnection)
                    return;

                defaultCfg.BaseUrl = txtBaseUrl.Text;
                defaultCfg.UserName = txtUserName.Text;
                defaultCfg.Password = txtPassword.Text;
                var defaultProfile = StoredProcs
                        .ExtensionConfiguration_GetConfigurations(TfsConfigurer.TypeQualifiedName)
                        .Execute()
                        .Where(p => p.Default_Indicator == Domains.YN.Yes)
                        .FirstOrDefault() ?? new Tables.ExtensionConfigurations();

                StoredProcs
                    .ExtensionConfiguration_SaveConfiguration(
                        Util.NullIf(defaultProfile.ExtensionConfiguration_Id, 0),
                        TfsConfigurer.TypeQualifiedName,
                        defaultProfile.Profile_Name ?? "Default",
                        Util.Persistence.SerializeToPersistedObjectXml(defaultCfg),
                        Domains.YN.Yes)
                    .Execute();
            };
        }

        private void CreateSelectProjectControls()
        {
            var config = (TfsConfigurer)this.GetExtensionConfigurer();

            var ddlTeamProject = new TeamProjectPicker(config);

            var ddlBuildDefinition = new BuildDefinitionPicker(config);
            ddlTeamProject.SelectedIndexChanged += (s, e) => { ddlBuildDefinition.TeamProject = ddlTeamProject.SelectedValue; };

            this.wizardSteps.TfsBuildDefinition.Controls.Add(
                new FormFieldGroup("Team Project",
                    "The name of the team project.",
                    false,
                    new StandardFormField("Team Project:", ddlTeamProject)
                ),
                new FormFieldGroup("Build Definition",
                    "The name of the build definition used to create a build.",
                    false,
                    new StandardFormField("Build Definition:", ddlBuildDefinition)
                )
            );

            this.WizardStepChange += (s, e) =>
            {
                if (e.CurrentStep != this.wizardSteps.TfsBuildDefinition) return;

                this.TeamProject = ddlTeamProject.SelectedValue;
                this.BuildDefinition = ddlBuildDefinition.SelectedValue;
            };
        }

        private void CreateSelectDeploymentPathControls()
        {
            var ctlTargetDeploymentPath = new SourceControlFileFolderPicker()
            {
                DisplayMode = SourceControlBrowser.DisplayModes.Folders,
                ServerId = 1,
                Width = 350,
                Text = @"C:\TfsTestDeploys\"
            };


            this.wizardSteps.SelectDeploymentPath.Controls.Add(
                new FormFieldGroup(
                    "Deployment Target",
                    "Select a directory where the artifact will be deployed. You can change the server/path in which this gets deployed to later.",
                    true,
                    new StandardFormField("Target Directory:", ctlTargetDeploymentPath)
                )
            );

            this.WizardStepChange += (s, e) =>
            {
                if (e.CurrentStep != this.wizardSteps.SelectDeploymentPath)
                    return;

                this.TargetDeploymentPath = ctlTargetDeploymentPath.Text;
            };
        }

        private void CreateSummaryControls()
        {
            this.wizardSteps.Summary.Controls.Add(
                new FormFieldGroup(
                    "Summary",
                    "This is a summary of the Deploy Build from TFS wizard application - once created, you can change it to customize it however you'd like "
                        + "by editing the application's deployment plan.",
                    true,
                    new StandardFormField("", new Summary(this))
                )
            );
        }

        private sealed class Summary : Control
        {
            private DeployTeamBuildRecipeEditor editor;

            public Summary(DeployTeamBuildRecipeEditor editor)
            {
                this.editor = editor;
            }

            protected override void Render(HtmlTextWriter writer)
            {
                if (editor.TargetDeploymentPath == null || string.IsNullOrEmpty(editor.TeamProject) || string.IsNullOrEmpty(editor.BuildDefinition))
                    return;

                writer.Write(
                    "<p><strong>Team Project: </strong> {0}</p>" +
                    "<p><strong>Build Definition: </strong> {1}</p>" +
                    "<p><strong>Deployment Target Path: </strong> {2}</p>",
                    editor.TeamProject,
                    editor.BuildDefinition,
                    editor.TargetDeploymentPath
                );
            }
        }
    }
}
