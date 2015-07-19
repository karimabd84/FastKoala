﻿using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Wijits.FastKoala.Logging;
using Wijits.FastKoala.SourceControl;
using Wijits.FastKoala.Transformations;
using Wijits.FastKoala.Utilities;

namespace Wijits.FastKoala
{
    /// <summary>
    ///     This is the class that implements the package exposed by this assembly.
    ///     The minimum requirement for a class to be considered a valid package for Visual Studio
    ///     is to implement the IVsPackage interface and register itself with the shell.
    ///     This package uses the helper classes defined inside the Managed Package Framework (MPF)
    ///     to do it: it derives from the Package class that provides the implementation of the
    ///     IVsPackage interface and uses the registration attributes defined in the framework to
    ///     register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [Guid(GuidList.guidFastKoalaPkgString)]
    [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Local")]
    public sealed class FastKoalaPackage : Package
    {

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation

        #region Package Members

        /// <summary>
        ///     Initialization of the package; this method is called right after the package is sited, so this is the place
        ///     where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", ToString()));
            VsEnvironment.Initialize(this);
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = GetService(typeof (IMenuCommandService)) as OleMenuCommandService;
            if (null == mcs) return;

            var enableBuildTimeTransformationsCmd = new CommandID(GuidList.guidFastKoalaProjItemMenuCmdSet,
                (int) PkgCmdIDList.cmdidEnableBuildTimeTransformationsProjItem);
            var enableBuildTimeTransformationsMenuItem =
                new OleMenuCommand(EnableBuildTimeTransformationsMenuItem_Invoke, enableBuildTimeTransformationsCmd);
            enableBuildTimeTransformationsMenuItem.BeforeQueryStatus +=
                EnableBuildTimeTransformationsMenuItem_BeforeQueryStatus;
            mcs.AddCommand(enableBuildTimeTransformationsMenuItem);

            var enableBuildTimeTransformationsProjCmd = new CommandID(GuidList.guidFastKoalaProjMenuCmdSet,
                (int) PkgCmdIDList.cmdidEnableBuildTimeTransformationsProj);
            var enableBuildTimeTransformationsProjectMenuItem =
                new OleMenuCommand(EnableBuildTimeTransformationsMenuItem_Invoke,
                    enableBuildTimeTransformationsProjCmd);
            enableBuildTimeTransformationsProjectMenuItem.BeforeQueryStatus +=
                EnableBuildTimeTransformationsMenuItemProject_BeforeQueryStatus;
            mcs.AddCommand(enableBuildTimeTransformationsProjectMenuItem);

            var addMissingTransformationsCmd = new CommandID(GuidList.guidFastKoalaProjItemMenuCmdSet,
                (int)PkgCmdIDList.cmdidAddMissingTransformsProjItem);
            var addMissingTransformationsMenuItem =
                new OleMenuCommand(AddMissingTransformationsMenuItem_Invoke, addMissingTransformationsCmd);
            addMissingTransformationsMenuItem.BeforeQueryStatus +=
                AddMissingTransformationsMenuItem_BeforeQueryStatus;
            mcs.AddCommand(addMissingTransformationsMenuItem);
        }

        private async Task<BuildTimeTransformationsEnabler> GetTransformationsEnabler(EnvDTE.Project project)
        {
            var logger = Dte.GetLogger();
            var io = await VsFileSystemManipulatorFactory.GetFileSystemManipulatorForEnvironment(project);
            var nativeWindow = NativeWindow.FromHandle(new IntPtr(Dte.MainWindow.HWnd));
            Debug.Assert(project != null, "project != null");
            return new BuildTimeTransformationsEnabler(project, logger, io, nativeWindow);
        }

        private DTE Dte
        {
            get { return (DTE) GetService(typeof (DTE)); }
        }

        private EnvDTE.Project GetSelectedProject()
        {
            var monitorSelection = GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            IVsMultiItemSelect multiItemSelect = null;
            var hierarchyPtr = IntPtr.Zero;
            var selectionContainerPtr = IntPtr.Zero;
            uint itemid;
            var hr = monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemid, out multiItemSelect,
                out selectionContainerPtr);
            if (ErrorHandler.Failed(hr) || hierarchyPtr == IntPtr.Zero || itemid == VSConstants.VSITEMID_NIL)
            {
                // there is no selection
                return null;
            }
            var hierarchy = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;
            if (hierarchy == null) return null;
            object objProj;
            hierarchy.GetProperty(itemid, (int)__VSHPROPID.VSHPROPID_ExtObject, out objProj);
            var projectItem = objProj as EnvDTE.ProjectItem;
            var project = objProj as EnvDTE.Project;
            return project ?? projectItem.ContainingProject;
        }

        #endregion

#region EnableBuildTimeTransformations

