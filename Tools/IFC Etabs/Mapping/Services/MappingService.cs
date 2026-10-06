using System;
using System.IO;
using System.Text.Json;
using JNNTool.Tools.IFCEtabs.Mapping.Models;

namespace JNNTool.Tools.IFCEtabs.Mapping.Services
{
    public interface IMappingService
    {
        MappingConfig Config { get; }
        void Load();
        void Save();
        SectionMappingEntry? GetSectionMapping(string sourceSectionName);
        void SetSectionMapping(string sourceSectionName, string familyName, string typeName, double widthMm, double depthMm);
    }

    public class MappingService : IMappingService
    {
        private static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "JNNTool", "Settings");

        private static readonly string ConfigFilePath = Path.Combine(SettingsDir, "EtabsConverter_Mapping.json");

        public MappingConfig Config { get; private set; } = new();

        public static MappingService Instance { get; } = new();

        public MappingService()
        {
            Load();
        }

        public void Load()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    var loaded = JsonSerializer.Deserialize<MappingConfig>(json);
                    if (loaded != null)
                    {
                        Config = loaded;
                        return;
                    }
                }
            }
            catch { }

            Config = new MappingConfig();
        }

        public void Save()
        {
            try
            {
                if (!Directory.Exists(SettingsDir))
                {
                    Directory.CreateDirectory(SettingsDir);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(Config, options);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch { }
        }

        public SectionMappingEntry? GetSectionMapping(string sourceSectionName)
        {
            if (string.IsNullOrWhiteSpace(sourceSectionName)) return null;
            if (Config.SectionMappings.TryGetValue(sourceSectionName, out var entry))
            {
                return entry;
            }
            return null;
        }

        public void SetSectionMapping(string sourceSectionName, string familyName, string typeName, double widthMm, double depthMm)
        {
            if (string.IsNullOrWhiteSpace(sourceSectionName)) return;

            Config.SectionMappings[sourceSectionName] = new SectionMappingEntry
            {
                SourceSectionName = sourceSectionName,
                RevitFamilyName = familyName,
                RevitTypeName = typeName,
                WidthMm = widthMm,
                DepthMm = depthMm
            };
            Save();
        }
    }
}
