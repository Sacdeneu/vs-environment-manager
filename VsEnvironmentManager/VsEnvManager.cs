using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace VsEnvironmentManager
{
    public static class VsEnvManager
    {
        private static Dictionary<string, Dictionary<string, List<EnvVar>>> _cache;

        public static string Get(string variableName, string projectName = null, string environmentName = null)
        {
            // Charge le cache une seule fois
            if (_cache == null)
            {
                var file = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "VsEnvironmentManager",
                    "variables.json"
                );
                if (!File.Exists(file))
                    return null;
                var json = File.ReadAllText(file);
                _cache = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<EnvVar>>>>(json);
            }

            // Détection automatique du projet et de l'environnement
            projectName ??= GetCurrentProjectName();
            environmentName ??= GetCurrentConfiguration();

            if (_cache.TryGetValue(projectName, out var envs) &&
                envs.TryGetValue(environmentName, out var vars))
            {
                foreach (var v in vars)
                    if (v.Name == variableName)
                        return v.Value;
            }
            return null;
        }

        private static string GetCurrentProjectName()
        {
            // À adapter selon ta convention ou ton contexte
            return AppDomain.CurrentDomain.FriendlyName.Split('.')[0];
        }

        private static string GetCurrentConfiguration()
        {
            // Par défaut, Debug ou Release
#if DEBUG
            return "Debug";
#else
        return "Release";
#endif
        }

        public class EnvVar
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }
    }
}
