﻿using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading.Tasks;
using Aiwins.Rocket.Cli.Args;
using Aiwins.Rocket.Cli.ProjectModification;
using Aiwins.Rocket.Cli.Utils;
using Aiwins.Rocket.DependencyInjection;

namespace Aiwins.Rocket.Cli.Commands
{
    public class AddModuleCommand : IConsoleCommand, ITransientDependency
    {
        public ILogger<AddModuleCommand> Logger { get; set; }

        protected SolutionModuleAdder SolutionModuleAdder { get; }
        public SolutionRocketVersionFinder SolutionRocketVersionFinder { get; }

        public AddModuleCommand(SolutionModuleAdder solutionModuleAdder, SolutionRocketVersionFinder solutionRocketVersionFinder)
        {
            SolutionModuleAdder = solutionModuleAdder;
            SolutionRocketVersionFinder = solutionRocketVersionFinder;
            Logger = NullLogger<AddModuleCommand>.Instance;
        }

        public async Task ExecuteAsync(CommandLineArgs commandLineArgs)
        {
            if (commandLineArgs.Target == null)
            {
                throw new CliUsageException(
                    "Module name is missing!" +
                    Environment.NewLine + Environment.NewLine +
                    GetUsageInfo()
                );
            }

            var withSourceCode = commandLineArgs.Options.ContainsKey("with-source-code");

            var skipDbMigrations = Convert.ToBoolean(
                commandLineArgs.Options.GetOrNull(Options.DbMigrations.Skip) ?? "false");

            var solutionFile = GetSolutionFile(commandLineArgs);

            var version = commandLineArgs.Options.GetOrNull(Options.Version.Short, Options.Version.Long);
            if (version == null)
            {
                version = SolutionRocketVersionFinder.Find(solutionFile);
            }

            await SolutionModuleAdder.AddAsync(
                solutionFile,
                commandLineArgs.Target,
                commandLineArgs.Options.GetOrNull(Options.StartupProject.Short, Options.StartupProject.Long),
                version,
                skipDbMigrations,
                withSourceCode
            );
        }

        public string GetUsageInfo()
        {
            var sb = new StringBuilder();

            sb.AppendLine("");
            sb.AppendLine("'add-module' command is used to add a multi-package ROCKET module to a solution.");
            sb.AppendLine("It should be used in a folder containing a .sln file.");
            sb.AppendLine("");
            sb.AppendLine("Usage:");
            sb.AppendLine("  rocket add-module <module-name> [options]");
            sb.AppendLine("");
            sb.AppendLine("Options:");
            sb.AppendLine("  --with-source-code                              Downloads the source code of the module and adds it to your solution.");
            sb.AppendLine("  -s|--solution <solution-file>                   Specify the solution file explicitly.");
            sb.AppendLine("  --skip-db-migrations <boolean>                  Specify if a new migration will be added or not.");
            sb.AppendLine("  -sp|--startup-project <startup-project-path>    Relative path to the project folder of the startup project. Default value is the current folder.");
            sb.AppendLine("");
            sb.AppendLine("Examples:");
            sb.AppendLine("");
            sb.AppendLine("  rocket add-module Aiwins.Blogging                      Adds the module to the current solution.");
            sb.AppendLine("  rocket add-module Aiwins.Blogging -s Acme.BookStore    Adds the module to the given solution.");
            sb.AppendLine("  rocket add-module Aiwins.Blogging -s Acme.BookStore --skip-db-migrations false    Adds the module to the given solution but doesn't create a database migration.");
            sb.AppendLine(@"  rocket add-module Aiwins.Blogging -s Acme.BookStore -sp ..\Acme.BookStore.Web\Acme.BookStore.Web.csproj   Adds the module to the given solution and specify migration startup project.");
            sb.AppendLine("");
            sb.AppendLine("See the documentation for more info: https://docs.aiwins.cn/en/rocket/latest/CLI");

            return sb.ToString();
        }

        public string GetShortDescription()
        {
            return "Add a multi-package module to a solution by finding all packages of the module, " +
                   "finding related projects in the solution and adding each package to the corresponding project in the solution.";
        }

        protected virtual string GetSolutionFile(CommandLineArgs commandLineArgs)
        {
            var providedSolutionFile = PathHelper.NormalizePath(
                commandLineArgs.Options.GetOrNull(
                    Options.Solution.Short,
                    Options.Solution.Long
                )
            );

            if (!providedSolutionFile.IsNullOrWhiteSpace())
            {
                return providedSolutionFile;
            }

            var foundSolutionFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.sln");
            if (foundSolutionFiles.Length == 1)
            {
                return foundSolutionFiles[0];
            }

            if (foundSolutionFiles.Length == 0)
            {
                throw new CliUsageException("'rocket add-module' command should be used inside a folder containing a .sln file!");
            }

            //foundSolutionFiles.Length > 1

            var sb = new StringBuilder("There are multiple solution (.sln) files in the current directory. Please specify one of the files below:");

            foreach (var foundSolutionFile in foundSolutionFiles)
            {
                sb.AppendLine("* " + foundSolutionFile);
            }

            sb.AppendLine("Example:");
            sb.AppendLine($"rocket add-module {commandLineArgs.Target} -p {foundSolutionFiles[0]}");

            throw new CliUsageException(sb.ToString());
        }

        public static class Options
        {
            public static class Solution
            {
                public const string Short = "s";
                public const string Long = "solution";
            }
            public static class Version
            {
                public const string Short = "v";
                public const string Long = "version";
            }

            public static class DbMigrations
            {
                public const string Skip = "skip-db-migrations";
            }

            public static class StartupProject 
            {
                public const string Short = "sp";
                public const string Long = "startup-project";
            }
        }
    }
}
