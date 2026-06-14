using Streets;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VoogleRoute.Navigation;

using Capisoft.Lib.BaUnifiedUI.Core;
using Capisoft.Lib.BaUnifiedUI.Fluent;
using Capisoft.Lib.BaUnifiedUI.Controls;
namespace VoogleRoute.UI
{
    internal static class CityMapBookmarkAddDialog
    {
        private const string RootName = "VoogleRoute_BookmarkAddDialog_v2";
        private const int CanvasSortOrder = 12100;
        private const float PanelWidth = 520f;
        private const float PanelHeight = 300f;

        private static GameObject _root;
        private static TextMeshProUGUI _titleLabel;
        private static TextMeshProUGUI _infoLabel;
        private static TMP_InputField _nameField;
        private static TextMeshProUGUI _namePlaceholder;

        private static Address _pendingAddress;
        private static Vector3 _pendingWorldPos;
        private static string _pendingLocationLabel;
        private static bool _pendingWorldOnly;

        internal static bool IsOpen => _root != null && _root.activeSelf;
        internal static bool IsNameFocused => _nameField != null && _nameField.isFocused;

        internal static void EnsureCreated()
        {
            VoogleRoutePanelLifecycle.DestroyIfStale(ref _root, RootName, Destroy);
            if (_root != null)
            {
                BaUi.ApplyLayer(_root);
                return;
            }

            BaUi.EnsureReady();

            var built = BaUi.Modal(RootName, CanvasSortOrder, 0.45f)
                .OnDismiss(Close)
                .Panel(BaPanelRecipe.Modal, PanelWidth, height: PanelHeight)
                .Header(h => h.TitleCenter(ModUiText.BookmarkAddTitle))
                .SkipBody()
                .Build();

            _root = built.Root;
            var scale = built.Scale;
            var textScale = Mathf.Clamp(scale, 0.85f, 1.15f);
            _titleLabel = built.Header.Find("Title")?.GetComponent<TextMeshProUGUI>();

            var body = BaUiWidgets.CreateRect(built.Panel, "Body");
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(built.ContentInset, 88f);
            body.offsetMax = new Vector2(-built.ContentInset, -(BaUi.Layout.HeaderHeight + 12f));

            _infoLabel = CreateBodyLabel(body, "Info", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -96f), Vector2.zero, textScale);

            BuildNameField(body, textScale);

            BaUiWidgets.CreateFooterButton(
                built.Panel, "CancelButton", new Vector2(-130f, 24f), new Vector2(220f, 44f), textScale,
                ModUiText.BookmarkAddCancel, BaButtonStyle.Grey, Close);
            BaUiWidgets.CreateFooterButton(
                built.Panel, "AddButton", new Vector2(130f, 24f), new Vector2(220f, 44f), textScale,
                ModUiText.BookmarkAddConfirm, BaButtonStyle.Green, Confirm);

            BaUi.ApplyLayer(_root);
            _root.SetActive(false);
        }

        private static void BuildNameField(RectTransform body, float scale)
        {
            var fieldGo = CreateRect(body, "NameField");
            fieldGo.anchorMin = new Vector2(0f, 0f);
            fieldGo.anchorMax = new Vector2(1f, 0f);
            fieldGo.pivot = new Vector2(0.5f, 0f);
            fieldGo.anchoredPosition = Vector2.zero;
            fieldGo.sizeDelta = new Vector2(0f, 36f * scale);

            var bgGo = CreateRect(fieldGo, "Background");
            Stretch(bgGo);
            var bgImg = bgGo.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.35f);

            var textAreaGo = CreateRect(fieldGo, "TextArea");
            textAreaGo.anchorMin = Vector2.zero;
            textAreaGo.anchorMax = Vector2.one;
            textAreaGo.offsetMin = new Vector2(8f, 4f);
            textAreaGo.offsetMax = new Vector2(-8f, -4f);

            var placeholderGo = CreateRect(textAreaGo, "Placeholder");
            Stretch(placeholderGo);
            _namePlaceholder = placeholderGo.gameObject.AddComponent<TextMeshProUGUI>();
            _namePlaceholder.fontSize = 18f * scale;
            _namePlaceholder.color = new Color(1f, 1f, 1f, 0.45f);
            _namePlaceholder.fontStyle = FontStyles.Italic;
            _namePlaceholder.alignment = TextAlignmentOptions.MidlineLeft;
            BaUi.ApplyButtonFont(_namePlaceholder);

