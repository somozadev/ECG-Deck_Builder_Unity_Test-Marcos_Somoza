using ECG.Services;
using UnityEngine;

namespace ECG.Presentation
{
    /// <summary>
    /// ButtonLoadLevel:
    /// - Small UI helper meant to be called from a Unity UI Button OnClick().
    /// - Loads the Main Menu scene via SceneLoader.
    ///
    /// Notes:
    /// - This script assumes SceneLoader is a project utility that knows scene names/ids.
    /// - Keep methods public so UnityEvents can call them from the Inspector.
    /// </summary>
    public class ButtonLoadLevel : MonoBehaviour
    {
        /// <summary>
        /// Loads the project's Main Menu scene.
        /// Hook this up to a UI Button.
        /// </summary>
        public void LoadMainMenu()
        {
            SceneLoader.Load(SceneLoader.MainMenu);
        }
    }
}