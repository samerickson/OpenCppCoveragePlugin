// OpenCppCoverage is an open source code coverage for C++.
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

using GalaSoft.MvvmLight.Command;
using OpenCppCoverage.VSPackage.Helper;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.IO;
using System.Linq;

namespace OpenCppCoverage.VSPackage.Settings.UI
{
    //-------------------------------------------------------------------------
    internal class SelectableProject
    {
        //---------------------------------------------------------------------
        public SelectableProject(StartUpProjectSettings.CppProject project)
        {
            this.Name = Path.GetFileNameWithoutExtension(project.Path);
            this.FullName = project.Path;
            this.Project = project;
            this.IsSelected = true;
        }

        //---------------------------------------------------------------------
        public SelectableProject()
        {
        }

        public string Name { get; }
        public string FullName { get; }
        public StartUpProjectSettings.CppProject Project { get; }
        public bool IsSelected { get; set; }
        //---------------------------------------------------------------------
        public override string ToString()
        {
            return this.FullName;
        }
    }

    //-------------------------------------------------------------------------
    internal class BasicSettingController : PropertyChangedNotifier
    {
        public class BasicSettingsData : PropertyChangedNotifier
        {
            //---------------------------------------------------------------------
            private string _programToRun;
            public string ProgramToRun
            {
                get => this._programToRun; set => this.SetField(ref this._programToRun, value);
            }

            //---------------------------------------------------------------------
            private string _optionalWorkingDirectory;

            public string OptionalWorkingDirectory
            {
                get => this._optionalWorkingDirectory; set => this.SetField(ref this._optionalWorkingDirectory, value);
            }

            //---------------------------------------------------------------------
            private string _arguments;
            public string Arguments
            {
                get => this._arguments; set => this.SetField(ref this._arguments, value);
            }

            //---------------------------------------------------------------------
            private bool _compileBeforeRunning;
            public bool CompileBeforeRunning
            {
                get => this._compileBeforeRunning; set => this.SetField(ref this._compileBeforeRunning, value);
            }

            //---------------------------------------------------------------------
            private bool _optimizedBuild;
            public bool OptimizedBuild
            {
                get => this._optimizedBuild; set => this.SetField(ref this._optimizedBuild, value);
            }
        }

        //---------------------------------------------------------------------
        public static string None = "None";
        private bool _isAllSelected = true;
        //---------------------------------------------------------------------
        public BasicSettingController()
        {
            this.SelectableProjects = new List<SelectableProject>();
            this.ToggleSelectAllCommand = new RelayCommand(OnToggleSelectAll);
            this.BasicSettings = new BasicSettingsData();
        }
        //---------------------------------------------------------------------
        private BasicSettingsData _basicSettings;
        public BasicSettingsData BasicSettings
        {
            get => this._basicSettings; private set => this.SetField(ref this._basicSettings, value);
        }

        //---------------------------------------------------------------------
        public void UpdateStartUpProject(StartUpProjectSettings settings)
        {
            this.SelectableProjects = settings.CppProjects.Select(
                project => new SelectableProject(project)).ToList();
            this.BasicSettings.ProgramToRun = settings.Command;

            if (string.IsNullOrEmpty(settings.WorkingDir))
                this.HasWorkingDirectory = false;
            else
            {
                this.HasWorkingDirectory = true;
                this.BasicSettings.OptionalWorkingDirectory = settings.WorkingDir;
            }

            this.BasicSettings.Arguments = settings.Arguments;
            this.BasicSettings.OptimizedBuild = settings.IsOptimizedBuildEnabled;
            this.EnvironmentVariables = settings.EnvironmentVariables ?? new List<KeyValuePair<string, string>>();

            if (string.IsNullOrEmpty(settings.ProjectName) || string.IsNullOrEmpty(settings.SolutionConfigurationName))
            {
                this.CurrentProject = None;
                this.CurrentConfiguration = None;
                this.IsCompileBeforeRunningEnabled = false;
                this.CompileBeforeRunningToolTip = "Nothing to build (No startup project set).";
                this.BasicSettings.CompileBeforeRunning = false;
                this.IsOptimizedBuildCheckBoxEnabled = true;
                this.OptimizedBuildToolTip = null;
            }
            else
            {
                this.CurrentProject = settings.ProjectName;
                this.CurrentConfiguration = settings.SolutionConfigurationName;
                this.IsCompileBeforeRunningEnabled = true;
                this.CompileBeforeRunningToolTip = null;
                this.BasicSettings.CompileBeforeRunning = true;
                this.IsOptimizedBuildCheckBoxEnabled = false;
                this.OptimizedBuildToolTip = "This value is set according to your optimization setting.";
            }
        }

        //-----------------------------------------------------------------
        public class SettingsData
        {
            public BasicSettingsData Data { get; set; }
            public Dictionary<string, bool> IsSelectedByProjectPath { get; set; }
        }

