using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Albo1125.Common.CommonLibrary;
using LSPD_First_Response.Mod.API;
using Rage;
using Rage.Native;
using RAGENativeUI;
using RAGENativeUI.Elements;
using RAGENativeUI.PauseMenu;

namespace LSPDFR_
{
    internal static class Menus
    {
        //private static UIMenu ChecksMenu;

        
        //private static UIMenuItem CheckPlateItem;
        
        //private static UIMenuItem CheckCourtResultsItem;
        private static MenuPool _menuPool;

        //Speech, ID, ticket, warning, out of vehicle
        private static UIMenu _trafficStopMenu;
        private static UIMenuListItem _speechItem;
        private static UIMenuListItem _idItem;
        private static UIMenuItem _questionDriverItem;
        private static UIMenuItem _penaltyItem;
        private static UIMenuItem _warningItem;
        private static UIMenuListItem _outOfVehicleItem;
        private static readonly List<dynamic> OccupantSelector = new List<dynamic> { "Driver", "Passengers", "All occupants" };

        //public static UIMenuSwitchMenusItem MenuSwitchListItem;
        //private static UIMenu ActiveMenu = ChecksMenu;

        private static UIMenu _ticketMenu;
        private static UIMenuListItem _fineItem;
        private static readonly List<string> FineList = new List<string>();

        private static UIMenuListItem _pointsItem;
        private static readonly List<string> PointsList = new List<string>();

        //private static List<string> _defaultTicketReasonsList = new List<string>() { "Careless driving", "Speeding", "Mobile Phone", "Traffic light offence", "Illegal tyre", "Road Obstruction",
            //"No insurance", "Expired registration", "No seat belt", "Expired licence", "Unroadworthy vehicle", "Lane splitting", "No helmet", "Failure to yield", "Tailgating", "Unsecure load" };

        private static readonly UIMenuItem TicketOffenceSelectorItem = new UIMenuItem("Select Offences");


        private static UIMenuListItem _issueTicketItem;
        private static UIMenuCheckboxItem _seizeVehicleTicketCheckboxItem;
        

        private static UIMenu _questioningMenu;
        private static UIMenuItem _illegalInVehQuestionItem;
        private static UIMenuItem _drinkingQuestionItem;
        private static UIMenuItem _drugsQuestionItem;
        private static UIMenuItem _searchPermissionItem;

        private static readonly List<UIMenuItem> CustomQuestionsItems = new List<UIMenuItem>();
        private static readonly List<UIMenuItem> CustomQuestionsCallbacksAnswersItems = new List<UIMenuItem>();
        private static readonly List<UIMenuItem> CustomQuestionsAnswersCallbackItems = new List<UIMenuItem>();

        private static TabView _courtsMenu;

        public static TabSubmenuItem PendingResultsList;
        public static TabSubmenuItem PublishedResultsList;

        /*private static UIMenu _pursuitTacticsMenu;
        public static UIMenuCheckboxItem AutomaticTacticsCheckboxItem;
        public static UIMenuListItem PursuitTacticsListItem;
        private static readonly List<DisplayItem> PursuitTacticsOptionsList = new List<DisplayItem> { new DisplayItem("Safe"), new DisplayItem("Slightly Aggressive"), new DisplayItem("Full-out aggressive") };*/

        private static List<UIMenu> OffenceCategoryMenus = new List<UIMenu>();
        private static UIMenuSwitchMenusItem _offenceCategorySwitchItem;
        public static List<UIMenuCheckboxItem> Offences = new List<UIMenuCheckboxItem>();
        private static TupleList<UIMenuCheckboxItem, Offence> CheckboxItems_Offences = new TupleList<UIMenuCheckboxItem, Offence>();

        private static bool _trafficStopMenuEnabled = true;

        private static bool _standardQuestionsInMenu = true;

        private static void ToggleStandardQuestions(bool Enabled)
        {
            if (Enabled && !_standardQuestionsInMenu)
            {
                _questioningMenu.Clear();
                _questioningMenu.AddItem(_illegalInVehQuestionItem = new UIMenuItem(""));
                _questioningMenu.AddItem(_drinkingQuestionItem = new UIMenuItem(""));
                _questioningMenu.AddItem(_drugsQuestionItem = new UIMenuItem(""));
                _questioningMenu.AddItem(_searchPermissionItem = new UIMenuItem(""));
                _standardQuestionsInMenu = true;
            }

            else if (!Enabled && _standardQuestionsInMenu)
            {

                _questioningMenu.Clear();
                _standardQuestionsInMenu = false;

            }
        }

