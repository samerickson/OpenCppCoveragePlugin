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
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenCppCoverage.VSPackage.Settings.UI
{
    //-------------------------------------------------------------------------
    internal class MainSettingController : PropertyChangedNotifier
    {
        private readonly IOpenCppCoverageCmdLine _openCppCoverageCmdLine;
        private readonly ISettingsStorage _settingsStorage;
        private readonly CoverageRunner _coverageRunner;
        private readonly IStartUpProjectSettingsBuilder _startUpProjectSettingsBuilder;

        private string _selectedProjectPath;
        private string _solutionConfigurationName;
        private bool _displayProgramOutput;
        private ProjectSelectionKind _kind;

        //---------------------------------------------------------------------
        public MainSettingController(ISettingsStorage settingsStorage, IOpenCppCoverageCmdLine openCppCoverageCmdLine, IStartUpProjectSettingsBuilder startUpProjectSettingsBuilder, CoverageRunner coverageRunner)
        {
            this._settingsStorage = settingsStorage;
            this._openCppCoverageCmdLine = openCppCoverageCmdLine;
            this.RunCoverageCommand = new RelayCommand(OnRunCoverageCommand);
            this.CloseCommand = new RelayCommand(() =>
            {
                this.CloseWindowEvent?.Invoke(this, EventArgs.Empty);
            });
            this.ResetToDefaultCommand = new RelayCommand(
                () => UpdateStartUpProject(ComputeStartUpProjectSettings(_kind)));
            this.BasicSettingController = new BasicSettingController();
            this.FilterSettingController = new FilterSettingController();
            this.ImportExportSettingController = new ImportExportSettingController();
            this.MiscellaneousSettingController = new MiscellaneousSettingController();

            this._coverageRunner = coverageRunner;
            this._startUpProjectSettingsBuilder = startUpProjectSettingsBuilder;
        }

        //---------------------------------------------------------------------
        public void UpdateFields(ProjectSelectionKind kind, bool displayProgramOutput)
        {
            var settings = ComputeStartUpProjectSettings(kind);
            this.UpdateStartUpProject(settings);
            this._selectedProjectPath = settings.ProjectPath;
            this._displayProgramOutput = displayProgramOutput;
            this._solutionConfigurationName = settings.SolutionConfigurationName;
            this._kind = kind;

            var uiSettings = this._settingsStorage.TryLoad(this._selectedProjectPath, this._solutionConfigurationName);

            if (uiSettings == null) return;

            this.BasicSettingController.UpdateSettings(uiSettings.BasicSettingController);
            this.FilterSettingController.UpdateSettings(uiSettings.FilterSettingController);
            this.ImportExportSettingController.UpdateSettings(uiSettings.ImportExportSettingController);
            this.MiscellaneousSettingController.UpdateSettings(uiSettings.MiscellaneousSettingController);
        }

        //---------------------------------------------------------------------
        private StartUpProjectSettings ComputeStartUpProjectSettings(ProjectSelectionKind kind)
        {
            return this._startUpProjectSettingsBuilder.ComputeSettings(kind);
        }

        //---------------------------------------------------------------------
        private void UpdateStartUpProject(StartUpProjectSettings settings)
        {
            this.BasicSettingController.UpdateStartUpProject(settings);
            this.FilterSettingController.UpdateStartUpProject();
            this.ImportExportSettingController.UpdateStartUpProject();
            this.MiscellaneousSettingController.UpdateStartUpProject();
        }

        //---------------------------------------------------------------------
        public void SaveSettings()
        {
            var uiSettings = new UserInterfaceSettings
            {
                BasicSettingController = this.BasicSettingController.BuildJsonSettings(),
                FilterSettingController = this.FilterSettingController.Settings,
                ImportExportSettingController = this.ImportExportSettingController.Settings,
                MiscellaneousSettingController = this.MiscellaneousSettingController.Settings
            };
            this._settingsStorage.Save(this._selectedProjectPath, this._solutionConfigurationName, uiSettings);
        }

        //---------------------------------------------------------------------
        public MainSettings GetMainSettings()
        {
            return new MainSettings
            {
                BasicSettings = this.BasicSettingController.GetSettings(),
                FilterSettings = this.FilterSettingController.GetSettings(),
                ImportExportSettings = this.ImportExportSettingController.GetSettings(),
                MiscellaneousSettings = this.MiscellaneousSettingController.GetSettings(),
                DisplayProgramOutput = this._displayProgramOutput
            };
        }

        //---------------------------------------------------------------------
        public BasicSettingController BasicSettingController { get; }
        public FilterSettingController FilterSettingController { get; }
        public ImportExportSettingController ImportExportSettingController { get; }
        public MiscellaneousSettingController MiscellaneousSettingController { get; }

        //---------------------------------------------------------------------
        private string _commandLineText;
        public string CommandLineText
        {
            get => this._commandLineText; private set => this.SetField(ref this._commandLineText, value);
        }

        //---------------------------------------------------------------------
        public static string CommandLineHeader = "Command line";

        public TabItem SelectedTab
        {
            set
            {
                if (value == null || (string)value.Header != CommandLineHeader) return;
                try
                {
                    this.CommandLineText = this._openCppCoverageCmdLine.Build(this.GetMainSettings(), "\n");
                }
                catch (Exception e)
                {
                    this.CommandLineText = e.Message;
                }
            }
        }
        //---------------------------------------------------------------------
        private void OnRunCoverageCommand()
        {
            this._coverageRunner.RunCoverageOnStartupProject(this.GetMainSettings());
        }

        //---------------------------------------------------------------------
        public EventHandler CloseWindowEvent;

        //---------------------------------------------------------------------
        public ICommand CloseCommand { get; }
        public ICommand RunCoverageCommand { get; }
        public ICommand ResetToDefaultCommand { get; }
    }
}
