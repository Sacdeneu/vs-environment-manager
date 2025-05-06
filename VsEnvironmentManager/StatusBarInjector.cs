using Microsoft.VisualStudio.Shell;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VsEnvironmentManager
{

    internal static class StatusBarInjector
    {
        private static Panel _panel;

        private static DependencyObject FindChild(DependencyObject parent, string childName)
        {
            if (parent == null) return null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is FrameworkElement frameworkElement && frameworkElement.Name == childName)
                    return frameworkElement;

                child = FindChild(child, childName);
                if (child != null) return child;
            }
            return null;
        }

        public static async Task EnsureUIAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (_panel == null)
            {
                for (int retries = 0; retries < 10; retries++)
                {
                    if (Application.Current?.MainWindow != null)
                    {
                        _panel = FindChild(Application.Current.MainWindow, "StatusBarPanel") as Panel;
                        if (_panel != null) break;
                    }
                    await Task.Delay(500);
                }
            }
        }

        public static async Task<bool> InjectControlAsync(FrameworkElement element)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            await EnsureUIAsync();

            if (_panel != null)
            {
                try
                {
                    _panel.Children.Remove(element);

                    int insertIndex = _panel.Children.Count;
                    for (int i = _panel.Children.Count - 1; i >= 0; i--)
                    {
                        var child = _panel.Children[i];
                        if (child is FrameworkElement fe)
                        {
                            var dock = fe.GetValue(DockPanel.DockProperty);
                            if (dock != null && dock.Equals(Dock.Right))
                            {
                                insertIndex = i + 1;
                                break;
                            }
                        }
                    }

                    element.SetValue(DockPanel.DockProperty, Dock.Right);

                    _panel.Children.Insert(insertIndex, element);

                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur lors de l'injection dans la barre d'état: {ex.Message}");
                }
            }
            return false;
        }
    }
}
