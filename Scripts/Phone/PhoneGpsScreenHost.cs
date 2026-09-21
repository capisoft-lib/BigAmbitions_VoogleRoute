using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UI;

namespace VoogleRoute.Phone
{
    /// <summary>
    /// Hosts the GPS on the HUD phone's inner app grid (where the icons sit),
    /// never on the outer bezel / smartphone root.
    /// </summary>
    internal static class PhoneGpsScreenHost
    {
        private static readonly List<GameObject> Hidden = new List<GameObject>();
        private static readonly List<Behaviour> DisabledLayouts = new List<Behaviour>();
        private static RectTransform _screen;

        internal static RectTransform Screen => _screen;

        internal static bool TryPrepare(out RectTransform screen, GameObject keep)
        {
            Restore();
            screen = ResolveScreen();
            _screen = screen;
            if (screen == null)
                return false;

            DestroyLegacyGps(screen, keep);
            DisableLayouts(screen);

            for (var i = 0; i < screen.childCount; i++)
            {
                var child = screen.GetChild(i).gameObject;
                if (keep != null && child == keep)
                    continue;
                if (child.name.StartsWith("VoogleRoute_PhoneGps"))
                    continue;
                if (!child.activeSelf)
                    continue;
                Hidden.Add(child);
                child.SetActive(false);
            }

            return true;
        }

        internal static void Restore()
        {
            for (var i = 0; i < Hidden.Count; i++)
            {
                if (Hidden[i] != null)
                    Hidden[i].SetActive(true);
            }

            Hidden.Clear();

            for (var i = 0; i < DisabledLayouts.Count; i++)
            {
                if (DisabledLayouts[i] != null)
                    DisabledLayouts[i].enabled = true;
            }

            DisabledLayouts.Clear();
            _screen = null;
        }

        internal static void FitInner(RectTransform gps, RectTransform host)
        {
            if (gps == null || host == null)
                return;

            var pad = ReadPadding(host);
            gps.anchorMin = Vector2.zero;
            gps.anchorMax = Vector2.one;
            gps.pivot = new Vector2(0.5f, 0.5f);
            gps.offsetMin = new Vector2(pad.x, pad.y);
            gps.offsetMax = new Vector2(-pad.z, -pad.w);
        }

        private static RectTransform ResolveScreen()
        {
            var grid = VoogleRoutePhoneApp.ResolveAppGrid() as RectTransform;
            if (grid == null)
                return null;

            var phone = InstanceBehavior<UIs>.Instance?.smartphoneUI?.transform as RectTransform;

            // The icon grid is already laid out on the inner glass. Using any ancestor
            // (especially SmartphoneUI itself) fills the outer phone chassis.
            if (!IsOuterChassis(grid, phone))
                return grid;

            var parent = grid.parent as RectTransform;
            if (parent != null && !IsOuterChassis(parent, phone))
                return parent;

            return grid;
        }

        private static void DestroyLegacyGps(RectTransform host, GameObject keep)
        {
            var targets = new List<GameObject>();
            CollectLegacyGps(host, keep, targets);
            var phone = InstanceBehavior<UIs>.Instance?.smartphoneUI?.transform;
            if (phone != null && phone != host)
                CollectLegacyGps(phone, keep, targets);

            for (var i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null)
                    Object.Destroy(targets[i]);
            }
        }

        private static void CollectLegacyGps(Transform root, GameObject keep, List<GameObject> targets)
        {
            if (root == null)
                return;

            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (!child.name.StartsWith("VoogleRoute_PhoneGps"))
                    continue;
                if (keep != null && child.gameObject == keep)
                    continue;
                targets.Add(child.gameObject);
            }
        }

        private static void DisableLayouts(Component host)
        {
            var layouts = host.GetComponents<LayoutGroup>();
            for (var i = 0; i < layouts.Length; i++)
            {
                if (layouts[i] == null || !layouts[i].enabled)
                    continue;
                DisabledLayouts.Add(layouts[i]);
                layouts[i].enabled = false;
            }

            var fitter = host.GetComponent<ContentSizeFitter>();
            if (fitter != null && fitter.enabled)
            {
                DisabledLayouts.Add(fitter);
                fitter.enabled = false;
            }
        }

        private static Vector4 ReadPadding(RectTransform host)
        {
            var pad = Vector4.zero;
            var layout = host.GetComponent<LayoutGroup>();
            if (layout != null)
            {
                var padding = layout.padding;
                pad = new Vector4(padding.left, padding.bottom, padding.right, padding.top);
            }

            var phone = InstanceBehavior<UIs>.Instance?.smartphoneUI?.transform as RectTransform;
            if (!IsOuterChassis(host, phone))
                return pad;

            var size = host.rect.size;
            pad.x = Mathf.Max(pad.x, size.x * 0.09f);
            pad.y = Mathf.Max(pad.y, size.y * 0.09f);
            pad.z = Mathf.Max(pad.z, size.x * 0.09f);
            pad.w = Mathf.Max(pad.w, size.y * 0.07f);
            return pad;
        }

        private static bool IsOuterChassis(RectTransform candidate, RectTransform phone)
        {
            if (candidate == null)
                return false;
            if (phone == null)
                return false;
            if (candidate == phone)
                return true;

            var a = candidate.rect.size;
            var b = phone.rect.size;
            if (b.x < 8f || b.y < 8f)
                return false;
            return a.x >= b.x * 0.92f && a.y >= b.y * 0.92f;
        }
    }
}
