using System;
using Inedo.Extensibility.IssueSources;
using Inedo.Extensions.TFS.VisualStudioOnline.Model;

namespace Inedo.Extensions.TFS.Clients.Rest
{
    public sealed class TfsRestIssue : IIssueTrackerIssue
    {
        public TfsRestIssue(GetWorkItemResponse w)
        {
            this.Id = w.id.ToString();
            this.Title = w.fields.GetValueOrDefault("System.Title")?.ToString();
            this.Description = w.fields.GetValueOrDefault("System.Description")?.ToString();
            this.Status = w.fields.GetValueOrDefault("System.State")?.ToString();
            this.IsClosed = "Done".Equals(this.Status, StringComparison.OrdinalIgnoreCase);
            this.SubmittedDate = AH.ParseDate(w.fields.GetValueOrDefault("System.CreatedDate")?.ToString()) ?? DateTime.MinValue;
            this.Submitter = w.fields.GetValueOrDefault("System.CreatedBy")?.ToString();
            this.Type = w.fields.GetValueOrDefault("System.WorkItemType")?.ToString();
            this.Url = w._links.html.href;
        }

        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public bool IsClosed { get; }
        public string Status { get; }
        public DateTime SubmittedDate { get; }
        public string Submitter { get; }
        public string Type { get; }
        public string Url { get; }
    }
}
