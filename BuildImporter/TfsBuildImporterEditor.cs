using System;
using System.Text.RegularExpressions;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Extensibility.BuildImporters;
using Inedo.BuildMaster.Web.Controls.Extensions.BuildImporters;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.TFS.BuildImporter
{
    internal sealed class TfsBuildImporterEditor : BuildImporterEditorBase<TfsBuildImporterTemplate>
    {
        private ValidatingTextBox txtBuildNumber;

        public override BuildImporterBase CreateFromForm()
        {
            var importer = new TfsBuildImporter
            {
                ArtifactName = Util.CoalesceStr(this.Template.ArtifactName, this.Template.TeamProject),
                TeamProject = this.Template.TeamProject,
                BuildDefinition = this.Template.BuildDefinition,
                TfsBuildNumber = (this.txtBuildNumber.Text != "last succeeded build" && this.txtBuildNumber.Text != "last completed build") ? this.txtBuildNumber.Text : null, 
                IncludeUnsuccessful = this.txtBuildNumber.Text == "last completed build",
                ServerId = this.Template.ServerId
            };

            if (InedoLib.Util.Int.ParseN(importer.TfsBuildNumber) == null)
            {
                var config = (TfsConfigurer)this.GetExtensionConfigurer();
                
                string tfsBuildNumber;
                using (var agent = Util.Agents.CreateAgentFromId(config.ServerId))
                {
                    
                    tfsBuildNumber = agent.GetService<IRemoteMethodExecuter>().InvokeFunc((cfg,i) =>
                        {
                            var helper = new TfsHelper(cfg);
                            var build = helper.GetBuild(i.TeamProject, i.BuildDefinition, i.TfsBuildNumber, i.IncludeUnsuccessful);
                            return build == null ? null : build.BuildNumber;
                        }, config, importer);
                }

                var grp = Regex.Match(tfsBuildNumber, this.Template.BuildNumberPattern).Groups["num"];
                if (grp == null || !grp.Success || string.IsNullOrEmpty(grp.Value))
                    throw new InvalidOperationException(
                        "A build number could not be extracted using "
                        + this.Template.BuildNumberPattern + " from TFS build number " + tfsBuildNumber);

                if (grp.Value.Length > 10)
                    throw new InvalidOperationException(
                        "The extracted build number (" + grp.Value + ") is longer than 10 characters.");

                importer.BuildNumber = grp.Value;

            }

            return importer;
        }

        protected override void CreateChildControls()
        {
            this.txtBuildNumber = new ValidatingTextBox
            {
                AutoCompleteValues = new[] { "last completed build", "last succeeded build" },
                Text = this.Template.BuildNumberLocked ? (this.Template.IncludeUnsuccessful ? "last completed build" : "last succeeded build") : string.Empty,
                Enabled = !this.Template.BuildNumberLocked,
                DefaultText = "last succeeded build"
            };

            this.Controls.Add(
                new SlimFormField("Team project:", this.Template.TeamProject),
                new SlimFormField("Build definition:", this.Template.BuildDefinition),
                new SlimFormField("TFS build number:", this.txtBuildNumber)
            );
        }
    }
}
