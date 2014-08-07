using System;
using System.Linq;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Artifacts;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Extensibility.BuildImporters;
using Inedo.BuildMaster.Files;
using Inedo.BuildMaster.Web;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;

namespace Inedo.BuildMasterExtensions.TFS
{
    internal sealed class TfsHelper
    {
        private TfsConfigurer config;

        public TfsHelper(TfsConfigurer config)
        {
            this.config = config;
        }

        public IBuildDetail GetBuild(string teamProject, string buildDefinition, string buildNumber, bool includeUnsuccessful)
        {
            using (var collection = this.GetTeamProjectCollection())
            {
                return this.GetBuild(collection, teamProject, buildDefinition, buildNumber, includeUnsuccessful);
            }
        }
        public IBuildDetail GetBuild(TfsTeamProjectCollection collection, string teamProject, string buildDefinition, string buildNumber, bool includeUnsuccessful)
        {
            var buildService = collection.GetService<IBuildServer>();

            var spec = buildService.CreateBuildDetailSpec(teamProject, InedoLib.Util.CoalesceStr(buildDefinition, "*"));
            spec.BuildNumber = InedoLib.Util.CoalesceStr(buildNumber, "*");
            spec.MaxBuildsPerDefinition = 1;
            spec.QueryOrder = BuildQueryOrder.FinishTimeDescending;
            spec.Status = includeUnsuccessful ? (BuildStatus.Failed | BuildStatus.Succeeded | BuildStatus.PartiallySucceeded) : BuildStatus.Succeeded;

            var result = buildService.QueryBuilds(spec);
            
            return result.Builds.FirstOrDefault();
        }

        public TfsTeamProjectCollection GetTeamProjectCollection()
        {
            return TfsActionBase.GetTeamProjectCollection(this.config);
        }
    }
}
