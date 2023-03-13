// OpenCppCoverage is an open source code coverage for C++.
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

using OpenCppCoverage.VSPackage.Settings;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace OpenCppCoverage.VSPackage
{
    class OpenCppCoverageRunner
    {
        private readonly OutputWindowWriter _outputWindowWriter;
        private readonly OpenCppCoverageCmdLine _openCppCoverageCmdLine;

        //---------------------------------------------------------------------
        public OpenCppCoverageRunner(
            OutputWindowWriter outputWindowWriter,
            OpenCppCoverageCmdLine openCppCoverageCmdLine)
        {
            this._outputWindowWriter = outputWindowWriter;
            this._openCppCoverageCmdLine = openCppCoverageCmdLine;
        }

        //---------------------------------------------------------------------
        public Task RunCodeCoverageAsync(MainSettings settings)
        {
            var basicSettings = settings.BasicSettings;
            var fileName = GetOpenCppCoveragePath(basicSettings.ProgramToRun);
            var arguments = this._openCppCoverageCmdLine.Build(settings);

            this._outputWindowWriter.WriteLine("Run:");
            this._outputWindowWriter.WriteLine($@"""{fileName}"" {arguments}");

            // Run in a new thread to not block UI thread.
            return Task.Run(() =>
            {
                using (var process = new Process())
                {
                    var startInfo = process.StartInfo;
                    startInfo.FileName = fileName;
                    startInfo.Arguments = arguments;
                    startInfo.UseShellExecute = false;
                    startInfo.CreateNoWindow = !settings.DisplayProgramOutput;

                    var environmentVariables = startInfo.EnvironmentVariables;
                    foreach (var environment in basicSettings.EnvironmentVariables)
                        environmentVariables[environment.Key] = environment.Value;

                    if (!string.IsNullOrEmpty(basicSettings.WorkingDirectory))
                        startInfo.WorkingDirectory = basicSettings.WorkingDirectory;
                    process.Start();
                    process.WaitForExit();
                }
            });
        }

        //---------------------------------------------------------------------
        static string GetOpenCppCoveragePath(string commandPath)
        {
            var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var assemblyFolder = Path.GetDirectoryName(assemblyLocation);
            var openCppCoverageFolder = Environment.Is64BitOperatingSystem ?
                                            "OpenCppCoverage-x64" : "OpenCppCoverage-x86";
            return Path.Combine(assemblyFolder, openCppCoverageFolder, "OpenCppCoverage.exe");
        }
    }
}
