using System;
using System.Linq;
using System.Web.UI.WebControls;

namespace Inedo.BuildMasterExtensions.TFS
{
    internal sealed class BuildDefinitionPicker : DropDownList
    {
        private TfsConfigurer config;

        public BuildDefinitionPicker(TfsConfigurer config)
        {
            this.config = config;
        }

        public string TeamProject { get; set; }
        public string InitialSelection { get; set; }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            if (!string.IsNullOrEmpty(this.TeamProject))
            {
                var buildDefinitions = config.GetBuildDefinitions(this.TeamProject).OrderBy(def => def);

                this.Items.Clear();
                this.Items.AddRange(
                    buildDefinitions.Select(d => new ListItem(d) 
                        { 
                            Selected = string.Equals(this.InitialSelection, d, StringComparison.OrdinalIgnoreCase)
                        }).ToArray()
                    );
            }
        }
    }
}
