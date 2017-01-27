using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensions.TFS.Clients.Rest;
using Inedo.Extensions.TFS.SuggestionProviders;

#if BuildMaster
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMaster.Web;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMasterExtensions.TFS.SuggestionProviders;
#elif Otter
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.Credentials;
using Inedo.Otter.Extensibility.Operations;
using Inedo.Otter.Extensions;
using Inedo.Otter.Web.Controls;
using Inedo.OtterExtensions.TFS.Credentials;
#endif

namespace Inedo.Extensions.TFS.Operations
{
    [DisplayName("Queue TFS 2015 / VSO Build")]
    [Description("Queues a build in TFS 2015 or Visual Studio Online, optionally waiting for its completion.")]
    [ScriptAlias("Queue-Build")]
    [Tag("builds")]
    [Tag("tfs")]
    public sealed class QueueVsoBuildOperation : TfsOperation
    {
        [ScriptAlias("Credentials")]
        [DisplayName("Credentials")]
        public override string CredentialName { get; set; }

        [ScriptAlias("TeamProject")]
        [DisplayName("Team project")]
        [SuggestibleValue(typeof(TeamProjectNameSuggestionProvider))]
        public string TeamProject { get; set; }

        [Required]
        [ScriptAlias("BuildDefinition")]
        [DisplayName("Build definition")]
        [SuggestibleValue(typeof(BuildDefinitionNameSuggestionProvider))]
        public string BuildDefinition { get; set; }

        [ScriptAlias("WaitForCompletion")]
        [DisplayName("Wait for completion")]
        [DefaultValue(true)]
        public bool WaitForCompletion { get; set; } = true;

        [ScriptAlias("Validate")]
        [DisplayName("Validate success")]
        [DefaultValue(true)]
        public bool ValidateBuild { get; set; } = true;

        [Output]
        [ScriptAlias("TfsBuildNumber")]
        [DisplayName("Set build number to variable")]
        [Description("The TFS build number can be output into a runtime variable.")]
        [PlaceholderText("e.g. $TfsBuildNumber")]
        public string TfsBuildNumber { get; set; }

        public async override Task ExecuteAsync(IOperationExecutionContext context)
        {
            var api = new TfsRestApi(this, this);

            this.LogDebug("Finding VSO build definition...");
            var definitionResult = await api.GetBuildDefinitionsAsync(this.TeamProject);
            var definition = definitionResult.FirstOrDefault(d => string.IsNullOrEmpty(this.BuildDefinition) || string.Equals(d.name, this.BuildDefinition, StringComparison.OrdinalIgnoreCase));

            if (definition == null)
                throw new InvalidOperationException("Could not find a build definition named: " + AH.CoalesceString(this.BuildDefinition, "any"));

            this.LogInformation($"Queueing VSO build of {this.TeamProject}, build definition {definition.name}...");

            var queuedBuild = await api.QueueBuildAsync(this.TeamProject, definition.id);

            this.LogInformation($"Build number \"{queuedBuild.buildNumber}\" created for definition \"{queuedBuild.definition.name}\".");

            this.TfsBuildNumber = queuedBuild.buildNumber;

            if (this.WaitForCompletion)
            {
                string lastStatus = queuedBuild.status;
                this.LogInformation($"Current build status is \"{lastStatus}\", waiting for \"completed\" status...");

                while (!string.Equals(queuedBuild.status, "completed", StringComparison.OrdinalIgnoreCase))
                {
                    await Task.Delay(4000, context.CancellationToken);
                    queuedBuild = await api.GetBuildAsync(this.TeamProject, queuedBuild.id);
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
