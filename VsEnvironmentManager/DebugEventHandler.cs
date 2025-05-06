using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VsEnvironmentManager
{
    public class DebugEventHandler : IVsUpdateSolutionEvents2
    {
        private readonly EnvironmentVariableStorage _storage;
        private IVsSolutionBuildManager2 _buildManager;
        private uint _updateSolutionEventsCookie;

        public DebugEventHandler(EnvironmentVariableStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public async Task InitializeAsync(IVsSolutionBuildManager2 buildManager)
        {
            _buildManager = buildManager ?? throw new ArgumentNullException(nameof(buildManager));

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            ErrorHandler.ThrowOnFailure(_buildManager.AdviseUpdateSolutionEvents(this, out _updateSolutionEventsCookie));
        }

        public void Dispose()
        {
            if (_buildManager != null && _updateSolutionEventsCookie != 0)
            {
                ThreadHelper.JoinableTaskFactory.Run(async delegate
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    _buildManager.UnadviseUpdateSolutionEvents(_updateSolutionEventsCookie);
                    _updateSolutionEventsCookie = 0;
                });
            }
        }

        #region IVsUpdateSolutionEvents Implementation

        public int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            return 0;
        }

        public int UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                string projectName = GetActiveProjectName();
                string environmentName = GetActiveEnvironmentName();

                if (!string.IsNullOrEmpty(projectName) && !string.IsNullOrEmpty(environmentName))
                {
                    var variables = await _storage.GetVariablesAsync(projectName, environmentName);

                    foreach (var variable in variables)
                    {
                        ApplyEnvironmentVariable(projectName, variable.Name, variable.Value);
                    }
                }
            });

            return 0;
        }

        public int UpdateSolution_Cancel()
        {
            return 0;
        }

        public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            return 0;
        }

        public int UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return 0;
        }

        #endregion

        #region IVsUpdateSolutionEvents2 Implementation

        public int UpdateProjectCfg_Begin(IVsHierarchy pHierarchy, IVsCfg pCfg, IVsCfg pCfgTargetOfAction, uint dwAction, ref int pfCancel)
        {
            return 0;
        }

        public int UpdateProjectCfg_Done(IVsHierarchy pHierarchy, IVsCfg pCfg, IVsCfg pCfgTargetOfAction, uint dwAction, int fSuccess, int fCancel)
        {
            return 0;
        }

        public int OnBeforeActiveSolutionCfgChange(IVsCfg pOldActiveSlnCfg, IVsCfg pNewActiveSlnCfg)
        {
            return 0;
        }

        public int OnAfterActiveSolutionCfgChange(IVsCfg pOldActiveSlnCfg, IVsCfg pNewActiveSlnCfg)
        {
            return 0;
        }

        #endregion

        #region Méthodes utilitaires

        private string GetActiveProjectName()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                if (dte?.ActiveSolutionProjects is Array activeSolutionProjects && activeSolutionProjects.Length > 0)
                {
                    var project = activeSolutionProjects.GetValue(0) as EnvDTE.Project;
                    return project?.Name;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la récupération du projet actif: {ex.Message}");
            }

            return null;
        }

        private string GetActiveEnvironmentName()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                return dte?.Solution.SolutionBuild.ActiveConfiguration.Name;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la récupération de l'environnement actif: {ex.Message}");
            }

            return null;
        }
        private void ApplyEnvironmentVariable(string projectName, string name, string value)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;

                foreach (EnvDTE.Project project in dte.Solution.Projects)
                {
                    if (project.Name == projectName)
                    {
                        var properties = project.Properties;
                        if (properties == null)
                            continue;

                        Property envVarsProp = null;
                        try
                        {
                            envVarsProp = properties.Item("EnvironmentVariables");
                        }
                        catch
                        {
                            System.Diagnostics.Debug.WriteLine("La propriété EnvironmentVariables n'existe pas sur ce projet.");
                        }

                        if (envVarsProp != null)
                        {
                            var envVars = envVarsProp.Value as string ?? "";

                            var envVarsList = new Dictionary<string, string>();
                            if (!string.IsNullOrEmpty(envVars))
                            {
                                var pairs = envVars.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var pair in pairs)
                                {
                                    var keyValue = pair.Split(new[] { '=' }, 2);
                                    if (keyValue.Length == 2)
                                    {
                                        envVarsList[keyValue[0]] = keyValue[1];
                                    }
                                }
                            }

                            envVarsList[name] = value;

                            var newEnvVars = string.Join("\n",
                                envVarsList.Select(kv => $"{kv.Key}={kv.Value}")
                            );

                            envVarsProp.Value = newEnvVars;
                            System.Diagnostics.Debug.WriteLine($"Variable d'environnement {name}={value} appliquée au projet {projectName}");
                        }
                        else
                        {
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'application de la variable d'environnement: {ex.Message}");
            }
        }

        #endregion
    }
}
