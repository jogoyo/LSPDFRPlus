using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Albo1125.Common.CommonLibrary;
using LSPD_First_Response.Mod.API;
using Rage;
using Rage.Attributes;
using Rage.Native;

[assembly: Plugin("LSPDFR+", Description = "INSTALL IN PLUGINS/LSPDFR FOLDER. Enhances policing in LSPDFR", Author = "Albo1125")]
namespace LSPDFR_
{
    public class EntryPoint
    {
        public static void Main()
        {
            Game.DisplayNotification("You have installed LSPDFR+ incorrectly. You must install it in GTAV/Plugins/LSPDFR. It will then be automatically loaded when going on duty - you must NOT load it yourself via RAGEPluginHook. This is also explained in the Readme and Documentation. You will now be redirected to the installation tutorial.");
            GameFiber.Wait(5000);
            Process.Start("https://www.youtube.com/watch?v=af434m72rIo&list=PLEKypmos74W8PMP4k6xmVxpTKdebvJpFb");
        }
    }
    internal static class LSPDFRPlusHandler
    {
        public static bool TrafficPolicerRunning;

        private static readonly KeysConverter Kc = new KeysConverter();
        private static Popup _trafficStopMenuPopup;
        private const string LspdfrKeyIniPath = "lspdfr/keys.ini";
        private static Keys _stockTrafficStopInteractKey = Keys.E;
        private static Keys _stockTrafficStopInteractModifierKey = Keys.None;
        private static ControllerButtons _stockTrafficStopInteractControllerButton = ControllerButtons.DPadRight;
        private static ControllerButtons _stockTrafficStopInteractModifierControllerButton = ControllerButtons.None;
        public static void Initialise()
        {
            //ini stuff

            InitializationFile ini = new InitializationFile("Plugins/LSPDFR/LSPDFR+.ini");
            ini.Create();
            try
            {
                EnhancedTrafficStop.BringUpTrafficStopMenuControllerButton = ini.ReadEnum("General", "BringUpTrafficStopMenuControllerButton", ControllerButtons.DPadRight);
                EnhancedTrafficStop.BringUpTrafficStopMenuKey = (Keys)Kc.ConvertFromString(ini.ReadString("General", "BringUpTrafficStopMenuKey", "D7"));
              
                try
                {
                    string[] stockinicontents = File.ReadAllLines(LspdfrKeyIniPath);
                    //Alternative INI reading implementation, RPH doesn't work with sectionless INIs.
                    foreach (string line in stockinicontents)
                    {
                        if (line.StartsWith("TRAFFICSTOP_INTERACT_Key="))
                        {
                            _stockTrafficStopInteractKey = (Keys)Kc.ConvertFromString(line.Substring(line.IndexOf('=') + 1));
                        }
                        else if (line.StartsWith("TRAFFICSTOP_INTERACT_ModifierKey"))
                        {
                            _stockTrafficStopInteractModifierKey = (Keys)Kc.ConvertFromString(line.Substring(line.IndexOf('=') + 1));
                        }
                        else if (line.StartsWith("TRAFFICSTOP_INTERACT_ControllerKey"))
                        {
                            _stockTrafficStopInteractControllerButton = (ControllerButtons)Enum.Parse(typeof(ControllerButtons), line.Substring(line.IndexOf('=') + 1));
                        }
                        else if (line.StartsWith("TRAFFICSTOP_INTERACT_ControllerModifierKey"))
                        {
                            _stockTrafficStopInteractModifierControllerButton = (ControllerButtons)Enum.Parse(typeof(ControllerButtons), line.Substring(line.IndexOf('=') + 1));
                        }
                    }
                    if ((EnhancedTrafficStop.BringUpTrafficStopMenuKey == _stockTrafficStopInteractKey && _stockTrafficStopInteractModifierKey == Keys.None) || (EnhancedTrafficStop.BringUpTrafficStopMenuControllerButton == _stockTrafficStopInteractControllerButton && _stockTrafficStopInteractModifierControllerButton == ControllerButtons.None))
                    {
                        _trafficStopMenuPopup = new Popup("LSPDFR+: Traffic Stop Menu Conflict", "Your LSPDFR+ traffic stop menu keys (plugins/lspdfr/lspdfr+.ini) are the same as the default LSPDFR traffic stop keys (lspdfr/keys.ini TRAFFICSTOP_INTERACT_Key and TRAFFICSTOP_INTERACT_ControllerKey). How would you like to solve this?",
                            new List<string> { "Recommended: Automatically disable the default LSPDFR traffic stop menu keys (this will edit keys.ini TRAFFICSTOP_INTERACT_Key and TRAFFICSTOP_INTERACT_ControllerKey to None)", "I know what I'm doing, I will change the keys in the INIs myself!" }, false, true, TrafficStopMenuCb);
                        _trafficStopMenuPopup.Display();
                    }
                }
                catch (Exception e)
                {
                    Game.LogTrivial($"Failed to determine stock LSPDFR key bind/controller button for traffic stop keys: {e}");
                }
              
                CourtSystem.OpenCourtMenuKey = (Keys)Kc.ConvertFromString(ini.ReadString("OnlyWithoutBritishPolicingScriptInstalled", "OpenCourtMenuKey", "F9"));
                CourtSystem.OpenCourtMenuModifierKey = (Keys)Kc.ConvertFromString(ini.ReadString("OnlyWithoutBritishPolicingScriptInstalled", "OpenCourtMenuModifierKey", "None"));
                EnhancedTrafficStop.EnhancedTrafficStopsEnabled = ini.ReadBoolean("General", "EnhancedTrafficStopsEnabled", true);
                /*EnhancedPursuitAI.EnhancedPursuitAiEnabled = ini.ReadBoolean("General", "EnhancedPursuitAIEnabled", true);
                EnhancedPursuitAI.AutoPursuitBackupEnabled = ini.ReadBoolean("General", "AutoPursuitBackupEnabled", false);
                EnhancedPursuitAI.OpenPursuitTacticsMenuKey = (Keys)kc.ConvertFromString(ini.ReadString("General", "OpenPursuitTacticsMenuKey", "Q"));
                EnhancedPursuitAI.OpenPursuitTacticsMenuModifierKey = (Keys)kc.ConvertFromString(ini.ReadString("General", "OpenPursuitTacticsMenuModifierKey", "LShiftKey"));
                EnhancedPursuitAI.DefaultAutomaticAi = ini.ReadBoolean("General", "DefaultAutomaticAI", true);*/

                Offence.Maxpoints = ini.ReadInt32("General", "MaxPoints", 12);
                Offence.Pointincstep = ini.ReadInt32("General", "PointsIncrementalStep", 1);
                Offence.MaxFine = ini.ReadInt32("General", "MaxFine", 5000);

                Offence.OpenTicketMenuKey = (Keys)Kc.ConvertFromString(ini.ReadString("General", "OpenTicketMenuKey", "Q"));
                Offence.OpenTicketMenuModifierKey = (Keys)Kc.ConvertFromString(ini.ReadString("General", "OpenTicketMenuModifierKey", "LShiftKey"));
                Offence.EnablePoints = ini.ReadBoolean("General", "EnablePoints", true);

                CourtSystem.RealisticCourtDates = ini.ReadBoolean("OnlyWithoutBritishPolicingScriptInstalled", "RealisticCourtDates", true);
            }
            catch (Exception e)
            {
                Game.LogTrivial(e.ToString());
                Game.LogTrivial("Error loading LSPDFR+ INI file. Loading defaults");
                Game.DisplayNotification("~r~Error loading LSPDFR+ INI file. Loading defaults");
            }
            BetaCheck();
        }

