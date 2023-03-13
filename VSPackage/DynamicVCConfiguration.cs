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

namespace OpenCppCoverage.VSPackage
{
    internal class DynamicVcConfiguration
    {
        //---------------------------------------------------------------------
        public DynamicVcConfiguration(dynamic configuration)
        {
            this._configuration = configuration;
            this.DebugSettings = new DynamicVcDebugSettings(_configuration.DebugSettings);

            var compilerTool = GetTool(configuration, "VCCLCompilerTool");
            if (compilerTool != null)
                this.OptionalVCCLCompilerTool = new DynamicVCCLCompilerTool(compilerTool);
        }

        //---------------------------------------------------------------------
        private static dynamic GetTool(dynamic configuration, string toolKindToFind)
        {
            foreach (var tool in configuration.Tools)
            {
                if (tool.ToolKind == toolKindToFind)
                    return tool;
            }

            return null;
        }

        //---------------------------------------------------------------------
        public string ConfigurationName => _configuration.ConfigurationName;

        //---------------------------------------------------------------------
        public string PlatformName => _configuration.Platform.Name;

        //---------------------------------------------------------------------
        public string Evaluate(string str)
        {
            return _configuration.Evaluate(str);
        }

        //---------------------------------------------------------------------
        public DynamicVcDebugSettings DebugSettings { get; }

        //---------------------------------------------------------------------
        public DynamicVCCLCompilerTool OptionalVCCLCompilerTool { get; }

        //---------------------------------------------------------------------
        public string PrimaryOutput => _configuration.PrimaryOutput;

        private readonly dynamic _configuration;
    }
}
