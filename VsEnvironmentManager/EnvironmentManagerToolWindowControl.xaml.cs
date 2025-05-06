using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace VsEnvironmentManager
{

    public partial class EnvironmentManagerToolWindowControl : UserControl
    {
        private ObservableCollection<EnvironmentVariable> variables = new();
        private bool isProjectView = false;

        public EnvironmentManagerToolWindowControl()
        {
            InitializeComponent();
            VariablesListView.ItemsSource = variables;
            this.Loaded += EnvironmentManagerToolWindowControl_Loaded;
        }
        private void EnvironmentManagerToolWindowControl_Loaded(object sender, RoutedEventArgs e)
        {
            ShowSolutionView();
        }

        private async void AddVariableToProjectPopup_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ProjectEnvViewModel proj)
            {
                var dlg = new AddVariableDialog { Owner = Window.GetWindow(this) };
                if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.VariableName))
                {
                    proj.Variables.Add(new EnvironmentVariable { Name = dlg.VariableName, Value = dlg.VariableValue });
                    await SaveProjectVariablesAsync(proj);
                }
            }
        }


        private void SwitchViewButton_Click(object sender, RoutedEventArgs e)
        {
            if (isProjectView)
                ShowSolutionView();
            else
                ShowProjectView();
        }

        private void ShowSolutionView()
        {
            isProjectView = false;
            SolutionScrollViewer.Visibility = Visibility.Visible;
            ProjectViewPanel.Visibility = Visibility.Collapsed;
            SwitchViewButton.Content = "Vue projet";
            _ = LoadSolutionVariablesAsync();
        }

        private void ShowProjectView()
        {
            isProjectView = true;
            SolutionScrollViewer.Visibility = Visibility.Collapsed;
            ProjectViewPanel.Visibility = Visibility.Visible;
            SwitchViewButton.Content = "Vue solution";
            _ = LoadProjectVariablesAsync();
        }

        private async Task LoadSolutionVariablesAsync()
        {
            var storage = EnvironmentVariableStorage.Instance;
            string envName = GetCurrentEnvironmentName();
            SolutionEnvironmentNameText.Text = envName ?? "(aucun)";
            var projects = GetAllProjects();

            var solutionVM = new SolutionEnvViewModel();
            foreach (var p in projects)
            {
                var vars = await storage.GetVariablesAsync(p.Name, envName);
                solutionVM.Projects.Add(new ProjectEnvViewModel
                {
                    ProjectName = p.Name,
                    Variables = new ObservableCollection<EnvironmentVariable>(vars)
                });
            }
            SolutionTreeView.DataContext = solutionVM;
        }

        private async Task LoadProjectVariablesAsync()
        {
            string projectName = GetCurrentProjectName();
            string environmentName = GetCurrentEnvironmentName();

            ProjectNameText.Text = projectName ?? "(aucun)";
            EnvironmentNameText.Text = environmentName ?? "(aucun)";

            var storage = EnvironmentVariableStorage.Instance;
            var loadedVars = await storage.GetVariablesAsync(projectName, environmentName);

            variables.Clear();
            foreach (var v in loadedVars)
                variables.Add(v);
        }


        private async void AddVariable_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(NewVarName.Text))
            {
                variables.Add(new EnvironmentVariable { Name = NewVarName.Text, Value = NewVarValue.Text });
                NewVarName.Clear();
                NewVarValue.Clear();
                await SaveCurrentVariablesAsync();
            }
        }

        private async void DeleteVariable_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is EnvironmentVariable variable)
            {
                variables.Remove(variable);
                await SaveCurrentVariablesAsync();
            }
        }

        private async Task SaveCurrentVariablesAsync()
        {
            string projectName = GetCurrentProjectName();
            string environmentName = GetCurrentEnvironmentName();

            var storage = EnvironmentVariableStorage.Instance;
            await storage.SaveVariablesAsync(projectName, environmentName, variables.ToList());
        }


        private async void AddVariableToProject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ProjectEnvViewModel proj)
            {
                if (!string.IsNullOrWhiteSpace(proj.NewVarName))
                {
                    proj.Variables.Add(new EnvironmentVariable { Name = proj.NewVarName, Value = proj.NewVarValue });
                    proj.NewVarName = "";
                    proj.NewVarValue = "";
                    proj.OnPropertyChanged(nameof(proj.NewVarName));
                    proj.OnPropertyChanged(nameof(proj.NewVarValue));
                    await SaveProjectVariablesAsync(proj);
                }
            }
        }

        private async void DeleteVariableFromProject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is EnvironmentVariable variable)
            {
                foreach (var proj in (SolutionTreeView.DataContext as SolutionEnvViewModel)?.Projects ?? Enumerable.Empty<ProjectEnvViewModel>())
                {
                    if (proj.Variables.Contains(variable))
                    {
                        proj.Variables.Remove(variable);
                        await SaveProjectVariablesAsync(proj);
                        break;
                    }
                }
            }
        }

        private async Task SaveProjectVariablesAsync(ProjectEnvViewModel proj)
        {
            var storage = EnvironmentVariableStorage.Instance;
            string envName = GetCurrentEnvironmentName();
            await storage.SaveVariablesAsync(proj.ProjectName, envName, proj.Variables.ToList());
        }


        private string GetCurrentProjectName()
        {
            try
            {
                var dte = (EnvDTE.DTE)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE));
                if (dte?.ActiveSolutionProjects is Array activeProjects && activeProjects.Length > 0)
                {
                    var project = activeProjects.GetValue(0) as EnvDTE.Project;
                    return project?.Name;
                }
            }
            catch { }
            return null;
        }

        private string GetCurrentEnvironmentName()
        {
            try
            {
                var dte = (EnvDTE.DTE)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE));
                return dte?.Solution?.SolutionBuild?.ActiveConfiguration?.Name;
            }
            catch { }
            return null;
        }

        private IEnumerable<EnvDTE.Project> GetAllProjects()
        {
            var dte = (EnvDTE.DTE)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE));
            var projects = new List<EnvDTE.Project>();
            if (dte?.Solution?.Projects == null)
                return projects;

            foreach (EnvDTE.Project project in dte.Solution.Projects)
            {
                AddProjectAndSubProjects(project, projects);
            }
            return projects;
        }

        private void AddProjectAndSubProjects(EnvDTE.Project project, List<EnvDTE.Project> list)
        {
            if (project == null)
                return;

            if (project.Kind == EnvDTE80.ProjectKinds.vsProjectKindSolutionFolder)
            {
                var projectItems = project.ProjectItems;
                if (projectItems != null)
                {
                    foreach (EnvDTE.ProjectItem item in projectItems)
                    {
                        var subProject = item.SubProject;
                        if (subProject != null)
                            AddProjectAndSubProjects(subProject, list);
                    }
                }
            }
            else
            {
                list.Add(project);
            }
        }
    }

}
