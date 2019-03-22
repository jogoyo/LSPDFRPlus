using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using Albo1125.Common.CommonLibrary;
using LSPD_First_Response.Engine.Scripting.Entities;
using Rage;
using RAGENativeUI.PauseMenu;

namespace LSPDFR_
{
    internal static class CourtSystem
    {
        public static Keys OpenCourtMenuKey = Keys.F9;
        public static Keys OpenCourtMenuModifierKey = Keys.None;
        public static readonly List<CourtCase> PendingCourtCases = new List<CourtCase>();
        public static readonly List<CourtCase> PublishedCourtCases = new List<CourtCase>();
        private const string CourtCaseFilePath = "Plugins/LSPDFR/LSPDFR+/CourtCases.xml";
        public static bool LoadingXmlFileCases = true;
        public static bool RealisticCourtDates = true;

        public static void CourtSystemMainLogic()
        {
            GameFiber.StartNew(delegate
            {
                Directory.CreateDirectory(Directory.GetParent(CourtCaseFilePath).FullName);
                if (!File.Exists(CourtCaseFilePath))
                {
                    new XDocument(
                            new XElement("LSPDFRPlus")
                        )
                        .Save(CourtCaseFilePath);
                }

                LoadCourtCasesFromXmlFile(CourtCaseFilePath);
                while (true)
                {
                    GameFiber.Yield();
                    foreach (CourtCase ccase in PendingCourtCases.ToArray())
                    {
                        ccase.CheckForCaseUpdatedStatus();
                    }
                }
            });
        }

        private static void LoadCourtCasesFromXmlFile(string File)
        {
            try
            {
                XDocument xdoc = XDocument.Load(File);
                char[] trim = {'\'', '\"', ' '};
                List<CourtCase> allCourtCases = xdoc.Descendants("CourtCase").Select(x => new CourtCase
                {
                    SuspectName = x.Element("SuspectName")?.Value.Trim(trim),
                    SuspectDob = DateTime.FromBinary(long.Parse(x.Element("SuspectDOB")?.Value)),
                    Crime = x.Element("Crime")?.Value.Trim(trim),
                    CrimeDate = DateTime.FromBinary(long.Parse(x.Element("CrimeDate")?.Value)),
                    GuiltyChance = int.Parse(x.Element("GuiltyChance") != null
                        ? x.Element("GuiltyChance")?.Value.Trim(trim)
                        : "100"),
                    CourtVerdict = x.Element("CourtVerdict")?.Value.Trim(trim),
                    ResultsPublishTime = DateTime.FromBinary(long.Parse(x.Element("ResultsPublishTime")?.Value)),
                    ResultsPublished = bool.Parse(x.Element("Published")?.Value.Trim(trim)),
                    ResultsPublishedNotificationShown =
                        bool.Parse(x.Element("ResultsPublishedNotificationShown")?.Value.Trim(trim))
                }).ToList();

                foreach (CourtCase courtcase in allCourtCases)
                {
                    courtcase.AddToCourtsMenuAndLists();
                }
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception e)
            {
                Game.LogTrivial("LSPDFR+ encountered an exception reading \'" + File + "\'. It was: " + e);
                Game.DisplayNotification("~r~LSPDFR+: Error reading CourtCases.xml. Setting default values.");
            }
            finally
            {
                LoadingXmlFileCases = false;
            }
        }

        public static void OverwriteCourtCase(CourtCase CourtCase)
        {
            DeleteCourtCaseFromXmlFile(CourtCaseFilePath, CourtCase);
            AddCourtCaseToXmlFile(CourtCaseFilePath, CourtCase);
        }

