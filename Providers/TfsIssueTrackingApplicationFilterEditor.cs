using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Extensibility.IssueTrackerConnections;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.BuildMaster.Web.Security;
using Inedo.Web.ClientResources;
using Inedo.Web.Controls;
using Inedo.Web.Controls.SimpleHtml;
using Inedo.Web.Handlers;

namespace Inedo.BuildMasterExtensions.TFS.Providers
{
    internal sealed class TfsIssueTrackingApplicationFilterEditor : IssueTrackerApplicationConfigurationEditorBase
    {
        private ComboSelect ddlCollection;
        private ComboSelect ddlUseWiql;
        private HiddenField ctlProject;
        private HiddenField ctlAreaPath;
        private TextBox txtCustomWiql;
        private Lazy<TfsCollectionInfo[]> collections;

        public TfsIssueTrackingApplicationFilterEditor()
        {
            this.collections = new Lazy<TfsCollectionInfo[]>(this.GetCollections);
            this.ValidateBeforeCreate += this.TfsIssueTrackingApplicationFilterEditor_ValidateBeforeCreate;
        }

        private void TfsIssueTrackingApplicationFilterEditor_ValidateBeforeCreate(object sender, ValidationEventArgs<IssueTrackerApplicationConfigurationBase> e)
        {
            try
            {
                GC.KeepAlive(this.collections.Value);
            }
            catch (Exception ex)
            {
                e.ValidLevel = ValidationLevel.Error;
                e.Message = "Unable to contact TFS: " + ex.ToString();
            }
        }

        public override void BindToForm(IssueTrackerApplicationConfigurationBase extension)
        {
            var filter = (TfsIssueTrackingApplicationFilter)extension;

            if (filter.CollectionId != null)
            {
                this.ddlCollection.SelectedValue = filter.CollectionId.ToString();
            }
            else if(filter.CollectionName != null)
            {
                var item = this.ddlCollection.Items.Cast<ListItem>().FirstOrDefault(i => i.Text == filter.CollectionName);
                if (item != null)
                    item.Selected = true;
            }

            this.ctlProject.Value = filter.ProjectName;
            this.ctlAreaPath.Value = filter.AreaPath;
        }
        public override IssueTrackerApplicationConfigurationBase CreateFromForm()
        {
            return new TfsIssueTrackingApplicationFilter
            {
                CollectionId = Guid.Parse(this.ddlCollection.SelectedValue),
                CollectionName = this.ddlCollection.SelectedItem.Text,
                ProjectName = this.ctlProject.Value,
                AreaPath = this.ctlAreaPath.Value
            };
        }

        protected override void OnPreRender(EventArgs e)
        {
            this.IncludeClientResourceInPage(
                new JavascriptResource
                {
                    ResourcePath = "~/extension-resources/TFS/TfsIssueTrackingApplicationFilterEditor.js?" + typeof(TfsIssueTrackingApplicationFilterEditor).Assembly.GetName().Version,
                    CompatibleVersions = { InedoLibCR.Versions.jq171 },
                    Dependencies = { InedoLibCR.select2.select2_js }
                }
            );

            base.OnPreRender(e);
        }
        protected override void CreateChildControls()
        {
            this.ddlCollection = new ComboSelect();
            this.ddlCollection.Items.AddRange(
                from c in this.collections.Value
                orderby c.Name
                select new ListItem(c.Name, c.Id.ToString())
            );

            this.ddlUseWiql = new ComboSelect
            {
                Items =
                {
                    new ListItem("Not using a custom query", "False"),
                    new ListItem("Custom WIQL query", "True")
                }
            };

            this.ctlProject = new HiddenField { ID = "ctlProject" };
            this.ctlAreaPath = new HiddenField { ID = "ctlAreaPath" };

            var ctlNoWiql = new Div(
                new SlimFormField("Project:", this.ctlProject),
                new SlimFormField("Area Path:", this.ctlAreaPath)
            );

            this.txtCustomWiql = new TextBox
            {
                TextMode = TextBoxMode.MultiLine,
                Rows = 5
            };

            var ctlWiql = new SlimFormField("Custom query:", this.txtCustomWiql);

            this.Controls.Add(
                new SlimFormField("Collection:", this.ddlCollection),
                new SlimFormField("Query mode:", this.ddlUseWiql),
                ctlNoWiql,
                ctlWiql,
                new RenderJQueryDocReadyDelegator(
                    w =>
                    {
                        w.Write("TfsIssueTrackingApplicationFilterEditor_Init(");
                        InedoLib.Util.JavaScript.WriteJson(
                            w,
                            new
                            {
                                ddlCollection = ddlCollection.ClientID,
                                ddlUseWiql = ddlUseWiql.ClientID,
                                ctlWiql = ctlWiql.ClientID,
                                ctlNoWiql = ctlNoWiql.ClientID,
                                ctlProject = ctlProject.ClientID,
                                ctlArea = ctlAreaPath.ClientID,
                                getProjectsUrl = DynamicHttpHandling.GetJavascriptDataUrl<int, string, object>(GetProjects),
                                getAreasUrl = DynamicHttpHandling.GetJavascriptDataUrl<int, string, string, object>(GetAreas),
                                applicationId = this.EditorContext.ApplicationId
                            }
                        );
                        w.Write(");");
                    }
                )
            );
        }

        private TfsCollectionInfo[] GetCollections()
        {
            using (var provider = this.GetProvider())
            {
                return provider.GetCollections();
            }
        }
        private TfsIssueTrackingProvider GetProvider()
        {
            var application = StoredProcs.Applications_GetApplication(this.EditorContext.ApplicationId)
                .Execute()
                .Applications_Extended
                .First();

            return (TfsIssueTrackingProvider)Util.Providers.CreateProviderFromId<IssueTrackingProviderBase>(application.IssueTracking_Provider_Id.Value);
        }

        [AjaxMethod]
        private static object GetProjects(int applicationId, string collectionId)
        {
            WebUserContext.ValidatePrivileges(SecuredTask.Applications_EditApplication, applicationId: applicationId);

            var application = StoredProcs.Applications_GetApplication(applicationId)
                .Execute()
                .Applications_Extended
                .First();

            using (var provider = (TfsIssueTrackingProvider)Util.Providers.CreateProviderFromId<IssueTrackingProviderBase>(application.IssueTracking_Provider_Id.Value))
            {
                return from p in provider.GetProjects(Guid.Parse(collectionId))
                       orderby p.Name
                       select new
                       {
                           id = p.Name,
                           text = p.Name
                       };
            }
        }

        [AjaxMethod]
        private static object GetAreas(int applicationId, string collectionId, string projectName)
        {
            WebUserContext.ValidatePrivileges(SecuredTask.Applications_EditApplication, applicationId: applicationId);

            var application = StoredProcs.Applications_GetApplication(applicationId)
                .Execute()
                .Applications_Extended
                .First();

            using (var provider = (TfsIssueTrackingProvider)Util.Providers.CreateProviderFromId<IssueTrackingProviderBase>(application.IssueTracking_Provider_Id.Value))
            {
                return GetAreasInternal(null, provider.GetAreas(Guid.Parse(collectionId), projectName));
            }
        }

        private static IEnumerable<object> GetAreasInternal(string rootPath, TfsAreaInfo[] areas)
        {
            var root = !string.IsNullOrWhiteSpace(rootPath) ? (rootPath + "\\") : string.Empty;
            foreach (var area in areas)
            {
                yield return new
                {
                    id = root + area.Name,
                    text = area.Name,
                    children = GetAreasInternal(root + area.Name, area.Children)
                };
            }
        }
    }
}
