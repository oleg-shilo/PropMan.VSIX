using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace PropMan
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("fc26c4c4-b324-4a0f-939e-3b2277c8c565")]
    public class ConfigToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigToolWindow"/> class.
        /// </summary>
        public ConfigToolWindow() : base(null)
        {
            this.Caption = "ConfigToolWindow";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new ConfigToolWindowControl();
        }
    }
}
