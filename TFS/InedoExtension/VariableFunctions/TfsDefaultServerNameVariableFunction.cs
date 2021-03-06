﻿using System.ComponentModel;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.VariableFunctions;

namespace Inedo.Extensions.TFS.VariableFunctions
{
    [ScriptAlias("TfsDefaultServerName")]
    [Description("The name of the server to connect to when browsing from the web UI; otherwise a local agent is used.")]
    [Tag("tfs")]
    [ExtensionConfigurationVariable(Required = false)]
    public sealed class SvnDefaultServerNameVariableFunction : ScalarVariableFunction
    {
        protected override object EvaluateScalar(IVariableFunctionContext context) => string.Empty;
    }
}
