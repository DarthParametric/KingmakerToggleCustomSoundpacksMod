using Harmony12;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToggleCustomSoundpacks
{
    class SoundBankPatches
    {
        [HarmonyPatch(typeof(AkBankHandle), "LoadBank")]
        static class AkBankHandle_LoadBank_Patch
        {
            static bool Prefix(AkBankHandle __instance)
            {
                try
                {
                    if (!Main.enabled) return true;
                    if (!Main.CustomSoundpackPaths.ContainsKey(__instance.bankName)) return true;
                    __instance.relativeBasePath = Main.CustomSoundpackPaths[__instance.bankName];
                }
                catch (Exception ex)
                {
                    Main.DebugError(ex);
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(AkBankHandle), "LoadBankAsync")]
        static class AkBankHandle_LoadBankAsync_Patch
        {
            static bool Prefix(AkBankHandle __instance)
            {
                try
                {
                    if (!Main.enabled) return true;
                    if (!Main.CustomSoundpackPaths.ContainsKey(__instance.bankName)) return true;
                    __instance.LoadBank();
                    return false;
                } catch(Exception ex)
                {
                    Main.DebugError(ex);
                }
                return true;
            }
        }
    }
}
