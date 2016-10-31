using System;
using System.Collections.Generic;
using System.Linq;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;
using Inedo.BuildMasterExtensions.TFS.Providers;

namespace Inedo.BuildMasterExtensions.TFS
{
    partial class TfsIssueTrackingProvider : ICategoryFilterable
    {
        private TfsIssueTrackingApplicationFilter legacyFilter;

        string[] ICategoryFilterable.CategoryIdFilter
        {
            get
            {
                if (this.legacyFilter == null)
                    return null;

                var path = new List<string>();
                path.Add(this.legacyFilter.CollectionName);
                path.Add(this.legacyFilter.ProjectName);
                if (!string.IsNullOrWhiteSpace(this.legacyFilter.AreaPath))
                    path.AddRange(this.legacyFilter.AreaPath.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries));

                return path.ToArray();
            }
            set
            {
                if (value == null || value.Length < 2)
                    this.legacyFilter = null;

                var filter = new TfsIssueTrackingApplicationFilter
                {
                    CollectionName = value[0],
                    ProjectName = value[1]
                };

                if (value.Length > 2)
                    filter.AreaPath = string.Join("\\", value.Skip(2));

                this.legacyFilter = filter;
            }
        }
        string[] ICategoryFilterable.CategoryTypeNames
        {
            get { return new[] { "Collection", "Project", "Area Path" }; }
        }

        IssueTrackerCategory[] ICategoryFilterable.GetCategories()
        {
            throw new NotImplementedException();
        }
    }
}
