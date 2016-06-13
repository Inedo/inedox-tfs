using System;
using System.ComponentModel;
using System.Linq;
using Inedo.Agents;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Web;
using Inedo.Documentation;
using Inedo.Serialization;
using Microsoft.TeamFoundation.Build.Client;

namespace Inedo.BuildMasterExtensions.TFS
{
    [DisplayName("Queue TFS Build")]
    [Description("Queues a new build in TFS.")]
    [RequiresInterface(typeof(IFileOperationsExecuter))]
    [CustomEditor(typeof(CreateTfsBuildActionEditor))]
    [Tag(Tags.Builds)]
    [Tag("tfs")]
    public sealed class CreateTfsBuildAction : TfsActionBase
    {
        /// <summary>
        /// Gets or sets the build number if not empty, or includes all builds in the search.
        /// </summary>
        public string BuildNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the action should wait until the build completes before continuing.
        /// </summary>
        [Persistent]
        public bool WaitForCompletion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the action should validate a successful build status.
        /// </summary>
        [Persistent]
        public bool ValidateBuild { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the action should create the $TfsBuildNumber build variable 
        /// containing the value of the TFS build number.
        /// </summary>
        [Persistent]
        public bool CreateBuildNumberVariable { get; set; } = true;

        public override ExtendedRichDescription GetActionDescription()
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Queue TFS Build for ", new Hilite(this.TeamProject)
                ),
                new RichDescription(
                    "using the build definition ",
                    new Hilite(this.BuildDefinition),
                    this.WaitForCompletion ? " and wait until the build completes" + (this.ValidateBuild ? " successfully" : "") : "",
                    "."
                )
            );
        }

        protected override void Execute()
        {
            var collection = this.GetTeamProjectCollection();

            var buildService = collection.GetService<IBuildServer>();
            var buildDefinition = buildService.GetBuildDefinition(this.TeamProject, this.BuildDefinition);

            if (buildDefinition == null)
                throw new InvalidOperationException($"Build definition \"{this.BuildDefinition}\" was not found.");

            this.LogInformation($"Queueing build of {this.TeamProject}, build definition {Util.CoalesceStr(this.BuildDefinition, "any")}...");
            var queuedBuild = buildService.QueueBuild(buildDefinition);

            this.LogInformation($"Build number \"{queuedBuild.Build.BuildNumber}\" created for definition \"{queuedBuild.BuildDefinition.Name}\".");

            if (this.CreateBuildNumberVariable)
            {
                this.LogDebug($"Setting $TfsBuildNumber build variable to {queuedBuild.Build.BuildNumber}...");
                DB.Variables_CreateOrUpdateVariableDefinition(
                    Variable_Name: "TfsBuildNumber",
                    Environment_Id: null,
                    Server_Id: null,
                    ApplicationGroup_Id: null,
                    Application_Id: this.Context.ApplicationId,
                    Deployable_Id: null,
                    Release_Number: this.Context.ReleaseNumber,
                    Build_Number: this.Context.BuildNumber,
                    Execution_Id: null,
                    Promotion_Id: null,
                    Value_Text: queuedBuild.Build.BuildNumber,
                    Sensitive_Indicator: false
                );

                this.LogInformation("$TfsBuildNumber build variable set to: " + queuedBuild.Build.BuildNumber);
            }

            if (this.WaitForCompletion)
            {
                this.LogInformation("Waiting for build completion...");
                queuedBuild.StatusChanged +=
                    (s, e) =>
                    {
                        this.ThrowIfCanceledOrTimeoutExpired();
                        this.LogDebug("TFS Build status reported: " + ((IQueuedBuild)s).Status);
                    };
                queuedBuild.WaitForBuildCompletion(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(this.Timeout));
            }

            if (this.ValidateBuild)
                this.ValidateBuildStatus();

            this.LogInformation($"TFS build {queuedBuild.Build.BuildNumber} created.");
        }

        private void ValidateBuildStatus()
        {
            this.LogDebug("Validating build completed successfully...");
            var collection = this.GetTeamProjectCollection();

            var buildService = collection.GetService<IBuildServer>();
            var buildDefinition = buildService.GetBuildDefinition(this.TeamProject, this.BuildDefinition);

            var spec = buildService.CreateBuildDetailSpec(this.TeamProject, this.BuildDefinition);
            spec.MaxBuildsPerDefinition = 1;
            spec.QueryOrder = BuildQueryOrder.FinishTimeDescending;

            var result = buildService.QueryBuilds(spec);
            var build = result.Builds.FirstOrDefault();
            if (build == null)
                throw new InvalidOperationException($"Build {this.BuildNumber} for team project {this.TeamProject} definition {this.BuildDefinition} did not return any builds.");

            if (build.Status != BuildStatus.Succeeded)
            {
                this.LogError(
                    $"There was a build error during the TFS Build {this.BuildNumber} for team project {this.TeamProject} " +
                    "and the \"Fail if the TFS build does not succeed\" option was selected for this build."
                );

                var buildErrors = InformationNodeConverters.GetBuildErrors(build);

                this.LogError("Build errors were reported:");
                foreach (var error in buildErrors)
                    this.LogError($"{error.ServerPath}; Line {error.LineNumber}: ErrMsg {error.Message}");
            }
        }
    }
}
