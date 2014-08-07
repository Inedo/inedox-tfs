using System;
using System.Linq;
using System.Web.UI.WebControls;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Agents;
using Microsoft.TeamFoundation.Server;

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

            this.Items.Add("");

            string[] projectNames;
            using (var agent = Util.Agents.CreateAgentFromId(config.ServerId))
            {
                projectNames = agent.GetService<IRemoteMethodExecuter>().InvokeFunc(cfg =>
                {
                    using (var collection = TfsActionBase.GetTeamProjectCollection(cfg))
                    {
                        var structureService = collection.GetService<ICommonStructureService>();
                        return structureService.ListProjects().Select(p => p.Name).ToArray();
                    }
                }, config);
            }
            this.Items.AddRange(projectNames.Select(p => new ListItem(p)).ToArray());
        }
    }
}
