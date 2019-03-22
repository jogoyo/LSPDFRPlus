using Rage;

namespace LSPDFR_.API
{
    internal static class TrafficPolicerFuncs
    {
        public static bool IsPedOverAlcoholLimit(Ped p)
        {
            return Traffic_Policer.API.Functions.IsPedOverTheAlcoholLimit(p);
        }

        public static bool IsPedOverDrugsLimit(Ped p)
        {
            return Traffic_Policer.API.Functions.DoesPedHaveDrugsInSystem(p);
        }
    }
}
