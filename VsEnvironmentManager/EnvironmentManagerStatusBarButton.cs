using Microsoft.VisualStudio.Shell;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace VsEnvironmentManager
{
    internal class EnvironmentManagerStatusBarButton
    {
        private static VsEnvironmentManagerPackage _package;
        private static EnvStatusBarControl _statusControl;

        public static async Task InitializeAsync(VsEnvironmentManagerPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            _package = package;

            _statusControl = new EnvStatusBarControl();
            _statusControl.MouseLeftButtonUp += OnStatusBarClick;

            await StatusBarInjector.InjectControlAsync(_statusControl);

            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            timer.Tick += async (s, e) => await UpdateStatusAsync();
            timer.Start();

            await UpdateStatusAsync();
        }
        private static void OnStatusBarClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                ToolWindowPane window = _package.FindToolWindow(typeof(EnvironmentManagerToolWindow), 0, true);
                if (window?.Frame != null)
                {
                    var windowFrame = window.Frame as Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame;
                    Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
                }
            });
        }
        private static async Task UpdateStatusAsync()
        {
            var dte = (EnvDTE.DTE)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE));
            string projectName = null, envName = null;
            try
            {
                if (dte?.ActiveSolutionProjects is Array activeProjects && activeProjects.Length > 0)
                {
                    var project = activeProjects.GetValue(0) as EnvDTE.Project;
                    projectName = project?.Name;
                }
                envName = dte?.Solution?.SolutionBuild?.ActiveConfiguration?.Name;
            }
            catch { }

            int varCount = 0;
            if (!string.IsNullOrEmpty(projectName) && !string.IsNullOrEmpty(envName))
            {
                var storage = EnvironmentVariableStorage.Instance;
                if (storage != null)
                {
                    var vars = await storage.GetVariablesAsync(projectName, envName);
                    varCount = vars?.Count ?? 0;
                }
            }

            _statusControl.SetStatus(varCount);
        }

        private static void OnButtonClick(object sender, RoutedEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                ToolWindowPane window = _package.FindToolWindow(typeof(EnvironmentManagerToolWindow), 0, true);
                if (window?.Frame != null)
                {
                    Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame windowFrame = window.Frame as Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame;
                    Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
                }
            });
        }
    }
}
