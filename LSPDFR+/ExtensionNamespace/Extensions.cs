using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using Rage;

namespace LSPDFR_.ExtensionNamespace
{
    internal static class Extensions
    {
        public static void ShowDrivingLicence(this Ped p)
        {
            if (!p) return;
            Persona pers = Functions.GetPersonaForPed(p);
            Game.DisplayNotification("mpcharselect", "mp_generic_avatar", "STATE ISSUED IDENTIFICATION",
                pers.FullName,
                "~b~" + pers.FullName + "~n~~y~" + pers.Gender + "~s~. Born: ~y~" +
                pers.Birthday.ToShortDateString());
        }
    }
}