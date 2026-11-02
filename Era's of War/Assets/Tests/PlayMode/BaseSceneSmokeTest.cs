using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ErasOfWar.Tests
{
    /// <summary>
    /// Loads the Base scene, presses HUD buttons the way a player would, and fails on any error log
    /// or exception. Rules themselves are covered by Simulation.Tests (dotnet test).
    /// The player's real save file is moved aside during the test and put back afterwards.
    /// </summary>
    public class BaseSceneSmokeTest
    {
        private string SavePath => Path.Combine(Application.persistentDataPath, "base.json");
        private string BackupPath => SavePath + ".testbackup";

        [SetUp]
        public void MoveRealSaveAside()
        {
            if (File.Exists(BackupPath)) File.Delete(BackupPath);
            if (File.Exists(SavePath)) File.Move(SavePath, BackupPath);
        }

        [UnityTearDown]
        public IEnumerator RestoreRealSave()
        {
            // Destroy the controller first: leaving Play mode would otherwise save again (OnApplicationQuit).
            var controller = GameObject.Find("Base");
            if (controller != null) Object.Destroy(controller);
            yield return null;

            if (File.Exists(SavePath)) File.Delete(SavePath);
            if (File.Exists(BackupPath)) File.Move(BackupPath, SavePath);
        }

        [UnityTest]
        public IEnumerator BaseSceneLoadsAndPlacesACommandCenter()
        {
            SceneManager.LoadScene("Base");
            yield return Frames(10);

            var view = GameObject.Find("Base View");
            Assert.That(view, Is.Not.Null, "Base View was not created");
            Assert.That(view.transform.Find("Ground"), Is.Not.Null, "grid not drawn");

            var content = GameObject.Find("Content");
            Assert.That(content, Is.Not.Null, "build menu missing");
            Assert.That(content.transform.childCount, Is.EqualTo(33), "one build menu item per building type");

            // First build menu item is the Command Center (workbook order).
            content.transform.GetChild(0).GetComponent<Button>().onClick.Invoke();
            yield return Frames(3);
            Assert.That(GameObject.Find("Ghost command_center"), Is.Not.Null, "placement ghost not shown");

            ContextButton("place").onClick.Invoke();
            yield return Frames(3);
            Assert.That(view.GetComponentsInChildren<Transform>().Any(t => t.name == "Command Center #1"),
                "Command Center was not placed");
            Assert.That(File.Exists(SavePath), "base was not saved after placing");

            // Selected now: upgrade the CC (grid grows 20 -> 22).
            ContextButton("upgrade").onClick.Invoke();
            yield return Frames(3);
            StringAssert.Contains("\"level\": 2", File.ReadAllText(SavePath));
            // Any Debug.LogError or exception during the test fails it automatically (Unity Test Framework default).
        }

        private static Button ContextButton(string act)
        {
            var buttons = GameObject.Find("Buttons");
            Assert.That(buttons, Is.Not.Null, "context buttons missing");
            var t = buttons.transform.Find(act);
            Assert.That(t, Is.Not.Null, $"no '{act}' button in context bar");
            return t.GetComponent<Button>();
        }

        private static IEnumerator Frames(int n)
        {
            for (var i = 0; i < n; i++) yield return null;
        }
    }
}
