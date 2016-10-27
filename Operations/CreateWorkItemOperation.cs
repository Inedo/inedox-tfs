using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMaster.Web;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMasterExtensions.TFS.SuggestionProviders;
using Inedo.BuildMasterExtensions.TFS.VisualStudioOnline;
using Inedo.Diagnostics;
using Inedo.Documentation;

namespace Inedo.BuildMasterExtensions.TFS.Operations
{
    [DisplayName("Create TFS Work Item")]
    [Description("Creates a work item in TFS.")]
    [Tag(Tags.IssueTracking)]
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
        [SuggestibleValue(typeof(TeamProjectNameSuggestionProvider))]
        public string TeamProject { get; set; }
        [Required]
        [ScriptAlias("Type")]
        [DisplayName("Work item type")]
        [SuggestibleValue(typeof(WorkItemTypeSuggestionProvider))]
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
        [SuggestibleValue(typeof(IterationPathSuggestionProvider))]
        public string IterationPath { get; set; }

        [Output]
        [ScriptAlias("TfsIssueId")]
        [DisplayName("Set issue ID to a variable")]
        [Description("The TFS issue ID can be output into a runtime variable.")]
        [PlaceholderText("e.g. TfsIssueId")]
        public string TfsIssueId { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            this.LogInformation("Creating work item in TFS...");

            var client = new TfsRestApi(this);
            try
            {
                var result = await client.CreateOrUpdateWorkItemAsync(this.TeamProject, this.Type, this.Title, this.Description, this.IterationPath).ConfigureAwait(false);

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