        private static void TrafficStopMenuCb(Popup p)
        {
            switch (p.IndexOfGivenAnswer)
            {
                case 0:
                    GameFiber.StartNew(delegate
                    {
                        //RPH ini implementation does not work with INIs without sections!
                        string[] stockinicontents = File.ReadAllLines(LspdfrKeyIniPath);
                        using (StreamWriter writer = new StreamWriter(LspdfrKeyIniPath))
                        {
                            foreach (string line in stockinicontents)
                            {
                                if (line.StartsWith("TRAFFICSTOP_INTERACT_Key") &&
                                    EnhancedTrafficStop.BringUpTrafficStopMenuKey == _stockTrafficStopInteractKey &&
                                    _stockTrafficStopInteractModifierKey == Keys.None)
                                {
                                    writer.WriteLine("TRAFFICSTOP_INTERACT_Key=None");
                                }
                                else if (line.StartsWith("TRAFFICSTOP_INTERACT_ControllerKey") &&
                                         EnhancedTrafficStop.BringUpTrafficStopMenuControllerButton ==
                                         _stockTrafficStopInteractControllerButton &&
                                         _stockTrafficStopInteractModifierControllerButton == ControllerButtons.None)
                                {
                                    writer.WriteLine("TRAFFICSTOP_INTERACT_ControllerKey=None");
                                }
                                else
                                {
                                    writer.WriteLine(line);
                                }
                            }
                        }

                        Game.DisplayNotification(
                            "The default LSPDFR traffic stop menu keys have been disabled (INI changed to None). LSPDFR will now reload, type ~b~forceduty~w~ in the console to resume play.");
                        GameFiber.Wait(3000);
                        Game.ReloadActivePlugin();
                    });
                    break;
                case 1:
                    Game.DisplayNotification(
                        "Your ~g~LSPDFR+ Traffic Stop~w~ menu key/controller button is still the same as for the default LSPDFR Traffic Stop. This will cause ~r~problems~w~, ensure you change it!");
                    break;
            }
        }        

        //private static readonly Stopwatch TimeOnDutyStopWatch = new Stopwatch();
        private static void MainLoop()
        {
            GameFiber.StartNew(delegate
            {
                Game.LogTrivial("LSPDFR+ has been initialised successfully and is now loading INI, XML and dependencies. Standby...");
                AppDomain.CurrentDomain.AssemblyResolve += LSPDFRResolveEventHandler;
                GameFiber.Sleep(5000);
                Offence.DeserializeOffences();
                Game.LogTrivial("TrafficOffences:");
                Offence.CategorizedTrafficOffences.Values.ToList().ForEach(x => x.ForEach(y => Game.LogTrivial(y.ToString())));
                Menus.InitialiseMenus();

                CourtSystem.CourtSystemMainLogic();
                
                Game.LogTrivial("LSPDFR+ has been fully initialised successfully and is now working.");
            });

        }

        private static Assembly LSPDFRResolveEventHandler(object sender, ResolveEventArgs args)
        {
            return Functions.GetAllUserPlugins().FirstOrDefault(assembly => args.Name.ToLower().Contains(assembly.GetName().Name.ToLower()));
        }

        public static readonly Random Rnd = new Random();

        private static void BetaCheck()
        {
            GameFiber.StartNew(delegate
            {
                Game.LogTrivial("LSPDFR+, developed by Albo1125, has been loaded successfully!");
                GameFiber.Wait(6000);
                Game.DisplayNotification("~b~LSPDFR+~s~, developed by ~b~Albo1125 ~s~and repacked by ~b~Jogoyo~s~, has been loaded ~g~successfully.");
            });
            Game.LogTrivial("LSPDFR+ is not in beta.");

            MainLoop();
        }        
    }
}