        /// <summary>
        /// User should've right-clicked on a .config file; determine whether to show 
        /// "Enable build-time transformations" menu item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void EnableBuildTimeTransformationsMenuItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            // get the menu that fired the event
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null) return;

            // start by assuming that the menu will not be shown
            menuCommand.Visible = false;
            menuCommand.Enabled = false;

            IVsHierarchy hierarchy = null;
            var itemid = VSConstants.VSITEMID_NIL;

            if (!IsSingleProjectItemSelection(out hierarchy, out itemid)) return;
            // Get the file path
            string itemFullPath = null;
            ((IVsProject)hierarchy).GetMkDocument(itemid, out itemFullPath);
            var transformFileInfo = new FileInfo(itemFullPath);

            // then check if the file is named 'web.config'
            var isConfig = Regex.IsMatch(transformFileInfo.Name, @"[Web|App](\.\w+)?\.config",
                RegexOptions.IgnoreCase);

            // if not leave the menu hidden
            if (!isConfig) return;
            var project = GetSelectedProject();

            var transformationsEnabler = await GetTransformationsEnabler(project);
            if (!transformationsEnabler.CanEnableBuildTimeTransformations)
                return;

            menuCommand.Visible = true;
            menuCommand.Enabled = true;
        }

        /// <summary>
        /// User right-clicked on a project; determine whether to show "Enable build-time transformations" menu item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void EnableBuildTimeTransformationsMenuItemProject_BeforeQueryStatus(object sender, EventArgs e)
        {
            // get the menu that fired the event
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null) return;

            menuCommand.Visible = false;
            menuCommand.Enabled = false;

            var project = GetSelectedProject();

            var transformationsEnabler = await GetTransformationsEnabler(project);
            if (!transformationsEnabler.CanEnableBuildTimeTransformations)
                return;

            menuCommand.Visible = true;
            menuCommand.Enabled = true;
        }


        /// <summary>
        ///     This function is the callback used to execute a command when the a menu item is clicked.
        ///     See the Initialize method to see how the menu item is associated to this function using
        ///     the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private async void EnableBuildTimeTransformationsMenuItem_Invoke(object sender, EventArgs e)
        {
            var project = GetSelectedProject();
            var transformationsEnabler = await GetTransformationsEnabler(project);
            await transformationsEnabler.EnableBuildTimeConfigTransformations();
        }

#endregion

#region AddMissingTransformations

        private async void AddMissingTransformationsMenuItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            // get the menu that fired the event
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null) return;

            // start by assuming that the menu will not be shown
            menuCommand.Visible = false;
            menuCommand.Enabled = false;

            IVsHierarchy hierarchy = null;
            var itemid = VSConstants.VSITEMID_NIL;

            if (!IsSingleProjectItemSelection(out hierarchy, out itemid)) return;
            // Get the file path
            string itemFullPath = null;
            ((IVsProject)hierarchy).GetMkDocument(itemid, out itemFullPath);
            var transformFileInfo = new FileInfo(itemFullPath);

            // then check if the file is named 'web.config'
            var isConfig = Regex.IsMatch(transformFileInfo.Name, @"[Web|App](\.\w+)?\.config",
                RegexOptions.IgnoreCase);

            // if not leave the menu hidden
            if (!isConfig) return;

            var project = GetSelectedProject();
            var transformationsEnabler = await GetTransformationsEnabler(project);
            if (transformationsEnabler.HasMissingTransforms)
            {
                menuCommand.Visible = true;
                menuCommand.Enabled = true;
            }
        }

        private async void AddMissingTransformationsMenuItem_Invoke(object sender, EventArgs e)
        {
            var project = GetSelectedProject();
            var transformationsEnabler = await GetTransformationsEnabler(project);
            await transformationsEnabler.AddMissingTransforms();
        }

#endregion

        // source: http://www.diaryofaninja.com/blog/2014/02/18/who-said-building-visual-studio-extensions-was-hard
        private static bool IsSingleProjectItemSelection(out IVsHierarchy hierarchy, out uint itemid)
        {
            hierarchy = null;
            itemid = VSConstants.VSITEMID_NIL;
            var hr = VSConstants.S_OK;

            var monitorSelection = GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            var solution = GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            if (monitorSelection == null || solution == null)
            {
                return false;
            }

            IVsMultiItemSelect multiItemSelect = null;
            var hierarchyPtr = IntPtr.Zero;
            var selectionContainerPtr = IntPtr.Zero;

            try
            {
                hr = monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemid, out multiItemSelect,
                    out selectionContainerPtr);

                if (ErrorHandler.Failed(hr) || hierarchyPtr == IntPtr.Zero || itemid == VSConstants.VSITEMID_NIL)
                {
                    // there is no selection
                    return false;
                }

                // multiple items are selected
                if (multiItemSelect != null) return false;

                // there is a hierarchy root node selected, thus it is not a single item inside a project

                if (itemid == VSConstants.VSITEMID_ROOT) return false;

                hierarchy = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;
                if (hierarchy == null) return false;

                var guidProjectID = Guid.Empty;

                if (ErrorHandler.Failed(solution.GetGuidOfProject(hierarchy, out guidProjectID)))
                {
                    return false; // hierarchy is not a project inside the Solution if it does not have a ProjectID Guid
                }

                // if we got this far then there is a single project item selected
                return true;
            }
            finally
            {
                if (selectionContainerPtr != IntPtr.Zero)
                {
                    Marshal.Release(selectionContainerPtr);
                }

                if (hierarchyPtr != IntPtr.Zero)
                {
                    Marshal.Release(hierarchyPtr);
                }
            }
        }
    }
}