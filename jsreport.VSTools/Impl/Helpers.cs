using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace jsreport.VSTools.Impl
{
    public static class Helpers
    {
        private static DTE2 DTE { get { return (DTE2)Package.GetGlobalService(typeof(SDTE)); } }
        
        private static bool TryBuild()
        {
            DTE.ExecuteCommand("File.SaveAll");

            DTE.Solution.SolutionBuild.BuildProject(DTE.Solution.SolutionBuild.ActiveConfiguration.Name, ActiveProject.UniqueName, true);
            if (DTE.Solution.SolutionBuild.LastBuildInfo > 0)
            {
                MessageBox.Show("Fix build errors first", "jsreport error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        private static Project ActiveProject
        {
            get
            {
                Project activeProject = null;

                Array activeSolutionProjects = DTE.ActiveSolutionProjects as Array;
                if (activeSolutionProjects != null && activeSolutionProjects.Length > 0)
                {
                    activeProject = activeSolutionProjects.GetValue(0) as Project;
                }

                return activeProject;
            }
        }

        private static string ProjectPath
        {
            get
            {
                return Path.GetDirectoryName(ActiveProject.FullName);
            }
        }

        private static string JsReportDirPath
        {
            get
            {
                return Path.Combine(ProjectPath, "jsreport");
            }
        }

        private static string ExePath
        {
            get
            {
                return Path.Combine(JsReportDirPath, "jsreport.Local");
            }
        }

        private static string BinPath
        {
            get
            {
                return Path.Combine(ProjectPath, ActiveProject.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString());                
            }
        }

        private static string JsreportExeDistPath
        {
            get
            {
                return Path.Combine(BinPath, "jsreport.Local.Dist.dll");
            }
        }

        private static bool TryExtractExe()
        {
            if (!File.Exists(JsreportExeDistPath))
            {
                MessageBox.Show("Missing jsreport.Local.Dist.dll. Install jsreport.Local nuget package or jsreport.Local.Dist package", "jsreport error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;                
            }

            var exeStream = Assembly.Load(File.ReadAllBytes(JsreportExeDistPath)).GetManifestResourceStream("jsreport.Local.Dist.jsreport.Local");
            using (var fs = File.Create(ExePath))
            {
                exeStream.CopyTo(fs);
            }

            return true;
        }

        private static bool EnsureJsReportDirectory()
        {
            if (Directory.Exists(JsReportDirPath))
            {
                MessageBox.Show("jsreport directory already exists in this project", "jsreport error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            Directory.CreateDirectory(JsReportDirPath);
            return true;
        }

        private static void ExecuteJsReport()
        {
            var worker = new System.Diagnostics.Process()
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo(ExePath)
                {
                    Arguments = "start",
                    WorkingDirectory = JsReportDirPath
                }
            };

            worker.Start();

            System.Threading.Tasks.Task.Delay(5000).ContinueWith(t => System.Diagnostics.Process.Start("http://localhost:5488"));            
        }

        public static void AddJsReport()
        {
            if (!TryBuild() || !EnsureJsReportDirectory() || !TryExtractExe())
            {
                return;
            }                      


            File.WriteAllText(Path.Combine(JsReportDirPath, ".gitignore"), "jsreport.Local");

            var jsreportConfigPath = Path.Combine(JsReportDirPath, "dev.config.json");
            File.WriteAllText(jsreportConfigPath, Constants.JSREPORT_CONFIG);

            var csprojPath = Path.Combine(ProjectPath, ActiveProject.FileName);
            string finalMessage = "";

            if (!File.Exists(csprojPath))
            {
                finalMessage = $"Unable to find {csprojPath}. We were not able to include jsreport/** files into the project and set copy to output always. You need to do it by hand.";
            }

            var csprojContent = File.ReadAllText(csprojPath);

            if (!csprojContent.Contains("<Project Sdk=\"Microsoft.NET.Sdk\">"))
            {
                finalMessage = $"The csproj file likely doesn't support wildcard includes. We were not able to include jsreport/** files into the project and set copy to output always. You need to do it by hand.";
            } else
            {
                File.WriteAllText(csprojPath, csprojContent.Replace("</Project>", Constants.CSPROJ_UPDATE));
            } 

            ExecuteJsReport();
            
            if (finalMessage != "")
            {
                MessageBox.Show(finalMessage, "jsreport warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }            
        }

        public static void RunJsReport()
        {
            if (!Directory.Exists(JsReportDirPath))
            {
                MessageBox.Show("jsreport directory is not existing in this project. Run Add jsreport first. ", "jsreport error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!TryExtractExe())
            {
                return;
            }

            ExecuteJsReport();
        }       
    }
}
