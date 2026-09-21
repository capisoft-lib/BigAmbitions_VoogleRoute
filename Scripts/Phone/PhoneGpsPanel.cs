using System.Collections.Generic;
using BigAmbitions.InputSystem;
using Capisoft.Lib.BaUnifiedUI.Controls;
using Capisoft.Lib.BaUnifiedUI.Core;
using Capisoft.Lib.BaUnifiedUI.Fluent;
using Helpers;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VoogleRoute;
using VoogleRoute.Navigation;
using VoogleRoute.UI;

namespace VoogleRoute.Phone
{
    /// <summary>GPS app hosted inside the HUD phone screen. Uses the vanilla city map, heading-up.</summary>
    internal static class PhoneGpsPanel
    {
        private const string RootName = "VoogleRoute_PhoneGps_v7";
        private const int SearchRowCount = 6;

        private static readonly Color Cyan = new Color(0.18f, 0.78f, 1f, 1f);

        private static GameObject _root;
        private static RectTransform _panel;
        private static RectTransform _mapRect;
        private static RawImage _vanillaMap;
        private static PhoneGpsMapView _map;
        private static PhoneGpsMapInput _input;
        private static TextMeshProUGUI _emptySearch;
        private static TextMeshProUGUI _mapButtonLabel;
        private static RectTransform _setDestButton;
        private static TextMeshProUGUI _setDestLabel;
        private static BaUiSearchField _search;
        private static RectTransform _searchList;
        private static readonly List<SearchRow> SearchRows = new List<SearchRow>();
        private static bool _followPlayer = true;
        private static bool _resumeWhenPhoneReturns;
        private static bool _blockWorldMove;
        private static bool _hasDraft;
        private static bool _offerDestination;
        private static Vector3 _draftWorld;
        private static string _destinationName = "";
        private static PointerEventData _pointerData;
        private static readonly List<RaycastResult> RaycastHits = new List<RaycastResult>();
        private static float _nextSheet;
        private static float _visibleMeters = 160f;
        private static Vector3 _smoothLookAt;
        private static float _smoothHeading;
        private static Vector3 _lookAtVelocity;
        private static float _headingVelocity;
        private static bool _smoothReady;
        private static readonly List<InputAction> HeldPlayerActions = new List<InputAction>();

        internal static bool IsOpen => _root != null && _root.activeSelf;

        internal static void Show(bool recenter = true)
        {
            if (_root != null && _root.name != RootName)
                Destroy();

            if (!PhoneGpsScreenHost.TryPrepare(out var screen, _root))
            {
                ModLog.Info("[WARN] Phone GPS screen not found; staying on the home grid.");
                return;
            }

            EnsureCreated(screen);
            if (recenter)
                _followPlayer = true;
            _smoothReady = false;
            _lookAtVelocity = Vector3.zero;
            _headingVelocity = 0f;
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            PhoneGpsScreenHost.FitInner(_panel, screen);
            BaUi.ApplyLayer(_root);
            PhoneGpsVanillaCamera.Attach(_vanillaMap);
            PhonePagesInputGuard.SuppressPageScroll();
            RefreshLocalizedText();
            RefreshSearch("");
            if (_setDestButton != null)
                _setDestButton.gameObject.SetActive(_offerDestination);
            FollowView();
            ModLog.Info(
                "Phone GPS opened inside " + screen.name
                + " (" + screen.rect.width.ToString("F0") + "x" + screen.rect.height.ToString("F0") + ").");
        }

        internal static void Hide() => Hide(userDismissed: true);

        private static void Hide(bool userDismissed)
        {
            EndMapPressBlock();
            ClearGpsSelection();
            if (userDismissed)
            {
                _resumeWhenPhoneReturns = false;
                ClearDraftPin();
            }

            PhoneGpsVanillaCamera.Release();
            PhonePagesInputGuard.Release();
            RestorePlayerActions();
            if (_root != null)
                _root.SetActive(false);
            PhoneGpsScreenHost.Restore();
            BaUiFocus.ReleaseForMovement();
        }

        internal static void Destroy()
        {
            Hide();
            if (_root != null)
                Object.Destroy(_root);
            _root = null;
            _panel = null;
            _mapRect = null;
            _map = null;
            _vanillaMap = null;
            _input = null;
            _emptySearch = null;
            _mapButtonLabel = null;
            _setDestButton = null;
            _setDestLabel = null;
            _search = null;
            _searchList = null;
            SearchRows.Clear();
        }

