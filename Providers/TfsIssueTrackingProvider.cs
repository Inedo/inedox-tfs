using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Inedo.BuildMaster.Documentation;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Extensibility.IssueTrackerConnections;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.TFS.Providers;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.TFS
{
    [DisplayName("Team Foundation Server")]
    [Description("Supports TFS 2010-2015.")]
    [CustomEditor(typeof(TfsIssueTrackingProviderEditor))]
    public sealed partial class TfsIssueTrackingProvider : IssueTrackerConnectionBase, IIssueStatusUpdater, IIssueCloser
    {
        /// <summary>
        /// The base URL of the TFS store, should include collection name, e.g. "http://server:port/tfs"
        /// </summary>
        [Persistent]
        public string BaseUrl { get; set; }
        /// <summary>
        /// Indicates the full name of the custom field that contains the release number associated with the work item
        /// </summary>
        [Persistent]
        public string CustomReleaseNumberFieldName { get; set; }
        /// <summary>
        /// The username used to connect to the server
        /// </summary>
        [Persistent]
        public string UserName { get; set; }
        /// <summary>
        /// The password used to connect to the server
        /// </summary>
        [Persistent]
        public string Password { get; set; }
        /// <summary>
        /// The domain of the server
        /// </summary>
        [Persistent]
        public string Domain { get; set; }
        /// <summary>
        /// Returns true if BuildMaster should connect to TFS using its own account, false if the credentials are specified
        /// </summary>
        [Persistent]
        public bool UseSystemCredentials { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to allow HTML issue descriptions.
        /// </summary>
        [Persistent]
        public bool AllowHtmlIssueDescriptions { get; set; }
        [Persistent]
        public string CustomWiql { get; set; }
        [Persistent]
        public string[] CustomClosedStates { get; set; }

        public override RichDescription GetDescription()
        {
            return new RichDescription(
                "TFS at ",
                new Hilite(this.BaseUrl)
            );
        }

        public override IssueTrackerApplicationConfigurationBase GetDefaultApplicationConfiguration(int applicationId)
        {
            return this.legacyFilter ?? new TfsIssueTrackingApplicationFilter();
        }

        public override IEnumerable<IIssueTrackerIssue> EnumerateIssues(IssueTrackerConnectionContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            var remote = this.GetRemote();
            var wiql = this.GetWiql(context);

            var remoteAgent = this.Agent.GetService<IRemoteMethodExecuter>();
            var filter = (TfsIssueTrackingApplicationFilter)context.ApplicationConfiguration ?? this.legacyFilter;

            string iteration = null;
            if (string.IsNullOrWhiteSpace(this.CustomReleaseNumberFieldName) && string.IsNullOrWhiteSpace(this.CustomWiql) && string.IsNullOrWhiteSpace(filter.CustomWiql))
                iteration = context.ReleaseNumber;

            var issues = remoteAgent.InvokeFunc(remote.GetIssues, filter.CollectionId, filter.CollectionName, wiql, iteration);
            foreach (var issue in issues)
                issue.RenderAsHtml = this.AllowHtmlIssueDescriptions;

            return issues;
        }

        internal TfsCollectionInfo[] GetCollections()
        {
            var remote = this.GetRemote();
            var remoteAgent = this.Agent.GetService<IRemoteMethodExecuter>();
            return remoteAgent.InvokeFunc(remote.GetCollections);
        }
        internal TfsProjectInfo[] GetProjects(Guid collectionId)
        {
            var remote = this.GetRemote();
            var remoteAgent = this.Agent.GetService<IRemoteMethodExecuter>();
            return remoteAgent.InvokeFunc(remote.GetProjects, (Guid?)collectionId, (string)null);
        }
        internal TfsAreaInfo[] GetAreas(Guid collectionId, string projectName)
        {
            var remote = this.GetRemote();
            var remoteAgent = this.Agent.GetService<IRemoteMethodExecuter>();
            return remoteAgent.InvokeFunc(remote.GetAreas, (Guid?)collectionId, (string)null, projectName);
        }

        private string GetWiql(IssueTrackerConnectionContext context)
        {
            if (!string.IsNullOrWhiteSpace(this.CustomWiql))
                return this.CustomWiql;

            var filter = (TfsIssueTrackingApplicationFilter)context.ApplicationConfiguration ?? this.legacyFilter;
            if (!string.IsNullOrWhiteSpace(filter.CustomWiql))
                return filter.CustomWiql;

            var buffer = new StringBuilder();
            buffer.Append("SELECT [System.ID] FROM WorkItems WHERE [System.TeamProject] = '");
            buffer.Append(filter.ProjectName.Replace("'", "''"));
            buffer.Append('\'');

            if (!string.IsNullOrWhiteSpace(filter.AreaPath))
            {
                buffer.Append(" AND [System.AreaPath] under '");
                buffer.Append(filter.ProjectName.Replace("'", "''"));
                buffer.Append('\\');
                buffer.Append(filter.AreaPath.Replace("'", "''"));
                buffer.Append('\'');
            }

            if (!string.IsNullOrWhiteSpace(context.ReleaseNumber) && !string.IsNullOrWhiteSpace(this.CustomReleaseNumberFieldName))
            {
                buffer.Append(" AND [");
                buffer.Append(this.CustomReleaseNumberFieldName);
                buffer.Append("] = '");
                buffer.Append(context.ReleaseNumber.Replace("'", "''"));
                buffer.Append('\'');
            }

            return buffer.ToString();
        }

        private RemoteTfs GetRemote()
        {
            return new RemoteTfs
            {
                BaseUrl = this.BaseUrl,
                UserName = this.UserName,
                Password = this.Password,
                Domain = this.Domain,
                UseSystemCredentials = this.UseSystemCredentials,
                CustomClosedStates = this.CustomClosedStates
            };
        }

        public override bool IsAvailable()
        {
            try
            {
                return this.Agent.GetService<IRemoteMethodExecuter>().InvokeFunc(RemoteTfs.IsAvailable);
            }
            catch
            {
                return false;
            }
        }
        public override void ValidateConnection()
        {
            try
            {
                var remote = this.GetRemote();
                this.Agent.GetService<IRemoteMethodExecuter>().InvokeFunc(remote.GetCollections);
            }
            catch (Exception ex)
            {
                throw new NotAvailableException(ex.Message, ex);
            }
        }

        void IIssueStatusUpdater.ChangeIssueStatus(IssueTrackerConnectionContext context, string issueId, string issueStatus)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            if (string.IsNullOrWhiteSpace(issueId))
                throw new ArgumentNullException("issueId");

            var remote = this.GetRemote();
            var remoteAgent = this.Agent.GetService<IRemoteMethodExecuter>();
            var filter = (TfsIssueTrackingApplicationFilter)context.ApplicationConfiguration ?? this.legacyFilter;

            remoteAgent.InvokeAction(remote.ChangeIssueState, filter.CollectionId, filter.CollectionName, int.Parse(issueId), issueStatus);
        }
        void IIssueStatusUpdater.ChangeStatusForAllIssues(IssueTrackerConnectionContext context, string fromStatus, string toStatus)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            var remote = this.GetRemote();
            var wiql = this.GetWiql(context);

            var remoteAgent = this.Agent.GetService<IRemoteMethodExecuter>();
            var filter = (TfsIssueTrackingApplicationFilter)context.ApplicationConfiguration ?? this.legacyFilter;

            remoteAgent.InvokeMethod(new Action<Guid?, string, string, string, string>(remote.ChangeIssueStates), filter.CollectionId, filter.CollectionName, wiql, fromStatus, toStatus);
        }

        void IIssueCloser.CloseIssue(IssueTrackerConnectionContext context, string issueId)
        {
            ((IIssueStatusUpdater)this).ChangeIssueStatus(context, issueId, "Closed");
        }
        void IIssueCloser.CloseAllIssues(IssueTrackerConnectionContext context)
        {
            var closer = (IIssueCloser)this;

            foreach (var issue in this.EnumerateIssues(context))
            {
                if (!issue.IsClosed)
                {
                    this.LogDebug("Closing issue {0}...", issue.Id);
                    closer.CloseIssue(context, issue.Id);
                }
            }
        }
    }
}
