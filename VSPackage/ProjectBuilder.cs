﻿// OpenCppCoverage is an open source code coverage for C++.
// Copyright (C) 2016 OpenCppCoverage
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using EnvDTE;
using EnvDTE80;
using System;
using System.Runtime.InteropServices;

namespace OpenCppCoverage.VSPackage
{
    internal class ProjectBuilder
    {
        readonly DTE2 dte;
        readonly ErrorHandler errorHandler;
        readonly OutputWindowWriter outputWindowWriter;
        readonly BuildEvents buildEvents;

        //---------------------------------------------------------------------
        public ProjectBuilder(
            DTE2 dte,
            ErrorHandler errorHandler,
            OutputWindowWriter outputWindowWriter)
        {
            this.dte = dte;
            this.errorHandler = errorHandler;
            this.outputWindowWriter = outputWindowWriter;
            this.buildEvents = this.dte.Events.BuildEvents;
        }

        //---------------------------------------------------------------------
        public void Build(
            string solutionConfigurationName,
            string projectName,
            Action<bool> userCallBack)
        {
            var buildHandler = CreateBuildHandler(solutionConfigurationName, projectName, userCallBack);

            // buildEvents need to be a member to avoid a garbage collector issue.
            this.buildEvents.OnBuildProjConfigDone += buildHandler;

            this.outputWindowWriter.WriteLine(
                "Start building " + projectName
                + " " + solutionConfigurationName);

            var output = dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
            output?.Activate();

            var solutionBuild = this.dte.Solution.SolutionBuild;

            try
            {
                solutionBuild.BuildProject(solutionConfigurationName, projectName, false);
            }
            catch (COMException e)
            {
                throw new VSPackageException($"Error when building {projectName} with configuration {solutionConfigurationName}: {e.Message}");
            }
        }

        //---------------------------------------------------------------------
        private _dispBuildEvents_OnBuildProjConfigDoneEventHandler CreateBuildHandler(
            string solutionConfigurationName,
            string projectName,
            Action<bool> userCallBack)
        {
            var buildContext = new BuildContext();
            void OnBuildDone(string project, string projectConfig, string platform, string solutionConfig, bool success) =>
                OnBuildProjConfigDone(project, projectConfig, platform, solutionConfig, success, buildContext);

            buildContext.OnBuildDone = OnBuildDone;
            buildContext.UserCallBack = userCallBack;
            buildContext.ProjectName = projectName;
            buildContext.SolutionConfigurationName = solutionConfigurationName;

            return OnBuildDone;
        }

        //---------------------------------------------------------------------
        private void OnBuildProjConfigDone(string project, string projectConfig, string platform, string solutionConfig, bool success, BuildContext buildContext)
        {
            // This method is executed asynchronously and so we need to catch errors.
            this.errorHandler.Execute(() =>
            {
                if (project != buildContext.ProjectName
                    || solutionConfig != buildContext.SolutionConfigurationName) return;

                this.buildEvents.OnBuildProjConfigDone -= buildContext.OnBuildDone;
                buildContext.UserCallBack(success);
            });
        }

        //---------------------------------------------------------------------
        private class BuildContext
        {
            public _dispBuildEvents_OnBuildProjConfigDoneEventHandler OnBuildDone { get; set; }
            public Action<bool> UserCallBack { get; set; }
            public string SolutionConfigurationName { get; set; }
            public string ProjectName { get; set; }
        }
    }
}