        public static void DeleteCourtCase(CourtCase CourtCase)
        {
            DeleteCourtCaseFromXmlFile(CourtCaseFilePath, CourtCase);
            if (PublishedCourtCases.Contains(CourtCase))
            {
                if (Menus.PublishedResultsList.Items.Count == 1)
                {
                    Menus.PublishedResultsList.Items.Add(new TabItem(" "));
                    CourtCase.ResultsMenuCleared = false;
                }

                Menus.PublishedResultsList.Items.RemoveAt(PublishedCourtCases.IndexOf(CourtCase));
                PublishedCourtCases.Remove(CourtCase);
            }

            if (PendingCourtCases.Contains(CourtCase))
            {
                if (Menus.PendingResultsList.Items.Count == 1)
                {
                    Menus.PendingResultsList.Items.Add(new TabItem(" "));
                    CourtCase.PendingResultsMenuCleared = false;
                }

                Menus.PendingResultsList.Items.RemoveAt(PendingCourtCases.IndexOf(CourtCase));
                PendingCourtCases.Remove(CourtCase);
            }
        }

        private static void AddCourtCaseToXmlFile(string File, CourtCase ccase)
        {
            try
            {
                XDocument xdoc = XDocument.Load(File);
                XElement lspdfrPlusElement = xdoc.Element("LSPDFRPlus");
                XElement ccaseElement = new XElement("CourtCase",
                    new XAttribute("ID", ccase.XmlIdentifier),
                    new XElement("SuspectName", ccase.SuspectName),
                    new XElement("SuspectDOB", ccase.SuspectDob.ToBinary()),
                    new XElement("Crime", ccase.Crime),
                    new XElement("CrimeDate", ccase.CrimeDate.ToBinary()),
                    new XElement("GuiltyChance", ccase.GuiltyChance.ToString()),
                    new XElement("CourtVerdict", ccase.CourtVerdict),
                    new XElement("ResultsPublishTime", ccase.ResultsPublishTime.ToBinary()),
                    new XElement("Published", ccase.ResultsPublished.ToString()),
                    new XElement("ResultsPublishedNotificationShown",
                        ccase.ResultsPublishedNotificationShown.ToString()));
                lspdfrPlusElement?.Add(ccaseElement);
                xdoc.Save(File);
            }
            catch (Exception e)
            {
                Game.LogTrivial("LSPDFR+ encountered an exception writing a court case to \'" + File + "\'. It was: " +
                                e);
                Game.DisplayNotification("~r~LSPDFR+: Error while working with CourtCases.xml.");
            }
        }

        private static void DeleteCourtCaseFromXmlFile(string File, CourtCase ccase)
        {
            try
            {
                XDocument xdoc = XDocument.Load(File);
                char[] trim = {'\'', '\"', ' '};
                List<XElement> courtCasesToBeDeleted = (from x in xdoc.Descendants("CourtCase")
                    where (((string) x.Attribute("ID")).Trim(trim) == ccase.XmlIdentifier)
                    select x).ToList();

                if (courtCasesToBeDeleted.Count > 0)
                {
                    foreach (XElement ele in courtCasesToBeDeleted)
                    {
                        ele.Remove();
                    }
                }

                xdoc.Save(File);
            }
            catch (Exception e)
            {
                Game.LogTrivial("LSPDFR+ encountered an exception deleting an element from \'" + File + "\'. It was: " +
                                e);
                Game.DisplayNotification("~r~LSPDFR+: Error while working with CourtCases.xml.");
            }
        }

        private static void CreateNewCourtCase(string SuspectName, DateTime SuspectDob, string Crime,
            DateTime CrimeDate, int GuiltyChance, string CourtVerdict, DateTime ResultsPublishTime, bool Published,
            bool ResultsPublishedNotificationShown)
        {
            CourtCase courtcase = new CourtCase(SuspectName, SuspectDob, Crime, CrimeDate, GuiltyChance, CourtVerdict,
                ResultsPublishTime, Published, ResultsPublishedNotificationShown);
            //Game.LogTrivial("ResultsPublishTime: " + ResultsPublishTime.ToString());
            AddCourtCaseToXmlFile(CourtCaseFilePath, courtcase);
            courtcase.AddToCourtsMenuAndLists();
        }

