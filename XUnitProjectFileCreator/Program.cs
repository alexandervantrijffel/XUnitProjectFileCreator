using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using CommandLine;
using CommandLine.Text;
using Microsoft.SqlServer.Server;

namespace XUnitProjectFileCreator
{
    public class ProgramOptions
    {
        [Option('p', Required = false, HelpText = "Search pattern")]
        public string SearchPattern { get; set; }

        [Option('o', Required = false, HelpText = "Output file name")]
        public string OutputFile { get; set; }

        [Option('s', Required = false, HelpText = "Start directory")]
        public string StartDirectory { get; set; }

        [Option('e', Required = false, HelpText = "Comma separated list of exclude patterns.")]
        public string ExcludePatterns { get; set; }
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
            var programOptions = new ProgramOptions();
            if (Parser.Default.ParseArguments(args, programOptions))
            {
                CreateProjectFile(programOptions);

            }
            else
            {
                var helpText = HelpText.AutoBuild(programOptions);
                helpText.Heading = "XUnit Project File Creator";
                helpText.AddPreOptionsLine("The command line arguments are incorrect. Please make sure that all required arguments are specified:");
                helpText.AddPreOptionsLine("");
                helpText.AddPostOptionsLine("Example:");
                helpText.AddPostOptionsLine("");
                helpText.AddPostOptionsLine("xunitprojectfilecreator.exe -p *test*.dll -o myproject.xunit -e *\\obj\\*,*acceptancetests*");
                Console.Write(helpText);
            }
        }

        private static void CreateProjectFile(ProgramOptions programOptions)
        {
            string startDirectory;
            if (!string.IsNullOrEmpty(programOptions.StartDirectory))
            {
                if (!Directory.Exists(programOptions.StartDirectory))
                {
                    Console.WriteLine("Start directory '{0}' doesn't exist: exiting", programOptions.StartDirectory);
                    return;
                }
                startDirectory = programOptions.StartDirectory;
            }
            else
            {
                startDirectory = Directory.GetCurrentDirectory();
            }

            using (var xml = new XmlTextWriter(programOptions.OutputFile, Encoding.UTF8))
            {
                xml.Formatting = Formatting.Indented;
                xml.WriteStartElement("xunit");
                xml.WriteStartElement("assemblies");
                var files = new DirectoryInfo(startDirectory).GetFiles(programOptions.SearchPattern, SearchOption.AllDirectories);
                var exclude = !string.IsNullOrEmpty(programOptions.ExcludePatterns);
                var regexes = new List<Regex>();
                if (exclude)
                {
                    foreach(var pattern in programOptions.ExcludePatterns.Split(','))
                        regexes.Add(new Regex(pattern.Trim().Replace("*", ".*").Replace("\\", "\\\\"), RegexOptions.IgnoreCase));
                }
                foreach (var fi in files)
                {
                    if (exclude)
                    {
                        if (regexes.Any(r => r.IsMatch(fi.FullName)))
                        {
                            Console.WriteLine("Skipping file {0} because of exclude pattern.",fi.FullName);
                            continue;
                        }
                    }

                    xml.WriteStartElement("assembly");
                    xml.WriteAttributeString("filename", fi.FullName);
                    xml.WriteAttributeString("shadow-copy", "true");
                    xml.WriteEndElement();
                }
                xml.WriteEndElement();
                xml.WriteEndElement();
            }
            Console.WriteLine("Created file {0} successfully.", programOptions.OutputFile);
        }
    }
}
