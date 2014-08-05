using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Configurers.Extension;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.TFS
{
    internal sealed class TfsConfigurerEditor : ExtensionConfigurerEditorBase
    {
        private ValidatingTextBox txtBaseUrl;
        private ValidatingTextBox txtUserName;
        private PasswordTextBox txtPassword;
        private ValidatingTextBox txtDomain;
        private DropDownList ddlAuthentication;

        public override void BindToForm(ExtensionConfigurerBase extension)
        {
            var configurer = (TfsConfigurer)extension;

            if (configurer.UseSystemCredentials)
                ddlAuthentication.SelectedValue = "system";
            else
                ddlAuthentication.SelectedValue = "specify";

            this.txtBaseUrl.Text = configurer.BaseUrl;
            this.txtUserName.Text = configurer.UserName;
            this.txtPassword.Text = configurer.Password;
            this.txtDomain.Text = configurer.Domain;
        }

        public override ExtensionConfigurerBase CreateFromForm()
        {
            return new TfsConfigurer
            {
                BaseUrl = this.txtBaseUrl.Text,
                UserName = this.txtUserName.Text,
                Password = this.txtPassword.Text,
                Domain = this.txtDomain.Text,
                UseSystemCredentials = (this.ddlAuthentication.SelectedValue == "system")
            };
        }

        public override void InitializeDefaultValues()
        {
            BindToForm(new TfsConfigurer());
        }

        protected override void CreateChildControls()
        {
            this.txtBaseUrl = new ValidatingTextBox();

            this.txtUserName = new ValidatingTextBox();

            this.txtDomain = new ValidatingTextBox();

            this.txtPassword = new PasswordTextBox();

            ddlAuthentication = new DropDownList();
            ddlAuthentication.Items.Add(new ListItem("System", "system"));
            ddlAuthentication.Items.Add(new ListItem("Specify account...", "specify"));

            var ffgAuthentication = new FormFieldGroup("Authentication",
                    "The method used for authenticating a connection to Team Foundation Server",
                    false,
                    new StandardFormField("Authentication:", ddlAuthentication)
                );

            var ffgCredentials = new FormFieldGroup("Credentials",
                    "Specify the credentials of the account you would like to use to connect to Team Foundation Server",
                    false,
                    new StandardFormField("Username:", txtUserName),
                    new StandardFormField("Password:", txtPassword),
                    new StandardFormField("Domain:", txtDomain)
                );

            this.Controls.Add(
                new FormFieldGroup("TFS Server Name",
                    "The name of the Team Foundation Server to connect to, e.g. http://tfsserver:8080/tfs",
                    false,
                    new StandardFormField(
                        "Server Name:",
                        txtBaseUrl,
                        new RenderClientScriptDelegator(w =>
                        {
                            w.WriteLine(
@"$().ready(function(){
    var onAuthorizationChange = function(){
        if($('#" + ddlAuthentication.ClientID + @" option:selected').val() == 'system') {
            $('#" + ffgCredentials.ClientID + @"').hide();
        }
        else {
            $('#" + ffgCredentials.ClientID + @"').show();
        }
    };
    onAuthorizationChange();
    $('#" + ddlAuthentication.ClientID + @"').change(onAuthorizationChange);
});");
                        })
                    )
                ),
                ffgAuthentication,
                ffgCredentials
              );
        }
    }
}
