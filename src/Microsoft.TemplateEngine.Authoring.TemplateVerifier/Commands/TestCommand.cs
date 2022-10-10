// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.Logging;

namespace Microsoft.TemplateEngine.Authoring.TemplateVerifier.Commands
{
    internal abstract class TestCommand
    {
        protected TestCommand(ILogger log)
        {
            Log = log;
        }

        public ILogger Log { get; }

        public string? WorkingDirectory { get; set; }

        public List<string> Arguments { get; set; } = new List<string>();

        public List<string> EnvironmentToRemove { get; } = new List<string>();

        //  These only work via Execute(), not when using GetProcessStartInfo()
        public Action<string>? CommandOutputHandler { get; set; }

        public Action<Process>? ProcessStartedHandler { get; set; }

        protected Dictionary<string, string> Environment { get; set; } = new Dictionary<string, string>();

        public TestCommand WithEnvironmentVariable(string name, string value)
        {
            Environment[name] = value;
            return this;
        }

        public TestCommand WithWorkingDirectory(string workingDirectory)
        {
            WorkingDirectory = workingDirectory;
            return this;
        }

        public ProcessStartInfo GetProcessStartInfo(params string[] args)
        {
            var commandSpec = CreateCommandSpec(args);

            var psi = commandSpec.ToProcessStartInfo();

            return psi;
        }

        public CommandResult Execute(params string[] args)
        {
            IEnumerable<string> enumerableArgs = args;
            return Execute(enumerableArgs);
        }

        public virtual CommandResult Execute(IEnumerable<string> args)
        {
            var command = CreateCommandSpec(args)
                .ToCommand()
                .CaptureStdOut()
                .CaptureStdErr();

            if (CommandOutputHandler != null)
            {
                command.OnOutputLine(CommandOutputHandler);
            }

            var result = ((Command)command).Execute(ProcessStartedHandler);

            Log.LogInformation($"> {result.StartInfo.FileName} {result.StartInfo.Arguments}");
            Log.LogInformation(result.StdOut);

            if (!string.IsNullOrEmpty(result.StdErr))
            {
                Log.LogWarning("StdErr:");
                Log.LogWarning(result.StdErr);
            }

            if (result.ExitCode != 0)
            {
                Log.LogInformation($"Exit Code: {result.ExitCode}");
            }

            return result;
        }

        protected abstract SdkCommandSpec CreateCommand(IEnumerable<string> args);

        private SdkCommandSpec CreateCommandSpec(IEnumerable<string> args)
        {
            var commandSpec = CreateCommand(args);
            foreach (var kvp in Environment)
            {
                commandSpec.Environment[kvp.Key] = kvp.Value;
            }

            foreach (var envToRemove in EnvironmentToRemove)
            {
                commandSpec.EnvironmentToRemove.Add(envToRemove);
            }

            if (WorkingDirectory != null)
            {
                commandSpec.WorkingDirectory = WorkingDirectory;
            }

            if (Arguments.Any())
            {
                commandSpec.Arguments = Arguments.Concat(commandSpec.Arguments).ToList();
            }

            return commandSpec;
        }
    }
}
