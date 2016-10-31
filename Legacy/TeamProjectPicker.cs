using System;
using System.Linq;
using System.Web.UI.WebControls;

namespace Inedo.BuildMasterExtensions.TFS
{
    internal sealed class TeamProjectPicker : DropDownList
    {
        private TfsConfigurer config;

        public TeamProjectPicker(TfsConfigurer config)
        {
            this.config = config;
            this.AutoPostBack = true;
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            this.Items.Add(string.Empty);
            if (this.config == null || this.config.BaseUrl == null)
                return;
            var projectNames = config.GetTeamProjects().OrderBy(name => name);
            this.Items.AddRange(projectNames.Select(p => new ListItem(p)).ToArray());
        }
    }
}
