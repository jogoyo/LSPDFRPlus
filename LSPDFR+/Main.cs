using System;
using System.Reflection;
using Albo1125.Common;
using LSPD_First_Response.Mod.API;
using Rage;

namespace LSPDFR_
{
    internal class Main : Plugin
    {
        public override void Finally()
        {

        }

        public override void Initialize()
        {
            Functions.OnOnDutyStateChanged += Functions_OnOnDutyStateChanged;
            Game.LogTrivial("LSPDFR+ " + Assembly.GetExecutingAssembly().GetName().Version + ", developed by Albo1125, has been initialised.");
            Game.LogTrivial("Go on duty to start LSPDFR+.");
            UpdateChecker.VerifyXmlNodeExists(PluginName, FileId, DownloadUrl, Path);
            DependencyChecker.RegisterPluginForDependencyChecks(PluginName);
        }

        private static readonly Version Albo1125CommonVer = new Version("6.6.4.0");
        private static readonly Version MadeForGtaVersion = new Version("1.0.1604.1");
        private const float MinimumRphVersion = 0.51f;
        private static readonly string[] AudioFilesToCheckFor = { };
        private static readonly string[] OtherFilesToCheckFor = {  }; //"Plugins/LSPDFR/LSPDFR+/CourtCases.xml"
        private static readonly Version RageNativeUiVersion = new Version("1.6.3.0");
        private static readonly Version MadeForLspdfrVersion = new Version("0.4.39.22580");

        private const string DownloadUrl = "https://www.lcpdfr.com/files/file/11930-lspdfr-improved-pursuit-ai-better-traffic-stops-court-system/";

        private const string FileId = "11930";

        private const string PluginName = "LSPDFR+";
        private const string Path = "Plugins/LSPDFR/LSPDFR+.dll";

        private static readonly string[] ConflictingFiles = { "Plugins/LSPDFR/AutoPursuitBackupDisabler.dll", "Plugins/LSPDFR/SaferChasesRPH.dll" };

        private static void Functions_OnOnDutyStateChanged(bool onDuty)
        {
            if (!onDuty) return;
            UpdateChecker.InitialiseUpdateCheckingProcess();
            if (!DependencyChecker.DependencyCheckMain(PluginName, Albo1125CommonVer, MinimumRphVersion,
                MadeForGtaVersion, MadeForLspdfrVersion, RageNativeUiVersion, AudioFilesToCheckFor,
                OtherFilesToCheckFor)) return;
            DependencyChecker.CheckIfThereAreNoConflictingFiles(PluginName, ConflictingFiles);
            LSPDFRPlusHandler.Initialise();
        }
    }
}
