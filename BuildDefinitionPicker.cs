using System.Linq;
using System.Web.UI.WebControls;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Agents;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Server;

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

        protected override void OnPreRender(System.EventArgs e)
        {
            base.OnPreRender(e);

            if (string.IsNullOrEmpty(this.TeamProject)) return;

            string[] buildDefinitions;
            using (var agent = Util.Agents.CreateAgentFromId(config.ServerId))
            {
                buildDefinitions = agent.GetService<IRemoteMethodExecuter>().InvokeFunc((cfg,proj) =>
                {
                    using (var collection = TfsActionBase.GetTeamProjectCollection(cfg))
                    {
                        var buildService = collection.GetService<IBuildServer>();
                        return buildService.QueryBuildDefinitions(proj).Select(d => d.Name).ToArray();
                    }
                }, config, this.TeamProject);
            }

            this.Items.Clear();
            this.Items.AddRange(buildDefinitions.Select(d => new ListItem(d)).ToArray());
        }
    }
}
