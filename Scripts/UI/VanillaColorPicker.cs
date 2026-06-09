using System;
using UI;
using VoogleRoute;
using UnityEngine;

namespace VoogleRoute.UI
{
    /// <summary>Ouvre le sélecteur de couleur natif du jeu (<see cref="CustomColorPicker"/>).</summary>
    internal static class VanillaColorPicker
    {
        internal static bool IsOpen
        {
            get
            {
                try
                {
                    return CustomColorPicker.isOpen;
                }
                catch
                {
                    return false;
                }
            }
        }

        internal static bool TryOpen(Color initialColor, Action<Color> onColorChanged)
        {
            if (onColorChanged == null)
                return false;

            try
            {
                if (!UIs.IsInitialized || UIs.Instance == null)
                    return false;

                var picker = UIs.Instance.customColorPicker;
                if (picker == null)
                    return false;

                if (CustomColorPicker.isOpen)
                    return true;

                picker.Open(onColorChanged, initialColor);
                BringToFront();
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Error("Color picker unavailable", ex);
                return false;
            }
        }

        /// <summary>Place le sélecteur natif au-dessus des overlays mod (ex. fenêtre réglages).</summary>
        internal static void BringToFront()
        {
            try
            {
                if (!UIs.IsInitialized || UIs.Instance?.customColorPicker == null)
                    return;

                var picker = UIs.Instance.customColorPicker;
                var canvas = picker.GetComponentInParent<Canvas>(true);
                if (canvas != null)
                    canvas.sortingOrder = 12000;
            }
            catch
            {
                // picker pas encore monté sur un canvas
            }
        }
    }
}
