﻿using System;
using System.Threading.Tasks;
using Inedo.TFS.Clients.SourceControl;
using Inedo.TFS.TfsTiny;

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
            if (!inputArgs.Named.TryGetValue("url", out var url))
                throw new ConsoleException("Missing parameter --url");
            if (!inputArgs.Named.TryGetValue("username", out var username))
                throw new ConsoleException("Missing parameter --username");
            if (!inputArgs.Named.TryGetValue("password", out var password))
                throw new ConsoleException("Missing parameter --password");

            var source = inputArgs.Named.TryGetValue("source", out var s) ? s : "$/";
            var target = inputArgs.Named.TryGetValue("target", out var t) ? t : "\\TfsOut";
            var workspace = inputArgs.Named.TryGetValue("workspace", out var w) ? w : "\\TfsWorkSpace";

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
                    new WorkspaceInfo(workspace, inputArgs.Named.TryGetValue("workspace-name")),
                    target ?? "\\",
                    inputArgs.Named.TryGetValue("label")
                );
            }

            return Task.CompletedTask;
        }

        private static Task LabelSourceAsync(InputArgs inputArgs)
        {
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

        private static Task WriteUsageAsync()
        {
            Console.WriteLine("Usage: get --url=<URL of Azure DevOps instance> --username=<Azure DevOps user name > --password=<Azure DevOps Password or PAT> [--domain=<User's domain>] [--source=<path in TFVC repository>] [--workspace=<Workspace folder>] [--workspace-name=<Workspace name>] [--target=<location to save checked out files>] [--label=<Label to check out at>]");
            Console.WriteLine("Usage: label --url=<URL of Azure DevOps instance> --username=<Azure DevOps user name > --password=<Azure DevOps Password or PAT> [--domain=<User's domain>] [--source=<path in TFVC repository>] [--label=<label name to apply>] [--comment=<comment message for label>]");
            return Task.CompletedTask;
        }

        
    }
}