        private static void OpenOffencesMenu(UIMenu callingMenu, List<Offence> SelectedOffences)
        {            
            foreach (UIMenu men in OffenceCategoryMenus)
            {
                men.ParentMenu = callingMenu;         
                foreach (UIMenuItem it in men.MenuItems)
                {
                    if (it is UIMenuCheckboxItem)
                    {
                        ((UIMenuCheckboxItem)it).Checked = SelectedOffences.Contains(CheckboxItems_Offences.FirstOrDefault(x => x.Item1 == it)?.Item2);
                        
                    }
                }
            }
            OffenceCategoryMenus[0].Visible = true;
        }

        private static readonly List<TabItem> EmptyItems = new List<TabItem> { new TabItem(" ") };
        public static void InitialiseMenus()
        {
            Game.FrameRender += Process;
            _menuPool = new MenuPool();
            //ChecksMenu = new UIMenu("Checks", "");
            //_MenuPool.Add(ChecksMenu);
            _trafficStopMenu = new UIMenu("Traffic Stop", "LSPDFR+");
            _menuPool.Add(_trafficStopMenu);
            _ticketMenu = new UIMenu("Ticket", "");
            _menuPool.Add(_ticketMenu);

            /*_pursuitTacticsMenu = new UIMenu("Pursuit Tactics", "");
            _pursuitTacticsMenu.AddItem(AutomaticTacticsCheckboxItem = new UIMenuCheckboxItem("Automatic Tactics", EnhancedPursuitAI.DefaultAutomaticAi));
            _pursuitTacticsMenu.AddItem(PursuitTacticsListItem = new UIMenuListItem("Current Tactic", "", PursuitTacticsOptionsList));
            PursuitTacticsListItem.Enabled = false;
            _pursuitTacticsMenu.RefreshIndex();
            _pursuitTacticsMenu.OnItemSelect += OnItemSelect;
            _pursuitTacticsMenu.OnCheckboxChange += OnCheckboxChange;
            //TrafficStopMenu.OnListChange += OnListChange;
            _pursuitTacticsMenu.MouseControlsEnabled = false;
            _pursuitTacticsMenu.AllowCameraMovement = true;
            _menuPool.Add(_pursuitTacticsMenu);*/


            Dictionary<UIMenu, string> uiMenusCategories = new Dictionary<UIMenu, string>();
            foreach (string category in Offence.CategorizedTrafficOffences.Keys)
            {
                UIMenu newcategorymenu = new UIMenu(category, "LSPDFR+ offences");
                OffenceCategoryMenus.Add(newcategorymenu);
                uiMenusCategories.Add(newcategorymenu, category);

            }
            _offenceCategorySwitchItem = new UIMenuSwitchMenusItem("Categories", "", OffenceCategoryMenus);

            foreach (UIMenu newcategorymenu in OffenceCategoryMenus)
            {
                
                newcategorymenu.AddItem(_offenceCategorySwitchItem);
                string category = uiMenusCategories[newcategorymenu];
                foreach (string reason in Offence.CategorizedTrafficOffences[category].Select(x => x.name))
                {
                    UIMenuCheckboxItem newcheckboxitem = new UIMenuCheckboxItem(reason, false);
                    newcategorymenu.AddItem(newcheckboxitem);
                    CheckboxItems_Offences.Add(new Tuple<UIMenuCheckboxItem, Offence>(newcheckboxitem, Offence.CategorizedTrafficOffences[category].FirstOrDefault(x => x.name == reason)));
                }

                newcategorymenu.OnMenuClose += OnMenuClose;
                newcategorymenu.RefreshIndex();
                newcategorymenu.AllowCameraMovement = true;
                newcategorymenu.MouseControlsEnabled = false;
                _menuPool.Add(newcategorymenu);
            }



            var speech = new List<dynamic> { "Hello", "Insult", "Kifflom", "Thanks", "Swear", "Warn", "Threaten" };

            _trafficStopMenu.AddItem(_speechItem = new UIMenuListItem("Speech", "", speech));
            _trafficStopMenu.AddItem(_idItem = new UIMenuListItem("Ask for ID", "", OccupantSelector));
            _trafficStopMenu.AddItem(_questionDriverItem = new UIMenuItem("Question Driver"));
            _trafficStopMenu.AddItem(_penaltyItem = new UIMenuItem("Issue Penalty"));
            _trafficStopMenu.AddItem(_warningItem = new UIMenuItem("Issue Warning", "Let the driver go with words of advice."));
            _trafficStopMenu.AddItem(_outOfVehicleItem = new UIMenuListItem("Order out of Vehicle", "", OccupantSelector));

            _trafficStopMenu.RefreshIndex();
            _trafficStopMenu.OnItemSelect += OnItemSelect;

            _trafficStopMenu.MouseControlsEnabled = false;
            _trafficStopMenu.AllowCameraMovement = true;

            for (int i = 5; i<=Offence.MaxFine;i+=5)
            {
                FineList.Add(Offence.Currency + i);
            }

            for (int i = Offence.Minpoints; i <= Offence.Maxpoints; i += Offence.Pointincstep)
            {
                PointsList.Add(i.ToString());
            }
            _ticketMenu.AddItem(TicketOffenceSelectorItem);
            _ticketMenu.AddItem(_fineItem = new UIMenuListItem("Fine", "", FineList));

            _pointsItem = new UIMenuListItem("Points", "", PointsList);
            if (Offence.EnablePoints)
            {
                _ticketMenu.AddItem(_pointsItem);
            }
            
            //TicketMenu.AddItem(TicketReasonsListItem = new UIMenuListItem("Offence", TicketReasonsList, 0));            
            _ticketMenu.AddItem(_seizeVehicleTicketCheckboxItem = new UIMenuCheckboxItem("Seize Vehicle", false));
            List<dynamic> penaltyOptions = new List<dynamic> { "Ticket", "Court Summons" };
            _ticketMenu.AddItem(_issueTicketItem = new UIMenuListItem("~h~Issue ", "", penaltyOptions));
            _issueTicketItem.OnListChanged += OnIndexChange;
            _ticketMenu.ParentMenu = _trafficStopMenu;
            _ticketMenu.RefreshIndex();
            _ticketMenu.OnItemSelect += OnItemSelect;

            _ticketMenu.MouseControlsEnabled = false;
            _ticketMenu.AllowCameraMovement = true;
            _ticketMenu.SetMenuWidthOffset(80);


            _questioningMenu = new UIMenu("Questioning", "");
            _menuPool.Add(_questioningMenu);
            _questioningMenu.AddItem(_illegalInVehQuestionItem = new UIMenuItem(""));
            _questioningMenu.AddItem(_drinkingQuestionItem = new UIMenuItem(""));
            _questioningMenu.AddItem(_drugsQuestionItem = new UIMenuItem(""));
            _questioningMenu.AddItem(_searchPermissionItem = new UIMenuItem(""));
            _questioningMenu.ParentMenu = _trafficStopMenu;
            _questioningMenu.RefreshIndex();
            _questioningMenu.OnItemSelect += OnItemSelect;

            _questioningMenu.MouseControlsEnabled = false;
            _questioningMenu.AllowCameraMovement = true;
            _questioningMenu.SetMenuWidthOffset(120);

            _courtsMenu = new TabView("~b~~h~San Andreas Court");


            
            _courtsMenu.AddTab(PendingResultsList = new TabSubmenuItem("Pending Results", EmptyItems));
            _courtsMenu.AddTab(PublishedResultsList = new TabSubmenuItem("Results", EmptyItems));

            _courtsMenu.RefreshIndex();

            MainLogic();
        }