        public static void CreateNewCourtCase(string SuspectName, DateTime SuspectDob, string Crime, DateTime CrimeDate,
            int GuiltyChance, string CourtVerdict, DateTime ResultsPublishTime, bool Published)
        {
            CreateNewCourtCase(SuspectName, SuspectDob, Crime, CrimeDate, GuiltyChance, CourtVerdict,
                ResultsPublishTime, Published, false);
        }

        public static void CreateNewCourtCase(Persona Defendant, string Crime, int GuiltyChance, string CourtVerdict)
        {
            CreateNewCourtCase(Defendant.FullName, Defendant.Birthday, Crime, DateTime.Now, GuiltyChance, CourtVerdict,
                DetermineCourtHearingDate(), false);
        }
        //public static void CreateNewCourtCase(Persona Defendant, EnhancedTrafficStop.TrafficOffences TrafficOffence)
        //{
        //    bool GuiltyPlea = DeterminePleadGuilty();
        //    string crime = EnhancedTrafficStop.TrafficOffencesStrings[(int)TrafficOffence];
        //    string sentence = DetermineTrafficOffenceSentence(Defendant, TrafficOffence, GuiltyPlea);
        //    DateTime Hearingdate = DetermineCourtHearingDate();
        //    string fullname = Defendant.FullName;
        //    DateTime birthday = Defendant.BirthDay;
        //    DateTime now = DateTime.Now;


        //    CreateNewCourtCase(fullname, Defendant.BirthDay, crime, now, GuiltyPlea, sentence, Hearingdate, false, false);
        //}

        public static string DeterminePrisonSentence(int MinMonths, int MaxMonths, int SuspendedChance)
        {
            string verdict;

            float months = LSPDFRPlusHandler.Rnd.Next(MinMonths, MaxMonths + 1);
            if (months < 1)
            {
                verdict = "Discharged.";
            }
            else
            {
                string penaltyUnits;
                if (months % 12 == 0 || months % 12 == 6)
                {
                    months = months / 12;
                    penaltyUnits = "years";
                }
                else
                {
                    penaltyUnits = "months";
                }

                verdict = "Sentenced to " + months + " " + penaltyUnits + " in prison";

                if (LSPDFRPlusHandler.Rnd.Next(100) < SuspendedChance)
                {
                    verdict += ", suspended for " + LSPDFRPlusHandler.Rnd.Next(12, 25) + " months.";
                }
            }


            return verdict;
        }

        public static string DetermineFineSentence(int MinFine, int MaxFine)
        {
            int fine = (int) Math.Round(LSPDFRPlusHandler.Rnd.Next(MinFine, MaxFine + 1) / 5.0f) * 5;
            if (fine < MinFine)
            {
                fine = MinFine;
            }
            else if (fine > MaxFine)
            {
                fine = MaxFine;
            }

            return "Fined $" + fine + ".";
        }


        //public static string DetermineTrafficOffenceSentence(Persona Defendant, EnhancedTrafficStop.TrafficOffences Offence, bool PleadedGuilty)
        //{
        //    string s = "";
        //    if (Offence == EnhancedTrafficStop.TrafficOffences.RoadObstruction || Offence == EnhancedTrafficStop.TrafficOffences.SeatBelt)
        //    {
        //        int Fine = (int)Math.Round(((float)EntryPoint.rnd.Next(80, 180)) / 5.0f) * 5;
        //        int Costs = (int)Math.Round(((float)EntryPoint.rnd.Next(86)) / 5.0f) * 5;
        //        s = "Fined " + Fine.ToString() + " pounds with " + Costs.ToString() + " pounds costs.";
        //    }
        //    else if (Offence == EnhancedTrafficStop.TrafficOffences.DangerousDriving)
        //    {
        //        int Fine = (int)Math.Round(((float)EntryPoint.rnd.Next(100, 300)) / 5.0f) * 5;
        //        int Costs = (int)Math.Round(((float)EntryPoint.rnd.Next(161)) / 5.0f) * 5;
        //        int MonthsBanned = EntryPoint.rnd.Next(12, 23);

