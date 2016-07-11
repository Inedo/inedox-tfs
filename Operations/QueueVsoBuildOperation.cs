using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMasterExtensions.TFS.VisualStudioOnline;
using Inedo.Diagnostics;
using Inedo.Documentation;

namespace Inedo.BuildMasterExtensions.TFS.Operations
{
    [DisplayName("Queue TFS 2015 / VSO Build")]
    [Description("Queues a build in TFS 2015 or Visual Studio Online, optionally waiting for its completion.")]
    [ScriptAlias("Queue-Build")]
    [Tag(Tags.Builds)]
    public sealed class QueueVsoBuildOperation : TfsOperation
    {
        [ScriptAlias("TeamProject")]
        [DisplayName("Team project")]
        public string TeamProject { get; set; }

        [Required]
        [ScriptAlias("BuildDefinition")]
        [DisplayName("Build definition")]
        public string BuildDefinition { get; set; }

        [ScriptAlias("WaitForCompletion")]
        [DisplayName("Wait for completion")]
        public bool WaitForCompletion { get; set; }

        [ScriptAlias("Validate")]
        [DisplayName("Validate success")]
        public bool ValidateBuild { get; set; }

        [Category("Advanced")]
        [ScriptAlias("CreateBuildNumberVariable")]
        [DisplayName("Create $TfsBuildNumber")]
        [DefaultValue(true)]
        public bool CreateBuildNumberVariable { get; set; } = true;

        public async override Task ExecuteAsync(IOperationExecutionContext context)
        {
            var api = new TfsRestApi(this.TeamProjectCollectionUrl, this.TeamProject)
            {
                UserName = string.IsNullOrEmpty(this.Domain) ? this.UserName : string.Format("{0}\\{1}", this.Domain, this.UserName),
                Password = this.PasswordOrToken
            };

            this.LogDebug("Finding VSO build definition...");
            var definitionResult = await api.GetBuildDefinitionsAsync();
            var definition = definitionResult.FirstOrDefault(d => string.IsNullOrEmpty(this.BuildDefinition) || string.Equals(d.name, this.BuildDefinition, StringComparison.OrdinalIgnoreCase));

            if (definition == null)
                throw new InvalidOperationException("Could not find a build definition named: " + Util.CoalesceStr(this.BuildDefinition, "any"));

            this.LogInformation($"Queueing VSO build of {this.TeamProject}, build definition {definition.name}...");

            var queuedBuild = await api.QueueBuildAsync(definition.id);

            this.LogInformation($"Build number \"{queuedBuild.buildNumber}\" created for definition \"{queuedBuild.definition.name}\".");

            if (this.CreateBuildNumberVariable)
            {
                this.LogDebug($"Setting $TfsBuildNumber build variable to {queuedBuild.buildNumber}...");
                await new DB.Context(false).Variables_CreateOrUpdateVariableDefinitionAsync(
                    Variable_Name: "TfsBuildNumber",
                    Environment_Id: null,
                    ServerRole_Id: null,
                    Server_Id: null,
                    ApplicationGroup_Id: null,
                    Application_Id: context.ApplicationId,
                    Deployable_Id: null,
                    Release_Number: context.ReleaseNumber,
                    Build_Number: context.BuildNumber,
                    Execution_Id: null,
                    Promotion_Id: null,
                    Value_Text: queuedBuild.buildNumber,
                    Sensitive_Indicator: false
                );

                this.LogInformation("$TfsBuildNumber build variable set to: " + queuedBuild.buildNumber);
            }

            if (this.WaitForCompletion)
            {
                string lastStatus = queuedBuild.status;
                this.LogInformation($"Current build status is \"{lastStatus}\", waiting for \"completed\" status...");

                while (!string.Equals(queuedBuild.status, "completed", StringComparison.OrdinalIgnoreCase))
                {
                    await Task.Delay(4000, context.CancellationToken);
                    queuedBuild = await api.GetBuildAsync(queuedBuild.id);
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

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Queue VS Online Build for ", new Hilite(config[nameof(this.TeamProject)])
                ),
                new RichDescription(
                    "using the build definition ",
                    new Hilite(config[nameof(this.BuildDefinition)]),
                    this.WaitForCompletion ? " and wait until the build completes" + (config[nameof(this.ValidateBuild)] == "true" ? " successfully" : "") : "",
                    "."
                )
            );
        }
    }
}
