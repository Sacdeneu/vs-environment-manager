using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Threading.Tasks;

namespace VsEnvironmentManager
{
    public class DebugEventHandler : IVsUpdateSolutionEvents2
    {
        private readonly EnvironmentVariableStorage _storage;
        private IVsSolutionBuildManager2 _buildManager;
        private uint _updateSolutionEventsCookie;
        private static bool _isAutoRebuild = false;

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

                // Empêche le rebuild infini
                if (_isAutoRebuild)
                    return;

                string projectName = GetActiveProjectName();
                string environmentName = GetActiveEnvironmentName();

                if (!string.IsNullOrEmpty(projectName) && !string.IsNullOrEmpty(environmentName))
                {
                    var variables = await _storage.GetVariablesAsync(projectName, environmentName);

                    foreach (var variable in variables)
                    {
                        ApplyEnvironmentVariable(projectName, variable.Name, variable.Value);
                    }

                    // Déclenche le rebuild après application des variables
                    _isAutoRebuild = true;
                    try
                    {
                        await Task.Delay(1000); // Petit délai pour éviter les conflits
                        var dte = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                        dte?.ExecuteCommand("Build.Clean");
                        dte?.ExecuteCommand("Build.RebuildSolution");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erreur rebuild : {ex.Message}");
                    }
                    finally
                    {
                        _isAutoRebuild = false;
                    }
                }
            }).FileAndForget("vs-env-manager-rebuild");

            return VSConstants.S_OK;
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
                var dte = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
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
                var dte = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                return dte.Solution.Properties.Item("ActiveLaunchProfile").Value.ToString();
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

            var dte = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            foreach (EnvDTE.Project project in dte.Solution.Projects)
            {
                if (project.Name == projectName)
                {
                    // Pour les projets SDK .NET Core
                    if (project.Kind == "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}")
                    {
                        var configuration = project.ConfigurationManager.ActiveConfiguration;
                        configuration.Properties.Item("MSBuildProperties").Value = $"{name}={value};";
                    }
                    // Pour les anciens projets
                    else
                    {
                        var prop = project.Properties.Item("EnvironmentVariables");
                        prop.Value = $"{name}={value};{prop.Value}";
                    }
                    break;
                }
            }
        }

        #endregion
    }
}
