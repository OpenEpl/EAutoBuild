using CommandLine.Text;
using CommandLine;
using OpenEpl.EAutoBuild.Model;
using Microsoft.Extensions.FileSystemGlobbing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace OpenEpl.EAutoBuild
{
    internal class Program
    {
        public class Options
        {
            [Value(0, Required = true, HelpText = "Set root directory")]
            public string Root { get; set; } = ".";
            [Option("script-name", Required = false, HelpText = "Set the name of the generated ninja script", Default = "build.ninja")]
            public string? ScriptName { get; set; }
            [Option("include", Required = false, HelpText = "Set filter to include files")]
            public IEnumerable<string>? Includes { get; set; }
            [Option("exclude", Required = false, HelpText = "Set filter to exclude files")]
            public IEnumerable<string>? Excludes { get; set; }
            [Option("build", Required = false, HelpText = "Build instantly", Default = false)]
            public bool Build { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(Run);
        }

        private static void Run(Options option)
        {
            var solutionWorkingPath = Path.GetFullPath(option.Root);

            var srcMatcher = new Matcher();
            srcMatcher.AddInclude($"/**.ecode");
            srcMatcher.AddInclude($"/**.eform");
            srcMatcher.AddInclude($"/**/@Resource/*");
            var projectMatcher = new Matcher();
            if (option.Includes?.FirstOrDefault() == null)
            {
                projectMatcher.AddInclude("**.eproject");
            }
            else
            {
                projectMatcher.AddIncludePatterns(option.Includes);
            }
            if (option.Excludes is not null)
            {
                projectMatcher.AddExcludePatterns(option.Excludes);
            }

            IEnumerable<string> matchingFiles = projectMatcher.GetResultsInFullPath(solutionWorkingPath);
            var buildScriptPath = Path.Combine(solutionWorkingPath, option.ScriptName ?? "build.ninja");
            using (var writer = new StreamWriter(File.Open(buildScriptPath, FileMode.Create), new UTF8Encoding(false)))
            {
                writer.WriteLine("eplcflags = ");
                writer.WriteLine("rule eplc");
                writer.WriteLine("    command = eplc $in /o $out $eplcflags");
                foreach (var fullPath in matchingFiles)
                {
                    ProjectModel projectModel;
                    using (var reader = new JsonTextReader(new StreamReader(File.Open(fullPath, FileMode.Open), Encoding.UTF8)))
                    {
                        projectModel = JsonSerializer.Create().Deserialize<ProjectModel>(reader)!;
                    }
                    if (string.IsNullOrEmpty(projectModel.OutFile))
                    {
                        continue;
                    }
                    var projectWorkingPath = Path.GetDirectoryName(fullPath)!;
                    var srcBaseFullPath = Path.GetFullPath(projectModel.SourceSet!, projectWorkingPath);
                    var outFileFullPath = Path.GetFullPath(projectModel.OutFile, projectWorkingPath);
                    var outFile = Path.GetRelativePath(solutionWorkingPath, outFileFullPath);
                    var inputFile = Path.GetRelativePath(solutionWorkingPath, fullPath);

                    var implicitInputs = projectModel.Dependencies
                        .OfType<DependencyModel.ECom>()
                        .Select(x => Path.GetFullPath(x.Path, projectWorkingPath))
                        .Concat(srcMatcher.GetResultsInFullPath(srcBaseFullPath))
                        .Select(x => Path.GetRelativePath(solutionWorkingPath, x));
                    writer.Write($"build {EscapePath(outFile)}: eplc {EscapePath(inputFile)}");
                    var enumertorForImpInputs = implicitInputs.GetEnumerator();
                    if (enumertorForImpInputs.MoveNext())
                    {
                        writer.Write(" |");
                        do
                        {
                            writer.Write(" ");
                            writer.Write(EscapePath(enumertorForImpInputs.Current));
                        } while (enumertorForImpInputs.MoveNext());
                    }
                    writer.WriteLine();

                    string? eplcflags = projectModel.AutoBuildConfig?.EplcFlags;
                    if (string.IsNullOrEmpty(eplcflags))
                    {
                        if (projectModel.ProjectType == EplProjectType.WindowsConsole
                        || projectModel.ProjectType == EplProjectType.WindowsApp
                        || projectModel.ProjectType == EplProjectType.WindowsLibrary)
                        {
                            eplcflags = "/static";
                        }
                    }
                    if (!string.IsNullOrEmpty(eplcflags))
                    {
                        writer.WriteLine($"    eplcflags = {eplcflags}");
                    }
                }
            }

            if (option.Build)
            {
                var startInfo = new ProcessStartInfo("ninja");
                startInfo.ArgumentList.Add("-f");
                startInfo.ArgumentList.Add(buildScriptPath);
                startInfo.WorkingDirectory = solutionWorkingPath;
                int exitCode;
                using (var process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                    exitCode = process.ExitCode;
                }
                if (exitCode != 0)
                {
                    Console.Error.WriteLine($"Ninja failed with code: {exitCode}");
                    Environment.Exit(exitCode);
                }
            }
        }

        private static string EscapePath(string path)
        {
            return path.Replace("$", "$$").Replace(" ", "$ ").Replace(":", "$:");
        }
    }
}
