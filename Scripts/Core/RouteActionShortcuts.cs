using BAModAPI;
using BigAmbitions.Mods;
using Capisoft.Lib.BaUnifiedUI.Shortcuts;
using UnityEngine.InputSystem;
using VoogleRoute.UI;

namespace VoogleRoute
{
        /// <summary>Owns the two configurable shortcuts that mirror the current action-panel buttons.</summary>
    internal static class RouteActionShortcuts
    {
        private const string RouteShortcutOptionId = "route_shortcut";
        private const string AutoMoveShortcutOptionId = "auto_move_shortcut";

        private static readonly BaKeybind DefaultRouteShortcut =
            new BaKeybind(Key.Y, BaKeyModifiers.Control | BaKeyModifiers.Shift);
        private static readonly BaKeybind DefaultAutoMoveShortcut =
            new BaKeybind(Key.X, BaKeyModifiers.Control | BaKeyModifiers.Shift);

        private static BaKeybindHandle _routeShortcut;
        private static BaKeybindHandle _autoMoveShortcut;

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
                    uiText: ModUiText.CreateShortcutUiText());
        }

        internal static string AddRouteButtonHint(string label) =>
            AddButtonHint(label, _routeShortcut);

        internal static string AddAutoMoveButtonHint(string label) =>
            AddButtonHint(label, _autoMoveShortcut);

        internal static void Tick()
        {
            if (_routeShortcut != null && _routeShortcut.WasPressedThisFrame())
                RouteActionPanel.TryInvokeRouteShortcut();

            if (_autoMoveShortcut != null && _autoMoveShortcut.WasPressedThisFrame())
                RouteActionPanel.TryInvokeAutoMoveShortcut();
        }

        internal static void Shutdown() => DisposeHandles();

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
        }
    }
}
