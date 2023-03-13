// Guids.cs
// MUST match guids.h
using System;

namespace OpenCppCoverage.VSPackage
{
    internal static class GuidList
    {
        public const string GuidVsPackagePkgString = "c6a77aca-f53c-4cd1-97d7-0ed595751347";
        public const string GuidVsPackageCmdSetString = "fe1f442f-480d-4a2b-bf8a-adc8a0fc569d";

        public static readonly Guid GuidVsPackageCmdSet = new Guid(GuidVsPackageCmdSetString);
    };
}