using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using Task = System.Threading.Tasks.Task;

namespace vs_environment_manager
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class EnvironmentManagerToolWindowCommand
    {
        int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx");

        private readonly AsyncPackage package;

        private EnvironmentManagerToolWindowCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.ShowToolWindow, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static EnvironmentManagerToolWindowCommand Instance { get; private set; }

        private IAsyncServiceProvider ServiceProvider => package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new EnvironmentManagerToolWindowCommand(package, commandService);
        }

        private void ShowToolWindow(object sender, EventArgs e)
        {
            package.JoinableTaskFactory.Run(async delegate
            {
                ToolWindowPane window = await package.ShowToolWindowAsync(typeof(EnvironmentManagerToolWindow), 0, true, package.DisposalToken);
                if ((null == window) || (null == window.Frame))
                {
                    throw new NotSupportedException("Cannot create tool window");
                }
            });
        }
    }
}
