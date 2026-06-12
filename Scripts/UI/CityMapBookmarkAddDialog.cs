using Streets;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VoogleRoute.Navigation;

namespace VoogleRoute.UI
{
    internal static class CityMapBookmarkAddDialog
    {
        private const string RootName = "VoogleRoute_BookmarkAddDialog";
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
            if (_root != null)
            {
                GameStylePanelChrome.ApplyUiLayer(_root);
                return;
            }

            GameUiStyle.EnsureInitialized();

            _root = new GameObject(RootName);
            Object.DontDestroyOnLoad(_root);
            GameStylePanelChrome.SetupOverlayCanvas(_root, CanvasSortOrder);

            var dim = CreateRect(_root.transform, "Dimmer");
            Stretch(dim);
            var dimImg = dim.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.45f);
            dimImg.raycastTarget = true;
            var dimBtn = dim.gameObject.AddComponent<Button>();
            dimBtn.targetGraphic = dimImg;
            dimBtn.onClick.AddListener(ModUiFocus.Wrap((UnityAction)Close));

            var chrome = GameStylePanelChrome.Build(_root.transform, PanelWidth, PanelHeight, "Panel");
            var scale = chrome.Scale;
            var textScale = Mathf.Clamp(scale, 0.85f, 1.15f);

            var header = chrome.Header;
            GameStylePanelChrome.ApplyModalHeaderFrame(header, scale);
            var titleGo = CreateRect(header, "Title");
            titleGo.anchorMin = Vector2.zero;
            titleGo.anchorMax = Vector2.one;
            ApplyHeaderTitleInsets(titleGo, textScale);
            _titleLabel = titleGo.gameObject.AddComponent<TextMeshProUGUI>();
            _titleLabel.fontSize = NavPanelLayout.TitleFontSize * textScale;
            _titleLabel.fontStyle = FontStyles.Bold;
            _titleLabel.color = GameUiStyle.TitleColor;
            _titleLabel.alignment = TextAlignmentOptions.Center;
            GameUiStyle.ApplyTitleFont(_titleLabel);

            var body = CreateRect(chrome.Panel, "Body");
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(chrome.ContentInset, 88f);
            body.offsetMax = new Vector2(-chrome.ContentInset, -(NavPanelLayout.HeaderHeight + 12f));

            _infoLabel = CreateBodyLabel(body, "Info", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -96f), Vector2.zero, textScale);

            BuildNameField(body, textScale);

            CreateFooterButton(chrome.Panel, "CancelButton", new Vector2(-130f, 24f), textScale,
                ModUiText.BookmarkAddCancel, GameUiStyle.ApplyButtonGrey, Close);
            CreateFooterButton(chrome.Panel, "AddButton", new Vector2(130f, 24f), textScale,
                ModUiText.BookmarkAddConfirm, GameUiStyle.ApplyButtonGreen, Confirm);

            GameStylePanelChrome.ApplyUiLayer(_root);
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
            GameUiStyle.ApplyButtonFont(_namePlaceholder);

            var textGo = CreateRect(textAreaGo, "Text");
            Stretch(textGo);
            var textLabel = textGo.gameObject.AddComponent<TextMeshProUGUI>();
            textLabel.fontSize = 18f * scale;
            textLabel.color = GameUiStyle.BodyTextColor;
            textLabel.alignment = TextAlignmentOptions.MidlineLeft;
            GameUiStyle.ApplyButtonFont(textLabel);

            _nameField = fieldGo.gameObject.AddComponent<TMP_InputField>();
            _nameField.textViewport = textAreaGo;
            _nameField.textComponent = textLabel;
            _nameField.placeholder = _namePlaceholder;
            _nameField.lineType = TMP_InputField.LineType.SingleLine;
            _nameField.onSelect.AddListener(_ => OnNameFieldSelected());

            var guard = fieldGo.gameObject.AddComponent<InputHotkeyGuard>();
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

            ModUiFocus.ReleaseForMovement();

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
            tmp.color = GameUiStyle.BodyTextColor;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.enableWordWrapping = true;
            GameUiStyle.ApplyButtonFont(tmp);
            return tmp;
        }

        private static void CreateFooterButton(
            RectTransform panel,
            string name,
            Vector2 anchoredPos,
            float scale,
            string label,
            System.Action<Image> style,
            UnityAction onClick)
        {
            var rect = CreateRect(panel, name);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(220f, 44f);

            var img = GameUiStyle.CreateButtonGraphic(rect, scale, style);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            GameUiStyle.BindButtonClick(button, onClick);

            var labelGo = CreateRect(rect, "Label");
            labelGo.anchorMin = Vector2.zero;
            labelGo.anchorMax = Vector2.one;
            var tmp = labelGo.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 16f * scale;
            tmp.fontStyle = FontStyles.UpperCase;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            GameUiStyle.ApplyButtonFont(tmp);
        }

        private static void ApplyHeaderTitleInsets(RectTransform rect, float scale)
        {
            rect.offsetMin = new Vector2(
                NavPanelLayout.HeaderTextPaddingX * scale,
                NavPanelLayout.HeaderTextPaddingY * scale);
            rect.offsetMax = new Vector2(
                -NavPanelLayout.HeaderTextPaddingX * scale,
                -NavPanelLayout.HeaderTextPaddingY * scale);
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
