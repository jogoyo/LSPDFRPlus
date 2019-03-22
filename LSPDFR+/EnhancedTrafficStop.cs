using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Albo1125.Common.CommonLibrary;
using LSPDFR_.API;
using LSPDFR_.ExtensionNamespace;
using Rage;
using Rage.Native;
using Functions = LSPD_First_Response.Mod.API.Functions;

namespace LSPDFR_
{
    internal class EnhancedTrafficStop
    {
        public static bool EnhancedTrafficStopsEnabled = true;
        public static ControllerButtons BringUpTrafficStopMenuControllerButton = ControllerButtons.DPadRight;
        public static Keys BringUpTrafficStopMenuKey = Keys.D7;

        public static readonly TupleList<Ped, string, string> PedsWithCustomTrafficStopQuestionsAndAnswers =
            new TupleList<Ped, string, string>();

        public static readonly TupleList<Ped, string, Func<Ped, string>> PedsCustomTrafficStopQuestionsAndCallBackAnswer =
            new TupleList<Ped, string, Func<Ped, string>>();

        public static readonly TupleList<Ped, string, string, Action<Ped, string>> PedsCustomQuestionsAnswerCallback =
            new TupleList<Ped, string, string, Action<Ped, string>>();

        public static readonly List<Ped> PedsWhereStandardQuestionsAreHidden = new List<Ped>();

        private static readonly TupleList<Ped, TrafficStopQuestionsInfo> SuspectsTrafficStopQuestionsInfo =
            new TupleList<Ped, TrafficStopQuestionsInfo>();

        private static void PedBackIntoVehicleLogic(Ped suspect, Vehicle suspectvehicle)
        {
            GameFiber.StartNew(delegate
            {
                try
                {
                    while (true)
                    {
                        GameFiber.Yield();
                        if (!suspect.Exists() || !suspectvehicle.Exists())
                        {
                            return;
                        }

                        if (Functions.IsPedGettingArrested(suspect) || Functions.IsPedArrested(suspect) ||
                            suspect.IsDead)
                        {
                            return;
                        }

                        if (!Functions.IsPedStoppedByPlayer(suspect)) continue;
                        while (Functions.IsPedStoppedByPlayer(suspect))
                        {
                            GameFiber.Yield();
                            if (!suspect.Exists() || !suspectvehicle.Exists())
                            {
                                return;
                            }

                            if (Functions.IsPedGettingArrested(suspect) || Functions.IsPedArrested(suspect) ||
                                suspect.IsDead)
                            {
                                return;
                            }
                        }

                        if (suspect.DistanceTo(suspectvehicle) < 25f)
                        {
                            suspect.IsPersistent = true;
                            suspect.BlockPermanentEvents = true;
                            suspectvehicle.IsPersistent = true;
                            suspect.Tasks
                                .FollowNavigationMeshToPosition(
                                    suspectvehicle.GetOffsetPosition(Vector3.RelativeLeft * 2f),
                                    suspectvehicle.Heading, 1.45f).WaitForCompletion(10000);
                            if (suspectvehicle.GetFreeSeatIndex() != null)
                            {
                                int? freeseat = suspectvehicle.GetFreeSeatIndex();
                                suspect.Tasks
                                    .EnterVehicle(suspectvehicle, 6000, freeseat == null ? -1 : freeseat.Value)
                                    .WaitForCompletion(6100);
                            }
                        }

                        suspect.Dismiss();
                        suspectvehicle.Dismiss();
                        return;
                    }
                }
                catch (ThreadAbortException)
                {
                }
                catch (Exception e)
                {
                    Game.LogTrivial(e.ToString());
                    if (suspect.Exists())
                    {
                        suspect.Dismiss();
                    }

                    if (suspectvehicle)
                    {
                        suspectvehicle.Dismiss();
                    }
                }
            });
        }

        public static void VerifySuspectIsInTrafficStopInfo(Ped Suspect)
        {
            if (!SuspectsTrafficStopQuestionsInfo.Select(x => x.Item1).Contains(Suspect))
            {
                SuspectsTrafficStopQuestionsInfo.Add(Suspect, new TrafficStopQuestionsInfo());
            }
        }

        public Ped Suspect { get; }
        private Vehicle SuspectVehicle { get; }
        public readonly List<Offence> SelectedOffences = new List<Offence>();