        internal static void Tick()
        {
            TryResumeAfterOverlay();

            if (!IsOpen)
                return;

            var phone = InstanceBehavior<UIs>.Instance?.smartphoneUI;
            if (!PhonePagesPresence.IsActive() || !GameState.IsWorldReady() || phone == null)
            {
                Hide();
                return;
            }

            if (_root == null || _root.transform.parent == null)
            {
                Hide();
                return;
            }

            if (GameState.IsModUiHidden)
            {
                Hide();
                return;
            }

            if (GameState.HidesSmartphone())
            {
                _resumeWhenPhoneReturns = true;
                Hide(userDismissed: false);
                return;
            }

            if (!phone.gameObject.activeInHierarchy)
            {
                Hide();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (IsSearchFocused())
                {
                    _search.Field.DeactivateInputField();
                    if (EventSystem.current != null)
                        EventSystem.current.SetSelectedGameObject(null);
                    RestorePlayerActions();
                    return;
                }

                Hide();
                return;
            }

            PhonePagesInputGuard.SuppressPageScroll();
            if (IsSearchFocused() && !_blockWorldMove)
                CaptureSearchFocus();
            else if (!_blockWorldMove)
                RestorePlayerActions();

            if (!IsSearchFocused())
            {
                var wheel = Input.mouseScrollDelta.y;
                if (Mathf.Abs(wheel) > 0.01f)
                    OnMapScrolled(wheel);
            }

            if (Time.unscaledTime >= _nextSheet)
            {
                _nextSheet = Time.unscaledTime + 0.25f;
                RefreshMapButtonLabel();
            }
        }

        internal static void LateTick()
        {
            if (IsOpen && !Input.GetMouseButton(0))
                ClearGpsSelection();

            if (!IsOpen)
                return;

            if (IsSearchFocused() && !_blockWorldMove)
                CaptureSearchFocus();

            if (_map == null || _mapRect == null)
                return;

            FollowView();
        }

        internal static void RefreshLocalizedText()
        {
            if (_search != null)
                _search.SetPlaceholder(ModUiText.PhoneSearchPlaceholder);
            if (_emptySearch != null)
                _emptySearch.text = ModUiText.PhoneNoMatch;
            if (_setDestLabel != null)
                _setDestLabel.text = ModUiText.PhoneSetDestination;
            RefreshMapButtonLabel();
        }

        internal static void OnMapDragged(Vector2 delta)
        {
            _followPlayer = false;
            if (_map == null)
                return;

            var heading = 0f;
            if (TryGetPlayerPose(out _, out var playerHeading))
                heading = playerHeading;
            _map.SetView(_map.LookAt, heading, MapSize());
            _map.PanByPixels(delta);
            _visibleMeters = _map.VisibleMeters;
            _smoothLookAt = _map.LookAt;
            _lookAtVelocity = Vector3.zero;
            FollowView();
        }

        private static int _zoomFrame = -1;

        internal static void OnMapScrolled(float scrollY)
        {
            if (Mathf.Abs(scrollY) < 0.01f)
                return;
            if (Time.frameCount == _zoomFrame)
                return;
            _zoomFrame = Time.frameCount;

            _visibleMeters *= scrollY > 0f ? 0.86f : 1.16f;
            _visibleMeters = Mathf.Clamp(_visibleMeters, 50f, 480f);
            if (_map != null)
                _map.VisibleMeters = _visibleMeters;
            FollowView();
        }

        internal static void OnMapHeld(Vector2 local)
        {
            if (!TryMapLocalToWorld(local, out var world))
                return;

            _hasDraft = true;
            _draftWorld = world;
            FollowView();
        }

        internal static void OnMapHoldReleased()
        {
            if (!_hasDraft || _setDestButton == null)
                return;

            _offerDestination = true;
            _setDestButton.gameObject.SetActive(true);
            if (_setDestLabel != null)
                _setDestLabel.text = ModUiText.PhoneSetDestination;
        }

        private static void OnConfirmDraft()
        {
            if (!_hasDraft)
                return;

            var world = _draftWorld;
            ClearDraftPin();
            _destinationName = ModUiText.PhoneDroppedPin;
            WorldDestinationService.SetWorldDestination(
                world,
                _destinationName,
                NavigationTargetTracker.WorldPositionSource);
            var eventSystem = EventSystem.current;
            if (eventSystem != null
                && _setDestButton != null
                && eventSystem.currentSelectedGameObject == _setDestButton.gameObject)
                eventSystem.SetSelectedGameObject(null);
            FollowView();
        }

