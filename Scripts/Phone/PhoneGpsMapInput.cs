using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VoogleRoute.Phone
{
    internal sealed class PhoneGpsMapInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IScrollHandler
    {
        private const float HoldSeconds = 0.5f;
        private const float StaticSlopSq = 144f;

        internal Action<Vector2> Dragged;
        internal Action<float> Scrolled;
        internal Action<Vector2> Held;
        internal Action HoldReleased;

        private Vector2 _pressScreen;
        private Camera _pressCamera;
        private float _pressTime;
        private bool _down;
        private bool _moved;
        private bool _heldFired;

        public void OnPointerDown(PointerEventData eventData)
        {
            _down = true;
            _moved = false;
            _heldFired = false;
            _pressScreen = eventData.position;
            _pressCamera = eventData.pressEventCamera;
            _pressTime = Time.unscaledTime;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            var offer = _heldFired;
            _down = false;
            if (offer)
                HoldReleased?.Invoke();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if ((eventData.position - _pressScreen).sqrMagnitude > StaticSlopSq)
                _moved = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if ((eventData.position - _pressScreen).sqrMagnitude > StaticSlopSq)
                _moved = true;
            Dragged?.Invoke(eventData.delta);
        }

        public void OnScroll(PointerEventData eventData)
        {
            Scrolled?.Invoke(eventData.scrollDelta.y);
        }

        private void Update()
        {
            if (!_down || _moved || _heldFired)
                return;
            if (Time.unscaledTime - _pressTime < HoldSeconds)
                return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)transform,
                    _pressScreen,
                    _pressCamera,
                    out var local))
                return;

            _heldFired = true;
            Held?.Invoke(local);
        }
    }

    /// <summary>
    /// Selects the phone GPS before MouseController so a map drag is not a ground click.
    /// </summary>
    [DefaultExecutionOrder(-32000)]
    internal sealed class PhoneGpsMapPressGuard : MonoBehaviour
    {
        private void Update() => PhoneGpsPanel.MaintainMapPressBlock();
    }
}
