using Harmony12;
using Kingmaker;
using Kingmaker.Sound;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityModManagerNet;

namespace ToggleCustomSoundpacks
{
    public class Main
    {
        public static UnityModManagerNet.UnityModManager.ModEntry.ModLogger logger;
        [System.Diagnostics.Conditional("DEBUG")]
        public static void DebugLog(string msg)
        {
            if (logger != null) logger.Log(msg);
        }
        public static void DebugError(Exception ex)
        {
            if (logger != null) logger.Log(ex.ToString() + "\n" + ex.StackTrace);
        }
        public static bool enabled;
        static Settings settings;
        static HashSet<string> DirtySoundbanks = new HashSet<string>();
        public static Dictionary<string, string> CustomSoundpackPaths = new Dictionary<string, string>();
        public static Dictionary<string, string> PreviewableSoundPacks = new Dictionary<string, string>()
        {
            { "PC_Male_Aggressive_GVR_ENG" , "PC_Male_Aggressive__Test" },
            { "PC_Male_Brave_GVR_ENG" , "PC_Male_Brave_Test" },
            { "PC_Male_Confident_GVR_ENG" , "PC_Male_Confident_Test" },
            { "PC_Male_Madman_GVR_ENG" , "PC_Male_Madman_Test" },
            { "PC_Male_Pious_GVR_ENG" , "PC_Male_Pious_Test" },
            { "PC_Male_Pragmatic_GVR_ENG" , "PC_Male_Pragmatic_Test" },
            { "PC_Male_Reserved_GVR_ENG" , "PC_Male_Reserved_Test" },
            { "PC_Female_Aggressive_GVR_ENG" , "PC_Female_Aggressive_Test" },
            { "PC_Female_Brave_GVR_ENG" , "PC_Female_Brave_Test" },
            { "PC_Female_Confident_GVR_ENG" , "PC_Female_Confident_Test" },
            { "PC_Female_Madman_GVR_ENG" , "PC_Female_Madman_GVR_Test" },
            { "PC_Female_Pious_GVR_ENG" , "PC_Female_Pious_Test" },
            { "PC_Female_Pragmatic_GVR_ENG" , "PC_Female_Pragmatic_Test" },
            { "PC_Female_Reserved_GVR_ENG" , "PC_Female_Reserved_Test" },
        };
        public static GUIStyle YellowText;
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            try
            {
                settings = Settings.Load(modEntry);
                modEntry.OnToggle = OnToggle;
                modEntry.OnGUI = OnGUI;
                modEntry.OnSaveGUI = OnSaveGUI;
                logger = modEntry.Logger;
                var harmony = HarmonyInstance.Create(modEntry.Info.Id);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Reload(modEntry);
            }
            catch (Exception ex)
            {
                DebugError(ex);
                throw ex;
            }
            return true;
        }
        // Called when the mod is turned to on/off.
        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value /* active or inactive */)
        {
            enabled = value;
            return true; // Permit or not.
        }
        static void Reload(UnityModManager.ModEntry modEntry)
        {
            CustomSoundpackPaths.Clear();
            foreach (var soundPackSettings in settings.SoundPackSettingsList)
            {
                soundPackSettings.IsValid = soundPackSettings.Files.All(fileName => !CustomSoundpackPaths.ContainsKey(fileName));
                if (!soundPackSettings.IsValid) continue;
                if (!soundPackSettings.Enabled) continue;
                foreach (var soundBank in soundPackSettings.Files)
                {
                    var basePath = new Uri(AkBasePathGetter.GetValidBasePath());
                    var customPath = new Uri(Path.Combine(modEntry.Path, soundPackSettings.Name));
                    CustomSoundpackPaths[soundBank] = Uri.UnescapeDataString(basePath.MakeRelativeUri(customPath).ToString());
                }
            }
            var loadedBanks = Traverse.Create(typeof(SoundBanksManager)).Field("s_LoadCount").GetValue<Dictionary<string, int>>();
            foreach (var soundBank in DirtySoundbanks)
            {
                if (loadedBanks.ContainsKey(soundBank))
                {
                    AkBankManager.UnloadBank(soundBank);
                    AkBankManager.DoUnloadBanks();
                    AkBankManager.LoadBank(soundBank, false, false);
                }
            }
            DirtySoundbanks.Clear();
        }
        static void MarkDirty(Settings.SoundPackSettings soundPackSettings)
        {
            foreach(var file in soundPackSettings.Files)
            {
                DirtySoundbanks.Add(file);
            }
        }
        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }
        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            if (!enabled) return;
            try
            {
                if(YellowText == null)
                {
                    YellowText = new GUIStyle(GUI.skin.label);
                    YellowText.normal.textColor = Color.yellow;
                    YellowText.focused.textColor = Color.yellow;
                }
                foreach (var soundPackSetting in settings.SoundPackSettingsList.ToArray())
                {
                    GUILayout.BeginHorizontal();
                    var enabled = GUILayout.Toggle(soundPackSetting.Enabled, " " + soundPackSetting.Name);
                    if (enabled != soundPackSetting.Enabled)
                    {
                        soundPackSetting.Enabled = enabled;
                        MarkDirty(soundPackSetting);
                        Reload(modEntry);
                    }
                    if (soundPackSetting.Enabled && !soundPackSetting.IsValid)
                    {
                        GUILayout.Label("Soundpack Load Order Conflict", YellowText);
                    }
                    var firstSoundBank = soundPackSetting.Files.FirstOrDefault();
                    if (PreviewableSoundPacks.ContainsKey(firstSoundBank))
                    {
                        if (GUILayout.Button("Preview", GUILayout.ExpandWidth(false)))
                        {
                            var eventName = PreviewableSoundPacks[firstSoundBank];
                            AkSoundEngine.PostEvent(eventName, Game.Instance.UI.Common.gameObject);
                        }
                    }
                    if (GUILayout.Button("Up", GUILayout.ExpandWidth(false)))
                    {
                        if (soundPackSetting.LoadOrder > 0 && soundPackSetting.LoadOrder < settings.SoundPackSettingsList.Count)
                        {
                            var swapTarget = settings.SoundPackSettingsList[soundPackSetting.LoadOrder - 1];
                            soundPackSetting.LoadOrder--;
                            swapTarget.LoadOrder++;
                            settings.SoundPackSettingsList = settings.SoundPackSettingsList.OrderBy(sp => sp.LoadOrder).ToList();
                            MarkDirty(soundPackSetting);
                            MarkDirty(swapTarget);
                        }
                    }
                    if (GUILayout.Button("Down", GUILayout.ExpandWidth(false)))
                    {
                        if (soundPackSetting.LoadOrder >= 0 && soundPackSetting.LoadOrder < settings.SoundPackSettingsList.Count - 1)
                        {
                            var swapTarget = settings.SoundPackSettingsList[soundPackSetting.LoadOrder + 1];
                            soundPackSetting.LoadOrder++;
                            swapTarget.LoadOrder--;
                            settings.SoundPackSettingsList = settings.SoundPackSettingsList.OrderBy(sp => sp.LoadOrder).ToList();
                            MarkDirty(soundPackSetting);
                            MarkDirty(swapTarget);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
            catch (Exception ex)
            {
                DebugError(ex);
                throw ex;
            }
        }
    }
}
