using UnityEngine;

namespace VoogleRoute.Phone
{
    /// <summary>
    /// Runs before PlayerController so search focus is on the UI layer and selected
    /// before HasInputSelected / ShouldBlockKeyboardShortcuts are evaluated.
    /// </summary>
    [DefaultExecutionOrder(-32000)]
    internal sealed class PhoneGpsSearchFocus : MonoBehaviour
    {
        private void Update() => PhoneGpsPanel.CaptureSearchFocus();

        private void LateUpdate() => PhoneGpsPanel.CaptureSearchFocus();
    }
}