            var textGo = CreateRect(textAreaGo, "Text");
            Stretch(textGo);
            var textLabel = textGo.gameObject.AddComponent<TextMeshProUGUI>();
            textLabel.fontSize = 18f * scale;
            textLabel.color = BaUi.Colors.Body;
            textLabel.alignment = TextAlignmentOptions.MidlineLeft;
            BaUi.ApplyButtonFont(textLabel);

            _nameField = fieldGo.gameObject.AddComponent<TMP_InputField>();
            _nameField.textViewport = textAreaGo;
            _nameField.textComponent = textLabel;
            _nameField.placeholder = _namePlaceholder;
            _nameField.lineType = TMP_InputField.LineType.SingleLine;
            _nameField.onSelect.AddListener(_ => OnNameFieldSelected());

            var guard = fieldGo.gameObject.AddComponent<BaUiInputGuard>();
            guard.Bind(_nameField);
        }

        private static void OnNameFieldSelected()
        {
            if (_nameField == null || EventSystem.current == null)
                return;

            EventSystem.current.SetSelectedGameObject(_nameField.gameObject);
        }

        internal static void Show(Address address, string locationLabel, Vector3 worldPos, bool worldOnly = false)
        {
            EnsureCreated();
            _pendingAddress = address;
            _pendingWorldPos = worldPos;
            _pendingLocationLabel = locationLabel ?? "";
            _pendingWorldOnly = worldOnly;
            RefreshLocalizedText();
            _infoLabel.text = BuildInfoText(locationLabel, worldPos, address);
            _nameField.text = "";
            _root.SetActive(true);
            _nameField.ActivateInputField();
            OnNameFieldSelected();
        }

        private static string BuildInfoText(string locationLabel, Vector3 worldPos, Address address)
        {
            var coords = ModUiText.FormatBookmarkCoordinates(worldPos);
            var lines = coords;
            if (!string.IsNullOrWhiteSpace(locationLabel))
                lines = locationLabel + "\n" + coords;
            if (address != null)
            {
                try
                {
                    lines += "\n" + address.ToFormattedString();
                }
                catch
                {
                    // ignore
                }
            }

            return lines;
        }

        internal static void Close()
        {
            if (_nameField != null)
                _nameField.DeactivateInputField();

            BaUiFocus.ReleaseForMovement();

            if (_root != null)
                _root.SetActive(false);

            _pendingAddress = null;
            CityMapBookmarksPanel.CancelPickMode();
        }

        internal static void RefreshLocalizedText()
        {
            if (_titleLabel != null)
                _titleLabel.text = ModUiText.BookmarkAddTitle;
            if (_namePlaceholder != null)
                _namePlaceholder.text = ModUiText.BookmarkNamePlaceholder;
        }

        private static void Confirm()
        {
            if (!BookmarkStore.CanAdd())
            {
                Close();
                return;
            }

            var name = _nameField != null ? _nameField.text.Trim() : "";
            var entry = new BookmarkEntry
            {
                Name = name,
                LocationLabel = _pendingLocationLabel,
                WorldX = _pendingWorldPos.x,
                WorldY = _pendingWorldPos.y,
                WorldZ = _pendingWorldPos.z,
                WorldOnly = _pendingWorldOnly
            };

            if (!_pendingWorldOnly && _pendingAddress != null)
            {
                entry.StreetName = _pendingAddress.streetName;
                entry.StreetNumber = _pendingAddress.streetNumber;
            }

            BookmarkStore.TryAdd(entry);
            ModLog.Info("Bookmark added: " + entry.DisplayName);
            Close();
        }

        private static TextMeshProUGUI CreateBodyLabel(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            float scale)
        {
            var go = CreateRect(parent, name);
            go.anchorMin = anchorMin;
            go.anchorMax = anchorMax;
            go.offsetMin = offsetMin;
            go.offsetMax = offsetMax;
            var tmp = go.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 18f * scale;
            tmp.color = BaUi.Colors.Body;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.enableWordWrapping = true;
            BaUi.ApplyButtonFont(tmp);
            return tmp;
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        internal static void Destroy()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
                _titleLabel = null;
                _infoLabel = null;
                _nameField = null;
                _namePlaceholder = null;
            }
        }
    }
}

