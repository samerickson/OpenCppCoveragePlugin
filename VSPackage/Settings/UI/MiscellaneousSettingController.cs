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

using OpenCppCoverage.VSPackage.Helper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenCppCoverage.VSPackage.Settings.UI
{
    //-------------------------------------------------------------------------
    internal class MiscellaneousSettingController : PropertyChangedNotifier
    {
        public class SettingsData : PropertyChangedNotifier
        {
            //---------------------------------------------------------------------
            private string _optionalConfigFile;
            public string OptionalConfigFile
            {
                get => this._optionalConfigFile; set => this.SetField(ref this._optionalConfigFile, value);
            }

            //---------------------------------------------------------------------
            private MiscellaneousSettings.LogType _logTypeValue;
            public MiscellaneousSettings.LogType LogTypeValue
            {
                get => this._logTypeValue; set => this.SetField(ref this._logTypeValue, value);
            }

            //---------------------------------------------------------------------
            private bool _continueAfterCppExceptions;
            public bool ContinueAfterCppExceptions
            {
                get => this._continueAfterCppExceptions; set => this.SetField(ref this._continueAfterCppExceptions, value);
            }

        }

        //---------------------------------------------------------------------
        public MiscellaneousSettingController()
        {
            this.LogTypeValues = Enum.GetValues(typeof(MiscellaneousSettings.LogType))
                .Cast<MiscellaneousSettings.LogType>();
            this.Settings = new SettingsData();
        }

        //---------------------------------------------------------------------
        private SettingsData _settings;
        public SettingsData Settings
        {
            get => this._settings; private set => this.SetField(ref this._settings, value);
        }

        //---------------------------------------------------------------------
        public void UpdateStartUpProject()
        {
            this.HasConfigFile = false;
            this.Settings.OptionalConfigFile = null;
            this.Settings.LogTypeValue = MiscellaneousSettings.LogType.Normal;
            this.Settings.ContinueAfterCppExceptions = false;
        }

        //---------------------------------------------------------------------
        public void UpdateSettings(SettingsData settings)
        {
            this.Settings = settings;
            this.HasConfigFile = !string.IsNullOrEmpty(this.Settings.OptionalConfigFile);
        }

        //---------------------------------------------------------------------
        public MiscellaneousSettings GetSettings()
        {
            return new MiscellaneousSettings
            {
                OptionalConfigFile = this.Settings.OptionalConfigFile,
                LogTypeValue = this.Settings.LogTypeValue,
                ContinueAfterCppExceptions = this.Settings.ContinueAfterCppExceptions
            };
        }

        //---------------------------------------------------------------------
        private bool _hasConfigFile;
        public bool HasConfigFile
        {
            get => this._hasConfigFile;
            set
            {
                if (this.SetField(ref this._hasConfigFile, value) && !value)
                    this.Settings.OptionalConfigFile = null;
            }
        }

        //---------------------------------------------------------------------
        public IEnumerable<MiscellaneousSettings.LogType> LogTypeValues { get; }
    }
}
