using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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

                // Supprime la variable d'environnement système
                RemoveSystemEnvironmentVariable(variable.Name);

                await SaveCurrentVariablesAsync();

                // Feedback à l'utilisateur
                MessageBox.Show(
                    "La variable d'environnement système a été supprimée.\n" +
                    "⚠️ Vous devez redémarrer Visual Studio pour que la suppression soit prise en compte.",
                    "Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        private async Task SaveCurrentVariablesAsync()
        {
            string projectName = GetCurrentProjectName();
            string environmentName = GetCurrentEnvironmentName();

            var storage = EnvironmentVariableStorage.Instance;
            await storage.SaveVariablesAsync(projectName, environmentName, variables.ToList());

            // Applique chaque variable dans l'environnement système
            foreach (var v in variables)
                SetSystemEnvironmentVariable(v.Name, v.Value);

            // Affiche un feedback
            MessageBox.Show(
                "Les variables d'environnement système ont été modifiées.\n" +
                "⚠️ Vous devez redémarrer Visual Studio pour que les changements soient pris en compte dans vos projets.",
                "Information",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
        private void SetSystemEnvironmentVariable(string name, string value)
        {
            try
            {
                // Ajoute ou modifie la variable pour l'utilisateur courant
                Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.User);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la modification de la variable d'environnement système : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
        private void RemoveSystemEnvironmentVariable(string name)
        {
            try
            {
                Environment.SetEnvironmentVariable(name, null, EnvironmentVariableTarget.User);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la suppression de la variable d'environnement système : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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

                        // Supprime la variable d'environnement système
                        RemoveSystemEnvironmentVariable(variable.Name);

                        await SaveProjectVariablesAsync(proj);

                        // Feedback à l'utilisateur
                        MessageBox.Show(
                            "La variable d'environnement système a été supprimée.\n" +
                            "⚠️ Vous devez redémarrer Visual Studio pour que la suppression soit prise en compte.",
                            "Information",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
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

            // Ajoute ceci pour mettre à jour les variables d'environnement système
            foreach (var v in proj.Variables)
                SetSystemEnvironmentVariable(v.Name, v.Value);

            // Affiche le feedback à l'utilisateur
            MessageBox.Show(
                "Les variables d'environnement système ont été modifiées.\n" +
                "⚠️ Vous devez redémarrer Visual Studio pour que les changements soient pris en compte dans vos projets.",
                "Information",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
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

        private void SyncVariablesToLaunchSettings(string projectName, List<EnvironmentVariable> variables)
        {
            // Récupère le chemin du projet via DTE
            var dte = (EnvDTE.DTE)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE));
            EnvDTE.Project project = null;
            foreach (EnvDTE.Project p in dte.Solution.Projects)
            {
                if (p.Name == projectName)
                {
                    project = p;
                    break;
                }
            }
            if (project == null) return;

            string projectDir = Path.GetDirectoryName(project.FullName);
            string launchSettingsPath = Path.Combine(projectDir, "Properties", "launchSettings.json");
            if (!File.Exists(launchSettingsPath)) return;

            var json = JObject.Parse(File.ReadAllText(launchSettingsPath));
            var profiles = json["profiles"] as JObject;
            if (profiles == null) return;

            foreach (var profile in profiles.Properties())
            {
                var envVars = profile.Value["environmentVariables"] as JObject;
                if (envVars == null)
                {
                    envVars = new JObject();
                    profile.Value["environmentVariables"] = envVars;
                }
                // Ajoute ou met à jour chaque variable
                foreach (var v in variables)
                    envVars[v.Name] = v.Value;
            }

            File.WriteAllText(launchSettingsPath, json.ToString());
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
