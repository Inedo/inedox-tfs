using System.ComponentModel;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.VariableFunctions;
using Inedo.Documentation;

namespace Inedo.BuildMasterExtensions.TFS.VariableFunctions
{
    [ScriptAlias("TfsDefaultServerName")]
    [Description("The name of the server to connect to when browsing from the web UI, otherwise a local agent is used")]
    [Tag("tfs")]
    [ExtensionConfigurationVariable(Required = false)]
    public sealed class SvnDefaultServerNameVariableFunction : ScalarVariableFunction
    {
        protected override object EvaluateScalar(IGenericBuildMasterContext context)
        {
            return string.Empty;
        }
    }
}