        //        s = "Fined " + Fine.ToString() + " pounds with " + Costs.ToString() + " pounds costs. Disqualified from driving for " + MonthsBanned + " months.";

        //        if (EntryPoint.rnd.Next(6) < 3)
        //        {
        //            int HoursUnpaidWork = (int)Math.Round(((float)EntryPoint.rnd.Next(50, 200)) / 5.0f) * 5;
        //            s += " Community order made with " + HoursUnpaidWork.ToString() + " hours unpaid work.";
        //        }
        //    }
        //    else if (Offence == EnhancedTrafficStop.TrafficOffences.Speeding || Offence == EnhancedTrafficStop.TrafficOffences.MobilePhone || Offence == EnhancedTrafficStop.TrafficOffences.CarelessDriving || Offence == EnhancedTrafficStop.TrafficOffences.TrafficLight
        //         || Offence == EnhancedTrafficStop.TrafficOffences.DangerousVehicleCondition || Offence == EnhancedTrafficStop.TrafficOffences.IllegalTyre)
        //    {
        //        int Fine = (int)Math.Round(((float)EntryPoint.rnd.Next(70, 350)) / 5.0f) * 5;
        //        int Costs = (int)Math.Round(((float)EntryPoint.rnd.Next(86)) / 5.0f) * 5;
        //        int Points = EntryPoint.rnd.Next(3, 7);

        //        s = "Fined " + Fine.ToString() + " pounds with " + Costs.ToString() + " pounds costs. Driving licence endorsed with " + Points.ToString() + " points.";

        //        if (Defendant.PenaltyPoints + Points >= 12)
        //        {
        //            int MonthsDisqualified = EntryPoint.rnd.Next(6, 13);
        //            s += " Disqualified for " + MonthsDisqualified.ToString() + " months for totting up.";
        //        }

        //    }


        //    else if (Offence == EnhancedTrafficStop.TrafficOffences.NoInsurance)
        //    {
        //        int Fine = (int)Math.Round(((float)EntryPoint.rnd.Next(200, 600)) / 5.0f) * 5;
        //        int Costs = (int)Math.Round(((float)EntryPoint.rnd.Next(165)) / 5.0f) * 5;
        //        int Points = 6;

        //        s = "Fined " + Fine.ToString() + " pounds with " + Costs.ToString() + " pounds costs. Driving licence endorsed with " + Points.ToString() + " points.";

        //        if (Defendant.PenaltyPoints + Points >= 12)
        //        {
        //            int MonthsDisqualified = EntryPoint.rnd.Next(6, 13);
        //            s += " Disqualified for " + MonthsDisqualified.ToString() + " months for totting up.";
        //        }

        //    }

        //    return s;
        //}

        public static DateTime DetermineCourtHearingDate()
        {
            if (!RealisticCourtDates) return DateTime.Now.AddMinutes(LSPDFRPlusHandler.Rnd.Next(2, 10));
            DateTime courtDate = DateTime.Now;
            int minutes = (int) Math.Round(courtDate.Minute / 5.0f) * 5;
            while (courtDate.Minute != minutes)
            {
                courtDate = courtDate.AddMinutes(1);
                minutes = (int) Math.Round(courtDate.Minute / 5.0f) * 5;
            }

            while (courtDate.Hour > 17 || courtDate.Hour < 9)
            {
                courtDate = courtDate.AddHours(LSPDFRPlusHandler.Rnd.Next(1, 8));
            }


            courtDate = courtDate.AddDays(LSPDFRPlusHandler.Rnd.Next(1, 4));
            //Game.LogTrivial("DeterminedResultsPublishTime: " + CourtDate.ToString());

            return courtDate;

        }
    }

    internal class CourtCase
    {
        public string SuspectName;
        public DateTime SuspectDob;
        public string Crime;
        public DateTime CrimeDate;
        public int GuiltyChance;
        public string CourtVerdict;
        public DateTime ResultsPublishTime;
        public bool ResultsPublished;
        public bool ResultsPublishedNotificationShown;

