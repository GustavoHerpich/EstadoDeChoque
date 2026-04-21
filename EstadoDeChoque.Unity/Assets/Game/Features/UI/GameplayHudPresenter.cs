using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EstadoDeChoque.Gameplay.Assets.Game.Features.UI
{
    public sealed class GameplayHudPresenter : MonoBehaviour
    {
        [SerializeField]
        private Canvas _canvas;

        [SerializeField]
        private Image _staminaBackground;

        [SerializeField]
        private Image _staminaFill;

        [SerializeField]
        private Text _interactionPromptText;

        [SerializeField]
        private Text _objectiveText;

        [SerializeField]
        private Text _infoMessageText;

        [SerializeField]
        private Text _crosshairText;

        [SerializeField]
        private Image _fadeImage;

        [SerializeField]
        private Image _letterboxTop;

        [SerializeField]
        private Image _letterboxBottom;

        [SerializeField]
        private GameObject _modalRoot;

        [SerializeField]
        private Text _modalTitleText;

        [SerializeField]
        private Text _modalBodyText;

        [SerializeField]
        private Text _modalFooterText;

        [SerializeField]
        private GameObject _subtitleRoot;

        [SerializeField]
        private Text _subtitleSpeakerText;

        [SerializeField]
        private Text _subtitleBodyText;

        private Coroutine _hideMessageRoutine;

        public bool IsModalVisible => _modalRoot != null && _modalRoot.activeSelf;

        private void Awake()
        {
            EnsureRuntimeUi();
        }

        public void SetInteractionPrompt(string prompt)
        {
            EnsureRuntimeUi();
            if (IsModalVisible)
            {
                prompt = string.Empty;
            }

            _interactionPromptText.text = prompt;
            _interactionPromptText.enabled = !string.IsNullOrWhiteSpace(prompt);
        }

        public void SetStamina(float normalizedValue, bool visible)
        {
            EnsureRuntimeUi();

            _staminaBackground.enabled = visible;
            _staminaFill.enabled = visible;
            _staminaFill.fillAmount = Mathf.Clamp01(normalizedValue);
        }

        public void ShowMessage(string message, float duration = 2f)
        {
            EnsureRuntimeUi();

            if (_hideMessageRoutine != null)
            {
                StopCoroutine(_hideMessageRoutine);
            }

            _infoMessageText.text = message;
            _infoMessageText.enabled = !string.IsNullOrWhiteSpace(message);
            _hideMessageRoutine = StartCoroutine(HideMessageAfterDelay(duration));
        }

        public void ShowObjective(string objectiveText)
        {
            EnsureRuntimeUi();
            _objectiveText.text = objectiveText;
            _objectiveText.enabled = !string.IsNullOrWhiteSpace(objectiveText);
        }

        public void ShowModal(
            string title,
            string body,
            string footer = "Pressione E, clique esquerdo ou Espaco para continuar"
        )
        {
            EnsureRuntimeUi();

            _modalRoot.SetActive(true);
            _modalTitleText.text = title;
            _modalBodyText.text = body;
            _modalFooterText.text = footer;

            _crosshairText.enabled = false;
            _interactionPromptText.enabled = false;
        }

        public void HideModal()
        {
            EnsureRuntimeUi();

            _modalRoot.SetActive(false);
            _crosshairText.enabled = true;
            _interactionPromptText.enabled = false;
        }

        public void SetLetterbox(bool visible, float heightNormalized = 0.12f)
        {
            EnsureRuntimeUi();

            float clampedHeight = Mathf.Clamp(heightNormalized, 0f, 0.3f);
            float pixelHeight = 1080f * clampedHeight;

            _letterboxTop.rectTransform.sizeDelta = new Vector2(0f, pixelHeight);
            _letterboxBottom.rectTransform.sizeDelta = new Vector2(0f, pixelHeight);
            _letterboxTop.enabled = visible;
            _letterboxBottom.enabled = visible;

            if (visible)
            {
                _crosshairText.enabled = false;
                _interactionPromptText.enabled = false;
            }
            else if (!IsModalVisible)
            {
                _crosshairText.enabled = true;
            }
        }

        public void ShowSubtitle(string speaker, string body)
        {
            EnsureRuntimeUi();

            _subtitleRoot.SetActive(
                !string.IsNullOrWhiteSpace(speaker) || !string.IsNullOrWhiteSpace(body)
            );
            _subtitleSpeakerText.text = speaker;
            _subtitleSpeakerText.enabled = !string.IsNullOrWhiteSpace(speaker);
            _subtitleBodyText.text = body;
            _subtitleBodyText.enabled = !string.IsNullOrWhiteSpace(body);
        }

        public void HideSubtitle()
        {
            EnsureRuntimeUi();

            _subtitleSpeakerText.text = string.Empty;
            _subtitleBodyText.text = string.Empty;
            _subtitleSpeakerText.enabled = false;
            _subtitleBodyText.enabled = false;
            _subtitleRoot.SetActive(false);
        }

        public void SetFade(float alpha)
        {
            EnsureRuntimeUi();

            Color fadeColor = _fadeImage.color;
            fadeColor.a = Mathf.Clamp01(alpha);
            _fadeImage.color = fadeColor;
            _fadeImage.enabled = fadeColor.a > 0.001f;
        }

        private IEnumerator HideMessageAfterDelay(float duration)
        {
            yield return new WaitForSeconds(duration);
            _infoMessageText.text = string.Empty;
            _infoMessageText.enabled = false;
            _hideMessageRoutine = null;
        }

        private void EnsureRuntimeUi()
        {
            if (_canvas != null)
            {
                return;
            }

            _canvas = gameObject.GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = gameObject.AddComponent<Canvas>();
            }

            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.pixelPerfect = false;

            CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            if (gameObject.GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            Font builtinFont = LoadBuiltinFont();

            _crosshairText = CreateTextElement(
                "Crosshair",
                "+",
                new Vector2(0f, 0f),
                28,
                TextAnchor.MiddleCenter,
                builtinFont
            );
            _interactionPromptText = CreateTextElement(
                "InteractionPrompt",
                string.Empty,
                new Vector2(0f, -210f),
                24,
                TextAnchor.MiddleCenter,
                builtinFont
            );
            _infoMessageText = CreateTextElement(
                "InfoMessage",
                string.Empty,
                new Vector2(0f, 240f),
                26,
                TextAnchor.MiddleCenter,
                builtinFont
            );
            _infoMessageText.enabled = false;

            _objectiveText = CreateTextElement(
                "ObjectiveText",
                string.Empty,
                new Vector2(0f, 0f),
                24,
                TextAnchor.UpperLeft,
                builtinFont
            );

            RectTransform objectiveRect = _objectiveText.rectTransform;
            objectiveRect.anchorMin = new Vector2(0f, 1f);
            objectiveRect.anchorMax = new Vector2(0f, 1f);
            objectiveRect.pivot = new Vector2(0f, 1f);
            objectiveRect.anchoredPosition = new Vector2(28f, -28f);
            objectiveRect.sizeDelta = new Vector2(720f, 120f);
            _objectiveText.enabled = false;

            GameObject fadeObject = CreateUiObject("Fade");
            RectTransform fadeRect = fadeObject.GetComponent<RectTransform>();
            StretchToFullScreen(fadeRect);
            _fadeImage = fadeObject.AddComponent<Image>();
            _fadeImage.color = new Color(0f, 0f, 0f, 0f);
            _fadeImage.raycastTarget = false;
            _fadeImage.enabled = false;

            GameObject letterboxTopObject = CreateUiObject("LetterboxTop");
            RectTransform letterboxTopRect = letterboxTopObject.GetComponent<RectTransform>();
            letterboxTopRect.anchorMin = new Vector2(0f, 1f);
            letterboxTopRect.anchorMax = new Vector2(1f, 1f);
            letterboxTopRect.pivot = new Vector2(0.5f, 1f);
            letterboxTopRect.anchoredPosition = Vector2.zero;
            letterboxTopRect.sizeDelta = new Vector2(0f, 120f);
            _letterboxTop = letterboxTopObject.AddComponent<Image>();
            _letterboxTop.color = Color.black;
            _letterboxTop.raycastTarget = false;
            _letterboxTop.enabled = false;

            GameObject letterboxBottomObject = CreateUiObject("LetterboxBottom");
            RectTransform letterboxBottomRect = letterboxBottomObject.GetComponent<RectTransform>();
            letterboxBottomRect.anchorMin = new Vector2(0f, 0f);
            letterboxBottomRect.anchorMax = new Vector2(1f, 0f);
            letterboxBottomRect.pivot = new Vector2(0.5f, 0f);
            letterboxBottomRect.anchoredPosition = Vector2.zero;
            letterboxBottomRect.sizeDelta = new Vector2(0f, 120f);
            _letterboxBottom = letterboxBottomObject.AddComponent<Image>();
            _letterboxBottom.color = Color.black;
            _letterboxBottom.raycastTarget = false;
            _letterboxBottom.enabled = false;

            GameObject staminaRoot = CreateUiObject("StaminaBar");
            RectTransform staminaRect = staminaRoot.GetComponent<RectTransform>();
            staminaRect.anchorMin = new Vector2(0f, 0f);
            staminaRect.anchorMax = new Vector2(0f, 0f);
            staminaRect.pivot = new Vector2(0f, 0f);
            staminaRect.anchoredPosition = new Vector2(40f, 40f);
            staminaRect.sizeDelta = new Vector2(240f, 24f);

            _staminaBackground = staminaRoot.AddComponent<Image>();
            _staminaBackground.color = new Color(0f, 0f, 0f, 0.6f);

            GameObject fillObject = CreateUiObject("Fill", staminaRoot.transform);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(4f, 4f);
            fillRect.offsetMax = new Vector2(-4f, -4f);

            _staminaFill = fillObject.AddComponent<Image>();
            _staminaFill.color = new Color(0.75f, 0.88f, 0.46f, 0.95f);
            _staminaFill.type = Image.Type.Filled;
            _staminaFill.fillMethod = Image.FillMethod.Horizontal;
            _staminaFill.fillAmount = 1f;

            _modalRoot = CreateUiObject("ModalRoot");
            RectTransform modalRootRect = _modalRoot.GetComponent<RectTransform>();
            StretchToFullScreen(modalRootRect);

            GameObject modalBackdrop = CreateUiObject("Backdrop", _modalRoot.transform);
            RectTransform modalBackdropRect = modalBackdrop.GetComponent<RectTransform>();
            StretchToFullScreen(modalBackdropRect);
            Image modalBackdropImage = modalBackdrop.AddComponent<Image>();
            modalBackdropImage.color = new Color(0f, 0f, 0f, 0.7f);

            GameObject modalPanel = CreateUiObject("Panel", _modalRoot.transform);
            RectTransform modalPanelRect = modalPanel.GetComponent<RectTransform>();
            modalPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
            modalPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
            modalPanelRect.pivot = new Vector2(0.5f, 0.5f);
            modalPanelRect.anchoredPosition = Vector2.zero;
            modalPanelRect.sizeDelta = new Vector2(960f, 480f);
            Image modalPanelImage = modalPanel.AddComponent<Image>();
            modalPanelImage.color = new Color(0.09f, 0.1f, 0.12f, 0.98f);

            _modalTitleText = CreateTextElement(
                "ModalTitle",
                string.Empty,
                Vector2.zero,
                28,
                TextAnchor.UpperLeft,
                builtinFont
            );
            _modalTitleText.transform.SetParent(modalPanel.transform, false);
            RectTransform modalTitleRect = _modalTitleText.rectTransform;
            modalTitleRect.anchorMin = new Vector2(0f, 1f);
            modalTitleRect.anchorMax = new Vector2(1f, 1f);
            modalTitleRect.pivot = new Vector2(0f, 1f);
            modalTitleRect.offsetMin = new Vector2(28f, -74f);
            modalTitleRect.offsetMax = new Vector2(-28f, -24f);

            _modalBodyText = CreateTextElement(
                "ModalBody",
                string.Empty,
                Vector2.zero,
                24,
                TextAnchor.UpperLeft,
                builtinFont
            );
            _modalBodyText.transform.SetParent(modalPanel.transform, false);
            RectTransform modalBodyRect = _modalBodyText.rectTransform;
            modalBodyRect.anchorMin = new Vector2(0f, 0f);
            modalBodyRect.anchorMax = new Vector2(1f, 1f);
            modalBodyRect.offsetMin = new Vector2(28f, 72f);
            modalBodyRect.offsetMax = new Vector2(-28f, -92f);

            _modalFooterText = CreateTextElement(
                "ModalFooter",
                string.Empty,
                Vector2.zero,
                18,
                TextAnchor.LowerRight,
                builtinFont
            );
            _modalFooterText.transform.SetParent(modalPanel.transform, false);
            RectTransform modalFooterRect = _modalFooterText.rectTransform;
            modalFooterRect.anchorMin = new Vector2(0f, 0f);
            modalFooterRect.anchorMax = new Vector2(1f, 0f);
            modalFooterRect.pivot = new Vector2(1f, 0f);
            modalFooterRect.offsetMin = new Vector2(28f, 18f);
            modalFooterRect.offsetMax = new Vector2(-28f, 58f);

            _modalRoot.SetActive(false);

            _subtitleRoot = CreateUiObject("SubtitleRoot");
            RectTransform subtitleRootRect = _subtitleRoot.GetComponent<RectTransform>();
            subtitleRootRect.anchorMin = new Vector2(0.5f, 0f);
            subtitleRootRect.anchorMax = new Vector2(0.5f, 0f);
            subtitleRootRect.pivot = new Vector2(0.5f, 0f);
            subtitleRootRect.anchoredPosition = new Vector2(0f, 128f);
            subtitleRootRect.sizeDelta = new Vector2(1180f, 150f);

            GameObject subtitleBackdropObject = CreateUiObject(
                "SubtitleBackdrop",
                _subtitleRoot.transform
            );
            RectTransform subtitleBackdropRect =
                subtitleBackdropObject.GetComponent<RectTransform>();
            StretchToFullScreen(subtitleBackdropRect);
            Image subtitleBackdrop = subtitleBackdropObject.AddComponent<Image>();
            subtitleBackdrop.color = new Color(0f, 0f, 0f, 0.58f);
            subtitleBackdrop.raycastTarget = false;

            _subtitleSpeakerText = CreateTextElement(
                "SubtitleSpeaker",
                string.Empty,
                Vector2.zero,
                22,
                TextAnchor.UpperLeft,
                builtinFont
            );
            _subtitleSpeakerText.transform.SetParent(_subtitleRoot.transform, false);
            RectTransform subtitleSpeakerRect = _subtitleSpeakerText.rectTransform;
            subtitleSpeakerRect.anchorMin = new Vector2(0f, 1f);
            subtitleSpeakerRect.anchorMax = new Vector2(1f, 1f);
            subtitleSpeakerRect.pivot = new Vector2(0.5f, 1f);
            subtitleSpeakerRect.offsetMin = new Vector2(28f, -52f);
            subtitleSpeakerRect.offsetMax = new Vector2(-28f, -12f);
            _subtitleSpeakerText.color = new Color(0.89f, 0.89f, 0.74f, 1f);
            _subtitleSpeakerText.enabled = false;

            _subtitleBodyText = CreateTextElement(
                "SubtitleBody",
                string.Empty,
                Vector2.zero,
                26,
                TextAnchor.UpperLeft,
                builtinFont
            );
            _subtitleBodyText.transform.SetParent(_subtitleRoot.transform, false);
            RectTransform subtitleBodyRect = _subtitleBodyText.rectTransform;
            subtitleBodyRect.anchorMin = new Vector2(0f, 0f);
            subtitleBodyRect.anchorMax = new Vector2(1f, 1f);
            subtitleBodyRect.pivot = new Vector2(0.5f, 0.5f);
            subtitleBodyRect.offsetMin = new Vector2(28f, 18f);
            subtitleBodyRect.offsetMax = new Vector2(-28f, -50f);
            _subtitleBodyText.enabled = false;

            _subtitleRoot.SetActive(false);

            SetInteractionPrompt(string.Empty);
            SetStamina(1f, false);
            ShowObjective(string.Empty);
            HideSubtitle();
            SetLetterbox(false);
            SetFade(0f);
        }

        private Text CreateTextElement(
            string name,
            string initialText,
            Vector2 anchoredPosition,
            int fontSize,
            TextAnchor alignment,
            Font font
        )
        {
            GameObject textObject = CreateUiObject(name);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(960f, 80f);

            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = initialText;

            return text;
        }

        private GameObject CreateUiObject(string name, Transform parentOverride = null)
        {
            var uiObject = new GameObject(name, typeof(RectTransform));
            uiObject.transform.SetParent(
                parentOverride != null ? parentOverride : _canvas.transform,
                false
            );
            return uiObject;
        }

        private static void StretchToFullScreen(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static Font LoadBuiltinFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return font;
        }
    }
}
