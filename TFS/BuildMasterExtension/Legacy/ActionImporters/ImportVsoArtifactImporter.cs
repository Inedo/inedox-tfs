using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMasterExtensions.TFS.Operations;
using Inedo.BuildMasterExtensions.TFS.VisualStudioOnline;

namespace Inedo.BuildMasterExtensions.TFS.Legacy.ActionImporters
{
    internal sealed class ImportVsoArtifactImporter : IActionOperationConverter<ImportVsoArtifactAction, ImportVsoArtifactOperation>
    {
        public ConvertedOperation<ImportVsoArtifactOperation> ConvertActionToOperation(ImportVsoArtifactAction action, IActionConverterContext context)
        {
            var configurer = (TfsConfigurer)context.Configurer;
            
            return new ImportVsoArtifactOperation
            {
                ArtifactName = action.ArtifactName,
                BuildDefinition = action.BuildDefinition,
                BuildNumber = action.BuildNumber,
                UserName = configurer.UserName,
                PasswordOrToken = configurer.Password,
                Domain = configurer.Domain,
                TeamProjectCollectionUrl = configurer.BaseUrl,
                TeamProject = action.TeamProject
            };
        }
    }
}
