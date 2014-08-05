using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;
using Inedo.Web.Controls.SimpleHtml;

namespace Inedo.BuildMasterExtensions.TFS
{
    internal sealed class TfsIssueTrackingProviderEditor : ProviderEditorBase
    {
        private ValidatingTextBox txtBaseUrl;
        private ValidatingTextBox txtCustomReleaseNumberFieldName;
        private ValidatingTextBox txtUserName;
        private ValidatingTextBox txtDomain;
        private PasswordTextBox txtPassword;
        private DropDownList ddlAuthentication;
        private CheckBox chkAllowHtml;

        public override void BindToForm(ProviderBase extension)
        {
            var provider = (TfsIssueTrackingProvider)extension;

            this.txtBaseUrl.Text = provider.BaseUrl;
            this.txtCustomReleaseNumberFieldName.Text = provider.CustomReleaseNumberFieldName;
            this.txtUserName.Text = provider.UserName;
            this.txtPassword.Text = provider.Password;
            this.txtDomain.Text = provider.Domain;
            this.chkAllowHtml.Checked = provider.AllowHtmlIssueDescriptions;

            if (provider.UseSystemCredentials)
                this.ddlAuthentication.SelectedValue = "system";
            else
                this.ddlAuthentication.SelectedValue = "specify";
        }

        public override ProviderBase CreateFromForm()
        {
            return new TfsIssueTrackingProvider
            {
                BaseUrl = this.txtBaseUrl.Text,
                CustomReleaseNumberFieldName = this.txtCustomReleaseNumberFieldName.Text,
                UserName = this.txtUserName.Text,
                Password = this.txtPassword.Text,
                Domain = this.txtDomain.Text,
                UseSystemCredentials = (this.ddlAuthentication.SelectedValue == "system"),
                AllowHtmlIssueDescriptions = this.chkAllowHtml.Checked
            };
        }

        protected override void CreateChildControls()
        {
            this.txtBaseUrl = new ValidatingTextBox { DefaultText = "ex: http://tfsserver:80/tfs", Required = true };

            this.txtCustomReleaseNumberFieldName = new ValidatingTextBox { DefaultText = "iteration" };
            
            this.txtUserName = new ValidatingTextBox();

            this.txtDomain = new ValidatingTextBox();

            this.txtPassword = new PasswordTextBox();

            this.chkAllowHtml = new CheckBox { Text = "Allow HTML in issue descriptions" };
            
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
                new SlimFormField("Release number field:", this.txtCustomReleaseNumberFieldName)
                {
                    HelpText = HelpText.FromHtml("If you store your TFS work item release numbers in a custom field, enter the full field \"refname\" of the custom field here - otherwise leave this field blank and \"Iteration\" will be used to retrieve them.<br /><br />For more information on custom work item types, visit <a href=\"http://msdn.microsoft.com/en-us/library/ms400654.aspx\" target=\"_blank\">http://msdn.microsoft.com/en-us/library/ms400654.aspx</a>")
                },
                new SlimFormField("Options:", this.chkAllowHtml),
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
