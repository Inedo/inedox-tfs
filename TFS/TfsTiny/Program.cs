using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Security;
using System.Threading.Tasks;
using Inedo.TFS.Clients.Rest;
using Inedo.TFS.Clients.SourceControl;
using Inedo.TFS.TfsTiny;
using Inedo.TFS.VisualStudioOnline.Model;
using Microsoft.TeamFoundation.VersionControl.Client;
using Newtonsoft.Json;

namespace Inedo.TFS
{
    internal class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                var inputArgs = InputArgs.Parse(args);
                if (inputArgs.Positional.Length == 0)
                {
                    await WriteUsageAsync();
                    return 1;
                }

                await (inputArgs.Positional[0] switch
                {
                    "get" => GetSourceAsync(inputArgs),
                    "label" => LabelSourceAsync(inputArgs),
                    "list" => ListChangesAsync(inputArgs),
                    "help" => WriteUsageAsync(),
                    _ => throw new ConsoleException($"Invalid command: {inputArgs.Positional[0]}")
                });

                return 0;
            }
            catch (ConsoleException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return -1;
            }
        }

        private static Task GetSourceAsync(InputArgs inputArgs)
        {
            /*
             * params needed:
             * - url
             * - username
             * - password
             * - domain
             * - source
             * - workspace
             * - target
             * - label
             */


            if (!inputArgs.Named.TryGetValue("url", out var url))
                throw new ConsoleException("Missing parameter --url");
            if (!inputArgs.Named.TryGetValue("username", out var username))
                throw new ConsoleException("Missing parameter --username");
            if (!inputArgs.Named.TryGetValue("password", out var password))
                throw new ConsoleException("Missing parameter --password");

            var source = inputArgs.Named.TryGetValue("source", out var s) ? s : "$/";
            var target = inputArgs.Named.TryGetValue("target", out var t) ? t : "\\TfsOut";
            var workspace = inputArgs.Named.TryGetValue("workspace", out var w) ? t : "\\TfsWorkSpace";
            

            using (var client = new TfsSourceControlClient(
                url,
                username,
                password,
                inputArgs.Named.TryGetValue("domain"),
                ConsoleLogSink.Instance
            ))
            {

                client.GetSource(
                    new TfsSourcePath(source),
                    new Clients.SourceControl.WorkspaceInfo(null, workspace, null),
                    target ?? "\\",
                    inputArgs.Named.TryGetValue("label")
                );
            }

            return Task.CompletedTask;
        }

        private static Task LabelSourceAsync(InputArgs inputArgs)
        {
            /*
             * params needed:
             * - url
             * - username
             * - password
             * - domain
             * - source
             * - label
             * - comment
             */
            if (!inputArgs.Named.TryGetValue("url", out var url))
                throw new ConsoleException("Missing parameter --url");
            if (!inputArgs.Named.TryGetValue("username", out var username))
                throw new ConsoleException("Missing parameter --username");
            if (!inputArgs.Named.TryGetValue("password", out var password))
                throw new ConsoleException("Missing parameter --password");

            var source = inputArgs.Named.TryGetValue("source", out var s) ? s : "$/";

            using (var client = new TfsSourceControlClient(
                url,
                username,
                password,
                inputArgs.Named.TryGetValue("domain"),
                ConsoleLogSink.Instance
            ))
            {
                client.ApplyLabel(
                    new TfsSourcePath(source),
                    inputArgs.Named.TryGetValue("label"),
                    inputArgs.Named.TryGetValue("comment") ?? "Label applied by BuildMaster"
                );
            }

            return Task.CompletedTask;
        }

        private static async Task ListChangesAsync(InputArgs inputArgs)
        {
            /*
             * params needed:
             * - url
             * - username
             * - password
             * - domain
             * -source
             * -project
             */

            if (!inputArgs.Named.TryGetValue("url", out var url))
                throw new ConsoleException("Missing parameter --url");
            if (!inputArgs.Named.TryGetValue("username", out var username))
                throw new ConsoleException("Missing parameter --username");
            if (!inputArgs.Named.TryGetValue("password", out var password))
                throw new ConsoleException("Missing parameter --password");
            if (!inputArgs.Named.TryGetValue("project", out var project))
                throw new ConsoleException("Missing parameter --project");


            var source = inputArgs.Named.TryGetValue("source", out var s) ? s : "$/";

            var conn = new ConnectionInfo(username, password, url, inputArgs.Named.TryGetValue("domain"));
            var client = new TfsRestApi(conn, ConsoleLogSink.Instance);
            var dic = new Dictionary<string, TfsCommitStatus>();

            try
            {
                var checkins = await client.GetChangesetsAsync(project, source);

                dic.Add(source, new TfsCommitStatus { ChangeSetId = checkins.FirstOrDefault()?.changesetId });
            }
            catch (Exception ex)
            {
                string message = "An error occurred attempting to determine latest change set: " + ex.Message;
                dic.Add(source, new TfsCommitStatus { Error = message });
            }
            Console.WriteLine($"OUTPUT: {JsonConvert.SerializeObject(dic)}");
        }

        private static Task WriteUsageAsync()
        {
#warning finish these
            Console.WriteLine("Usage:get --url=<> --username=<> --password=<> [--domain=<>] [--source=<>] [--workspace=<>] [--target=<>] [--label=<>]");
            return Task.CompletedTask;
        }

        
    }

    public class ConnectionInfo : IVsoConnectionInfo
    {
        public ConnectionInfo(string username, string password, string projectCollectionUrl, string domain)
        {

            this.UserName = username;
            this.PasswordOrToken = password;
            this.Domain = domain;
            this.TeamProjectCollectionUrl = projectCollectionUrl;
        }

        public string UserName { get; }
        public string PasswordOrToken { get; }
        public string Domain { get; }
        public string TeamProjectCollectionUrl { get; }
    }

    public class TfsCommitStatus
    {
        public int? ChangeSetId { get; set; }
        public string Error { get; set; }
    }

    public class ConsoleLogSink : ILogSink
    {
        private ConsoleLogSink()
        {

        }

        public static ConsoleLogSink Instance => new ConsoleLogSink();

        public void Log(IMessage message)
        {
            if (message.Level == MessageLevel.Error)
            {
                Console.Error.WriteLine($"{message.Level}: {message.Message}");
                if (!string.IsNullOrWhiteSpace(message.Details))
                    Console.Error.WriteLine($"{message.Level}: {message.Details}");
            }
            else
            {
                Console.WriteLine($"{message.Level}: {message.Message}");
                if (!string.IsNullOrWhiteSpace(message.Details))
                    Console.WriteLine($"{message.Level}: {message.Details}");
            }
        }
    }
}