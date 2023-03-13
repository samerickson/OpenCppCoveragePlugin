﻿// OpenCppCoverage is an open source code coverage for C++.
// Copyright (C) 2014 OpenCppCoverage
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
using System.Linq;
using System.Text;

namespace OpenCppCoverage.VSPackage
{
    class ConfigurationManager : IConfigurationManager
    {
        //---------------------------------------------------------------------
        public static readonly string ProjectNotMarkedAsBuildError
            = "The project {0} is marked as not build for the active solution configuration. "
                + "Please check your solution Configuration Manager.";

        //---------------------------------------------------------------------
        public DynamicVcConfiguration GetConfiguration(
            SolutionConfiguration2 activeConfiguration,
            ExtendedProject project)
        {
            string error;
            var configuration = ComputeConfiguration(activeConfiguration, project, out error);

            if (configuration == null)
                throw new VSPackageException(error);

            return configuration;
        }

        //---------------------------------------------------------------------
        public DynamicVcConfiguration FindConfiguration(
            SolutionConfiguration2 activeConfiguration,
            ExtendedProject project)
        {
            string error;
            var configuration = ComputeConfiguration(activeConfiguration, project, out error);
            return configuration;
        }

        //---------------------------------------------------------------------
        public string GetSolutionConfigurationName(SolutionConfiguration2 activeConfiguration)
        {
            return activeConfiguration.Name + '|' + activeConfiguration.PlatformName;

        }

        //---------------------------------------------------------------------
        DynamicVcConfiguration ComputeConfiguration(
            SolutionConfiguration2 activeConfiguration,
            ExtendedProject project,
            out string error)
        {
            error = null;
            var context = ComputeContext(activeConfiguration, project, ref error);

            if (context == null)
                return null;

            if (!context.ShouldBuild)
            {
                error = string.Format(ProjectNotMarkedAsBuildError, project.UniqueName);
                return null;
            }

            return ComputeConfiguration(project, context, ref error);
        }

        //---------------------------------------------------------------------
        static DynamicVcConfiguration ComputeConfiguration(
            ExtendedProject project,
            SolutionContext context,
            ref string error)
        {
            var configurations = project.Configurations;
            var configuration = configurations.FirstOrDefault(
                c => c.ConfigurationName == context.ConfigurationName && c.PlatformName == context.PlatformName);

            if (configuration == null)
            {
                var builder = new StringBuilder();

                builder.AppendLine(string.Format("Cannot find a configuration for the project {0}", project.UniqueName));
                builder.AppendLine(string.Format(" - Solution: configuration: {0} platform: {1}", context.ConfigurationName, context.PlatformName));
                foreach (var config in configurations)
                    builder.AppendLine(string.Format(" - Project: configuration: {0} platform: {1}", config.ConfigurationName, config.PlatformName));
                error = builder.ToString();
                return null;
            }

            return configuration;
        }

        //---------------------------------------------------------------------
        SolutionContext ComputeContext(
            SolutionConfiguration2 activeConfiguration,
            ExtendedProject project,
            ref string error)
        {
            var contexts = activeConfiguration.SolutionContexts.Cast<SolutionContext>();
            var context = contexts.FirstOrDefault(c => c.ProjectName == project.UniqueName);

            if (context == null)
            {
                error = string.Format("Cannot find {0} in project contexts. "
                        + "Please check your solution Configuration Manager.",
                        project.UniqueName);
                return null;
            }

            return context;
        }
    }
}
