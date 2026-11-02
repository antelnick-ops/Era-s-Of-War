using System.IO;
using ErasOfWar.Simulation.Data;
using UnityEngine;

namespace ErasOfWar.Unity.Boot
{
    /// <summary>
    /// Loads game_data/*.json through the Simulation library when Play mode starts
    /// and logs a summary. Display/glue only: no game rules here.
    /// </summary>
    public static class GameDataBootstrap
    {
        /// <summary>Loaded data for other Unity scripts. Null until Play mode starts (or if loading failed).</summary>
        public static GameData Data { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Load()
        {
            Data = null;

            // Editor: the repo's game_data/ sits two levels above Assets/.
            // Device builds will read from StreamingAssets (wired up in a later prompt).
            var dir = GameDataLoader.FindGameDataDirectory(Application.dataPath);
            if (dir == null)
            {
                var streaming = Path.Combine(Application.streamingAssetsPath, GameDataLoader.DirectoryName);
                if (Directory.Exists(streaming)) dir = streaming;
            }

            if (dir == null)
            {
                Debug.LogError("[Eras of War] game_data/ folder not found above " + Application.dataPath);
                return;
            }

            try
            {
                Data = GameDataLoader.LoadFromDirectory(dir);
                Debug.Log("[Eras of War] Game data loaded: " + Data.Summary() + "\nFrom: " + dir);
            }
            catch (GameDataException e)
            {
                Debug.LogError("[Eras of War] Game data failed to load: " + e.Message);
            }
        }
    }
}