        public EnhancedTrafficStop()
        {
            if (!Functions.IsPlayerPerformingPullover()) return;
            SuspectVehicle = Functions.GetPulloverSuspect(Functions.GetCurrentPullover()).CurrentVehicle;
            Suspect = Functions.GetPulloverSuspect(Functions.GetCurrentPullover());
            Menus.TrafficStopMenuDistance = SuspectVehicle.IsBoat ? 6.0f : 3.8f;
            UpdateTrafficStopQuestioning();
        }

        public void UpdateTrafficStopQuestioning()
        {
            CustomQuestionsWithAnswers.Clear();
            CustomQuestionsWithCallbacksAnswers.Clear();
            CustomQuestionsAnswerWithCallbacks.Clear();
            foreach (Tuple<Ped, string, string> tuple in PedsWithCustomTrafficStopQuestionsAndAnswers)
            {
                if (tuple.Item1 == Suspect)
                {
                    CustomQuestionsWithAnswers.Add(tuple.Item2, tuple.Item3);
                }
            }

            foreach (Tuple<Ped, string, Func<Ped, string>> tuple in PedsCustomTrafficStopQuestionsAndCallBackAnswer)
            {
                if (tuple.Item1 == Suspect)
                {
                    CustomQuestionsWithCallbacksAnswers.Add(tuple.Item2, tuple.Item3);
                }
            }

            foreach (Tuple<Ped, string, string, Action<Ped, string>> tuple in PedsCustomQuestionsAnswerCallback)
            {
                if (tuple.Item1 == Suspect)
                {
                    CustomQuestionsAnswerWithCallbacks.Add(tuple.Item2, tuple.Item3, tuple.Item4);
                }
            }

            StandardQuestionsEnabled = !PedsWhereStandardQuestionsAreHidden.Contains(Suspect);
        }

        public static void PlaySpecificSpeech(string speech)
        {
            switch (speech)
            {
                case "Hello":
                    Game.LogTrivial("Playing hello");
                    Game.LocalPlayer.Character.PlayAmbientSpeech("GENERIC_HI");
                    break;
                case "Insult":
                {
                    Game.LogTrivial("Playing insult");
                    if (LSPDFRPlusHandler.Rnd.Next(2) == 0)
                    {
                        Game.LocalPlayer.Character.PlayAmbientSpeech("GENERIC_INSULT_MED");
                    }
                    else
                    {
                        Game.LocalPlayer.Character.PlayAmbientSpeech("GENERIC_INSULT_HIGH");
                    }

                    break;
                }
                case "Kifflom":
                    Game.LogTrivial("Playing kifflom");
                    Game.LocalPlayer.Character.PlayAmbientSpeech("KIFFLOM_GREET");
                    break;
                case "Thanks":
                    Game.LogTrivial("Playing thanks");
                    Game.LocalPlayer.Character.PlayAmbientSpeech("GENERIC_THANKS");
                    break;
                case "Swear":
                    Game.LogTrivial("Playing swear");
                    Game.LocalPlayer.Character.PlayAmbientSpeech("GENERIC_CURSE_HIGH");
                    break;
                case "Warn":
                    Game.LogTrivial("Playing warn");
                    Game.LocalPlayer.Character.PlayAmbientSpeech("CRIMINAL_WARNING");
                    //NativeFunction.CallByHash<uint>(0x8E04FEDD28D42462, Game.LocalPlayer.Character, "SHOUT_THREATEN_PED", "SPEECH_PARAMS_FORCE_SHOUTED_CRITICAL");
                    break;
                case "Threaten":
                    Game.LogTrivial("Playing threaten");
                    Game.LocalPlayer.Character.PlayAmbientSpeech("CHALLENGE_THREATEN");
                    break;
            }
        }

        public enum OccupantSelector
        {
            Driver,
            Passengers,
            AllOccupants
        }

        public void AskForId(OccupantSelector occupantselect)
        {
            GameFiber.StartNew(delegate
            {
                PlaySpecificSpeech("Kifflom");

                Game.LocalPlayer.Character.Tasks.AchieveHeading(
                    Game.LocalPlayer.Character.CalculateHeadingTowardsEntity(Suspect));
                GameFiber.Wait(1500);

                switch (occupantselect)
                {
                    case OccupantSelector.Driver:
                        Suspect.ShowDrivingLicence();
                        break;
                    case OccupantSelector.Passengers:
                    {
                        foreach (Ped occupant in SuspectVehicle.Passengers)
                        {
                            occupant.ShowDrivingLicence();
                        }

                        break;
                    }
                    case OccupantSelector.AllOccupants:
                    {
                        foreach (Ped occupant in SuspectVehicle.Occupants)
                        {
                            occupant.ShowDrivingLicence();
                        }

                        break;
                    }
                }

                Game.LocalPlayer.Character.Tasks.Clear();
            });
        }

