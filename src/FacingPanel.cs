using System.Globalization;
using Il2CppInterop.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ALMMD;

internal sealed class FacingPanel : IDisposable
{
    private GameObject root;
    private Slider slider;
    private TMP_InputField input;
    private Action<float> changed;
    private float angle;
    private Slider heightSlider;
    private TMP_InputField heightInput;
    private float height;
    private Action<float> heightChanged;
    private bool syncingHeight;

    public void Create(Transform parent, TMP_FontAsset font, float initial, Action<float> onChanged, Action<float> onHeightChanged)
    {
        changed = onChanged;
        root = Rect("Facing controls", parent, new Vector2(1400, 152), new Vector2(-230, -126));
        root.AddComponent<Image>().color = new Color(.04f,.05f,.07f,.92f);
        Label("0°", root.transform, font, new Vector2(80,50), new Vector2(-630,-16));
        Label("360°", root.transform, font, new Vector2(100,50), new Vector2(260,-16));
        var track = Rect("Angle slider", root.transform, new Vector2(800,40), new Vector2(-180,-20));
        var background = track.AddComponent<Image>(); background.color = new Color(.22f,.28f,.36f,1);
        slider = track.AddComponent<Slider>(); slider.minValue = 0; slider.maxValue = 360;
        slider.direction = Slider.Direction.LeftToRight;
        var handle = Rect("Handle", track.transform, new Vector2(26,48), new Vector2(0,4));
        var graphic = handle.AddComponent<Image>(); graphic.color = new Color(.45f,.8f,1,1);
        slider.handleRect = handle.GetComponent<RectTransform>(); slider.targetGraphic = graphic;
        var field = Rect("Angle input", root.transform, new Vector2(170,50), new Vector2(430,-16));
        var fieldImage = field.AddComponent<Image>(); fieldImage.color = new Color(.16f,.20f,.27f,1);
        input = field.AddComponent<TMP_InputField>(); input.targetGraphic = fieldImage;
        var viewport = Rect("Viewport", field.transform, new Vector2(150,46), new Vector2(0,-2));
        viewport.AddComponent<RectMask2D>(); input.textViewport = viewport.GetComponent<RectTransform>();
        input.textComponent = Label("Angle", viewport.transform, font, new Vector2(150,46), Vector2.zero);
        input.contentType = TMP_InputField.ContentType.DecimalNumber;
        input.lineType = TMP_InputField.LineType.SingleLine; input.richText = false;
        Label("degrees", root.transform, font, new Vector2(170,50), new Vector2(600,-16));
        slider.onValueChanged.AddListener((UnityEngine.Events.UnityAction<float>)(Action<float>)FromSlider);
        input.onValueChanged.AddListener((UnityEngine.Events.UnityAction<string>)(Action<string>)FromText);
        input.onEndEdit.AddListener((UnityEngine.Events.UnityAction<string>)(Action<string>)(_ => SyncText()));
        angle = Mathf.Clamp(initial,0,360); slider.SetValueWithoutNotify(angle); SyncText();
        heightChanged = onHeightChanged;
        Label("Height", root.transform, font, new Vector2(100,50), new Vector2(-630,-86));
        var heightTrack = Rect("Height slider", root.transform, new Vector2(800,40), new Vector2(-180,-90));
        heightTrack.AddComponent<Image>().color = background.color;
        heightSlider = heightTrack.AddComponent<Slider>(); heightSlider.minValue = -10; heightSlider.maxValue = 10;
        var heightHandle = Rect("Handle", heightTrack.transform, new Vector2(26,48), new Vector2(0,4));
        var heightGraphic = heightHandle.AddComponent<Image>(); heightGraphic.color = graphic.color;
        heightSlider.handleRect = heightHandle.GetComponent<RectTransform>(); heightSlider.targetGraphic = heightGraphic;
        var heightField = Rect("Height input", root.transform, new Vector2(170,50), new Vector2(430,-86));
        var heightBackground = heightField.AddComponent<Image>(); heightBackground.color = fieldImage.color;
        heightInput = heightField.AddComponent<TMP_InputField>(); heightInput.targetGraphic = heightBackground;
        var heightViewport = Rect("Viewport", heightField.transform, new Vector2(150,46), new Vector2(0,-2));
        heightViewport.AddComponent<RectMask2D>(); heightInput.textViewport = heightViewport.GetComponent<RectTransform>();
        heightInput.textComponent = Label("Height",heightViewport.transform,font,new Vector2(150,46),Vector2.zero);
        heightInput.contentType = TMP_InputField.ContentType.DecimalNumber; heightInput.lineType = TMP_InputField.LineType.SingleLine;
        heightInput.richText = false;
        heightSlider.onValueChanged.AddListener((UnityEngine.Events.UnityAction<float>)(Action<float>)SetHeight);
        heightInput.onValueChanged.AddListener((UnityEngine.Events.UnityAction<string>)(Action<string>)FromHeightText);
        heightInput.onEndEdit.AddListener((UnityEngine.Events.UnityAction<string>)(Action<string>)(_ => SyncHeightText()));
        foreach (int direction in new[] {-1,1})
        {
            int step = direction;
            var obj = Rect(direction < 0 ? "Down" : "Up", root.transform, new Vector2(70,50), new Vector2(600+direction*42,-86));
            var bg = obj.AddComponent<Image>(); bg.color = background.color;
            var btn = obj.AddComponent<Button>(); btn.targetGraphic = bg;
            btn.onClick.AddListener((UnityEngine.Events.UnityAction)(Action)(() => SetHeight(height + step*.1f)));
            Label(direction < 0 ? "−" : "+",obj.transform,font,new Vector2(70,50),Vector2.zero);
        }
        height = 0; heightSlider.SetValueWithoutNotify(0); SyncHeightText();
    }
    private void FromHeightText(string text)
    {
        if (!float.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out float value) || float.IsNaN(value) || float.IsInfinity(value)) return;
        height = value; SyncHeightSlider(); heightChanged?.Invoke(height);
    }
    private void SetHeight(float value) { if (syncingHeight) return; height=value; SyncHeightSlider(); SyncHeightText(); heightChanged?.Invoke(height); }
    private void SyncHeightSlider()
    {
        syncingHeight = true;
        try { heightSlider.minValue = Mathf.Min(-10,height); heightSlider.maxValue = Mathf.Max(10,height); heightSlider.SetValueWithoutNotify(height); }
        finally { syncingHeight = false; }
    }
    private void SyncHeightText() => heightInput.SetTextWithoutNotify(height.ToString("0.##",CultureInfo.InvariantCulture));
    internal static bool TryAngle(string text, out float value)
    {
        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || float.IsNaN(value) || float.IsInfinity(value)) return false;
        value = Mathf.Clamp(value,0,360); return true;
    }
    private void FromSlider(float value) { angle = value; SyncText(); changed?.Invoke(angle); }
    private void FromText(string text)
    {
        if (!TryAngle(text,out float value)) return;
        angle = value; slider.SetValueWithoutNotify(value); changed?.Invoke(angle);
    }
    private void SyncText() => input.SetTextWithoutNotify(angle.ToString("0.##", CultureInfo.InvariantCulture));
    public void Commit()
    {
        if (!input) return;
        FromText(input.text); SyncText(); input.DeactivateInputField();
        FromHeightText(heightInput.text); SyncHeightText(); heightInput.DeactivateInputField();
    }
    private static GameObject Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var obj = new GameObject(name, new Il2CppSystem.Type[] { Il2CppType.Of<RectTransform>() });
        var rect = obj.GetComponent<RectTransform>(); rect.SetParent(parent,false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,1);
        rect.sizeDelta = size; rect.anchoredPosition = position; return obj;
    }
    private static TextMeshProUGUI Label(string text, Transform parent, TMP_FontAsset font, Vector2 size, Vector2 position)
    {
        var label = Rect(text,parent,size,position).AddComponent<TextMeshProUGUI>();
        if (font) label.font = font;
        label.text = text; label.fontSize = 24; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        return label;
    }
    public void Dispose()
    {
        changed = null;
        heightChanged = null;
        if (slider) slider.onValueChanged.RemoveAllListeners();
        if (input) { input.onValueChanged.RemoveAllListeners(); input.onEndEdit.RemoveAllListeners(); }
        if (heightSlider) heightSlider.onValueChanged.RemoveAllListeners();
        if (heightInput) { heightInput.onValueChanged.RemoveAllListeners(); heightInput.onEndEdit.RemoveAllListeners(); }
        if (root) { root.SetActive(false); UnityEngine.Object.Destroy(root); }
        root = null; slider = null; input = null;
        heightSlider = null; heightInput = null;
    }
}
