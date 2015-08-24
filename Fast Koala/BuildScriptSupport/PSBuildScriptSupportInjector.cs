﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using EnvDTE;
using Wijits.FastKoala.BuildScriptSupport;
using Wijits.FastKoala.Logging;
using Wijits.FastKoala.Utilities;
// ReSharper disable LocalizableElement
// ReSharper disable SimplifyLinqExpression

namespace Wijits.FastKoala.BuildScriptInjections
{
    public class PSBuildScriptSupportInjector
    {
        private ILogger _logger;
        private EnvDTE.Project _project;
        private readonly string _projectName;
        private readonly ProjectProperties _projectProperties;

        public PSBuildScriptSupportInjector(EnvDTE.Project project, ILogger logger)
        {
            _project = project;
            _projectName = project.Name;
            _projectProperties = new ProjectProperties(project);
            _logger = logger;
        }
        
        public async Task AddPowerShellScript(string containerDirectory, 
            string scriptFile = null, bool? invokeAfter = null)
        {
            invokeAfter = invokeAfter ?? true;
            if (string.IsNullOrWhiteSpace(containerDirectory))
                containerDirectory = _project.GetDirectory();

            if (string.IsNullOrWhiteSpace(scriptFile))
            {
                var dialog = new AddBuildScriptNamePrompt(containerDirectory, ".ps1");
                var dialogResult = dialog.ShowDialog(VsEnvironment.OwnerWindow);
                if (dialogResult == DialogResult.Cancel) return;
                scriptFile = dialog.FileName;
                invokeAfter = dialog.InvokeAfter;
            }
            if (!scriptFile.Contains(":") && !scriptFile.StartsWith("\\\\"))
                scriptFile = Path.Combine(containerDirectory, scriptFile);
            var scriptFileRelativePath = FileUtilities.GetRelativePath(_project.GetDirectory(), scriptFile);

            if (!_project.Saved) _project.Save();
            _project = await EnsureProjectHasPowerShellEnabled();

            File.WriteAllText(scriptFile, "# Write-Output \"`$MSBuildProjectDirectory=$MSBuildProjectDirectory\"");
            var addedItem = _project.ProjectItems.AddFromFile(scriptFile);
            addedItem.Properties.Item("ItemType").Value 
                = invokeAfter.Value ? "InvokeAfter" : "InvokeBefore";
        }

        private async Task<EnvDTE.Project> EnsureProjectHasPowerShellEnabled()
        {
            if (!_projectProperties.PowerShellBuildEnabled)
                return await InjectPowerShellScriptSupport();
            return _project;
        }

