using System;
using System.Linq;
using System.Threading;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Web;
using Inedo.Data;

namespace Inedo.BuildMasterExtensions.TFS.VisualStudioOnline
{
    [ActionProperties(
        "Queue Build in VS Online",
        "Queues a new build in Visual Studio Online or TFS 2015.",
        DefaultToLocalServer = true)]
    [RequiresInterface(typeof(IFileOperationsExecuter))]
    [RequiresInterface(typeof(IRemoteZip))]
    [CustomEditor(typeof(QueueVsoBuildActionEditor))]
    [Tag(Tags.Builds)]
    [Tag("tfs")]
    public sealed class QueueVsoBuildAction : TfsActionBase
    {
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

        public override ActionDescription GetActionDescription()
        {
            return new ActionDescription(
                new ShortActionDescription(
                    "Queue VS Online Build for ", new Hilite(this.TeamProject)
                ),
                new LongActionDescription(
                    "using the build definition ",
                    new Hilite(this.BuildDefinition),
                    this.WaitForCompletion ? " and wait until the build completes" + (this.ValidateBuild ? " successfully" : "") : "",
                    "."
                )
            );
        }

        protected override void Execute()
        {
            var configurer = this.GetExtensionConfigurer();

            if (configurer == null)
                throw new InvalidOperationException("A configurer must be configured or selected in order to queue a VS online build.");
            if (string.IsNullOrEmpty(configurer.BaseUrl))
                throw new InvalidOperationException("The base URL property of the TFS configurer must be set to queue a VS online build.");

            var api = new TfsRestApi(configurer.BaseUrl, this.TeamProject)
            {
                UserName = string.IsNullOrEmpty(configurer.Domain) ? configurer.UserName : string.Format("{0}\\{1}", configurer.Domain, configurer.UserName),
                Password = configurer.Password
            };

            this.LogDebug("Finding VSO build definition...");
            var definition = api.GetBuildDefinitions()
                .FirstOrDefault(d => string.IsNullOrEmpty(this.BuildDefinition) || string.Equals(d.name, this.BuildDefinition, StringComparison.OrdinalIgnoreCase));

            if (definition == null)
                throw new InvalidOperationException("Could not find a build definition named: " + Util.CoalesceStr(this.BuildDefinition, "any"));

            this.LogInformation($"Queueing VSO build of {this.TeamProject}, build definition {definition.name}...");

            var queuedBuild = api.QueueBuild(definition.id);

            this.LogInformation($"Build number \"{queuedBuild.buildNumber}\" created for definition \"{queuedBuild.definition.name}\".");

            if (this.CreateBuildNumberVariable)
            {
                this.LogDebug($"Setting $TfsBuildNumber build variable to {queuedBuild.buildNumber}...");
                StoredProcs.Variables_CreateOrUpdateVariableDefinition(
                    Variable_Name: "TfsBuildNumber",
                    Environment_Id: null,
                    Server_Id: null,
                    ApplicationGroup_Id: null,
                    Application_Id: this.Context.ApplicationId,
                    Deployable_Id: null,
                    Release_Number: this.Context.ReleaseNumber,
                    Build_Number: this.Context.BuildNumber,
                    Execution_Id: null,
                    Value_Text: queuedBuild.buildNumber,
                    Sensitive_Indicator: YNIndicator.No
                ).Execute();

                this.LogInformation("$TfsBuildNumber build variable set to: " + queuedBuild.buildNumber);
            }

            if (this.WaitForCompletion)
            {
                string lastStatus = queuedBuild.status;
                this.LogInformation($"Current build status is \"{lastStatus}\", waiting for \"completed\" status...");

                while (!string.Equals(queuedBuild.status, "completed", StringComparison.OrdinalIgnoreCase))
                {
                    this.ThrowIfCanceledOrTimeoutExpired();
                    Thread.Sleep(4000);
                    queuedBuild = api.GetBuild(queuedBuild.id);
                    if (queuedBuild.status != lastStatus)
                    {
                        this.LogInformation($"Current build status changed from \"{lastStatus}\" to \"{queuedBuild.status}\"...");
                        lastStatus = queuedBuild.status;
                    }
                }

                this.LogInformation("Build status result is \"completed\".");

                if (this.ValidateBuild)
                {
                    this.LogInformation("Validating build status result is \"succeeded\"...");
                    if (!string.Equals("succeeded", queuedBuild.result, StringComparison.OrdinalIgnoreCase))
                    {
                        this.LogError("Build status result was not \"succeeded\".");
                        return;
                    }
                    this.LogInformation("Build status result was \"succeeded\".");
                }
            }

            this.LogInformation($"VSO build {queuedBuild.buildNumber} created.");
        }
    }
}