        //-----------------------------------------------------------------
        public void UpdateSettings(SettingsData settings)
        {
            this.BasicSettings = settings.Data;
            foreach (var project in this.SelectableProjects)
            {
                if (settings.IsSelectedByProjectPath.TryGetValue(project.FullName, out bool isSelected))
                    project.IsSelected = isSelected;
            }
            this.HasWorkingDirectory = !string.IsNullOrEmpty(this.BasicSettings.OptionalWorkingDirectory);
            if (!this.IsCompileBeforeRunningEnabled) this.BasicSettings.CompileBeforeRunning = false;
            if (!this.IsOptimizedBuildCheckBoxEnabled) this.BasicSettings.OptimizedBuild = false;
        }

        //---------------------------------------------------------------------
        public SettingsData BuildJsonSettings()
        {
            return new SettingsData
            {
                Data = this.BasicSettings,
                IsSelectedByProjectPath = this._selectableProjects.ToDictionary(p => p.FullName, p => p.IsSelected)
            };
        }

        //---------------------------------------------------------------------
        public BasicSettings GetSettings()
        {
            var selectedProjects = this.SelectableProjects
                .Where(p => p.IsSelected)
                .Select(p => p.Project)
                .ToList();
            return new BasicSettings
            {
                ModulePaths = selectedProjects.Select(project => project.ModulePath),
                SourcePaths = selectedProjects.SelectMany(project => project.SourcePaths),
                Arguments = this.BasicSettings.Arguments,
                ProgramToRun = this.BasicSettings.ProgramToRun,
                CompileBeforeRunning = this.BasicSettings.CompileBeforeRunning,
                WorkingDirectory = GetWorkingDirectory(),
                ProjectName = this.CurrentProject,
                SolutionConfigurationName = this.CurrentConfiguration,
                IsOptimizedBuildEnabled = this.BasicSettings.OptimizedBuild,
                EnvironmentVariables = this.EnvironmentVariables
            };
        }

        //---------------------------------------------------------------------
        private List<SelectableProject> _selectableProjects;
        public List<SelectableProject> SelectableProjects
        {
            get => this._selectableProjects;
            private set => this.SetField(ref this._selectableProjects, value);
        }

        //---------------------------------------------------------------------
        bool hasWorkingDirectory;
        public bool HasWorkingDirectory
        {
            get => this.hasWorkingDirectory;
            set
            {
                if (this.SetField(ref this.hasWorkingDirectory, value) && !value)
                    this.BasicSettings.OptionalWorkingDirectory = null;
            }
        }
        //---------------------------------------------------------------------
        private bool _isCompileBeforeRunningEnabled;
        public bool IsCompileBeforeRunningEnabled
        {
            get => this._isCompileBeforeRunningEnabled; set => this.SetField(ref this._isCompileBeforeRunningEnabled, value);
        }

        //---------------------------------------------------------------------
        private string _compileBeforeRunningToolTip;
        public string CompileBeforeRunningToolTip
        {
            get => this._compileBeforeRunningToolTip; set => this.SetField(ref this._compileBeforeRunningToolTip, value);
        }

        //---------------------------------------------------------------------
        private bool _isOptimizedBuildCheckBoxEnabled;
        public bool IsOptimizedBuildCheckBoxEnabled
        {
            get => this._isOptimizedBuildCheckBoxEnabled; set => this.SetField(ref this._isOptimizedBuildCheckBoxEnabled, value);
        }

        //---------------------------------------------------------------------
        private string _optimizedBuildToolTip;
        public string OptimizedBuildToolTip
        {
            get => this._optimizedBuildToolTip; set => this.SetField(ref this._optimizedBuildToolTip, value);
        }

        //---------------------------------------------------------------------
        private string _currentProject;
        public string CurrentProject
        {
            get => this._currentProject; set => this.SetField(ref this._currentProject, value);
        }

        //---------------------------------------------------------------------
        private string _currentConfiguration;
        public string CurrentConfiguration
        {
            get => this._currentConfiguration;
            set => this.SetField(ref this._currentConfiguration, value);
        }

        //---------------------------------------------------------------------
        private string GetWorkingDirectory()
        {
            return !string.IsNullOrWhiteSpace(this.BasicSettings.OptionalWorkingDirectory) ? this.BasicSettings.OptionalWorkingDirectory : Path.GetDirectoryName(this.BasicSettings.ProgramToRun);
        }

        //---------------------------------------------------------------------
        private void OnToggleSelectAll()
        {
            this._isAllSelected = !this._isAllSelected;
            foreach (var project in this.SelectableProjects)
            {
                project.IsSelected = this._isAllSelected;
            }
            this.SelectableProjects = new List<SelectableProject>(this.SelectableProjects);
        }

        //---------------------------------------------------------------------
        public IEnumerable<KeyValuePair<string, string>> EnvironmentVariables { get; private set; }
        public ICommand ToggleSelectAllCommand { get; }
    }
}
