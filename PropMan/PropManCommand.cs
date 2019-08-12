using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace OlegShilo.PropMan
{
    static class GuidList
    {
        public const string guidPropManPkgString = "7c64986f-f4d6-4983-a49a-518d5512b212";
        public const string guidPropManCmdSetString = "db2d94ca-ca87-4240-99ea-931d7f8e7701";
        public const string guidPropManCmdConfigSetString = "db2d94ca-ca87-4240-99ea-931d7f8e7733";
        public const string guidToolWindowPersistanceString = "fd0da38f-47b9-4072-b27d-01629b580799";

        public static readonly Guid guidPropManCmdSet = new Guid(guidPropManCmdSetString);
        public static readonly Guid guidPropManConfigCmdSet = new Guid(guidPropManCmdConfigSetString);
    };

    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class PropManCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("027fe469-162c-418e-8315-99f12cf9110b");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropManCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private PropManCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);

            // // Create the command for the menu item.
            // CommandID menuCommandID = new CommandID(GuidList.guidPropManCmdSet, (int)PkgCmdIDList.cmdidOpenCommand);
            // MenuCommand menuItem = new MenuCommand(MenuItemCallback, menuCommandID);
            // mcs.AddCommand(menuItem);

            // Create the command for the menu item.
            // menuCommandID = new CommandID(GuidList.guidPropManConfigCmdSet, (int)PkgCmdIDList.cmdidOpenConfigCommand);
            // menuItem = new MenuCommand(ShowToolWindow, menuCommandID);
            // mcs.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static PropManCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
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
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in ToolWindow1Command's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance = new PropManCommand(package, commandService);
        }

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.package.FindToolWindow(typeof(MyToolWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }
}