        public static void IssueWarning()
        {
            GameFiber.StartNew(delegate
            {
                PlaySpecificSpeech("Warn");
                GameFiber.Wait(2500);
                Functions.ForceEndCurrentPullover();
                //StatisticsCounter.AddCountToStatistic("Traffic Stop - Warnings Issued", "LSPDFR+");
            });
        }

        public void OutOfVehicle(OccupantSelector occupantselect)
        {
            GameFiber.StartNew(delegate
            {
                try
                {
                    Vehicle veh = Functions.GetPulloverSuspect(Functions.GetCurrentPullover()).CurrentVehicle;
                    if (occupantselect == OccupantSelector.Driver)
                    {
                        if (Functions.GetPulloverSuspect(Functions.GetCurrentPullover()).IsInAnyVehicle(false))
                        {
                            if (veh.IsBoat)
                            {
                                Functions.ForceEndCurrentPullover();
                                Vector3 pos = Suspect.GetBonePosition(0);
                                Suspect.Tasks.Clear();
                                Suspect.Position = pos;
                            }
                            else
                            {
                                Suspect.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(5000);
                                PedBackIntoVehicleLogic(Suspect, SuspectVehicle);
                            }

                            NativeFunction.Natives.RESET_PED_LAST_VEHICLE(Suspect);
                            API.Functions.OnPedOrderedOutOfVehicle(Suspect);

                            GameFiber.Wait(100);
                            Suspect.Tasks.StandStill(30000);
                            Functions.SetPedCantBeArrestedByPlayer(Suspect, true);
                            GameFiber.Yield();
                            Functions.SetPedCantBeArrestedByPlayer(Suspect, false);
                        }
                    }
                    else if (occupantselect == OccupantSelector.Passengers)
                    {
                        foreach (Ped pas in veh.Passengers)
                        {
                            if (veh.IsBoat)
                            {
                                Functions.ForceEndCurrentPullover();
                                Vector3 pos = pas.GetBonePosition(0);
                                pas.Tasks.Clear();
                                pas.Position = pos;
                            }
                            else
                            {
                                pas.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(6000);
                                PedBackIntoVehicleLogic(pas, SuspectVehicle);
                            }

                            NativeFunction.Natives.RESET_PED_LAST_VEHICLE(pas);
                            API.Functions.OnPedOrderedOutOfVehicle(pas);
                            GameFiber.Wait(100);
                            pas.Tasks.StandStill(30000);
                            Functions.SetPedCantBeArrestedByPlayer(pas, true);
                            GameFiber.Yield();
                            Functions.SetPedCantBeArrestedByPlayer(pas, false);
                        }
                    }
                    else if (occupantselect == OccupantSelector.AllOccupants)
                    {
                        foreach (Ped occ in veh.Occupants)
                        {
                            if (veh.IsBoat)
                            {
                                Functions.ForceEndCurrentPullover();
                                Vector3 pos = occ.GetBonePosition(0);
                                occ.Tasks.Clear();
                                occ.Position = pos;
                            }
                            else
                            {
                                occ.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(6000);
                                PedBackIntoVehicleLogic(occ, SuspectVehicle);
                            }

                            NativeFunction.Natives.RESET_PED_LAST_VEHICLE(occ);
                            API.Functions.OnPedOrderedOutOfVehicle(occ);
                            GameFiber.Wait(100);
                            occ.Tasks.StandStill(30000);
                            Functions.SetPedCantBeArrestedByPlayer(occ, true);
                            GameFiber.Yield();
                            Functions.SetPedCantBeArrestedByPlayer(occ, false);
                        }
                    }
                }
                catch (Exception e)
                {
                    Game.LogTrivial(e.ToString());
                    Game.LogTrivial("Error in getout handled - LSPDFR+");
                }
            });
        }

