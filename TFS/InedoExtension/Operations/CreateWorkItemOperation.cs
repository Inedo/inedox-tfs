using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.TFS.Clients.Rest;
using Inedo.Extensions.TFS.SuggestionProviders;
using Inedo.Web;

namespace Inedo.Extensions.TFS.Operations
{
    [DisplayName("Create TFS Work Item")]
    [Description("Creates a work item in TFS.")]
    [Tag("issue-tracking")]
    [ScriptAlias("Create-WorkItem")]
    [Example(@"
# create issue for the HDARS project
Create-WorkItem(
    Credentials: KarlVSO,
    TeamProject: HDARS,
    Type: Task,
    Title: QA Testing Required for $ApplicationName,
    Description: This issue was created by BuildMaster on $Date
);
")]
    public sealed class CreateWorkItemOperation : TfsOperation
    {
        [ScriptAlias("Credentials")]
        [DisplayName("Credentials")]
        public override string CredentialName { get; set; }
        [Required]
        [ScriptAlias("TeamProject")]
        [DisplayName("Team project")]
        [SuggestableValue(typeof(TeamProjectNameSuggestionProvider))]
        public string TeamProject { get; set; }
        [Required]
        [ScriptAlias("Type")]
        [DisplayName("Work item type")]
        [SuggestableValue(typeof(WorkItemTypeSuggestionProvider))]
        public string Type { get; set; }
        [Required]
        [ScriptAlias("Title")]
        [DisplayName("Title")]
        public string Title { get; set; }
        [ScriptAlias("Description")]
        [DisplayName("Description")]
        [FieldEditMode(FieldEditMode.Multiline)]
        public string Description { get; set; }
        [ScriptAlias("IterationPath")]
        [DisplayName("Iteration path")]
        [SuggestableValue(typeof(IterationPathSuggestionProvider))]
        public string IterationPath { get; set; }

        [Output]
        [ScriptAlias("TfsIssueId")]
        [DisplayName("Set issue ID to a variable")]
        [Description("The TFS issue ID can be output into a runtime variable.")]
        [PlaceholderText("e.g. $TfsIssueId")]
        public string TfsIssueId { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            this.LogInformation("Creating work item in TFS...");

            var client = new TfsRestApi(this, this);
            try
            {
                var result = await client.CreateWorkItemAsync(this.TeamProject, this.Type, this.Title, this.Description, this.IterationPath).ConfigureAwait(false);

                this.LogDebug($"Work item (ID={result.id}) created.");
                this.TfsIssueId = result.id.ToString();
            }
            catch (TfsRestException ex)
            {
                this.LogError(ex.FullMessage);
                return;
            }
            this.LogInformation("Work item created.");
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription("Create TFS Work Item for team project ", config[nameof(this.TeamProject)])
            );
        }
    }
}