        private async Task<EnvDTE.Project> InjectPowerShellScriptSupport()
        {
            var projRoot = _project.GetProjectRoot();
            if (projRoot.UsingTasks.Any(ut => ut.TaskName == "InvokePowerShell"))
                return _project;
            var usingTask = projRoot.AddUsingTask("InvokePowerShell",
                @"$(MSBuildToolsPath)\Microsoft.Build.Tasks.v$(MSBuildToolsVersion).dll", null);
            usingTask.TaskFactory = "CodeTaskFactory";
            var utParameters = usingTask.AddParameterGroup();

            var utParamsScriptFile = utParameters.AddParameter("ScriptFile");
            utParamsScriptFile.ParameterType = "System.String";
            utParamsScriptFile.Required = "true";
            if (!projRoot.PropertyGroups.Any(pg => pg.Label == "InvokeBeforeAfter"))
            {
                var invokeBeforeAfterGroup = projRoot.AddItemGroup();
                invokeBeforeAfterGroup.Label = "InvokeBeforeAfter";
                var invokeBefore = invokeBeforeAfterGroup.AddItem("AvailableItemName", "InvokeBefore");
                invokeBefore.AddMetadata("Visible", "false");
                var invokeAfter = invokeBeforeAfterGroup.AddItem("AvailableItemName", "InvokeAfter");
                invokeAfter.AddMetadata("Visible", "false");
            }
            var psScriptsBefore = projRoot.AddTarget("PSScriptsBefore");
            psScriptsBefore.BeforeTargets = "Build";
            var invokeBeforeExec = psScriptsBefore.AddTask("InvokePowerShell");
            invokeBeforeExec.SetParameter("ScriptFile", "%(InvokeBefore.Identity)");
            invokeBeforeExec.Condition = "'@(InvokeBefore)' != ''";
            var psScriptsAfter = projRoot.AddTarget("PSScriptsAfter");
            psScriptsAfter.AfterTargets = "Build";
            var invokeAfterExec = psScriptsAfter.AddTask("InvokePowerShell");
            invokeAfterExec.SetParameter("ScriptFile", "%(InvokeAfter.Identity)");
            invokeAfterExec.Condition = "'@(InvokeAfter)' != ''";

            _projectProperties.PowerShellBuildEnabled = true;
            var projectRootPath = _project.FullName;
            _project = await _project.SaveProjectRoot();
            
            VsEnvironment.Dte.UnloadProject(_project);

            var projXml = File.ReadAllText(projectRootPath);
            // ReSharper disable StringIndexOfIsCultureSpecific.1
            // ReSharper disable StringIndexOfIsCultureSpecific.2
            var injectLocation = projXml.IndexOf("</UsingTask",
                projXml.IndexOf("<UsingTask TaskName=\"InvokePowerShell\""));
            projXml = projXml.Substring(0, injectLocation)
                      + @"
        <Task>
        <Reference Include=""Microsoft.Build"" />
        <Reference Include=""System.Management.Automation"" />
        <Reference Include=""System.Xml"" />
        <Using Namespace=""System.Management.Automation"" />
        <Using Namespace=""System.Management.Automation.Runspaces"" />
        <Using Namespace=""Microsoft.Build.Evaluation"" />
        <Code Type=""Fragment"" Language=""cs""><![CDATA[
        if (!ScriptFile.ToLower().EndsWith("".ps1"")) return true;
        Project project = ProjectCollection.GlobalProjectCollection.GetLoadedProjects(BuildEngine.ProjectFileOfTaskNode).FirstOrDefault()
            ?? new Project(BuildEngine.ProjectFileOfTaskNode);
        if (!ScriptFile.Contains("":"") && !ScriptFile.StartsWith(""\\\\""))
            ScriptFile = project.DirectoryPath + ""\\"" + ScriptFile;
        var runspaceConfig = RunspaceConfiguration.Create();
        using (Runspace runspace = RunspaceFactory.CreateRunspace(runspaceConfig)) 
        { 
            runspace.Open();
            var vars = new System.Text.StringBuilder();
            foreach (ProjectProperty evaluatedProperty in project.AllEvaluatedProperties)
            {
                if (!evaluatedProperty.IsEnvironmentProperty)
                {
                    var name = evaluatedProperty.Name;
                    var value = evaluatedProperty.EvaluatedValue;
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            if (value.ToLower() == ""true"" || value.ToLower() == ""false"") {
                                vars.AppendLine(""$"" + name + "" = $"" + value);
                            } else {
                                vars.AppendLine(""$"" + name + "" = @\""\r\n"" + value.Replace(""\"""", ""\""\"""") + ""\r\n\""@"");
                            }
                        }
                    }
                }
            }
            using (RunspaceInvoke scriptInvoker = new RunspaceInvoke(runspace)) 
            { 
                using (Pipeline pipeline = runspace.CreatePipeline()) {
                    scriptInvoker.Invoke(vars.ToString()); 
                    var fileName = ScriptFile.Substring(project.DirectoryPath.Length + 1);
                    BuildEngine.LogMessageEvent(new BuildMessageEventArgs(
                        ""Executing script file: "" + fileName, """", """", MessageImportance.High));
                    pipeline.Commands.AddScript(""& \"""" + ScriptFile + ""\""""); 
                    try {
                        var results = pipeline.Invoke();
                        string soutput = """";
                        foreach (var result in results)
                        {
                            soutput += result.ToString();
                        }
                        BuildEngine.LogMessageEvent(new BuildMessageEventArgs(soutput, """", """", MessageImportance.High));
                    } catch (Exception e) {
                        BuildEngine.LogErrorEvent(new BuildErrorEventArgs("""", """", ScriptFile, 0, 0, 0, 0, e.ToString(), """", """", DateTime.Now));
                        throw;
                    }
                }
            } 
        }
        return true;
    ]]></Code>
        </Task>"
                      + projXml.Substring(injectLocation);
            File.WriteAllText(projectRootPath, projXml);
            VsEnvironment.Dte.ReloadJustUnloadedProject();
            return VsEnvironment.Dte.GetProjectByFullName(projectRootPath);
        }
    }
}