        private static void UpdatePenaltyType(int index)
        {
            if (_issueTicketItem.Collection[index].Value.ToString().Contains("Court"))
            {
                _fineItem.Description = "The estimated fine. A judge will decide on the final penalty.";
                _pointsItem.Description = "The estimated number of points. A judge will decide on the final penalty.";
                _pointsItem.Enabled = false;
                _fineItem.Enabled = false;

            }
            else
            {
                _fineItem.Description = "";
                _pointsItem.Description = "";
                _pointsItem.Enabled = true;
                _fineItem.Enabled = true;
            }
        }
/*        private static void OnCheckboxChange(UIMenu sender, UIMenuCheckboxItem changeditem, bool check)
        {
            if (sender != _pursuitTacticsMenu || changeditem != AutomaticTacticsCheckboxItem) return;
            EnhancedPursuitAI.AutomaticAi = check;
            PursuitTacticsListItem.Enabled = !check;
        }*/

        private static void OnIndexChange(UIMenuItem changeditem, int index)
        {
            if (changeditem == _issueTicketItem)
            {
                UpdatePenaltyType(index);
            }
        }
        private static void OnItemSelect(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            //if (sender == ChecksMenu)
            //{
                
            //    if (selectedItem == CheckCourtResultsItem)
            //    {
            //        sender.Visible = false;
            //        CourtsMenu.Visible = true;

            //    }
            //}
            if (sender == _trafficStopMenu)
            {
                if (selectedItem == _speechItem)
                {
                    string speech = _speechItem.Collection[_speechItem.Index].Value.ToString();
                    EnhancedTrafficStop.PlaySpecificSpeech(speech);

                }
                else if (selectedItem == _idItem)
                {
                    //Ask for ID

                    _currentEnhancedTrafficStop.AskForId((EnhancedTrafficStop.OccupantSelector)_idItem.Index);

                }
                else if (selectedItem == _questionDriverItem)
                {
                    sender.Visible = false;

                    UpdateTrafficStopQuestioning();
                    _questioningMenu.Visible = true;
                }
                else if (selectedItem == _penaltyItem)
                {
                    //Issue ticket(bind menu to item)?
                    sender.Visible = false;
                    //Menus.UpdateTicketReasons();
                    UpdatePenaltyType(_issueTicketItem.Index);
                    _ticketMenu.Visible = true;
                    
                }
                else if (selectedItem == _warningItem)
                {
                    //Let driver go
                    EnhancedTrafficStop.IssueWarning();
                    _menuPool.CloseAllMenus();
                }
                else if (selectedItem == _outOfVehicleItem)
                {
                    //Order driver out
                    _currentEnhancedTrafficStop.OutOfVehicle((EnhancedTrafficStop.OccupantSelector)_outOfVehicleItem.Index);
                    _menuPool.CloseAllMenus();
                }
            }

            else if (sender == _ticketMenu)
            {
                if (selectedItem == _issueTicketItem)
                {
                    //Issue TOR 
                                  
                    bool seizeVehicle = _seizeVehicleTicketCheckboxItem.Checked;
                    if (Functions.IsPlayerPerformingPullover())
                    {
                        _currentEnhancedTrafficStop.IssueTicket(seizeVehicle);
                    }
                    else
                    {
                        GameFiber.StartNew(EnhancedTrafficStop.PerformTicketAnimation);
                    }

                    _menuPool.CloseAllMenus();
                }
                else if (selectedItem == TicketOffenceSelectorItem)
                {
                    sender.Visible = false;
                    OpenOffencesMenu(sender, _currentEnhancedTrafficStop.SelectedOffences);
                }
            }
            else if (sender == _questioningMenu)
            {
                if (selectedItem == _illegalInVehQuestionItem)
                {
                    Game.DisplaySubtitle("~h~" + _currentEnhancedTrafficStop.AnythingIllegalInVehAnswer);
                }
                else if (selectedItem == _drinkingQuestionItem)
                {
                    Game.DisplaySubtitle("~h~" + _currentEnhancedTrafficStop.DrinkingAnswer);
                }
                else if (selectedItem == _drugsQuestionItem)
                {
                    Game.DisplaySubtitle("~h~" + _currentEnhancedTrafficStop.DrugsAnswer);
                }
                else if (selectedItem == _searchPermissionItem)
                {
                    Game.DisplaySubtitle("~h~" + _currentEnhancedTrafficStop.SearchVehAnswer);
                }
                else if (CustomQuestionsItems.Contains(selectedItem))
                {
                    Game.DisplaySubtitle("~h~" + _currentEnhancedTrafficStop.CustomQuestionsWithAnswers[CustomQuestionsItems.IndexOf(selectedItem)].Item2);
                }
                else if (CustomQuestionsCallbacksAnswersItems.Contains(selectedItem))
                {
                    Game.DisplaySubtitle("~h~" + _currentEnhancedTrafficStop.CustomQuestionsWithCallbacksAnswers[CustomQuestionsCallbacksAnswersItems.IndexOf(selectedItem)].Item2(_currentEnhancedTrafficStop.Suspect));
                }
                else if (CustomQuestionsAnswersCallbackItems.Contains(selectedItem))
                {
                    string text = _currentEnhancedTrafficStop.CustomQuestionsAnswerWithCallbacks[CustomQuestionsAnswersCallbackItems.IndexOf(selectedItem)].Item2;
                    Game.DisplaySubtitle("~h~" + text);

                    _currentEnhancedTrafficStop.CustomQuestionsAnswerWithCallbacks[CustomQuestionsAnswersCallbackItems.IndexOf(selectedItem)].Item3(_currentEnhancedTrafficStop.Suspect, text);
                }
            }
        }