        private static bool TryMapLocalToWorld(Vector2 local, out Vector3 world)
        {
            if (PhoneGpsVanillaCamera.TryLocalToWorld(_mapRect, local, ResolveGroundY(), out world))
                return true;

            if (_map == null)
            {
                world = default;
                return false;
            }

            world = _map.LocalToWorld(local);
            world.y = ResolveGroundY();
            return true;
        }

        private static void EnsureCreated(RectTransform screen)
        {
            if (_root != null)
            {
                _root.transform.SetParent(screen, false);
                PhoneGpsScreenHost.FitInner(_panel, screen);
                return;
            }

            BaUi.EnsureReady();
            _panel = CreateRect(screen, RootName, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            PhoneGpsScreenHost.FitInner(_panel, screen);
            _root = _panel.gameObject;
            _panel.gameObject.AddComponent<RectMask2D>();

            _mapRect = CreateRect(_panel, "Map", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            Stretch(_mapRect);
            _vanillaMap = _mapRect.gameObject.AddComponent<RawImage>();
            _vanillaMap.color = Color.white;
            _vanillaMap.raycastTarget = true;
            _vanillaMap.maskable = true;
            _mapRect.gameObject.AddComponent<RectMask2D>();
            _input = _mapRect.gameObject.AddComponent<PhoneGpsMapInput>();
            _input.Dragged = OnMapDragged;
            _input.Scrolled = OnMapScrolled;
            _input.Held = OnMapHeld;
            _input.HoldReleased = OnMapHoldReleased;
            if (_root.GetComponent<PhoneGpsMapPressGuard>() == null)
                _root.AddComponent<PhoneGpsMapPressGuard>();

            var overlay = CreateRect(_mapRect, "Overlay", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            Stretch(overlay);
            var player = CreateMarker(overlay, "Player", Color.white);
            player.gameObject.SetActive(false);
            var pin = CreateMarker(overlay, "Pin", Cyan);
            var draft = CreateMarker(overlay, "DraftPin", new Color(1f, 0.78f, 0.15f, 1f));
            draft.gameObject.SetActive(false);
            _map = new PhoneGpsMapView(overlay, player, pin, draft);
            _map.SetBackgroundVisible(false);

            var searchHost = CreateRect(_panel, "SearchHost", new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f));
            searchHost.sizeDelta = new Vector2(0f, 70f);
            searchHost.anchoredPosition = new Vector2(0f, -10f);
            searchHost.offsetMin = new Vector2(10f, searchHost.offsetMin.y);
            searchHost.offsetMax = new Vector2(-90f, searchHost.offsetMax.y);
            _search = BaUiSearchField.Create(searchHost, 1.7f);
            Stretch(_search.Rect);
            ConfigureSearchField(_search);
            _search.Wire(RefreshSearch, OnSearchSelected);
            if (_search.Rect.GetComponent<PhoneGpsSearchFocus>() == null)
                _search.Rect.gameObject.AddComponent<PhoneGpsSearchFocus>();

            var recenter = CreateTextButton(_mapRect, "Recenter", "◎", Recenter, 56f, 56f, 26f);
            recenter.anchorMin = recenter.anchorMax = new Vector2(1f, 0f);
            recenter.pivot = new Vector2(1f, 0f);
            recenter.anchoredPosition = new Vector2(-10f, 92f);
            recenter.gameObject.GetComponent<Image>().color = new Color(0.07f, 0.16f, 0.32f, 0.9f);

            var mapButton = CreateTextButton(_panel, "OpenMap", ModUiText.PhoneOpenMap("M"), OnOpenCityMap, 62f, 280f, 26f);
            mapButton.anchorMin = mapButton.anchorMax = new Vector2(0.5f, 0f);
            mapButton.pivot = new Vector2(0.5f, 0f);
            mapButton.anchoredPosition = new Vector2(0f, 14f);
            mapButton.gameObject.GetComponent<Image>().color = new Color(0.12f, 0.62f, 0.38f, 1f);
            _mapButtonLabel = mapButton.GetComponentInChildren<TextMeshProUGUI>();

            _setDestButton = CreateTextButton(
                _panel, "SetDestination", ModUiText.PhoneSetDestination, OnConfirmDraft, 52f, 300f, 20f);
            _setDestButton.anchorMin = _setDestButton.anchorMax = new Vector2(0.5f, 0f);
            _setDestButton.pivot = new Vector2(0.5f, 0f);
            _setDestButton.anchoredPosition = new Vector2(0f, 84f);
            _setDestButton.gameObject.GetComponent<Image>().color = new Color(0.12f, 0.45f, 0.82f, 1f);
            _setDestLabel = _setDestButton.GetComponentInChildren<TextMeshProUGUI>();
            _setDestButton.gameObject.SetActive(false);

            _searchList = CreateRect(_panel, "SearchList", new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f));
            _searchList.sizeDelta = new Vector2(0f, SearchRowCount * 42f);
            _searchList.anchoredPosition = new Vector2(0f, -88f);
            _searchList.offsetMin = new Vector2(10f, _searchList.offsetMin.y);
            _searchList.offsetMax = new Vector2(-90f, _searchList.offsetMax.y);
            _searchList.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.12f, 0.24f, 0.96f);
            _emptySearch = CreateLabel(_searchList, "Empty", 16f, new Color(1f, 1f, 1f, 0.65f), TextAlignmentOptions.Center);
            Stretch(_emptySearch.rectTransform);
            for (var i = 0; i < SearchRowCount; i++)
                SearchRows.Add(CreateSearchRow(_searchList, i));
            _searchList.gameObject.SetActive(false);

            CreateCloseButton(_panel).SetAsLastSibling();
            BaUi.ApplyLayer(_root);
            RefreshMapButtonLabel();
        }

