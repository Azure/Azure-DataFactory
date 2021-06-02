//------------------------------------------------------------------------------
// <copyright file="SecurePublishCommand.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.ADF.Deployment.AdfKeyVaultDeployment;
using Microsoft.ADF.Deployment.AdfKeyVaultDeployment.Models;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SecurePublishForm;

namespace SecurePublishMenuCommand
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class SecurePublishCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("5bdeebe7-037e-4381-bcc9-a1cd2cc98348");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        private string projName;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurePublishCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private SecurePublishCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);

                var menuItem = new OleMenuCommand(MenuItemCallback, menuCommandID);
                menuItem.BeforeQueryStatus += menuCommand_BeforeQueryStatus;

                commandService.AddCommand(menuItem);
            }
        }

        void menuCommand_BeforeQueryStatus(object sender, EventArgs e)
        {
            // get the menu that fired the event
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand != null)
            {
                // start by assuming that the menu will not be shown
                menuCommand.Visible = false;
                menuCommand.Enabled = false;

                IVsHierarchy hierarchy = null;
                uint itemid = VSConstants.VSITEMID_NIL;

                if (!IsSingleProjectItemSelection(out hierarchy, out itemid)) return;

                // Get the file path
                string itemFullPath = null;
                ((IVsProject)hierarchy).GetMkDocument(itemid, out itemFullPath);
                var projectFileInfo = new FileInfo(itemFullPath);

                // then check if the project extension is dfproj i.e. a data factory project
                bool isDfProj = projectFileInfo.Extension.Equals(".dfproj", StringComparison.InvariantCultureIgnoreCase);

                // if not leave the menu hidden
                if (!isDfProj) return;

                projName = projectFileInfo.FullName;
                menuCommand.Visible = true;
                menuCommand.Enabled = true;
            }
        }

        /// <summary>
        /// Checks to see if a single project item has been selected
        /// </summary>
        public static bool IsSingleProjectItemSelection(out IVsHierarchy hierarchy, out uint itemid)
        {
            hierarchy = null;
            itemid = VSConstants.VSITEMID_NIL;
            int hr = VSConstants.S_OK;

            var monitorSelection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            var solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            if (monitorSelection == null || solution == null)
            {
                return false;
            }

            IVsMultiItemSelect multiItemSelect = null;
            IntPtr hierarchyPtr = IntPtr.Zero;
            IntPtr selectionContainerPtr = IntPtr.Zero;

            try
            {
                hr = monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemid, out multiItemSelect, out selectionContainerPtr);

                if (ErrorHandler.Failed(hr) || hierarchyPtr == IntPtr.Zero || itemid == VSConstants.VSITEMID_NIL)
                {
                    // there is no selection
                    return false;
                }

                // multiple items are selected
                if (multiItemSelect != null) return false;

                // there is a hierarchy root node selected, thus it is not a single item inside a project
                if (itemid != VSConstants.VSITEMID_ROOT) return false;

                hierarchy = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;
                if (hierarchy == null) return false;

                Guid guidProjectID = Guid.Empty;

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


        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SecurePublishCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new SecurePublishCommand(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            MainWindow window = new MainWindow(projName);
            window.ShowDialog();
        }
    }
}
