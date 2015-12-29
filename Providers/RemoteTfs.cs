using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Inedo.BuildMasterExtensions.TFS.Providers
{
    [Serializable]
    internal sealed class RemoteTfs
    {
        private readonly TfsAreaInfo[] EmptyAreas = new TfsAreaInfo[0];
        private readonly string[] DefaultClosedStates = new [] { "Resolved", "Closed" };

        public string BaseUrl { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Domain { get; set; }
        public bool UseSystemCredentials { get; set; }
        public string CustomReleaseNumberFieldName { get; set; }
        public string CustomWiql { get; set; }
        public string[] CustomClosedStates { get; set; }

        public static bool IsAvailable()
        {
            typeof(TfsConfigurationServer).GetType();
            return true;
        }

        public TfsCollectionInfo[] GetCollections()
        {
            using (var tfs = this.GetTeamProjectCollection())
            {
                return tfs
                    .ConfigurationServer
                    .GetService<ITeamProjectCollectionService>()
                    .GetCollections()
                    .Select(c => new TfsCollectionInfo { Id = c.Id, Name = c.Name })
                    .ToArray();
            }
        }
        public TfsProjectInfo[] GetProjects(Guid? collectionId, string collectionName)
        {
            using (var tfs = this.GetTeamProjectCollection())
            {
                var tfsProjectCollection = FindCollection(tfs.ConfigurationServer, collectionId, collectionName);

                var workItemStore = tfsProjectCollection.GetService<WorkItemStore>();
                return workItemStore
                    .Projects
                    .Cast<Project>()
                    .Select(p => new TfsProjectInfo { Id = p.Id, Name = p.Name })
                    .ToArray();
            }
        }
        public TfsAreaInfo[] GetAreas(Guid? collectionId, string collectionName, string projectName)
        {
            using (var tfs = this.GetTeamProjectCollection())
            {
                var tfsProjectCollection = FindCollection(tfs.ConfigurationServer, collectionId, collectionName);
                var workItemStore = tfsProjectCollection.GetService<WorkItemStore>();
                var projects = workItemStore.Projects.Cast<Project>();
                var project = projects.First(p => string.Equals(p.Name, projectName, StringComparison.OrdinalIgnoreCase));

                return this.GetAreas(project).ToArray();
            }
        }
        public TfsIssue[] GetIssues(Guid? collectionId, string collectionName, string wiql, string iteration)
        {
            using (var tfs = this.GetTeamProjectCollection())
            {
                var tfsProjectCollection = FindCollection(tfs.ConfigurationServer, collectionId, collectionName);
                var workItemStore = tfsProjectCollection.GetService<WorkItemStore>();
                var hyperlinkService = tfsProjectCollection.GetService<TswaClientHyperlinkService>();
                var results = workItemStore.Query(wiql).Cast<WorkItem>();

                if (!string.IsNullOrWhiteSpace(iteration))
                {
                    var endsWith = "\\" + iteration;
                    results = results.Where(i => i.IterationPath.EndsWith(endsWith, StringComparison.OrdinalIgnoreCase));
                }

                return results
                    .Select(i => new TfsIssue(i, this.CustomClosedStates ?? this.DefaultClosedStates, hyperlinkService))
                    .ToArray();
            }
        }

        public void ChangeIssueState(Guid? collectionId, string collectionName, int issueId, string toState)
        {
            using (var tfs = this.GetTeamProjectCollection())
            {
                var tfsProjectCollection = FindCollection(tfs.ConfigurationServer, collectionId, collectionName);
                var workItemStore = tfsProjectCollection.GetService<WorkItemStore>();
                var workItem = workItemStore.GetWorkItem(issueId);
                workItem.State = toState;
                workItem.Save();
            }
        }
        public void ChangeIssueStates(Guid? collectionId, string collectionName, string wiql, string fromState, string toState)
        {
            using (var tfs = this.GetTeamProjectCollection())
            {
                var tfsProjectCollection = FindCollection(tfs.ConfigurationServer, collectionId, collectionName);
                var workItemStore = tfsProjectCollection.GetService<WorkItemStore>();
                var hyperlinkService = tfsProjectCollection.GetService<TswaClientHyperlinkService>();
                var results = workItemStore
                    .Query(wiql)
                    .Cast<WorkItem>()
                    .Where(i => string.Equals(i.State, fromState, StringComparison.OrdinalIgnoreCase));

                foreach (var issue in results)
                {
                    issue.State = toState;
                    issue.Save();
                }
            }
        }

        private static TfsTeamProjectCollection FindCollection(TfsConfigurationServer server, Guid? collectionId, string collectionName)
        {
            if (collectionId != null)
                return server.GetTeamProjectCollection(collectionId.Value);

            var match = server
                .GetService<ITeamProjectCollectionService>()
                .GetCollections()
                .FirstOrDefault(c => string.Equals(c.Name, collectionName, StringComparison.OrdinalIgnoreCase));

            if (match == null)
                return null;

            return server.GetTeamProjectCollection(match.Id);
        }

        private string GetWiql(string projectName, string areaPath, string releaseNumber)
        {
            if (!string.IsNullOrWhiteSpace(this.CustomWiql))
                return this.CustomWiql;

            var buffer = new StringBuilder();
            buffer.Append("SELECT [System.ID] FROM WorkItems WHERE [System.TeamProject] = '");
            buffer.Append(projectName.Replace("'", "''"));
            buffer.Append('\'');

            if (!string.IsNullOrWhiteSpace(areaPath))
            {
                buffer.Append(" AND [System.AreaPath] under '");
                buffer.Append(areaPath.Replace("'", "''"));
                buffer.Append('\'');
            }

            if (!string.IsNullOrWhiteSpace(releaseNumber))
            {
                if (!string.IsNullOrWhiteSpace(this.CustomReleaseNumberFieldName))
                {
                    buffer.Append(" AND [");
                    buffer.Append(this.CustomReleaseNumberFieldName);
                    buffer.Append("] = '");
                    buffer.Append(releaseNumber.Replace("'", "''"));
                    buffer.Append('\'');
                }
            }

            return buffer.ToString();
        }

        private IEnumerable<TfsAreaInfo> GetAreas(Project project)
        {
            foreach (Node area in project.AreaRootNodes)
            {
                if (!area.HasChildNodes)
                    yield return new TfsAreaInfo { Id = area.Id, Name = area.Name, Children = EmptyAreas };
                else
                    yield return new TfsAreaInfo { Id = area.Id, Name = area.Name, Children = this.GetAreasInNode(area).ToArray() };
            }
        }
        private IEnumerable<TfsAreaInfo> GetAreasInNode(Node node)
        {
            foreach (Node area in node.ChildNodes)
            {
                if (!area.HasChildNodes)
                    yield return new TfsAreaInfo { Id = area.Id, Name = area.Name, Children = EmptyAreas };
                else
                    yield return new TfsAreaInfo { Id = area.Id, Name = area.Name, Children = this.GetAreasInNode(area).ToArray() };
            }
        }

        private TfsTeamProjectCollection GetTeamProjectCollection()
        {
            if (this.UseSystemCredentials)
            {
                var projectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(this.BaseUrl));
                projectCollection.EnsureAuthenticated();
                return projectCollection;
            }
            else
            {
                var projectColleciton = new TfsTeamProjectCollection(new Uri(this.BaseUrl), new TfsClientCredentials(new WindowsCredential(new NetworkCredential(this.UserName, this.Password, this.Domain))));
                projectColleciton.EnsureAuthenticated();
                return projectColleciton;
            }
        }
    }

    [Serializable]
    internal sealed class TfsCollectionInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    [Serializable]
    internal sealed class TfsProjectInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [Serializable]
    internal sealed class TfsAreaInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public TfsAreaInfo[] Children { get; set; }
    }
}