        private static void OnMenuClose(UIMenu sender)
        {
            if (OffenceCategoryMenus.Contains(sender))
            {
                _currentEnhancedTrafficStop.SelectedOffences.Clear();
                foreach (UIMenu men in OffenceCategoryMenus)
                {
                    foreach (UIMenuItem it in men.MenuItems)
                    {
                        if (!(it is UIMenuCheckboxItem)) continue;
                        if (((UIMenuCheckboxItem)it).Checked)
                        {
                            _currentEnhancedTrafficStop.SelectedOffences.Add(CheckboxItems_Offences.FirstOrDefault(x => x.Item1 == it)?.Item2);
                        }
                    }
                }
                int fine = _currentEnhancedTrafficStop.SelectedOffences.Sum(x => x.fine);
                fine = fine - (fine % 5);
                if (fine >  5000) { fine = 5000; }
                else if (fine < 5) { fine = 5; }

                _fineItem.Index = fine / 5 - 1;
                int points = _currentEnhancedTrafficStop.SelectedOffences.Sum(x => x.points);
                points = points - (points % Offence.Pointincstep);
                if (points > Offence.Maxpoints) { points = Offence.Maxpoints; }
                else if (points < Offence.Minpoints) { points = Offence.Minpoints; }
                _pointsItem.Index = PointsList.IndexOf(points.ToString());
                _seizeVehicleTicketCheckboxItem.Checked = _currentEnhancedTrafficStop.SelectedOffences.Any(x => x.seizeVehicle);

            }
        }