        private static void FollowView()
        {
            if (_map == null || _mapRect == null)
                return;

            if (!TryGetVisualPose(out var position, out var heading)
                && !TryGetPlayerPose(out position, out heading))
                return;

            var dt = Time.unscaledDeltaTime;
            if (dt < 0.0001f)
                dt = 0.016f;

            if (!_smoothReady)
            {
                _smoothLookAt = _followPlayer ? position : _map.LookAt;
                _smoothHeading = heading;
                _lookAtVelocity = Vector3.zero;
                _headingVelocity = 0f;
                _smoothReady = true;
            }
            else
            {
                if (_followPlayer)
                {
                    _smoothLookAt = position;
                    _lookAtVelocity = Vector3.zero;
                }
                else
                {
                    _smoothLookAt = Vector3.SmoothDamp(
                        _smoothLookAt,
                        _map.LookAt,
                        ref _lookAtVelocity,
                        0.03f,
                        Mathf.Infinity,
                        dt);
                }

                _smoothHeading = Mathf.SmoothDampAngle(
                    _smoothHeading,
                    heading,
                    ref _headingVelocity,
                    0.04f,
                    Mathf.Infinity,
                    dt);
            }

            _map.SetView(_smoothLookAt, _smoothHeading, MapSize());
            _map.VisibleMeters = _visibleMeters;

            PhoneGpsVanillaCamera.Tick(_map.LookAt, _smoothHeading, _visibleMeters, _mapRect);
            if (_vanillaMap != null)
                _vanillaMap.enabled = PhoneGpsVanillaCamera.IsReady;

            var hasDest = NavigationTargetTracker.HasTarget;
            _map.SetDraft(_hasDraft, _draftWorld);
            _map.Redraw(
                position,
                hasDest,
                hasDest ? NavigationTargetTracker.ActiveTarget : default,
                drawBackground: !PhoneGpsVanillaCamera.IsReady);
        }

        private static Vector2 MapSize() =>
            _mapRect != null ? _mapRect.rect.size : Vector2.one;

        private static void Recenter()
        {
            _followPlayer = true;
            _smoothReady = false;
            FollowView();
        }

        private static void OnCloseClicked() => Hide();

        private static void TryResumeAfterOverlay()
        {
            if (!_resumeWhenPhoneReturns || IsOpen)
                return;
            if (!PhonePagesPresence.IsActive() || !GameState.IsWorldReady() || GameState.IsModUiHidden)
                return;

            var phone = InstanceBehavior<UIs>.Instance?.smartphoneUI;
            if (phone == null || !phone.gameObject.activeInHierarchy || GameState.HidesSmartphone())
                return;

            Show(recenter: false);
            if (IsOpen)
                _resumeWhenPhoneReturns = false;
        }