        public void IssueTicket(bool SeizeVehicle)
        {
            GameFiber.StartNew(delegate
            {
                Game.LocalPlayer.Character.Tasks.AchieveHeading(
                    Game.LocalPlayer.Character.CalculateHeadingTowardsEntity(Suspect));
                Functions.GetPersonaForPed(Suspect).Citations++;
                GameFiber.Wait(1500);
                Game.LocalPlayer.Character.Tasks.Clear();
                NativeFunction.Natives.TASK_START_SCENARIO_IN_PLACE(Game.LocalPlayer.Character,
                    "CODE_HUMAN_MEDIC_TIME_OF_DEATH", 0, true);

                //Do animation
                while (!NativeFunction.Natives.IS_PED_ACTIVE_IN_SCENARIO<bool>(Game.LocalPlayer.Character))
                {
                    GameFiber.Yield();
                }

                int waitcount = 0;
                while (NativeFunction.Natives.IS_PED_ACTIVE_IN_SCENARIO<bool>(Game.LocalPlayer.Character))
                {
                    GameFiber.Yield();
                    waitcount++;
                    if (waitcount >= 300)
                    {
                        Game.LocalPlayer.Character.Tasks.Clear();
                    }
                }

                GameFiber.Wait(4000);

                if (SeizeVehicle && SuspectVehicle.Exists())
                {
                    //Game.LogTrivial("Debug 4");
                    foreach (Ped occupant in SuspectVehicle.Occupants)
                    {
                        occupant.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(6000);
                        if (occupant.IsInAnyVehicle(false))
                        {
                            occupant.Tasks.LeaveVehicle(LeaveVehicleFlags.WarpOut).WaitForCompletion(100);
                        }

                        occupant.Tasks.Wander();
                    }

                    GameFiber.Wait(1000);
                    Game.LocalPlayer.Character.Tasks.ClearImmediately();
                }
                else
                {
                    GameFiber.Wait(2500);
                    Functions.ForceEndCurrentPullover();
                }

                //StatisticsCounter.AddCountToStatistic("Traffic Stop - Tickets Issued", "LSPDFR+");
            });
        }

        public static void PerformTicketAnimation()
        {
            Game.LocalPlayer.Character.Tasks.Clear();
            NativeFunction.Natives.TASK_START_SCENARIO_IN_PLACE(Game.LocalPlayer.Character,
                "CODE_HUMAN_MEDIC_TIME_OF_DEATH", 0, true);

            //Do animation
            while (!NativeFunction.Natives.IS_PED_ACTIVE_IN_SCENARIO<bool>(Game.LocalPlayer.Character))
            {
                GameFiber.Yield();
            }

            int waitcount = 0;
            while (NativeFunction.Natives.IS_PED_ACTIVE_IN_SCENARIO<bool>(Game.LocalPlayer.Character))
            {
                GameFiber.Yield();
                waitcount++;
                if (waitcount >= 300)
                {
                    Game.LocalPlayer.Character.Tasks.Clear();
                }
            }

            GameFiber.Wait(6000);
            Game.LocalPlayer.Character.Tasks.ClearImmediately();
        }

        public readonly TupleList<string, string> CustomQuestionsWithAnswers = new TupleList<string, string>();

        public readonly TupleList<string, Func<Ped, string>> CustomQuestionsWithCallbacksAnswers =
            new TupleList<string, Func<Ped, string>>();

        public readonly TupleList<string, string, Action<Ped, string>> CustomQuestionsAnswerWithCallbacks =
            new TupleList<string, string, Action<Ped, string>>();

        public bool StandardQuestionsEnabled = true;

        private static readonly List<List<string>> Questions = new List<List<string>>
        {
            new List<string>
            {
                "Do you have anything illegal in the vehicle?", "Anything in the vehicle that shouldn't be?",
                "Got anything illegal in your vehicle?"
            },
            new List<string>
                {"Have you been drinking?", "Have you had a drink today?", "Have you had a drink recently?"},
            new List<string>
            {
                "Have you done any illegal drugs recently?", "Have you taken any drugs in the past hours?",
                "Have you taken any drugs recently?"
            },
            new List<string>
            {
                "Can I search your vehicle?", "Would you mind if I searched your vehicle?",
                "Any objection to me searching your vehicle?"
            }
        };


        private static readonly List<List<string>> InnocentAnswers = new List<List<string>>
        {
            new List<string>
            {
                "Not that I know of, officer...", "Nope", "That's none of your business - I know my rights!",
                "Perhaps.", "You never know. Sometimes people borrow my car.", "No idea.", "Ummm, no?",
                "Of course not!", "Depends, I guess.", "No, why?", "Do I look like a criminal?"
            },
            new List<string>
            {
                "No", "Nope", "Only one.", "I don't need to answer that.", "I want my lawyer.",
                "Got nothing better to do? Of course not!", "Yup. Stay hydrated!"
            },
            new List<string>
                {"Nope", "Not recently, no.", "I'm not obliged to answer that.", "Am  I being detained?", "No, why?"},
            new List<string>
            {
                "No thanks!", "Well, sure, I guess.", "Why? No!", "Why are you messing with me? Go find real crime!",
                "You cops always take away my rights!", "Pff, hoping to find something? Have at it.", "I don't mind.",
                "I'd prefer it if you didn't.", "Sure, have at it.", "I don't have any issues with that. Go ahead.",
                "No. Can I search yours?"
            }
        };