        private static void MainLogic()
        {
            GameFiber.StartNew(delegate
            {
                try
                {
                    while (true)
                    {
                        GameFiber.Yield();
/*                        if (EnhancedPursuitAI.InPursuit && Game.LocalPlayer.Character.IsInAnyVehicle(false))
                        {
                            if (ExtensionMethods.IsKeyCombinationDownComputerCheck(EnhancedPursuitAI.OpenPursuitTacticsMenuKey, EnhancedPursuitAI.OpenPursuitTacticsMenuModifierKey))
                            {
                                _pursuitTacticsMenu.Visible = !_pursuitTacticsMenu.Visible;
                            }
                        }
                        else
                        {
                            _pursuitTacticsMenu.Visible = false;
                        }*/

                        if (Functions.IsPlayerPerformingPullover())
                        {
                            if (Functions.GetPulloverSuspect(Functions.GetCurrentPullover()) != _currentEnhancedTrafficStop.Suspect)
                            {
                                _currentEnhancedTrafficStop = new EnhancedTrafficStop();

                                //StatisticsCounter.AddCountToStatistic("Traffic Stops", "LSPDFR+");
                                Game.LogTrivial("Adding traffic stop count - LSPDFR+");
                                API.Functions.OnTrafficStopInitiated(Functions.GetPulloverSuspect(Functions.GetCurrentPullover()));

                            }
                        }
                        //Shift Q ticket menu handler.
                        else if (!_menuPool.IsAnyMenuOpen() && !Game.LocalPlayer.Character.IsInAnyVehicle(false) && ExtensionMethods.IsKeyCombinationDownComputerCheck(Offence.OpenTicketMenuKey, Offence.OpenTicketMenuModifierKey)
                        && Game.LocalPlayer.Character.GetNearbyPeds(1)[0].Exists() && Game.LocalPlayer.Character.DistanceTo(Game.LocalPlayer.Character.GetNearbyPeds(1)[0]) < 5f)
                        {

                            Game.LocalPlayer.Character.Tasks.ClearImmediately();
                            _menuPool.ResetMenus(true, true);
                            _currentEnhancedTrafficStop.SelectedOffences.Clear();
                            _seizeVehicleTicketCheckboxItem.Enabled = false;
                            _ticketMenu.ParentMenu = null;
                            foreach (UIMenu m in OffenceCategoryMenus)
                            {
                                m.Visible = false;
                            }
                            _ticketMenu.Visible = true;
                        }

                        if (ExtensionMethods.IsKeyDownComputerCheck(CourtSystem.OpenCourtMenuKey) && (ExtensionMethods.IsKeyDownRightNowComputerCheck(CourtSystem.OpenCourtMenuModifierKey) || CourtSystem.OpenCourtMenuModifierKey == Keys.None))
                        {
                            if (!_courtsMenu.Visible) { _courtsMenu.Visible = true; }
                        }

                        if (_menuPool.IsAnyMenuOpen()) { NativeFunction.Natives.SET_PED_STEALTH_MOVEMENT(Game.LocalPlayer.Character, 0, 0); }

                        //Prevent the traffic stop menu from being used when it shouldn't be.
                        if (_trafficStopMenu.Visible)
                        {                           
                            if (!Functions.IsPlayerPerformingPullover())
                            {
                                if (_trafficStopMenuEnabled)
                                {
                                    ToggleUiMenuEnabled(_trafficStopMenu, false);
                                    _trafficStopMenuEnabled = false;
                                }
                            }
                            else if (Vector3.Distance2D(Game.LocalPlayer.Character.Position, Functions.GetPulloverSuspect(Functions.GetCurrentPullover()).Position) > TrafficStopMenuDistance)
                            {
                                if (_trafficStopMenuEnabled)
                                {
                                    ToggleUiMenuEnabled(_trafficStopMenu, false);
                                    _trafficStopMenuEnabled = false;
                                }
                            }
                            else if (!_trafficStopMenuEnabled)
                            {
                                ToggleUiMenuEnabled(_trafficStopMenu, true);
                                _trafficStopMenuEnabled = true;
                            }
                        }

                        if (_courtsMenu.Visible)
                        {

                            if (!_courtsMenuPaused)
                            {
                                _courtsMenuPaused = true;
                                Game.IsPaused = true;
                            }
                            if (ExtensionMethods.IsKeyDownComputerCheck(Keys.Delete))
                            {
                                if (PendingResultsList.Active)
                                {
                                    if (CourtCase.PendingResultsMenuCleared)
                                    {
                                        CourtSystem.DeleteCourtCase(CourtSystem.PendingCourtCases[PendingResultsList.Index]);
                                        PendingResultsList.Index = 0;
                                    }
                                }
                                else if (PublishedResultsList.Active)
                                {
                                    if (CourtCase.ResultsMenuCleared)
                                    {
                                        CourtSystem.DeleteCourtCase(CourtSystem.PublishedCourtCases[PublishedResultsList.Index]);

                                        PublishedResultsList.Index = 0;
                                    }
                                }
                            }

                            if (!ExtensionMethods.IsKeyDownComputerCheck(Keys.Insert)) continue;
                            if (!PendingResultsList.Active) continue;
                            if (!CourtCase.PendingResultsMenuCleared) continue;
                            CourtSystem.PendingCourtCases[PendingResultsList.Index].ResultsPublishTime = DateTime.Now;
                            PendingResultsList.Index = 0;
                        }
                        else if (_courtsMenuPaused)
                        {
                            _courtsMenuPaused = false;
                            Game.IsPaused = false;
                        }

                    }
                }
                catch (ThreadAbortException) { }
                catch (Exception e) { Game.LogTrivial(e.ToString()); }
            });
        }