        internal static void MaintainMapPressBlock()
        {
            if (!IsOpen || _root == null)
            {
                EndMapPressBlock();
                return;
            }

            var mouse = (Vector2)Input.mousePosition;
            var uiHit = TryHitPhoneGps(mouse, out var hit);
            if (!Input.GetMouseButton(0) || (!uiHit && !IsScreenOnMap(mouse)))
            {
                EndMapPressBlock();
                return;
            }

            if (!uiHit)
                hit = _mapRect.gameObject;

            _blockWorldMove = true;
            var select = hit;
            if (_search?.Rect != null && _search.Field != null && hit.transform.IsChildOf(_search.Rect))
                select = _search.Field.gameObject;

            ForceUiLayer(select);
            var eventSystem = EventSystem.current;
            var selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            var alreadyOnGps = selected != null && selected.transform.IsChildOf(_root.transform);
            if (eventSystem != null && !alreadyOnGps)
                eventSystem.SetSelectedGameObject(select);
            HoldPlayerActions();
        }

        private static void EndMapPressBlock()
        {
            if (!_blockWorldMove)
                return;

            _blockWorldMove = false;
            if (!IsSearchFocused())
                RestorePlayerActions();
        }

        private static void ClearGpsSelection()
        {
            if (IsSearchFocused())
                return;

            var eventSystem = EventSystem.current;
            var selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            if (selected == null || _root == null || !selected.transform.IsChildOf(_root.transform))
                return;

            eventSystem.SetSelectedGameObject(null);
        }

        private static bool IsScreenOnMap(Vector2 screen)
        {
            if (_mapRect == null || !_mapRect.gameObject.activeInHierarchy)
                return false;

            var canvas = _mapRect.GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera;
            return RectTransformUtility.RectangleContainsScreenPoint(_mapRect, screen, cam);
        }

        private static bool TryHitPhoneGps(Vector2 screen, out GameObject hit)
        {
            hit = null;
            var eventSystem = EventSystem.current;
            if (eventSystem == null || _root == null)
                return false;

            if (_pointerData == null)
                _pointerData = new PointerEventData(eventSystem);
            _pointerData.Reset();
            _pointerData.position = screen;
            RaycastHits.Clear();
            eventSystem.RaycastAll(_pointerData, RaycastHits);
            if (RaycastHits.Count == 0)
                return false;

            hit = RaycastHits[0].gameObject;
            return hit != null && hit.transform.IsChildOf(_root.transform);
        }

        private static void ClearDraftPin()
        {
            _hasDraft = false;
            _offerDestination = false;
            _draftWorld = default;
            if (_setDestButton != null)
                _setDestButton.gameObject.SetActive(false);
            _map?.SetDraft(false, default);
        }

        private static void OnOpenCityMap()
        {
            try
            {
                if (!CityMap.CanOpenMap())
                    return;

                _resumeWhenPhoneReturns = true;
                Hide(userDismissed: false);
                InstanceBehavior<CityManager>.Instance?.cityMap?.Toggle();
            }
            catch
            {
                // City map can be unavailable during scene transitions.
            }
        }

        private static void OnSearchSelected()
        {
            if (_search?.Field == null)
                return;
            _search.Field.ActivateInputField();
            CaptureSearchFocus();
        }

        private static bool IsSearchFocused() =>
            _search?.Field != null && _search.Field.isFocused;

        internal static void CaptureSearchFocus()
        {
            if (!IsOpen || _search?.Field == null || !_search.Field.isFocused)
                return;

            var field = _search.Field;
            ForceUiLayer(_search.Rect.gameObject);
            if (_search.Rect.parent != null)
                ForceUiLayer(_search.Rect.parent.gameObject);

            var eventSystem = EventSystem.current;
            if (eventSystem != null && eventSystem.currentSelectedGameObject != field.gameObject)
                eventSystem.SetSelectedGameObject(field.gameObject);

            HoldPlayerActions();
        }

        private static void ForceUiLayer(GameObject go)
        {
            if (go == null)
                return;
            BaUi.ApplyLayer(go);
            go.layer = LayerHelper.UiLayerIndex;
            var parent = go.transform.parent;
            if (parent != null)
                parent.gameObject.layer = LayerHelper.UiLayerIndex;
        }

