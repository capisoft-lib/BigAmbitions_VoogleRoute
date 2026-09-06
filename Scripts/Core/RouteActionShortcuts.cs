using BAModAPI;
using BigAmbitions.Mods;
using Capisoft.Lib.BaUnifiedUI.Shortcuts;
using UnityEngine.InputSystem;
using VoogleRoute.UI;

namespace VoogleRoute
{
    /// <summary>Owns the configurable shortcuts for route actions and Hide/Show UI.</summary>
    internal static class RouteActionShortcuts
    {
        private const string RouteShortcutOptionId = "route_shortcut";
        private const string AutoMoveShortcutOptionId = "auto_move_shortcut";
        private const string HideUiShortcutOptionId = "hide_ui_shortcut";

        private static readonly BaKeybind DefaultRouteShortcut =
            new BaKeybind(Key.Y, BaKeyModifiers.Control | BaKeyModifiers.Shift);
        private static readonly BaKeybind DefaultAutoMoveShortcut =
            new BaKeybind(Key.X, BaKeyModifiers.Control | BaKeyModifiers.Shift);
        private static readonly BaKeybind DefaultHideUiShortcut =
            new BaKeybind(Key.C, BaKeyModifiers.Control | BaKeyModifiers.Shift);

        private static BaKeybindHandle _routeShortcut;
        private static BaKeybindHandle _autoMoveShortcut;
        private static BaKeybindHandle _hideUiShortcut;

        internal static bool UiHidden { get; private set; }

        internal static ModOptions AddOptions(ModOptions options)
        {
            DisposeHandles();

            return options
                .AddSplitter()
                .AddKeybind(
                    RouteShortcutOptionId,
                    "voogle_route_options_route_shortcut",
                    DefaultRouteShortcut,
                    out _routeShortcut,
                    OnBindingChanged,
                    uiText: ModUiText.CreateShortcutUiText())
                .AddKeybind(
                    AutoMoveShortcutOptionId,
                    "voogle_route_options_auto_move_shortcut",
                    DefaultAutoMoveShortcut,
                    out _autoMoveShortcut,
                    OnBindingChanged,
                    uiText: ModUiText.CreateShortcutUiText())
                .AddKeybind(
                    HideUiShortcutOptionId,
                    "voogle_route_options_hide_ui_shortcut",
                    DefaultHideUiShortcut,
                    out _hideUiShortcut,
                    uiText: ModUiText.CreateShortcutUiText());
        }

        internal static string AddRouteButtonHint(string label) =>
            AddButtonHint(label, _routeShortcut);

        internal static string AddAutoMoveButtonHint(string label) =>
            AddButtonHint(label, _autoMoveShortcut);

        internal static void Tick()
        {
            // Hide/Show must work on the city map even when a UI widget is selected.
            if (_hideUiShortcut != null && _hideUiShortcut.WasPressedThisFrame(respectGameUi: false))
                ToggleUiHidden();

            if (_routeShortcut != null && _routeShortcut.WasPressedThisFrame())
                RouteActionPanel.TryInvokeRouteShortcut();

            if (_autoMoveShortcut != null && _autoMoveShortcut.WasPressedThisFrame())
                RouteActionPanel.TryInvokeAutoMoveShortcut();
        }

        internal static void Shutdown()
        {
            UiHidden = false;
            DisposeHandles();
        }

        private static void ToggleUiHidden()
        {
            UiHidden = !UiHidden;
            if (UiHidden)
                HideTransientWindows();

            RouteActionPanel.ForceUpdateVisibility();
            ModLog.Info("VoogleRoute UI hidden = " + UiHidden);
        }

        private static void HideTransientWindows()
        {
            RouteSettingsUi.Close();
            AutoDriveConfirmPopup.Close();
            CityMapDestinationPopup.Close();
            CityMapBookmarkAddDialog.Close();
            RouteRecalcBanner.ForceHide();
        }

        private static string AddButtonHint(string label, BaKeybindHandle shortcut)
        {
            if (shortcut == null || !shortcut.IsBound)
                return label;

            var binding = shortcut.Binding.ToDisplayString().Replace(" + ", "+");
            return label + "\n<size=70%>[" + binding + "]</size>";
        }

        private static void OnBindingChanged(BaKeybind _) => RouteActionPanel.RefreshVisual();

        private static void DisposeHandles()
        {
            _routeShortcut?.Dispose();
            _routeShortcut = null;
            _autoMoveShortcut?.Dispose();
            _autoMoveShortcut = null;
            _hideUiShortcut?.Dispose();
            _hideUiShortcut = null;
        }
    }
}
