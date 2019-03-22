using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;
using Rage;

namespace LSPDFR_
{
    public class Offence
    {
        public string name = "Default";
        public int points = 0;
        public int fine = 5;
        public bool seizeVehicle = false;
        private string offenceCategory = "Default";
        public override string ToString()
        {
            return "OFFENCE<" + name + points + fine + seizeVehicle + offenceCategory + ">";
        }


        internal static Dictionary<string, List<Offence>> CategorizedTrafficOffences = new Dictionary<string, List<Offence>>();
        internal static void DeserializeOffences()
        {
            List<Offence> allOffences = new List<Offence>();
            if (Directory.Exists("Plugins/LSPDFR/LSPDFR+/Offences"))
            {
                foreach (string file in Directory.EnumerateFiles("Plugins/LSPDFR/LSPDFR+/Offences", "*.xml", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        using (StreamReader reader = new StreamReader(file))
                        {
                            XmlSerializer deserializer = new XmlSerializer(typeof(List<Offence>),
                                new XmlRootAttribute("Offences"));
                            allOffences.AddRange((List<Offence>)deserializer.Deserialize(reader));
                        }
                    }
                    catch (Exception e)
                    {
                        Game.LogTrivial(e.ToString());
                        Game.LogTrivial("LSPDFR+ - Error parsing XML from " + file);
                    }
                }
            }

            if (allOffences.Count == 0)
            {
                allOffences.Add(new Offence());
                Game.DisplayNotification("~r~~h~LSPDFR+ couldn't find a valid XML file with offences in Plugins/LSPDFR/LSPDFR+/Offences. Setting just the default offence.");
            }
            CategorizedTrafficOffences = allOffences.GroupBy(x => x.offenceCategory).ToDictionary(x => x.Key, x => x.ToList());
            return;
        }
        internal static int Maxpoints = 12;
        internal const int Minpoints = 0;
        internal static int Pointincstep = 1;
        internal static int MaxFine = 5000;

        internal static Keys OpenTicketMenuKey = Keys.Q;
        internal static Keys OpenTicketMenuModifierKey = Keys.LShiftKey;

        internal static readonly string Currency = "$";
        internal static bool EnablePoints = true;
    }
}
