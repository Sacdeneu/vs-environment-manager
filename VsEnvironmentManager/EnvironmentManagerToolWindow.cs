using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace VsEnvironmentManager
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
    [Guid("916ce0aa-0ce2-4c01-8fbe-c5c9d59a7148")]
    public class EnvironmentManagerToolWindow : ToolWindowPane
    {
        public EnvironmentManagerToolWindow() : base(null)
        {
            this.Caption = "Environment Manager";
            this.Content = new EnvironmentManagerToolWindowControl();
        }
    }
}
