using TMPro;
using UnityEngine;

namespace ECG.Presentation
{
    /// <summary>
    /// LoadingOverlay:
    /// - Simple UI overlay to indicate loading/busy state.
    /// - Can show a text label and enable/disable a root GameObject.
    ///
    /// Setup:
    /// - If "root" is assigned, we toggle that object (recommended).
    /// - If "root" is NOT assigned, we fall back to toggling this GameObject itself.
    ///
    /// Note:
    /// - The fallback toggling behavior is preserved as-is from the original code.
    ///   (Be mindful when using it, because enabling/disabling the same object that
    ///   owns the script can be confusing during debugging.)
    /// </summary>
    public sealed class LoadingOverlay : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text label;

        private void Awake()
        {
            // Default state: hidden.
            Hide();
        }

        /// <summary>
        /// Shows the overlay and optionally sets the label text.
        /// </summary>
        public void Show(string text = "Loading...")
        {
            if (label)
                label.text = text;

            // Preferred: toggle the dedicated root.
            if (root) root.SetActive(true);
            else gameObject.SetActive(false); // fallback behavior preserved from original script
        }

        /// <summary>
        /// Hides the overlay.
        /// </summary>
        public void Hide()
        {
            if (root) root.SetActive(false);
            else gameObject.SetActive(true); // fallback behavior preserved from original script
        }
    }
}