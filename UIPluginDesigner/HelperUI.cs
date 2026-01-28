using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System;

namespace UIPluginDesigner
{
    // --- Helper UI Class (Mimicking ChroMapper-SongDataChanger/UserInterface/UI.cs) ---
    public class HelperUI : MonoBehaviour
    {
        private static readonly Type[] editActionMapsDisabled =
        {
            typeof(CMInput.IBookmarksActions), typeof(CMInput.IEditorScaleActions),
            typeof(CMInput.ISongSpeedActions), typeof(CMInput.IPlaybackActions),
            typeof(CMInput.IUIModeActions), typeof(CMInput.IPauseMenuActions),
            typeof(CMInput.IAudioActions), typeof(CMInput.ILightshowActions),
            typeof(CMInput.IDebugActions), typeof(CMInput.IMenusExtendedActions),
            typeof(CMInput.IPlatformDisableableObjectsActions), typeof(CMInput.IRefreshMapActions),
            typeof(CMInput.IEventUIActions), typeof(CMInput.IWorkflowsActions)
        };

        // SongDataChangerのUIクラスはDontDestroyOnLoadされる常駐オブジェクト
        // インスタンスメソッドとして機能を提供する

        public UIButton AddButton(Transform parent, string name, string text, float fontSize, float sizeX, float sizeY, float anchorX, float anchorY, float anchorPosX, float anchorPosY, UnityAction onClick, float pivotX = 0.5f, float pivotY = 0.5f)
        {
            var button = UnityEngine.Object.Instantiate(PersistentUI.Instance.ButtonPrefab, parent);
            button.name = name;
            button.Button.onClick.AddListener(onClick);
            button.SetText(text);
            button.Text.enableAutoSizing = false;
            button.Text.fontSize = fontSize;
            MoveTransform(button.transform, sizeX, sizeY, anchorX, anchorY, anchorPosX, anchorPosY, pivotX, pivotY);
            return button;
        }

        public (RectTransform, TextMeshProUGUI) AddLabel(Transform parent, string name, string text, float sizeX, float sizeY, float anchorX, float anchorY, float anchorPosX, float anchorPosY, TextAlignmentOptions alignment = TextAlignmentOptions.Center, float fontSize = 16, float pivotX = 0.5f, float pivotY = 0.5f)
        {
            var entryLabel = new GameObject(name + " Label", typeof(TextMeshProUGUI));
            var rectTransform = (RectTransform)entryLabel.transform;
            rectTransform.SetParent(parent);
            var textComponent = entryLabel.GetComponent<TextMeshProUGUI>();
            textComponent.name = name;
            textComponent.font = PersistentUI.Instance.ButtonPrefab.Text.font;
            textComponent.alignment = alignment;
            textComponent.fontSize = fontSize;
            textComponent.text = text;
            MoveTransform(rectTransform, sizeX, sizeY, anchorX, anchorY, anchorPosX, anchorPosY, pivotX, pivotY);
            return (rectTransform, textComponent);
        }

        public UITextInput AddTextInput(Transform parent, string title, string value, TextAlignmentOptions alignment, float fontSize, float sizeX, float sizeY, float anchorX, float anchorY, float anchorPosX, float anchorPosY, UnityAction<string> onChange, float pivotX = 0.5f, float pivotY = 0.5f)
        {
            var textInput = UnityEngine.Object.Instantiate(PersistentUI.Instance.TextInputPrefab, parent);
            textInput.GetComponent<Image>().pixelsPerUnitMultiplier = 3;
            textInput.name = title;
            textInput.InputField.text = value;
            textInput.InputField.onFocusSelectAll = false;
            textInput.InputField.textComponent.alignment = alignment;
            textInput.InputField.textComponent.fontSize = fontSize;
            textInput.InputField.onValueChanged.AddListener(onChange);

            textInput.InputField.onSelect.AddListener((_) => {
                CMInputCallbackInstaller.DisableActionMaps(typeof(HelperUI), editActionMapsDisabled);
            });
            textInput.InputField.onEndEdit.AddListener((_) => {
                CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(HelperUI), editActionMapsDisabled);
            });

            MoveTransform(textInput.transform, sizeX, sizeY, anchorX, anchorY, anchorPosX, anchorPosY, pivotX, pivotY);
            return textInput;
        }

        public UIDropdown AddDropdown(Transform parent, List<string> options, int value, float sizeX, float sizeY, float anchorX, float anchorY, float anchorPosX, float anchorPosY, UnityAction<int> onChange, float pivotX = 0.5f, float pivotY = 0.5f)
        {
            var dropdown = UnityEngine.Object.Instantiate(PersistentUI.Instance.DropdownPrefab, parent);
            dropdown.SetOptions(options);
            dropdown.Dropdown.onValueChanged.AddListener(onChange);
            dropdown.Dropdown.SetValueWithoutNotify(value);
            var image = dropdown.GetComponent<Image>();
            image.color = new Color(0.35f, 0.35f, 0.35f, 1f);
            image.pixelsPerUnitMultiplier = 1.5f;

            MoveTransform(dropdown.transform, sizeX, sizeY, anchorX, anchorY, anchorPosX, anchorPosY, pivotX, pivotY);
            return dropdown;
        }

        public Toggle AddCheckbox(Transform parent, bool value, float sizeX, float sizeY, float anchorX, float anchorY, float anchorPosX, float anchorPosY, UnityAction<bool> onClick, float pivotX = 0.5f, float pivotY = 0.5f)
        {
            // テンプレート検索か新規作成
            var original = Resources.FindObjectsOfTypeAll<Toggle>().FirstOrDefault(t => t.name == "Toggle");
            Toggle toggleComponent;
            if (original != null)
            {
                var toggleObject = UnityEngine.Object.Instantiate(original, parent);
                toggleComponent = toggleObject.GetComponent<Toggle>();
            }
            else
            {
                var go = new GameObject("Toggle");
                go.transform.SetParent(parent);
                toggleComponent = go.AddComponent<Toggle>();
            }

            toggleComponent.isOn = value;
            toggleComponent.onValueChanged.RemoveAllListeners();
            toggleComponent.onValueChanged.AddListener(onClick);
            MoveTransform(toggleComponent.transform, sizeX, sizeY, anchorX, anchorY, anchorPosX, anchorPosY, pivotX, pivotY);
            return toggleComponent;
        }

        public void AttachImage(GameObject obj, Color color)
        {
            var imageSetting = obj.AddComponent<Image>();
            imageSetting.sprite = PersistentUI.Instance.Sprites.Background;
            imageSetting.type = Image.Type.Sliced;
            imageSetting.color = color;
        }

        public void MoveTransform(Transform transform, float sizeX, float sizeY, float anchorX, float anchorY, float anchorPosX, float anchorPosY, float pivotX = 0.5f, float pivotY = 0.5f)
        {
            if (!(transform is RectTransform rectTransform)) return;
            rectTransform.localScale = new Vector3(1, 1, 1);
            rectTransform.sizeDelta = new Vector2(sizeX, sizeY);
            rectTransform.pivot = new Vector2(pivotX, pivotY);
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(anchorX, anchorY);
            rectTransform.anchoredPosition = new Vector3(anchorPosX, anchorPosY, 0);
        }
    }
}
