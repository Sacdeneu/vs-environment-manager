using System;
using System.IO;
using System.Text.Json;
using vs_environment_manager.Environments.Utils;
using vs_environment_manager.Models;

namespace vs_environment_manager.Services
{

    public class StorageService
    {
        private readonly string storagePath;

        public StorageService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            storagePath = Path.Combine(appData, "vs-environment-manager");
            if (!Directory.Exists(storagePath))
                Directory.CreateDirectory(storagePath);
        }

        public void SaveProjectData(ProjectEnvironmentData data)
        {
            string filePath = Path.Combine(storagePath, $"{data.ProjectName}.json");
            string json = JsonSerializer.Serialize(data);
            string encrypted = CryptoUtils.Encrypt(json);
            File.WriteAllText(filePath, encrypted);
        }

        public ProjectEnvironmentData LoadProjectData(string projectName)
        {
            string filePath = Path.Combine(storagePath, $"{projectName}.json");
            if (!File.Exists(filePath))
                return new ProjectEnvironmentData(projectName);

            string encrypted = File.ReadAllText(filePath);
            string json = CryptoUtils.Decrypt(encrypted);
            return JsonSerializer.Deserialize<ProjectEnvironmentData>(json);
        }

        public void DeleteProjectData(string projectName)
        {
            string filePath = Path.Combine(storagePath, $"{projectName}.json");
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}
