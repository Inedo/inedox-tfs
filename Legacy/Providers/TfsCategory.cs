using System;
using System.Collections.Generic;
using System.Linq;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Inedo.BuildMasterExtensions.TFS
{
    [Serializable]
    internal sealed class TfsCategory : IssueTrackerCategory
    {
        public enum CategoryTypes { Collection, Project, AreaPath }

        public CategoryTypes CategoryType { get; private set; }

        private TfsCategory(string categoryId, string categoryName, IssueTrackerCategory[] subCategories, CategoryTypes categoryType)
            : base(categoryId, categoryName, subCategories)
        {
            CategoryType = categoryType;
        }

        internal static TfsCategory CreateCollection(TeamProjectCollection projectCollection, TfsCategory[] projectCategories)
        {
            return new TfsCategory(projectCollection.Name,
                projectCollection.Name,
                projectCategories,
                CategoryTypes.Collection);
        }

        internal static TfsCategory CreateProject(Project project)
        {
            var areaPaths = new List<TfsCategory>();

            foreach (Node area in project.AreaRootNodes)
            {
                areaPaths.Add(new TfsCategory(area.Path, area.Path, new TfsCategory[0], CategoryTypes.AreaPath));

                foreach (Node item in area.ChildNodes)
                {
                    areaPaths.Add(new TfsCategory(item.Path, item.Path, new TfsCategory[0], CategoryTypes.AreaPath));
                }
            }

            return new TfsCategory(project.Name,
                project.Name,
                areaPaths.ToArray(),
                CategoryTypes.Project);
        }
    }
}
