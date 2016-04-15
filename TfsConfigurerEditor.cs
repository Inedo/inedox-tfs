using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Configurers.Extension;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;
using Inedo.Web.Controls.SimpleHtml;

namespace Inedo.BuildMasterExtensions.TFS
{
    internal sealed class TfsConfigurerEditor : ExtensionConfigurerEditorBase
    {
        private ValidatingTextBox txtBaseUrl;
        private ValidatingTextBox txtUserName;
        private PasswordTextBox txtPassword;
        private ValidatingTextBox txtDomain;
        private DropDownList ddlAuthentication;
        private ActionServerPicker ctlServerPicker;

        public override void BindToForm(ExtensionConfigurerBase extension)
        {
            var configurer = (TfsConfigurer)extension;

            if (configurer.UseSystemCredentials)
                this.ddlAuthentication.SelectedValue = "system";
            else
                this.ddlAuthentication.SelectedValue = "specify";

            this.ctlServerPicker.ServerId = configurer.ServerId;
            this.txtBaseUrl.Text = configurer.BaseUrl;
            this.txtUserName.Text = configurer.UserName;
            this.txtPassword.Text = configurer.Password;
            this.txtDomain.Text = configurer.Domain;
        }

        public override ExtensionConfigurerBase CreateFromForm()
        {
            return new TfsConfigurer
            {
                ServerId = this.ctlServerPicker.ServerId,
                BaseUrl = this.txtBaseUrl.Text,
                UserName = this.txtUserName.Text,
                Password = this.txtPassword.Text,
                Domain = this.txtDomain.Text,
                UseSystemCredentials = (this.ddlAuthentication.SelectedValue == "system")
            };
        }

        public override void InitializeDefaultValues()
        {
            this.BindToForm(new TfsConfigurer());
        }

        protected override void CreateChildControls()
        {
            this.ctlServerPicker = new ActionServerPicker { ServerId = 1, ID = "ctlServerPicker" };

            this.txtBaseUrl = new ValidatingTextBox();

            this.txtUserName = new ValidatingTextBox();

            this.txtDomain = new ValidatingTextBox();

            this.txtPassword = new PasswordTextBox();

            this.ddlAuthentication = new DropDownList { ID = "ddlAuthentication" };
            this.ddlAuthentication.Items.Add(new ListItem("System", "system"));
            this.ddlAuthentication.Items.Add(new ListItem("Specify account...", "specify"));

            var sffAuthentication = new SlimFormField("Authentication:", this.ddlAuthentication) { ID = "sffAuthentication" };

            var sffCredentials = new SlimFormField("Credentials:",
                new Div(new Div("Username:"), new Div(this.txtUserName)),
                new Div(new Div("Password / access token:"), new Div(this.txtPassword)),
                new Div(new Div("Domain:"), new Div(this.txtDomain))
                )
            {
                ID = "sffCredentials",
                HelpText = new LiteralHtml("For Visual Studio Online, either <a href=\"https://www.visualstudio.com/en-us/integrate/get-started/auth/overview\" target=\"_blank\">Alternate Credentials or Personal Access Tokens</a> must be used in the password field.", false)
            };

            this.Controls.Add(
                new SlimFormField("TFS client:", this.ctlServerPicker) { HelpText = "The server where the TFS Client (Visual Studio or Team Explorer) is installed." },
                new SlimFormField("TFS collection url:", this.txtBaseUrl) { HelpText = "The is the api of the TFS Collection to use, e.g. http://tfsserver:8080/tfs" },
                sffAuthentication,
                sffCredentials,
                new RenderJQueryDocReadyDelegator(w =>
                        {
                            w.WriteLine(
@"  var onAuthorizationChange = function(){
        if($('#" + ddlAuthentication.ClientID + @" option:selected').val() == 'system') {
            $('#" + sffCredentials.ClientID + @"').hide();
        }
        else {
            $('#" + sffCredentials.ClientID + @"').show();
        }
    };
    onAuthorizationChange();
    $('#" + this.ddlAuthentication.ClientID + @"').change(onAuthorizationChange);
");
                        })
              );
        }
    }
}