        public static bool PendingResultsMenuCleared;
        public static bool ResultsMenuCleared;

        public string XmlIdentifier => SuspectName + (SuspectDob.ToBinary()) + Crime + (CrimeDate.ToBinary());

        private string MenuLabel(bool NewLine)
        {
            string s = "~r~" + SuspectName + "~s~, ";
            if (NewLine)
            {
                s += "~n~";
            }

            s += "~b~" + SuspectDob.ToShortDateString();
            return s;
        }


        public CourtCase()
        {
        }

        public CourtCase(string SuspectName, DateTime SuspectDob, string Crime, DateTime CrimeDate, int GuiltyChance,
            string CourtVerdict, DateTime ResultsPublishTime, bool Published, bool ResultsPublishedNotificationShown)
        {
            this.SuspectName = SuspectName;
            this.SuspectDob = SuspectDob;
            this.Crime = Crime;
            this.GuiltyChance = GuiltyChance;
            if (GuiltyChance < 0)
            {
                this.GuiltyChance = 0;
            }
            else if (GuiltyChance > 100)
            {
                this.GuiltyChance = 100;
            }

            this.CourtVerdict = CourtVerdict;
            this.ResultsPublishTime = ResultsPublishTime;
            ResultsPublished = Published;
            this.CrimeDate = CrimeDate;
            this.ResultsPublishedNotificationShown = ResultsPublishedNotificationShown;
        }

        public void AddToCourtsMenuAndLists()
        {
            if (ResultsPublished || DateTime.Now > ResultsPublishTime)
            {
                PublishCourtResults();
            }
            else
            {
                //add to pending menu
                AddToPendingCases();
            }
        }

        public void CheckForCaseUpdatedStatus()
        {
            if (!ResultsPublished && DateTime.Now > ResultsPublishTime)
            {
                PublishCourtResults();
            }
        }

        private void PublishCourtResults()
        {
            if (CourtSystem.PublishedCourtCases.Contains(this)) return;
            if (CourtSystem.PendingCourtCases.Contains(this))
            {
                Menus.PendingResultsList.Items.RemoveAt(CourtSystem.PendingCourtCases.IndexOf(this));
                if (Menus.PendingResultsList.Items.Count == 0)
                {
                    Menus.PendingResultsList.Items.Add(new TabItem(" "));
                    PendingResultsMenuCleared = false;
                }

                Menus.PendingResultsList.Index = 0;
                CourtSystem.PendingCourtCases.Remove(this);
            }

            CourtSystem.PublishedCourtCases.Insert(0, this);
            string crimeString = char.ToUpper(Crime[0]) + Crime.ToLower().Substring(1);
            if (GuiltyChance < 0)
            {
                GuiltyChance = 0;
            }
            else if (GuiltyChance > 100)
            {
                GuiltyChance = 100;
            }

            if (LSPDFRPlusHandler.Rnd.Next(100) >= GuiltyChance && !ResultsPublishedNotificationShown)
            {
                CourtVerdict = "Found not guilty and cleared of all charges.";
            }

            TabTextItem item = new TabTextItem(MenuLabel(false), "Court Result",
                MenuLabel(false) + "~s~. ~r~" + crimeString + (Crime[Crime.Length - 1] == '.' ? "" : "~s~.")
                + "~s~~n~ " + CourtVerdict + (CourtVerdict[CourtVerdict.Length - 1] == '.' ? "" : "~s~.")
                + "~s~~n~ Offence took place on ~b~" + CrimeDate.ToShortDateString() + "~s~ at ~b~" +
                CrimeDate.ToShortTimeString()
                + "~s~.~n~ Hearing was on ~b~" + ResultsPublishTime.ToShortDateString() + "~s~ at ~b~" +
                ResultsPublishTime.ToShortTimeString() + "."
                + "~n~~n~~y~Select this case and press ~b~Delete ~y~to dismiss it.");

            Menus.PublishedResultsList.Items.Insert(0, item);
            Menus.PublishedResultsList.RefreshIndex();

            if (!ResultsMenuCleared)
            {
                Game.LogTrivial("Empty items, clearing menu at index 1.");
                Menus.PublishedResultsList.Items.RemoveAt(1);
                ResultsMenuCleared = true;
            }

            ResultsPublished = true;
            if (ResultsPublishedNotificationShown) return;
            if (CourtSystem.LoadingXmlFileCases)
            {
                GameFiber.StartNew(delegate
                {
                    GameFiber.Wait(25000);
                    if (CourtSystem.OpenCourtMenuKey != Keys.None)
                    {
                        Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "~b~San Andreas Court",
                            "~r~" + SuspectName,
                            "A court case you're following has been heard. Press ~b~" +
                            ExtensionMethods.GetKeyString(CourtSystem.OpenCourtMenuKey,
                                CourtSystem.OpenCourtMenuModifierKey) + ".");
                    }
                });
            }
            else
            {
                if (CourtSystem.OpenCourtMenuKey != Keys.None)
                {
                    Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "~b~San Andreas Court",
                        "~r~" + SuspectName,
                        "A court case you're following has been heard. Press ~b~" +
                        ExtensionMethods.GetKeyString(CourtSystem.OpenCourtMenuKey,
                            CourtSystem.OpenCourtMenuModifierKey) + ".");
                }
            }

