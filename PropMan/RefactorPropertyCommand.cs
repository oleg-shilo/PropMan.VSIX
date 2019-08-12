using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;
using Microsoft.AutoPropertyConverterVSX;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Task = System.Threading.Tasks.Task;

namespace OlegShilo.PropMan
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class RefactorPropertyCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 256;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("2566205f-f6ef-4a3e-8bd0-d0858a8737b8");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="RefactorPropertyCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private RefactorPropertyCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static RefactorPropertyCommand Instance
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
            // Switch to the main thread - the call to AddCommand in RefactorPropertyCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance = new RefactorPropertyCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // MenuItemCallback();

            // string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            // string title = "RefactorPropertyCommand";

            // // Show a message box to prove we were here
            // VsShellUtilities.ShowMessageBox(
            //     this.package,
            //     message,
            //     title,
            //     OLEMSGICON.OLEMSGICON_INFO,
            //     OLEMSGBUTTON.OLEMSGBUTTON_OK,
            //     OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

            try
            {
                var txtxMgr = (IVsTextManager)GetService<SVsTextManager>();

                IVsUIShell uiShell = (IVsUIShell)GetService<SVsUIShell>();
                IntPtr mainWnd;
                uiShell.GetDialogOwnerHwnd(out mainWnd);

                var dialog = new Window1();
                var helper = new WindowInteropHelper(dialog) { Owner = mainWnd };
                dialog.ShowDialog();

                if (dialog.SelectedOperation == Window1.Operation.AutoFull)
                {
                    var plugin = new AutoPropertyConverter(txtxMgr);
                    plugin.Execute();
                }
                else if (dialog.SelectedOperation == Window1.Operation.Encapsulate)
                {
                    var plugin = new FieldConverter(txtxMgr);
                    plugin.Execute();
                }
            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    ex.Message,
                    "PropMan",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        object GetService<T>()
        {
            return this.ServiceProvider.GetServiceAsync(typeof(T))?.Result;
        }
    }
}