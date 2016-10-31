using System;
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
        private DropDownList ddlUseWiql;
        private ValidatingTextBox txtWiql;
        private ValidatingTextBox txtCustomClosedStates;

        public override void BindToForm(ProviderBase extension)
        {
            var provider = (TfsIssueTrackingProvider)extension;

            this.txtBaseUrl.Text = provider.BaseUrl;
            this.txtCustomReleaseNumberFieldName.Text = provider.CustomReleaseNumberFieldName;
            this.txtUserName.Text = provider.UserName;
            this.txtPassword.Text = provider.Password;
            this.txtDomain.Text = provider.Domain;
            this.chkAllowHtml.Checked = provider.AllowHtmlIssueDescriptions;
            this.txtWiql.Text = provider.CustomWiql;
            this.txtCustomClosedStates.Text = string.Join(Environment.NewLine, provider.CustomClosedStates ?? new string[0]);

            if (provider.UseSystemCredentials)
                this.ddlAuthentication.SelectedValue = "system";
            else
                this.ddlAuthentication.SelectedValue = "specify";

            if (string.IsNullOrWhiteSpace(provider.CustomWiql))
                this.ddlUseWiql.SelectedValue = "False";
            else
                this.ddlUseWiql.SelectedValue = "True";
        }
        public override ProviderBase CreateFromForm()
        {
            return new TfsIssueTrackingProvider
            {
                BaseUrl = this.txtBaseUrl.Text.Trim(),
                CustomReleaseNumberFieldName = this.txtCustomReleaseNumberFieldName.Text.Trim(),
                UserName = this.txtUserName.Text.Trim(),
                Password = this.txtPassword.Text,
                Domain = this.txtDomain.Text.Trim(),
                UseSystemCredentials = (this.ddlAuthentication.SelectedValue == "system"),
                AllowHtmlIssueDescriptions = this.chkAllowHtml.Checked,
                CustomWiql = bool.Parse(this.ddlUseWiql.SelectedValue) ? this.txtWiql.Text : null,
                CustomClosedStates = this.txtCustomClosedStates.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            };
        }

        protected override void CreateChildControls()
        {
            this.txtBaseUrl = new ValidatingTextBox
            {
                DefaultText = "ex: http://tfsserver:80/tfs",
                Required = true
            };

            this.txtCustomReleaseNumberFieldName = new ValidatingTextBox { DefaultText = "iteration" };

            this.txtUserName = new ValidatingTextBox();

            this.txtDomain = new ValidatingTextBox();

            this.txtPassword = new PasswordTextBox();

            this.chkAllowHtml = new CheckBox { Text = "Allow HTML in issue descriptions" };

            this.txtCustomClosedStates = new ValidatingTextBox()
            {
                TextMode = TextBoxMode.MultiLine,
                Rows = 3,
                DefaultText = "Closed\r\nResolved"
            };

            ddlAuthentication = new DropDownList
            {
                ID = "ddlAuthentication",
                Items =
                {
                    new ListItem("System", "system"),
                    new ListItem("Specify account...", "specify")
                }
            };

            this.ddlUseWiql = new DropDownList
            {
                ID = "ddlUseWiql",
                Items =
                {
                    new ListItem("Not using a custom query", "False"),
                    new ListItem("Custom WIQL query", "True")
                }
            };

            var ffgAuthentication = new SlimFormField("Authentication:", ddlAuthentication);

            var ffgCredentials = new Div(
                new SlimFormField("User name:", this.txtUserName),
                new SlimFormField("Password:", this.txtPassword),
                new SlimFormField("Domain:", this.txtDomain)
            );

            this.txtWiql = new ValidatingTextBox
            {
                TextMode = TextBoxMode.MultiLine,
                Rows = 5
            };

            var ctlWiql = new SlimFormField("WIQL query:", this.txtWiql)
            {
                HelpText = "This will be sent to TFS directly, after BuildMaster variables have been replaced. This WIQL query should return all issues "
                         + "for the current BuildMaster release. Any release-level or higher BuildMaster variables may be used in this query."
            };

            var ctlNoWiql = new SlimFormField("Release number field:", this.txtCustomReleaseNumberFieldName)
            {
                HelpText = new LiteralHtml("If you store your TFS work item release numbers in a custom field, enter the full field \"refname\" of the custom field here - otherwise leave this field blank and \"Iteration\" will be used to retrieve them.<br /><br />For more information on custom work item types, visit <a href=\"http://msdn.microsoft.com/en-us/library/ms400654.aspx\" target=\"_blank\">http://msdn.microsoft.com/en-us/library/ms400654.aspx</a>", false)
            };

            this.Controls.Add(
                new SlimFormField("TFS URL:", this.txtBaseUrl),
                ffgAuthentication,
                ffgCredentials,
                new SlimFormField("Query mode:", this.ddlUseWiql),
                ctlNoWiql,
                ctlWiql,
                new SlimFormField("Options:", this.chkAllowHtml),
                new RenderJQueryDocReadyDelegator(
                    w =>
                    {
                        w.Write("$('#{0}').change(function(){{", this.ddlAuthentication.ClientID);
                        w.Write("if($(this).val() == 'system') $('#{0}').hide(); else $('#{0}').show();", ffgCredentials.ClientID);
                        w.Write("});");
                        w.Write("$('#{0}').change();", this.ddlAuthentication.ClientID);

                        w.Write("$('#{0}').change(function(){{", this.ddlUseWiql.ClientID);
                        w.Write("if($(this).val() == 'False') {{ $('#{0}').hide(); $('#{1}').show(); }} else {{ $('#{0}').show(); $('#{1}').hide(); }}", ctlWiql.ClientID, ctlNoWiql.ClientID);
                        w.Write("});");
                        w.Write("$('#{0}').change();", this.ddlUseWiql.ClientID);
                    }
                ),
                new SlimFormField("Closed statuses:", this.txtCustomClosedStates)
                {
                    HelpText = "The newline-separated list of issue states in TFS that BuildMaster will use to determine if a synchronized issue is closed."
                }
            );
        }
    }
}
