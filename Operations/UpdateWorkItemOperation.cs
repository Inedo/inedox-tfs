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
    [DisplayName("Update TFS Work Item")]
    [Description("Updates an existing work item in TFS.")]
    [Tag(Tags.IssueTracking)]
    [ScriptAlias("Update-WorkItem")]
    [Example(@"
# Update issue stored in package variable to 'In Progress'
Create-WorkItem(
    Credentials: KarlVSO,
    TeamProject: HDARS,
    Id: $TfsIssueId,
    State: In Progress
);
")]
    public sealed class UpdateWorkItemOperation : TfsOperation
    {
        [ScriptAlias("Credentials")]
        [DisplayName("Credentials")]
        public override string CredentialName { get; set; }
        [Required]
        [ScriptAlias("Id")]
        [DisplayName("Id")]
        [Description("The ID for issues may be stored as output variables of the Create-WorkItem operation.")]
        public string Id { get; set; }
        [ScriptAlias("Title")]
        [DisplayName("Title")]
        [PlaceholderText("Unchanged")]
        public string Title { get; set; }
        [ScriptAlias("Description")]
        [DisplayName("Description")]
        [PlaceholderText("Unchanged")]
        [FieldEditMode(FieldEditMode.Multiline)]
        public string Description { get; set; }
        [ScriptAlias("IterationPath")]
        [DisplayName("Iteration path")]
        [PlaceholderText("Unchanged")]
        [SuggestibleValue(typeof(IterationPathSuggestionProvider))]
        public string IterationPath { get; set; }
        [ScriptAlias("State")]
        [DisplayName("State")]
        [PlaceholderText("Unchanged")]
        public string State { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            this.LogInformation($"Updating work item (ID={this.Id}) in TFS...");

            var client = new TfsRestApi(this);
            try
            {
                await client.UpdateWorkItemAsync(this.Id, this.Title, this.Description, this.IterationPath, this.State).ConfigureAwait(false);
            }
            catch (TfsRestException ex)
            {
                this.LogError(ex.FullMessage);
                return;
            }
            this.LogInformation("Work item updated.");
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            string title = config[nameof(this.Title)];
            string description = config[nameof(this.Description)];
            string iteration = config[nameof(this.IterationPath)];
            string state = config[nameof(this.State)];

            var longDescription = new RichDescription();
            if (!string.IsNullOrEmpty(title))
                longDescription.AppendContent("Title = ", new Hilite(title), "; ");
            if (!string.IsNullOrEmpty(description))
                longDescription.AppendContent("Description = ", new Hilite(description), "; ");
            if (!string.IsNullOrEmpty(iteration))
                longDescription.AppendContent("Iteration = ", new Hilite(iteration), "; ");
            if (!string.IsNullOrEmpty(state))
                longDescription.AppendContent("State = ", new Hilite(state), "; ");

            return new ExtendedRichDescription(
                new RichDescription("Update TFS Work Item"),
                longDescription
            );
        }
    }
}
