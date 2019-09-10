using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace ReleaseBot
{
    class Program
    {
        private const int V = 2; // <--- pythonnet_netstandard version!
        private static readonly string ProjectPath = "WrapperProject";
        private const string ProjectName = "Python.Runtime.Wrapper.csproj";
        private const string Description = "Pythonnet compiled against .NetStandard 2.0 and CPython ";
        private const string Tags = "Python, pythonnet, interop";

        static void Main(string[] args)
        {
            var specs = new ReleaseSpec[]
            {
                new ReleaseSpec() { Version = "2.7."+V, Description = Description + "2.7", PythonMajorVersion = 2, PythonMinorVersion = 7, PackageTags = Tags, RelativeProjectPath = ProjectPath, ProjectName = ProjectName },
                new ReleaseSpec() { Version = "3.5."+V, Description = Description + "3.5", PythonMajorVersion = 3, PythonMinorVersion = 5, PackageTags = Tags, RelativeProjectPath = ProjectPath, ProjectName = ProjectName },
                new ReleaseSpec() { Version = "3.6."+V, Description = Description + "3.6", PythonMajorVersion = 3, PythonMinorVersion = 6, PackageTags = Tags, RelativeProjectPath = ProjectPath, ProjectName = ProjectName },
                new ReleaseSpec() { Version = "3.7."+V, Description = Description + "3.7", PythonMajorVersion = 3, PythonMinorVersion = 7, PackageTags = Tags, RelativeProjectPath = ProjectPath, ProjectName = ProjectName },

            };
            foreach (var spec in specs)
            {
                //try
                {
                    spec.Process();
                }
                //catch (Exception e)
                //{
                //    Console.WriteLine(e.Message);
                //    Console.WriteLine(e.StackTrace);
                //}
            }

            var key = File.ReadAllText(Path.Combine("..", "..", "nuget.key")).Trim();
            foreach (var nuget in Directory.GetFiles(Path.Combine(ProjectPath, "bin", "Release"), "*.nupkg"))
            {
                Console.WriteLine("Push " + nuget);
                var nugetExePath = Path.Combine(AppContext.BaseDirectory, "nuget.exe");
                var arg = $"push -Source https://api.nuget.org/v3/index.json -ApiKey {key} {nuget}";                
                var p = new Process() { StartInfo = new ProcessStartInfo(nugetExePath, arg) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false} };
                p.OutputDataReceived += (x, data) => Console.WriteLine(data.Data);
                p.ErrorDataReceived += (x, data) => Console.WriteLine("Error: " + data.Data);
                //p.Start();
                //p.WaitForExit();
                Console.WriteLine("... pushed");
            }
            Thread.Sleep(3000);
        }
    }

    public class ReleaseSpec
    {
        /// <summary>
        /// The assembly / nuget package version
        /// </summary>
        public string Version;

        /// <summary>
        /// The Python major version to use
        /// </summary>
        public int PythonMajorVersion;

        /// <summary>
        /// The Python minor version to use
        /// </summary>
        public int PythonMinorVersion;

        /// <summary>
        /// Project description
        /// </summary>
        public string Description;

        /// <summary>
        /// Project description
        /// </summary>
        public string PackageTags;

        /// <summary>
        /// Path to the csproj file, relative to the execution directory of ReleaseBot
        /// </summary>
        public string RelativeProjectPath;

        /// <summary>
        /// Name of the csproj file
        /// </summary>
        public string ProjectName;

        public string FullProjectPath => Path.Combine(RelativeProjectPath, ProjectName);

        public void Process()
        {
            if (!File.Exists(FullProjectPath))
                throw new InvalidOperationException("Project not found at: "+FullProjectPath);
            
            // Pack in release mode
            Console.WriteLine("Pack " + Description);
            var dotnetArguments = new[]
            {
                "pack",
                "-c Release",
                EscapeCommandLineArgument("-p:Version=" + Version),
                EscapeCommandLineArgument($@"-p:Description=""{Description}"""),
                "-p:PythonMajorVersion=" + PythonMajorVersion.ToString(CultureInfo.InvariantCulture),
                "-p:PythonMinorVersion=" + PythonMinorVersion.ToString(CultureInfo.InvariantCulture),
                EscapeCommandLineArgument($@"-p:PackageTags=""{PackageTags}""")
            };
            
            var p=new Process(){ StartInfo = new ProcessStartInfo("dotnet", string.Join(" ", dotnetArguments)) { WorkingDirectory = Path.GetFullPath(RelativeProjectPath) } };
            p.Start();
            p.WaitForExit();
        }

        private static string EscapeCommandLineArgument(string arg)
        {
            // http://stackoverflow.com/a/6040946/784387
            arg = Regex.Replace(arg, @"(\\*)" + "\"", @"$1$1\" + "\"");
            arg = "\"" + Regex.Replace(arg, @"(\\+)$", @"$1$1") + "\"";
            return arg;
        }
    }
}
