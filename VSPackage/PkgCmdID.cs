﻿namespace OpenCppCoverage.VSPackage
{
    static class PkgCmdIDList
    {
        public const uint RunOpenCppCoverageCommand = 0x100;
        public const uint RunOpenCppCoverageSettingsCommand = 0x101;

        public const uint RunOpenCppCoverageFromSelectedProjectCommand = 0x0200;
        public const uint RunOpenCppCoverageFromSelectedProjectSettingsCommand = 0x0201;

        public const uint ShowCoverageTree = 0x0300;
    };
}