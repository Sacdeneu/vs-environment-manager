using System.Collections.Generic;
using vs_environment_manager.Models;

namespace vs_environment_manager.Services
{

    public class EnvironmentService
    {
        private readonly StorageService storageService;

        public EnvironmentService()
        {
            storageService = new StorageService();
        }

        public ProjectEnvironmentData Load(string projectName)
        {
            return storageService.LoadProjectData(projectName);
        }

        public void Save(ProjectEnvironmentData data)
        {
            storageService.SaveProjectData(data);
        }

        public void AddOrUpdateVariable(ProjectEnvironmentData data, string environment, EnvironmentVariable variable)
        {
            if (!data.EnvironmentVariables.ContainsKey(environment))
                data.EnvironmentVariables[environment] = new List<EnvironmentVariable>();

            var vars = data.EnvironmentVariables[environment];
            var existing = vars.Find(v => v.Name == variable.Name);
            if (existing != null)
                existing.Value = variable.Value;
            else
                vars.Add(variable);
        }

        public void RemoveVariable(ProjectEnvironmentData data, string environment, string varName)
        {
            if (!data.EnvironmentVariables.ContainsKey(environment))
                return;

            var vars = data.EnvironmentVariables[environment];
            vars.RemoveAll(v => v.Name == varName);
        }

        public void RemoveVariableFromAllEnvironments(ProjectEnvironmentData data, string varName)
        {
            foreach (var env in data.EnvironmentVariables.Keys)
            {
                data.EnvironmentVariables[env].RemoveAll(v => v.Name == varName);
            }
        }
    }

}
