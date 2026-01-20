using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ECG.Services
{
    /// <summary>
    /// Central place for scene name constants + simple load helpers.
    /// Keep scene names here so you don't hardcode strings everywhere.
    /// </summary>
    public static class SceneLoader
    {
        public const string MainMenu = "Lvl_MainMenu";
        public const string DeckBuilder = "Lvl_DeckBuilder";
        public const string DeckViewer = "Lvl_DeckViewer";

        /// <summary>
        /// Synchronous scene load (simple and fine for this test).
        /// </summary>
        public static void Load(string scene)
        {
            SceneManager.LoadScene(scene);
        }

        /// <summary>
        /// Async loading coroutine (optional usage).
        /// </summary>
        public static IEnumerator LoadAsync(string scene, LoadSceneMode mode)
        {
            var op = SceneManager.LoadSceneAsync(scene, mode);
            yield return op;
        }
    }
}