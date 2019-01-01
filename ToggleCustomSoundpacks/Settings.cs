using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityModManagerNet;

namespace ToggleCustomSoundpacks
{
    public class Settings : UnityModManager.ModSettings
    {
        public class SoundPackSettings
        {
            public string Name = "";
            public int LoadOrder = 1000;
            public bool Enabled = false;
            [XmlIgnore]
            public bool IsValid = true;
            [XmlIgnore]
            public string[] Files = new string[] { };
        }
        public List<SoundPackSettings> SoundPackSettingsList = new List<SoundPackSettings>();
        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
        public static Settings Load(UnityModManager.ModEntry modEntry)
        {
            var settings =  UnityModManager.ModSettings.Load<Settings>(modEntry);
            var directories = Directory.GetDirectories(modEntry.Path).Select(d => Path.GetFileName(d)).ToList();
            foreach(var directory in directories)
            {
                if(!settings.SoundPackSettingsList.Any(sp => sp.Name == directory))
                {
                    settings.SoundPackSettingsList.Add(new SoundPackSettings
                    {
                        Name = directory,                       
                    });
                }
            }
            settings.SoundPackSettingsList = settings.SoundPackSettingsList
                .Where(sp => directories.Contains(sp.Name))
                .ToList();
            foreach (var sp in settings.SoundPackSettingsList)
            {

                sp.Files = Directory.GetFiles(Path.Combine(modEntry.Path, sp.Name), "*.bnk")
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .ToArray();
            }
            settings.SoundPackSettingsList = settings.SoundPackSettingsList
                .Where(sp => sp.Files.Length > 0)
                .OrderBy(sp => sp.LoadOrder).ToList();
            for(int i = 0; i < settings.SoundPackSettingsList.Count; i++)
            {
                settings.SoundPackSettingsList[i].LoadOrder = i;
            }                                              
            return settings;
        }
    }
}
