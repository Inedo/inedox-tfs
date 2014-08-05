using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;
using Inedo.Web.Controls.SimpleHtml;

namespace Inedo.BuildMasterExtensions.TFS
{
    internal sealed class TfsSourceControlProviderEditor : ProviderEditorBase
    {
        private ValidatingTextBox txtBaseUrl;
        private ValidatingTextBox txtUserName;
        private ValidatingTextBox txtDomain;
        private PasswordTextBox txtPassword;
        private DropDownList ddlAuthentication;

        public override void BindToForm(ProviderBase extension)
        {
            var tfsProvider = (TfsSourceControlProvider)extension;
            this.txtBaseUrl.Text = tfsProvider.BaseUrl;
            this.txtUserName.Text = tfsProvider.UserName;
            this.txtPassword.Text = tfsProvider.Password;
            this.txtDomain.Text = tfsProvider.Domain;

            if (tfsProvider.UseSystemCredentials)
                this.ddlAuthentication.SelectedValue = "system";
            else
                this.ddlAuthentication.SelectedValue = "specify";
        }

        public override ProviderBase CreateFromForm()
        {
            return new TfsSourceControlProvider
            {
                BaseUrl = this.txtBaseUrl.Text,
                UserName = this.txtUserName.Text,
                Password = this.txtPassword.Text,
                Domain = this.txtDomain.Text,
                UseSystemCredentials = (this.ddlAuthentication.SelectedValue == "system")
            };
        }

        protected override void CreateChildControls()
        {
            this.txtBaseUrl = new ValidatingTextBox { DefaultText = "ex: http://tfsserver:80/tfs", Required = true };

            this.txtUserName = new ValidatingTextBox();

            this.txtDomain = new ValidatingTextBox();

            this.txtPassword = new PasswordTextBox();

            ddlAuthentication = new DropDownList();
            ddlAuthentication.Items.Add(new ListItem("System", "system"));
            ddlAuthentication.Items.Add(new ListItem("Specify account...", "specify"));

            var ffgAuthentication = new SlimFormField("Authentication:", ddlAuthentication);

            var ffgCredentials = new Div(
                new SlimFormField("User name:", this.txtUserName),
                new SlimFormField("Password:", this.txtPassword),
                new SlimFormField("Domain:", this.txtDomain)
            );

            this.Controls.Add(
                new SlimFormField("TFS URL:", this.txtBaseUrl),
                ffgAuthentication,
                ffgCredentials,
                new RenderJQueryDocReadyDelegator(
                    w =>
                    {
                        w.Write("$('#{0}').change(function(){{", this.ddlAuthentication.ClientID);
                        w.Write("if($(this).val() == 'system') $('#{0}').hide(); else $('#{0}').show();", ffgCredentials.ClientID);
                        w.Write("});");
                        w.Write("$('#{0}').change();", this.ddlAuthentication.ClientID);
                    }
                )
            );
        }
    }
}
