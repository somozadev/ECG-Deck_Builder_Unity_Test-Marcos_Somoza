using ECG.Services;
using UnityEngine;
using UnityEngine.UI;

namespace ECG.Core
{
    /// <summary>
    /// Main menu flow:
    /// - New User: generates a new UUID, stores it in PlayerPrefs, loads DeckBuilder.
    /// - Continue: only available if a UUID exists; loads DeckViewer.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button newUserButton;
        [SerializeField] private Button continueButton;

        private void OnEnable()
        {
            // Register listeners when the object becomes active.
            if (newUserButton) newUserButton.onClick.AddListener(OnNewUser);
            if (continueButton) continueButton.onClick.AddListener(OnContinue);
        }

        private void OnDisable()
        {
            // Unregister to avoid multiple subscriptions when re-entering the scene.
            if (newUserButton) newUserButton.onClick.RemoveListener(OnNewUser);
            if (continueButton) continueButton.onClick.RemoveListener(OnContinue);
        }

        private void Start()
        {
            RefreshContinueButton();
        }

        private void RefreshContinueButton()
        {
            // Constraint: Continue must be hidden/disabled if no UUID exists.
            bool hasUser = UserIdService.HasUserId();
            if (!continueButton) return;

            continueButton.interactable = hasUser;
            continueButton.gameObject.SetActive(hasUser);
        }

        private void OnNewUser()
        {
            // Create a new user UUID and store it locally (PlayerPrefs).
            UserIdService.CreateUserId();

            // Move to deck building core loop.
            SceneLoader.Load(SceneLoader.DeckBuilder);
        }

        private void OnContinue()
        {
            // Safety: if UUID missing, keep UI consistent.
            if (!UserIdService.HasUserId())
            {
                RefreshContinueButton();
                return;
            }

            SceneLoader.Load(SceneLoader.DeckViewer);
        }
    }
}
