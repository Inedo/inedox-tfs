using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Inedo.Agents;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Agents;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.Operations;
using Inedo.Extensibility.SecureResources;
using Inedo.Extensions.Credentials;
using Inedo.Extensions.TFS.Credentials;
using Inedo.IO;

namespace Inedo.Extensions.TFS.Operations
{
    [Serializable]
    public abstract class TfsOperation : ExecuteOperation, ILogSink
    {
        private protected TfsOperation()
        {
        }

        public abstract string CredentialName { get; set; }

        [Category("Connection/Identity")]
        [ScriptAlias("Url")]
        [DisplayName("Project collection URL")]
        [PlaceholderText("Use team project from credentials")]
        public string TeamProjectCollectionUrl { get; set; }

        [Category("Connection/Identity")]
        [ScriptAlias("UserName")]
        [DisplayName("User name")]
        [PlaceholderText("Use user name from credentials")]
        public string UserName { get; set; }

        [Category("Connection/Identity")]
        [ScriptAlias("Password")]
        [DisplayName("Password / token")]
        [PlaceholderText("Use password/token from credentials")]
        public string PasswordOrToken { get; set; }

        [Category("Connection/Identity")]
        [ScriptAlias("Domain")]
        [DisplayName("Domain name")]
        [PlaceholderText("Use domain from credentials")]
        public string Domain { get; set; }

#warning change to a better argument builder
        protected async Task<TfsTinyExecutionResult> ExecuteCommandAsync(IOperationExecutionContext context, string command, params string[] arguments)
        {
            this.LogDebug($"Executing TfsTiny.exe {command}");
            var executer = context.Agent.GetService<IRemoteProcessExecuter>();
            var argBuilder = new StringBuilder();
            argBuilder.Append(command).Append(" ");

            UsernamePasswordCredentials credentials = null;
            TfsSecureResource resource = null;
            if (!string.IsNullOrEmpty(this.CredentialName))
            {
                resource = (TfsSecureResource)SecureResource.TryCreate(this.CredentialName, context);
                credentials = (UsernamePasswordCredentials)resource.GetCredentials(context);
            }

            argBuilder.Append($"--url={(this.TeamProjectCollectionUrl ?? resource?.TeamProjectCollectionUrl)} ");
            argBuilder.Append($"--username={(this.UserName ?? credentials?.UserName)} ");
            argBuilder.Append($"--password={(this.PasswordOrToken ?? AH.Unprotect(credentials?.Password))} ");
            if (!string.IsNullOrWhiteSpace(this.Domain))
                argBuilder.Append($"--domain={this.Domain} ");

            argBuilder.Append(string.Join(' ', arguments));

            var startInfo = new RemoteProcessStartInfo
            {
                FileName = GetEmbeddedTfsTinyExePath(context.Agent),
                Arguments = argBuilder.ToString(),
                WorkingDirectory = context.WorkingDirectory
            };
            this.LogDebug("Working directory: " + startInfo.WorkingDirectory);
            //this.LogDebug("Executing: " + startInfo.FileName + " " + arguments.ToSensitiveString());
            using (var process = executer.CreateProcess(startInfo))
            {
                var outputLines = new List<string>();
                var errorLines = new List<string>();

                process.OutputDataReceived += (s, e) => { 
                    if (e?.Data != null) { 
                        outputLines.Add(e.Data);
                        if (e.Data.StartsWith("INFO:"))
                            this.LogInformation(e.Data);
                        else if (e.Data.StartsWith("DEBUG:"))
                            this.LogDebug(e.Data);
                        else if (e.Data.StartsWith("WARN:"))
                            this.LogWarning(e.Data);
                        else if (e.Data.StartsWith("ERROR:"))
                            this.LogError(e.Data);
                        else
                            this.LogInformation(e.Data);
                    }
                };
                process.ErrorDataReceived += (s, e) => {
                    if (e?.Data != null)
                    {
                        errorLines.Add(e.Data);
                        this.LogError(e.Data);
                    }
                };

                process.Start();

                await process.WaitAsync(context.CancellationToken).ConfigureAwait(false);

                this.LogDebug($"Finished executing TfsTiny.exe {command}");
                return new TfsTinyExecutionResult(process.ExitCode ?? -1, outputLines, errorLines);
            }
        }

        public sealed class TfsTinyExecutionResult
        {
            public TfsTinyExecutionResult(int exitCode, IList<string> outputLines, IList<string> errorLines)
            {
                this.ExitCode = exitCode;
                this.OutputLines = outputLines;
                this.ErrorLines = errorLines;
            }

            public int ExitCode { get; }
            public IList<string> OutputLines { get; }
            public IList<string> ErrorLines { get; }
        }

        private static string GetEmbeddedTfsTinyExePath(Agent agent)
        {
            var executer = agent.GetService<IRemoteMethodExecuter>();
            string assemblyDir = executer.InvokeFunc(GetAgentProviderAssemblyDirectory);
            var fileOps = agent.GetService<IFileOperationsExecuter>();
            return fileOps.CombinePath(assemblyDir, "TfsTiny", "TfsTiny.exe");
        }
        private static string GetAgentProviderAssemblyDirectory()
        {
            return PathEx.GetDirectoryName(typeof(TfsOperation).Assembly.Location);
        }
    }
}