        private static void HoldPlayerActions()
        {
            try
            {
                var map = InputActionHelper.PlayerInputActionMap;
                if (map == null)
                    return;

                foreach (var pair in map)
                {
                    var action = pair.Value;
                    if (action == null || !action.enabled)
                        continue;
                    action.Disable();
                    if (!HeldPlayerActions.Contains(action))
                        HeldPlayerActions.Add(action);
                }
            }
            catch
            {
                // Layer + EventSystem selection still feed HasInputSelected.
            }
        }

        private static void RestorePlayerActions()
        {
            for (var i = 0; i < HeldPlayerActions.Count; i++)
            {
                try
                {
                    HeldPlayerActions[i]?.Enable();
                }
                catch
                {
                    // Ignore if the action was torn down with the scene.
                }
            }

            HeldPlayerActions.Clear();
        }

        private static void ConfigureSearchField(BaUiSearchField search)
        {
            if (search?.Field == null)
                return;

            var field = search.Field;
            field.customCaretColor = true;
            field.caretColor = Color.white;
            field.caretWidth = 3;
            field.caretBlinkRate = 0.85f;
            field.selectionColor = new Color(0.25f, 0.65f, 1f, 0.35f);
            if (field.textComponent != null)
            {
                field.textComponent.fontSize = 26f;
                field.textComponent.color = Color.white;
            }

            if (search.Placeholder != null)
                search.Placeholder.fontSize = 24f;

            var hit = search.Rect.GetComponent<Image>();
            if (hit == null)
                hit = search.Rect.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0.35f);
            hit.raycastTarget = true;

            var guard = search.Rect.GetComponent<BaUiInputGuard>();
            if (guard == null)
                guard = search.Rect.gameObject.AddComponent<BaUiInputGuard>();
            guard.Bind(field);
        }

        private static void RefreshSearch(string value)
        {
            var filter = value ?? "";
            var shown = 0;
            if (!string.IsNullOrWhiteSpace(filter))
            {
                var bookmarks = BookmarkStore.All;
                for (var i = 0; i < bookmarks.Count && shown < SearchRowCount; i++)
                {
                    var bookmark = bookmarks[i];
                    if (bookmark == null || !bookmark.MatchesFilter(filter))
                        continue;

                    var row = SearchRows[shown];
                    row.Root.SetActive(true);
                    row.Label.text = bookmark.DisplayName;
                    var captured = bookmark;
                    row.Button.onClick.RemoveAllListeners();
                    row.Button.onClick.AddListener(() => SelectBookmark(captured));
                    shown++;
                }
            }

            for (var i = shown; i < SearchRows.Count; i++)
                SearchRows[i].Root.SetActive(false);

            var hasQuery = !string.IsNullOrWhiteSpace(filter);
            _searchList.gameObject.SetActive(hasQuery);
            _emptySearch.gameObject.SetActive(hasQuery && shown == 0);
        }

        private static void SelectBookmark(BookmarkEntry bookmark)
        {
            if (!BookmarkDestinationService.TrySetFromBookmark(bookmark))
                return;

            _destinationName = bookmark.DisplayName;
            _search.Field.text = "";
            RefreshSearch("");
            if (_search.Field != null)
                _search.Field.DeactivateInputField();
            RestorePlayerActions();
            BaUiFocus.ReleaseForMovement();
            FollowView();
        }

        private static void RefreshMapButtonLabel()
        {
            if (_mapButtonLabel == null)
                return;
            _mapButtonLabel.text = ModUiText.PhoneOpenMap(ResolveOpenMapBinding());
        }

        private static string ResolveOpenMapBinding()
        {
            try
            {
                var action = InputActionHelper.PlayerInputActionMap[PlayerAction.OpenMap];
                if (action != null)
                {
                    var display = action.GetBindingDisplayString();
                    if (!string.IsNullOrWhiteSpace(display))
                        return display.Replace("Press ", "").Trim();
                }
            }
            catch
            {
                // Fall back to the vanilla default.
            }

            return "M";
        }

        private static bool TryGetVisualPose(out Vector3 position, out float heading)
        {
            position = default;
            heading = 0f;
            try
            {
                if (TryGetVehicleVisual(out position, out heading))
                    return true;

                var player = GameManager.Instance?.playerController ?? PlayerHelper.PlayerController;
                if (player != null)
                {
                    var transform = player.transform;
                    position = transform.position;
                    heading = transform.eulerAngles.y;
                    return position.sqrMagnitude > 0.01f;
                }
            }
            catch
            {
                // Fall back to the location snapshot.
            }

            return false;
        }

