using System.Collections.Generic;

namespace vs_environment_manager.Models
{
    public class ProjectEnvironmentData
    {
        public string ProjectName { get; set; }
        // Dictionnaire : clé = environnement (Debug, Dev, etc), valeur = liste variables
        public Dictionary<string, List<EnvironmentVariable>> EnvironmentVariables { get; set; } = new();

        public ProjectEnvironmentData(string projectName)
        {
            ProjectName = projectName;
        }
    }
}