        //Huge method to handle the traffic stop questioning layout.
        public static void UpdateTrafficStopQuestioning()
        {
            if (!Functions.IsPlayerPerformingPullover()) return;
            _currentEnhancedTrafficStop.UpdateTrafficStopQuestioning();
            ToggleStandardQuestions(_currentEnhancedTrafficStop.StandardQuestionsEnabled);
            if (_currentEnhancedTrafficStop.StandardQuestionsEnabled)
            {
                _illegalInVehQuestionItem.Text = _currentEnhancedTrafficStop.AnythingIllegalInVehQuestion;
                _drinkingQuestionItem.Text = _currentEnhancedTrafficStop.DrinkingQuestion;
                _drugsQuestionItem.Text = _currentEnhancedTrafficStop.DrugsQuestion;
                _searchPermissionItem.Text = _currentEnhancedTrafficStop.SearchVehQuestion;
            }
            if (CustomQuestionsItems.Count > 0)
            {
                foreach (UIMenuItem item in _questioningMenu.MenuItems.ToArray())
                {
                    if (CustomQuestionsItems.Contains(item))
                    {
                        _questioningMenu.RemoveItemAt(_questioningMenu.MenuItems.IndexOf(item));
                    }
                }
                CustomQuestionsItems.Clear();
            }
            foreach (Tuple<string, string> tuple in _currentEnhancedTrafficStop.CustomQuestionsWithAnswers)
            {
                UIMenuItem customquestionitem = new UIMenuItem(tuple.Item1);
                _questioningMenu.AddItem(customquestionitem);
                CustomQuestionsItems.Add(customquestionitem);

            }
            if (CustomQuestionsCallbacksAnswersItems.Count > 0)
            {
                foreach (UIMenuItem item in _questioningMenu.MenuItems.ToArray())
                {
                    if (CustomQuestionsCallbacksAnswersItems.Contains(item))
                    {
                        _questioningMenu.RemoveItemAt(_questioningMenu.MenuItems.IndexOf(item));
                    }
                }
                CustomQuestionsCallbacksAnswersItems.Clear();
            }
            foreach (Tuple<string, Func<Ped, string>> tuple in _currentEnhancedTrafficStop.CustomQuestionsWithCallbacksAnswers)
            {
                UIMenuItem customquestionitem = new UIMenuItem(tuple.Item1);
                _questioningMenu.AddItem(customquestionitem);
                CustomQuestionsCallbacksAnswersItems.Add(customquestionitem);

            }

            if (CustomQuestionsAnswersCallbackItems.Count > 0)
            {
                foreach (UIMenuItem item in _questioningMenu.MenuItems.ToArray())
                {
                    if (CustomQuestionsAnswersCallbackItems.Contains(item))
                    {
                        _questioningMenu.RemoveItemAt(_questioningMenu.MenuItems.IndexOf(item));
                    }
                }
                CustomQuestionsAnswersCallbackItems.Clear();
            }
            foreach (Tuple<string,string, Action<Ped, string>> tuple in _currentEnhancedTrafficStop.CustomQuestionsAnswerWithCallbacks)
            {
                UIMenuItem customquestionitem = new UIMenuItem(tuple.Item1);
                _questioningMenu.AddItem(customquestionitem);
                CustomQuestionsAnswersCallbackItems.Add(customquestionitem);

            }
        }
        public static float TrafficStopMenuDistance = 3.7f;
        private static EnhancedTrafficStop _currentEnhancedTrafficStop = new EnhancedTrafficStop();
        private static bool _courtsMenuPaused;
        private static void Process(object sender, GraphicsEventArgs e)
        {
            try
            {
                if (Functions.IsPlayerPerformingPullover() && !_menuPool.IsAnyMenuOpen() && EnhancedTrafficStop.EnhancedTrafficStopsEnabled)
                {
                    Ped pulloverSuspect = Functions.GetPulloverSuspect(Functions.GetCurrentPullover());
                    if (pulloverSuspect &&
                        pulloverSuspect.IsInAnyVehicle(false) &&
                        Vector3.Distance2D(Game.LocalPlayer.Character.Position, pulloverSuspect.CurrentVehicle.Position) < TrafficStopMenuDistance + 0.1f
                        )
                    {
                        //ExtensionNamespace.Extensions.DisEnableControls(false);
                        //ExtensionNamespace.Extensions.DisableTrafficStopControls();

                        if (ExtensionMethods.IsKeyDownComputerCheck(EnhancedTrafficStop.BringUpTrafficStopMenuKey) || Game.IsControllerButtonDown(EnhancedTrafficStop.BringUpTrafficStopMenuControllerButton))
                        {
                            _menuPool.ResetMenus(true, true);
                            _seizeVehicleTicketCheckboxItem.Enabled = true;
                            _ticketMenu.ParentMenu = _trafficStopMenu;
                            _ticketMenu.Visible = false;
                            foreach (UIMenu m in OffenceCategoryMenus)
                            {
                                m.Visible = false;
                            }
                            _trafficStopMenu.Visible = true;

                        }
                    }
                }

                _menuPool.ProcessMenus();
                if (!_courtsMenu.Visible) return;
                Game.IsPaused = true;
                _courtsMenu.Update();
            }
            catch (Exception exception)
            {
                Game.LogTrivial($"Handled {exception}");
            }
           
        }

        private static void ToggleUiMenuEnabled(UIMenu menu, bool Enabled)
        {

            foreach (UIMenuItem item in menu.MenuItems)
            {
                item.Enabled = Enabled;               
            }

        }
    }
}