        private static bool TryGetVehicleVisual(out Vector3 position, out float heading)
        {
            position = default;
            heading = 0f;
            var vehicle = GameManager.Instance?.selectedVehicle
                ?? VehicleHelper.GetCurrentVehicleBase();
            if (vehicle == null)
                return false;

            try
            {
                if (vehicle.vehicleType != null && vehicle.vehicleType.spawnInPlayerObject)
                    return false;
            }
            catch
            {
                // Keep using the selected vehicle if its type cannot be read.
            }

            var transform = vehicle.transform;
            position = transform.position;
            var forward = transform.forward;
            forward.y = 0f;
            heading = forward.sqrMagnitude > 0.0001f
                ? Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg
                : transform.eulerAngles.y;
            return position.sqrMagnitude > 0.01f;
        }

        private static bool TryGetPlayerPose(out Vector3 position, out float heading)
        {
            heading = PlayerLocationSession.Snapshot.HeadingDeg;
            if (MovementModeDetector.TryGetPathOrigin(out position))
                return true;

            if (PlayerLocationSession.IsAvailable)
            {
                position = PlayerLocationSession.Snapshot.Position;
                return position.sqrMagnitude > 0.01f;
            }

            position = default;
            return false;
        }

        private static float ResolveGroundY()
        {
            if (MovementModeDetector.TryGetPathOrigin(out var origin))
                return origin.y;
            return PlayerLocationSession.IsAvailable ? PlayerLocationSession.Snapshot.Position.y : 0f;
        }

        private static SearchRow CreateSearchRow(RectTransform parent, int index)
        {
            var row = CreateRect(parent, "Row" + index, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f));
            row.sizeDelta = new Vector2(0f, 36f);
            row.anchoredPosition = new Vector2(0f, -2f - index * 38f);
            row.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.04f);
            var button = row.gameObject.AddComponent<Button>();
            var label = CreateLabel(row, "Label", 16f, Color.white, TextAlignmentOptions.MidlineLeft);
            Stretch(label.rectTransform, 8f, 0f, 6f, 0f);
            row.gameObject.SetActive(false);
            return new SearchRow { Root = row.gameObject, Button = button, Label = label };
        }

        private static RectTransform CreateCloseButton(RectTransform parent)
        {
            var rect = CreateRect(parent, "Close", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
            rect.sizeDelta = new Vector2(66f, 66f);
            rect.anchoredPosition = new Vector2(-8f, -10f);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.78f, 0.14f, 0.14f, 1f);
            var button = rect.gameObject.AddComponent<Button>();
            button.onClick.AddListener(OnCloseClicked);
            CreateCloseBar(rect, 45f);
            CreateCloseBar(rect, -45f);
            return rect;
        }

        private static void CreateCloseBar(RectTransform parent, float rotationZ)
        {
            var bar = CreateRect(parent, "Bar", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            bar.sizeDelta = new Vector2(34f, 6f);
            bar.localEulerAngles = new Vector3(0f, 0f, rotationZ);
            var image = bar.gameObject.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;
        }

        private static RectTransform CreateTextButton(
            Transform parent,
            string name,
            string caption,
            UnityEngine.Events.UnityAction onClick,
            float height,
            float width = 32f,
            float fontSize = 14f)
        {
            var rect = CreateRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rect.sizeDelta = new Vector2(width, height);
            rect.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);
            var button = rect.gameObject.AddComponent<Button>();
            button.onClick.AddListener(onClick);
            var label = CreateLabel(rect, "Label", fontSize, Color.white, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            label.text = caption;
            return rect;
        }

        private static RectTransform CreateMarker(RectTransform parent, string name, Color color)
        {
            var rect = CreateRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rect.sizeDelta = new Vector2(16f, 16f);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static TextMeshProUGUI CreateLabel(
            Transform parent,
            string name,
            float size,
            Color color,
            TextAlignmentOptions align)
        {
            var rect = CreateRect(parent, name, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.enableWordWrapping = false;
            BaUi.ApplyButtonFont(text);
            return text;
        }

        private static RectTransform CreateRect(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            return rect;
        }

        private static void Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private sealed class SearchRow
        {
            internal GameObject Root;
            internal Button Button;
            internal TextMeshProUGUI Label;
        }
    }
}