        private static readonly List<List<string>> GuiltyAnswers = new List<List<string>>
        {
            new List<string>(),
            new List<string>
            {
                "Yes, I've had a few.", "Breathalyze me, then...", "Yarr, want some too?", "Just a few beers.",
                "Umm, no..?", "Well, I can't really remember...", "I don't think so, at least.",
                "Ughh...Such a headache...", "No.", "Nope.", "Yes. Got a problem with that?"
            },
            new List<string>
            {
                "Yes, officer.", "My lawyer! Now!", "I can't remember, officer...Ugh.",
                "My vision... Those... flowers... Hell no", "I'm high as hell!", "Nope", "Am I being detained?",
                "I'm in the clouds!", "Yes. Any problems with that?", "I don't need to answer that.",
                "What you do care?"
            },
            new List<string>()
        };

        private string _anythingIllegalInVehQuestion;

        public string AnythingIllegalInVehQuestion =>
            _anythingIllegalInVehQuestion ?? (_anythingIllegalInVehQuestion =
                Questions[0][LSPDFRPlusHandler.Rnd.Next(Questions[0].Count)]);

        private string _anythingIllegalInVehAnswer;

        public string AnythingIllegalInVehAnswer =>
            _anythingIllegalInVehAnswer ?? (_anythingIllegalInVehAnswer =
                InnocentAnswers[0][LSPDFRPlusHandler.Rnd.Next(InnocentAnswers[0].Count)]);

        private string _drinkingQuestion;

        public string DrinkingQuestion =>
            _drinkingQuestion ??
            (_drinkingQuestion = Questions[1][LSPDFRPlusHandler.Rnd.Next(Questions[1].Count)]);

        private string _drinkingAnswer;

        public string DrinkingAnswer
        {
            get
            {
                if (_drinkingAnswer != null) return _drinkingAnswer;
                if (LSPDFRPlusHandler.TrafficPolicerRunning && TrafficPolicerFuncs.IsPedOverAlcoholLimit(Suspect))
                {
                    _drinkingAnswer = GuiltyAnswers[1][LSPDFRPlusHandler.Rnd.Next(GuiltyAnswers[1].Count)];
                }
                else
                {
                    _drinkingAnswer = InnocentAnswers[1][LSPDFRPlusHandler.Rnd.Next(InnocentAnswers[1].Count)];
                }

                return _drinkingAnswer;
            }
        }

        private string _drugsQuestion;

        public string DrugsQuestion =>
            _drugsQuestion ??
            (_drugsQuestion = Questions[2][LSPDFRPlusHandler.Rnd.Next(Questions[2].Count)]);

        private string _drugsAnswer;

        public string DrugsAnswer
        {
            get
            {
                if (_drugsAnswer != null) return _drugsAnswer;
                if (LSPDFRPlusHandler.TrafficPolicerRunning && TrafficPolicerFuncs.IsPedOverDrugsLimit(Suspect))
                {
                    _drugsAnswer = GuiltyAnswers[2][LSPDFRPlusHandler.Rnd.Next(GuiltyAnswers[2].Count)];
                }
                else
                {
                    _drugsAnswer = InnocentAnswers[2][LSPDFRPlusHandler.Rnd.Next(InnocentAnswers[2].Count)];
                }

                return _drugsAnswer;
            }
        }

        private string _searchVehQuestion;

        public string SearchVehQuestion =>
            _searchVehQuestion ??
            (_searchVehQuestion = Questions[3][LSPDFRPlusHandler.Rnd.Next(Questions[3].Count)]);

        private string _searchVehAnswer;

        public string SearchVehAnswer =>
            _searchVehAnswer ?? (_searchVehAnswer =
                InnocentAnswers[3][LSPDFRPlusHandler.Rnd.Next(InnocentAnswers[3].Count)]);
    }

    internal class TrafficStopQuestionsInfo
    {
        public List<string> CustomReasons = new List<string>();
        public bool HideDefaultReasons = false;
    }
}