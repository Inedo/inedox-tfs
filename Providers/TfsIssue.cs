using System;
using System.Linq;
using System.Web;
using Inedo.BuildMaster.Extensibility.IssueTrackerConnections;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Inedo.BuildMasterExtensions.TFS
{
    [Serializable]
    internal sealed class TfsIssue : IIssueTrackerIssue
    {
        public TfsIssue(WorkItem workItem, string[] closedStates, TswaClientHyperlinkService hyperlinkService)
        {
            this.Id = workItem.Id;
            this.Title = workItem.Title;
            this.Description = workItem.Description;
            this.Status = workItem.State;
            this.SubmittedDate = EnsureUtc(workItem.CreatedDate);
            this.Submitter = workItem.CreatedBy;
            this.IsClosed = closedStates.Contains(workItem.State, StringComparer.OrdinalIgnoreCase);
            this.Url = hyperlinkService.GetWorkItemEditorUrl(workItem.Id).AbsoluteUri;
        }

        public int Id { get; }
        public bool IsClosed { get; }
        public string Title { get; }
        public string Description { get; }
        public string Status { get; }
        public DateTime SubmittedDate { get; }
        public string Submitter { get; }
        public string Url { get; }
        public bool RenderAsHtml { get; set; }

        string IIssueTrackerIssue.Id
        {
            get { return this.Id.ToString(); }
        }
        string IIssueTrackerIssue.Description
        {
            get { return this.RenderAsHtml ? this.Description : HttpUtility.HtmlEncode(this.Description ?? string.Empty); }
        }

        private static DateTime EnsureUtc(DateTime t)
        {
            if (t.Kind != DateTimeKind.Utc)
                return t.ToUniversalTime();
            else
                return t;
        }
    }
}