            ResultsPublishedNotificationShown = true;
            CourtSystem.OverwriteCourtCase(this);
        }

        private void AddToPendingCases()
        {
            if (CourtSystem.PendingCourtCases.Contains(this)) return;
            if (CourtSystem.PublishedCourtCases.Contains(this))
            {
                Menus.PublishedResultsList.Items.RemoveAt(CourtSystem.PublishedCourtCases.IndexOf(this));
                if (Menus.PublishedResultsList.Items.Count == 0)
                {
                    Menus.PublishedResultsList.Items.Add(new TabItem(" "));
                    ResultsMenuCleared = false;
                }

                Menus.PublishedResultsList.Index = 0;
                CourtSystem.PublishedCourtCases.Remove(this);
            }

            CourtSystem.PendingCourtCases.Insert(0, this);
            string crimeString = char.ToUpper(Crime[0]) + Crime.ToLower().Substring(1);
            TabTextItem item = new TabTextItem(MenuLabel(false), "Court Date Pending",
                MenuLabel(false) + ". ~n~Hearing is for: ~r~" + crimeString + ".~s~~n~ Offence took place on ~b~"
                + CrimeDate.ToShortDateString() + "~s~ at ~b~" + CrimeDate.ToShortTimeString() +
                "~s~~n~ Hearing date: ~y~" + ResultsPublishTime.ToShortDateString() + " " +
                ResultsPublishTime.ToShortTimeString()
                + "~n~~n~~y~Select this case and press ~b~Insert ~s~to make the hearing take place immediately, or ~b~Delete ~y~to dismiss it.");
            Menus.PendingResultsList.Items.Insert(0, item);
            Menus.PendingResultsList.RefreshIndex();

            if (!PendingResultsMenuCleared)
            {
                Game.LogTrivial("Empty items, clearing menu at index 1.");
                Menus.PendingResultsList.Items.RemoveAt(1);
                PendingResultsMenuCleared = true;
            }

            if (!CourtSystem.LoadingXmlFileCases)
            {
                if (CourtSystem.OpenCourtMenuKey != Keys.None)
                {
                    Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "~b~San Andreas Court",
                        "~r~" + SuspectName,
                        "You're now following a new pending court case. Press ~b~" +
                        ExtensionMethods.GetKeyString(CourtSystem.OpenCourtMenuKey,
                            CourtSystem.OpenCourtMenuModifierKey) + ".");
                }
            }

            ResultsPublished = false;
        }
    }
}