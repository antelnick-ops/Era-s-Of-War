using System;
using System.IO;
using ErasOfWar.Simulation.Base;
using UnityEngine;

namespace ErasOfWar.Unity.Base
{
    /// <summary>Reads and writes the base save file in the app's private storage on the device.</summary>
    public static class BaseSaveStore
    {
        public static string SavePath => Path.Combine(Application.persistentDataPath, "base.json");

        public static bool Exists => File.Exists(SavePath);

        /// <summary>Write via a temp file so a crash mid-save can't leave a half-written base.</summary>
        public static void Save(BaseState state)
        {
            try
            {
                var json = BaseSerializer.ToJson(state);
                var tmp = SavePath + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(SavePath)) File.Delete(SavePath);
                File.Move(tmp, SavePath);
            }
            catch (Exception e)
            {
                Debug.LogError("[Eras of War] Could not save base: " + e.Message);
            }
        }

        /// <summary>Load the saved base, or null if there is no save. A broken file is kept as base.json.bad.</summary>
        public static BaseLoadResult Load(BuildingCatalog catalog)
        {
            if (!Exists) return null;
            string json;
            try
            {
                json = File.ReadAllText(SavePath);
            }
            catch (Exception e)
            {
                Debug.LogError("[Eras of War] Could not read save: " + e.Message);
                return null;
            }

            var result = BaseSerializer.FromJson(catalog, json);
            if (!result.Ok)
            {
                Debug.LogError("[Eras of War] " + result.Error);
                try { File.Copy(SavePath, SavePath + ".bad", true); } catch (Exception) { /* best effort */ }
            }
            foreach (var d in result.Dropped)
                Debug.LogWarning("[Eras of War] Saved building not restored: " + d);
            return result;
        }

        public static void Delete()
        {
            try { if (Exists) File.Delete(SavePath); }
            catch (Exception e) { Debug.LogError("[Eras of War] Could not delete save: " + e.Message); }
        }
    }
}
