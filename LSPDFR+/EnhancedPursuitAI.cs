using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Albo1125.Common.CommonLibrary;
using LSPD_First_Response.Mod.API;
using Rage;
using Rage.Native;

namespace LSPDFR_
{
    public enum PursuitTactics { Safe, SlightlyAggressive, FulloutAggressive }
    [Obsolete("LSPDFR 0.4 appears to use custom AI rather than pursuit natives, this no longer works well unless disabling it.")]
    internal static class EnhancedPursuitAI
    {
        
        public static bool EnhancedPursuitAiEnabled = true;
        public static bool AutoPursuitBackupEnabled = false;
        public static Keys OpenPursuitTacticsMenuKey = Keys.Q;
        public static Keys OpenPursuitTacticsMenuModifierKey = Keys.LShiftKey;
        public static bool DefaultAutomaticAi = true;


        public static PursuitTactics CurrentPursuitTactic = PursuitTactics.Safe;
        public static bool AutomaticAi = true;

        public static bool SetSafePursuit = true;
        private static readonly List<Ped> CopsInPursuit = new List<Ped>();
        public static bool InPursuit;

        public static void MainLoop()
        {
            GameFiber.StartNew(delegate
            {
                while (true)
                {
                    try
                    {
                        GameFiber.Yield();

                        if (Functions.GetActivePursuit() != null)
                        {
                            if (!AutoPursuitBackupEnabled)
                            {
                                Functions.SetPursuitCopsCanJoin(Functions.GetActivePursuit(), false);
                            }

                            if (!InPursuit)
                            {
                                //StatisticsCounter.AddCountToStatistic("Pursuits", "LSPDFR+");
                                InPursuit = true;
                                API.Functions.OnPlayerJoinedActivePursuit();
                                if (EnhancedPursuitAiEnabled)
                                {
                                    Game.DisplayHelp("Press ~b~" + ExtensionMethods.GetKeyString(OpenPursuitTacticsMenuKey, OpenPursuitTacticsMenuModifierKey) + " ~s~to open the pursuit tactics menu.");
                                    //Menus.AutomaticTacticsCheckboxItem.Checked = true;
                                }
                            }


                            if (EnhancedPursuitAiEnabled)
                            {
                                if (Functions.GetPursuitPeds(Functions.GetActivePursuit()).Length <= 0) { Functions.ForceEndPursuit(Functions.GetActivePursuit()); continue; }
                                Ped[] pursuitpeds = (from x in Functions.GetPursuitPeds(Functions.GetActivePursuit()) where x.Exists() orderby Game.LocalPlayer.Character.DistanceTo(x) select x).ToArray();
                                if (pursuitpeds.Length > 0)
                                {
                                    AutomaticAi = Menus.AutomaticTacticsCheckboxItem.Checked;
                                    Menus.PursuitTacticsListItem.Enabled = !Menus.AutomaticTacticsCheckboxItem.Checked;
                                    if (AutomaticAi)
                                    {
                                        if (Game.LocalPlayer.Character.IsInAnyVehicle(false) && pursuitpeds[0].IsInAnyVehicle(false))
                                        {
                                            if (Game.LocalPlayer.Character.DistanceTo(pursuitpeds[0].GetOffsetPosition(Vector3.RelativeFront * 4f)) < Game.LocalPlayer.Character.DistanceTo(pursuitpeds[0].GetOffsetPosition(Vector3.RelativeBack * 4f)) && Game.LocalPlayer.Character.DistanceTo(pursuitpeds[0]) < 40f)
                                            {
                                                CurrentPursuitTactic = PursuitTactics.SlightlyAggressive;
                                            }
                                            else if (Game.LocalPlayer.Character.DistanceTo(pursuitpeds[0].Position) > 130f)
                                            {
                                                CurrentPursuitTactic = PursuitTactics.FulloutAggressive;
                                            }
                                            else
                                            {
                                                CurrentPursuitTactic = PursuitTactics.Safe;
                                            }
                                        }
                                        else
                                        {
                                            CurrentPursuitTactic = PursuitTactics.FulloutAggressive;
                                        }
                                        Menus.PursuitTacticsListItem.Index = (int)CurrentPursuitTactic;
                                    }
                                    else
                                    {

                                        CurrentPursuitTactic = (PursuitTactics)Menus.PursuitTacticsListItem.Index;
                                    }

                                    foreach (Ped ped in Game.LocalPlayer.Character.GetNearbyPeds(16))
                                    {

                                        if (ped.Exists() && !CopsInPursuit.Contains(ped))
                                        {

                                            if (ped.IsInAnyPoliceVehicle && ped.IsPolicePed())
                                            {
                                                if (!ped.CurrentVehicle.IsInAir && !ped.CurrentVehicle.IsHelicopter && !ped.CurrentVehicle.IsBoat)
                                                {
                                                    CopsInPursuit.Add(ped);
                                                }

                                            }
                                            else if (!Functions.GetPursuitPeds(Functions.GetActivePursuit()).Contains(ped) && ped.IsInAnyVehicle(false))
                                            {
                                                NativeFunction.Natives.SET_DRIVE_TASK_DRIVING_STYLE(ped, 786603);
                                            }
                                        }
                                    }
                                    //Game.LogTrivial("Pursuittactic: " + CurrentPursuitTactic);
                                    float distance = Vector3.Distance(pursuitpeds[0].Position, Game.LocalPlayer.Character.Position) + 10f;
                                    
                                    Ped[] copsInPursuitOrdered = (from x in CopsInPursuit where x.Exists() orderby x.DistanceTo(pursuitpeds[0].Position) select x).ToArray();
                                    //Ped[] CopsInPursuitOrdered = (from x in (from y in CopsInPursuit where y.Exists() select y) orderby x.DistanceTo(pursuitpeds[0].Position) select x).ToArray();
                                    //Ped[] NearbyPedsInOrder = (from x in (from y in NearbyPeds where y.Exists() select y) orderby x.DistanceTo(Game.LocalPlayer.Character.Position) select x).ToArray();
                                    foreach (Ped ped in copsInPursuitOrdered)
                                    {
                                        GameFiber.Yield();
                                        if (ped.Exists())
                                        {
                                            if (ped.IsInAnyVehicle(false) && !ped.CurrentVehicle.IsInAir)
                                            {
                                                NativeFunction.Natives.SET_DRIVER_ABILITY(ped, 1.0f);

                                                //8: Medium-aggressive boxing tactic with a bit of PIT
                                                //1: Aggressive ramming of suspect
                                                //2: Ram attempts
                                                //32: Stay back from suspect, no tactical contact. Convoy-like.
                                                //16: Ramming, seems to be slightly less aggressive than 1-2.

                                                distance += 15;



                                                if (Game.LocalPlayer.Character.IsInAnyVehicle(false) && pursuitpeds[0].IsInAnyVehicle(false))
                                                {
                                                    int flag;
                                                    switch (CurrentPursuitTactic)
                                                    {
                                                        case PursuitTactics.Safe:
                                                            NativeFunction.Natives.SET_TASK_VEHICLE_CHASE_IDEAL_PURSUIT_DISTANCE(ped, distance);
                                                            flag = 32;
                                                            //Game.LogTrivial("Setting pursuit distance " + distance.ToString());
                                                            NativeFunction.Natives.SET_TASK_VEHICLE_CHASE_BEHAVIOR_FLAG(ped, flag, true);
                                                            //Game.LogTrivial("Setting behaviour flag to " + flag.ToString());
                                                            NativeFunction.Natives.SET_DRIVER_AGGRESSIVENESS(ped, 0.1f);
                                                            break;
                                                        case PursuitTactics.SlightlyAggressive:
                                                            NativeFunction.Natives.SET_TASK_VEHICLE_CHASE_IDEAL_PURSUIT_DISTANCE(ped, 0f);
                                                            //Game.LogTrivial("Setting pursuit distance 0");
                                                            flag = 8;
                                                            NativeFunction.Natives.SET_TASK_VEHICLE_CHASE_BEHAVIOR_FLAG(ped, flag, true);
                                                            //Game.LogTrivial("Setting behaviour flag to " + flag.ToString());
                                                            NativeFunction.Natives.SET_DRIVER_AGGRESSIVENESS(ped, 0.5f);
                                                            break;
                                                        default:
                                                            ped.Tasks.Clear();
                                                            ped.Tasks.ChaseWithGroundVehicle(pursuitpeds[0]);
                                                            NativeFunction.Natives.SET_DRIVER_AGGRESSIVENESS(ped, 1.0f);
                                                            break;
                                                    }
                                                }
                                                else
                                                {
                                                    ped.Tasks.Clear();
                                                    ped.Tasks.ChaseWithGroundVehicle(pursuitpeds[0]);
                                                    NativeFunction.Natives.SET_DRIVER_AGGRESSIVENESS(ped, 1.0f);


                                                }
                                                //else
                                                //{
                                                //    Rage.Native.NativeFunction.Natives.SET_TASK_VEHICLE_CHASE_IDEAL_PURSUIT_DISTANCE(ped, 0f);
                                                //    Game.LogTrivial("Setting pursuit distance 0");
                                                //    flag = 8;
                                                //}



                                                //Works with: 32, 16, 8, 

                                            }
                                        }
                                    }
                                    if (CurrentPursuitTactic == PursuitTactics.FulloutAggressive)
                                    {
                                        GameFiber.Wait(600);
                                    }
                                }
                            }
                        }
                        else
                        {
                            InPursuit = false;
                            if (CopsInPursuit.Count > 0)
                            {
                                CopsInPursuit.Clear();
                            }
                        }
                    }
                    catch (ThreadAbortException) { break; }
                    catch (Exception e)
                    {
                        Game.LogTrivial(e.ToString());
                        Game.LogTrivial("Handled");
                    }
                }
            });
        }
    }
}
