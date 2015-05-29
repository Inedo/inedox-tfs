using System;
using System.Collections.Generic;
using System.Web;
using Inedo.BuildMaster.Extensibility.IssueTrackerConnections;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Inedo.BuildMasterExtensions.TFS
{
    [Serializable]
    internal sealed class TfsIssue : IIssueTrackerIssue
    {
        public TfsIssue(WorkItem workItem, HashSet<string> closedStates, TswaClientHyperlinkService hyperlinkService)
        {
            this.Id = workItem.Id;
            this.Title = workItem.Title;
            this.Description = workItem.Description;
            this.Status = workItem.State;
            this.SubmittedDate = EnsureUtc(workItem.CreatedDate);
            this.Submitter = workItem.CreatedBy;
            this.IsClosed = closedStates.Contains(workItem.State);
            this.Url = hyperlinkService.GetWorkItemEditorUrl(workItem.Id).AbsoluteUri;
        }

        //public override IssueTrackerIssue.RenderMode IssueDescriptionRenderMode
        //{
        //    get { return this.allowHtml ? RenderMode.Html : RenderMode.Text; }
        //}

        //private static string GetReleaseNumber(WorkItem workItem, string customReleaseNumberFieldName)
        //{
        //    if (string.IsNullOrEmpty(customReleaseNumberFieldName))
        //        return workItem.IterationPath.Substring(workItem.IterationPath.LastIndexOf('\\') + 1);
        //    else
        //        return workItem.Fields[customReleaseNumberFieldName].Value.ToString().Trim();
        //}

        public int Id { get; private set; }
        public bool IsClosed { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string Status { get; private set; }
        public DateTime SubmittedDate { get; private set; }
        public string Submitter { get; private set; }
        public string Url { get; private set; }
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
