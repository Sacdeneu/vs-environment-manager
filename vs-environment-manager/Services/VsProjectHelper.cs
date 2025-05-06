using EnvDTE;
using EnvDTE80;
using System;

namespace vs_environment_manager.Services
{
    public class VsProjectHelper
    {
        private readonly DTE2 dte;

        public VsProjectHelper(DTE2 dte)
        {
            this.dte = dte;
        }

        public string GetActiveProjectName()
        {
            Array activeSolutionProjects = (Array)dte.ActiveSolutionProjects;
            if (activeSolutionProjects.Length > 0)
            {
                Project activeProject = (Project)activeSolutionProjects.GetValue(0);
                return activeProject.Name;
            }
            return null;
        }

        public string GetActiveConfigurationName()
        {
            return dte.Solution.SolutionBuild.ActiveConfiguration.Name;
        }
    }
}
