using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ErasOfWar.Simulation.Data
{
    /// <summary>
    /// Reads game_data/*.json into typed classes. No Unity code: Unity passes either a folder
    /// path or a file-reading callback (needed later for Android StreamingAssets).
    /// </summary>
    public static class GameDataLoader
    {
        public const string DirectoryName = "game_data";
        public const string UnitsFile = "units.json";
        public const string TechTreeFile = "tech_tree.json";
        public const string EconomyFile = "economy.json";
        public const string ConstantsFile = "constants.json";
        public const string LeadersFile = "leaders.json";
        public const string GeneralsFile = "generals.json";
        public const string ResourceTablesFile = "tables_03_ResourceSystem.json";
        public const string BuildingTablesFile = "tables_04_Buildings.json";
        public const string BaseLayoutTablesFile = "tables_07_BaseLayout.json";

        // Unknown JSON fields are an error, so a renamed workbook column fails loudly instead of silently reading 0.
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Error,
            NullValueHandling = NullValueHandling.Include,
        };

        /// <summary>Load everything from a folder containing the game_data JSON files.</summary>
        public static GameData LoadFromDirectory(string gameDataDirectory)
        {
            if (string.IsNullOrEmpty(gameDataDirectory) || !Directory.Exists(gameDataDirectory))
                throw new GameDataException($"game_data folder not found: '{gameDataDirectory}'");

            return Load(file => File.ReadAllText(Path.Combine(gameDataDirectory, file)));
        }

        /// <summary>Load everything using a callback that returns a file's JSON text by file name.</summary>
        public static GameData Load(Func<string, string> readFile)
        {
            if (readFile == null) throw new ArgumentNullException(nameof(readFile));

            return new GameData(
                Parse<List<UnitDef>>(readFile, UnitsFile),
                Parse<TechTreeDef>(readFile, TechTreeFile),
                Parse<EconomyDef>(readFile, EconomyFile),
                Parse<ConstantsDef>(readFile, ConstantsFile),
                Parse<List<LeaderDef>>(readFile, LeadersFile),
                Parse<List<GeneralDef>>(readFile, GeneralsFile),
                new Dictionary<string, TableFile>
                {
                    [ResourceTablesFile] = ParseTable(readFile, ResourceTablesFile),
                    [BuildingTablesFile] = ParseTable(readFile, BuildingTablesFile),
                    [BaseLayoutTablesFile] = ParseTable(readFile, BaseLayoutTablesFile),
                });
        }

        private static TableFile ParseTable(Func<string, string> readFile, string fileName)
        {
            var root = Parse<JObject>(readFile, fileName);
            return new TableFile(fileName, root);
        }

        /// <summary>
        /// Walk up from <paramref name="startDirectory"/> looking for game_data/units.json.
        /// Returns null if not found. Used by tests (bin folder) and the Unity editor (Assets folder).
        /// </summary>
        public static string FindGameDataDirectory(string startDirectory)
        {
            var dir = string.IsNullOrEmpty(startDirectory) ? null : new DirectoryInfo(startDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, DirectoryName);
                if (File.Exists(Path.Combine(candidate, UnitsFile)))
                    return candidate;
                dir = dir.Parent;
            }
            return null;
        }

        private static T Parse<T>(Func<string, string> readFile, string fileName) where T : class
        {
            string json;
            try
            {
                json = readFile(fileName);
            }
            catch (Exception e)
            {
                throw new GameDataException($"Could not read {fileName}: {e.Message}", e);
            }

            if (string.IsNullOrWhiteSpace(json))
                throw new GameDataException($"{fileName} is empty");

            try
            {
                return JsonConvert.DeserializeObject<T>(json, Settings)
                       ?? throw new GameDataException($"{fileName} parsed to null");
            }
            catch (JsonException e)
            {
                throw new GameDataException($"{fileName}: {e.Message}", e);
            }
        }
    }
}
