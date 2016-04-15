using System.Net;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.Serialization;
using Microsoft.TeamFoundation.Client;

namespace Inedo.BuildMasterExtensions.TFS
{
    public abstract class TfsActionBase : AgentBasedActionBase
    {
        protected TfsActionBase()
        {
        }

        /// <summary>
        /// Gets or sets the team project.
        /// </summary>
        [Persistent]
        public string TeamProject { get; set; }

        /// <summary>
        /// Gets or sets the name of the build definition if not empty, or includes all build definitions in the search.
        /// </summary>
        [Persistent]
        public string BuildDefinition { get; set; }

        public new TfsConfigurer GetExtensionConfigurer() => (TfsConfigurer)base.GetExtensionConfigurer();

        /// <summary>
        /// Gets the appropriate version control server based by connecting to TFS using the persisted credentials
        /// </summary>
        protected TfsTeamProjectCollection GetTeamProjectCollection()
        {
            return GetTeamProjectCollection(this.GetExtensionConfigurer());
        }

        internal static TfsTeamProjectCollection GetTeamProjectCollection(TfsConfigurer configurer)
        {
            if (configurer.UseSystemCredentials)
            {
                var projectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(configurer.BaseUri);
                projectCollection.EnsureAuthenticated();
                return projectCollection;
            }
            else
            {
                var projectCollection = new TfsTeamProjectCollection(configurer.BaseUri, new TfsClientCredentials(new WindowsCredential(new NetworkCredential(configurer.UserName, configurer.Password, configurer.Domain))));
                projectCollection.EnsureAuthenticated();
                return projectCollection;
            }
        }
    }
}
