using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace VsEnvironmentManager
{
    public class EnvironmentVariableStorage
    {
        private static EnvironmentVariableStorage _instance;
        private readonly AsyncPackage _package;

        private EnvironmentVariableStorage(AsyncPackage package)
        {
            _package = package;
        }

        public static EnvironmentVariableStorage Initialize(AsyncPackage package)
        {
            _instance = new EnvironmentVariableStorage(package);
            return _instance;
        }

        public static EnvironmentVariableStorage Instance => _instance;

        public async Task SaveVariablesAsync(string projectName, string environmentName, List<EnvironmentVariable> variables)
        {
            var allVars = await GetAllVariablesAsync();

            if (!allVars.ContainsKey(projectName))
            {
                allVars[projectName] = new Dictionary<string, List<EnvironmentVariable>>();
            }

            allVars[projectName][environmentName] = variables;

            string json = JsonConvert.SerializeObject(allVars, Formatting.Indented);

            var folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VsEnvironmentManager");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, "variables.json");
            await Task.Run(() => File.WriteAllText(filePath, json));
        }

        public async Task<List<EnvironmentVariable>> GetVariablesAsync(string projectName, string environmentName)
        {
            var allVars = await GetAllVariablesAsync();

            if (allVars.ContainsKey(projectName) && allVars[projectName].ContainsKey(environmentName))
            {
                return allVars[projectName][environmentName];
            }

            return new List<EnvironmentVariable>();
        }

        private static async Task<Dictionary<string, Dictionary<string, List<EnvironmentVariable>>>> GetAllVariablesAsync()
        {
            try
            {
                var filePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "VsEnvironmentManager",
                    "variables.json");

                if (File.Exists(filePath))
                {
                    string json = await Task.Run(() => File.ReadAllText(filePath));
                    var result = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<EnvironmentVariable>>>>(json);
                    return result ?? new Dictionary<string, Dictionary<string, List<EnvironmentVariable>>>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Erreur de lecture des variables d'environnement : " + ex.Message);
            }

            return new Dictionary<string, Dictionary<string, List<EnvironmentVariable>>>();
        }
    }

    public class EnvironmentVariable : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }

    public class ProjectEnvViewModel : INotifyPropertyChanged
    {
        public string ProjectName { get; set; }
        public ObservableCollection<EnvironmentVariable> Variables { get; set; } = new();
        public string NewVarName { get; set; }
        public string NewVarValue { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }

    public class SolutionEnvViewModel
    {
        public ObservableCollection<ProjectEnvViewModel> Projects { get; set; } = new();
    }

}
