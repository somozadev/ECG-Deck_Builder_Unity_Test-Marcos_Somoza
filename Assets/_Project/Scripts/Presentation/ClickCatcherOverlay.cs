using System;
using ECG.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ECG.Presentation
{
    /// <summary>
    /// ClickCatcherOverlay:
    /// - Fullscreen transparent raycast catcher.
    /// - Receives pointer events even without a Button component.
    ///
    /// Typical usage:
    /// - Enable it when a modal/focus UI is open (e.g., a focused card "ghost").
    /// - When the user clicks anywhere, raise Clicked to close the modal/focus state.
    /// </summary>
    public class ClickCatcherOverlay : MonoBehaviour, IPointerDownHandler
    {
        /// <summary>
        /// Fired whenever the overlay receives a pointer down event.
        /// </summary>
        public event Action Clicked;

        /// <summary>
        /// Unity UI callback invoked on pointer down.
        /// This object should have a RaycastTarget enabled (e.g., an Image) to receive events.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            // Useful debug to confirm we're receiving input and have listeners.
            Debug.Log($"[ClickCatcher] Clicked: {name} (instanceID {GetInstanceID()}) listenersNull={Clicked == null}");
            Clicked?.Invoke();
        }
    }
}