using UnityEngine;

namespace ECG.Presentation
{
    /// <summary>
    /// RotatingUI:
    /// - Utility component that continuously rotates a UI element around its Z axis.
    /// - Commonly used for spinners/loading icons.
    ///
    /// Notes:
    /// - Rotation is frame-rate independent via Time.deltaTime.
    /// - "speed" is degrees per second.
    /// </summary>
    public class RotatingUI : MonoBehaviour
    {
        [SerializeField] private float speed = 50f;

        private void Update()
        {
            // Rotate around Z axis (UI rotation in 2D).
            transform.Rotate(0, 0, speed * Time.deltaTime);
        }
    }
}