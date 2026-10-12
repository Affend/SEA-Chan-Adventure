using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Kumpulan helper untuk membangun UI dari kode (tombol, teks, panel, slider).
public static class UIFactory
{
    public static readonly Color Navy = new Color(0.06f, 0.08f, 0.13f, 1f);
    public static readonly Color Panel = new Color(0.10f, 0.13f, 0.20f, 0.98f);
    public static readonly Color PanelSoft = new Color(0.13f, 0.17f, 0.26f, 1f);
    public static readonly Color Accent = new Color(0.20f, 0.78f, 0.72f, 1f);
    public static readonly Color Accent2 = new Color(1f, 0.78f, 0.28f, 1f);
    public static readonly Color TextMain = new Color(0.93f, 0.95f, 0.98f, 1f);
    public static readonly Color TextDim = new Color(0.68f, 0.73f, 0.82f, 1f);
    public static readonly Color ButtonBg = new Color(0.16f, 0.22f, 0.34f, 1f);
    public static readonly Color ButtonBgHi = new Color(0.24f, 0.33f, 0.49f, 1f);

    static Font _font;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        _font = null;
        _gradient = null;
        _gradientH = null;
    }

    public static Font UIFont
    {
        get
        {
            if (_font == null)
            {
                // font "futuristik/retro" bawaan Windows (tanpa perlu download)
                try
                {
                    _font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Bahnschrift", "Consolas", "Cascadia Mono", "Segoe UI", "Tahoma", "Arial" }, 32);
                }
                catch { }
                if (_font == null) { try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { } }
                if (_font == null) { try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
            }
            return _font;
        }
    }

    // Garis tepi gelap di teks supaya lebih tajam/jelas (gaya game arcade).
    public static void AddOutline(Graphic g, float dist = 1.6f)
    {
        if (g == null) return;
        var ol = g.gameObject.GetComponent<Outline>();
        if (ol == null) ol = g.gameObject.AddComponent<Outline>();
        ol.effectColor = new Color(0f, 0f, 0f, 0.6f);
        ol.effectDistance = new Vector2(dist, -dist);
    }

    public static Canvas CreateCanvas(Transform parent, string name, int sortOrder)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static void EnsureEventSystem(Transform parent)
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem");
        go.transform.SetParent(parent, false);
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    public static RectTransform Rect(string name, Transform parent, Vector2 aMin, Vector2 aMax,
        Vector2 pivot, Vector2 anchored, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
        rt.anchoredPosition = anchored; rt.sizeDelta = size;
        return rt;
    }

    public static RectTransform Stretch(string name, Transform parent)
    {
        var rt = Rect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return rt;
    }

    public static Image Image(string name, Transform parent, Color color, Vector2 aMin, Vector2 aMax,
        Vector2 pivot, Vector2 anchored, Vector2 size)
    {
        var rt = Rect(name, parent, aMin, aMax, pivot, anchored, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    public static Image FullPanel(Transform parent, string name, Color color)
    {
        var rt = Stretch(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = true;
        return img;
    }

    // Kartu: kotak dengan garis tepi berwarna (dibuat dari 2 kotak bertumpuk).
    public static RectTransform Card(Transform parent, string name, Vector2 anchored, Vector2 size, Color border)
    {
        var outer = Rect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), anchored, size);
        var oi = outer.gameObject.AddComponent<Image>();
        oi.color = border; oi.raycastTarget = false;

        var inner = Stretch("Inner", outer);
        inner.offsetMin = new Vector2(4f, 4f);
        inner.offsetMax = new Vector2(-4f, -4f);
        var ii = inner.gameObject.AddComponent<Image>();
        ii.color = Panel; ii.raycastTarget = false;
        return inner;
    }

    public static Text Label(Transform parent, string text, Vector2 aMin, Vector2 aMax, Vector2 pivot,
        Vector2 anchored, Vector2 size, int fontSize, Color color, TextAnchor anchor, bool bold = false)
    {
        var rt = Rect("Text", parent, aMin, aMax, pivot, anchored, size);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = UIFont;
        t.text = text;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = anchor;
        t.raycastTarget = false;
        t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    public static Button TextButton(Transform parent, string text, Vector2 anchored, Vector2 size,
        int fontSize, System.Action onClick)
    {
        var rt = Rect("Button", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), anchored, size);

        var img = rt.gameObject.AddComponent<Image>();
        img.color = ButtonBg;
        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;

        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.35f, 1.4f, 1.5f, 1f);
        colors.pressedColor = new Color(0.75f, 0.85f, 1f, 1f);
        colors.selectedColor = Color.white;
        btn.colors = colors;

        var labelRt = Stretch("Label", rt);
        labelRt.offsetMin = new Vector2(12f, 6f);
        labelRt.offsetMax = new Vector2(-12f, -6f);
        var t = labelRt.gameObject.AddComponent<Text>();
        t.font = UIFont;
        t.text = text;
        t.fontSize = fontSize;
        t.color = TextMain;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        t.fontStyle = FontStyle.Bold;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        AddOutline(t, 1.6f);

        // garis aksen di sisi kiri tombol
        var stripe = Rect("Stripe", rt, new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(6f, -16f));
        var si = stripe.gameObject.AddComponent<Image>();
        si.color = Accent;
        si.raycastTarget = false;

        if (onClick != null)
        {
            btn.onClick.AddListener(() =>
            {
                AudioManager.Play("ui_click");
                onClick();
            });
        }
        return btn;
    }

    public static Slider VolumeSlider(Transform parent, string name, Vector2 anchored, Vector2 size, Color fillColor)
    {
        var rt = Rect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), anchored, size);

        var track = rt.gameObject.AddComponent<Image>();
        track.color = new Color(0.08f, 0.10f, 0.16f, 1f);

        var slider = rt.gameObject.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;

        var fillArea = Rect("Fill Area", rt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        fillArea.offsetMin = new Vector2(8f, 7f);
        fillArea.offsetMax = new Vector2(-8f, -7f);
        var fill = Rect("Fill", fillArea, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f),
            Vector2.zero, Vector2.zero);
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        var fillImg = fill.gameObject.AddComponent<Image>();
        fillImg.color = fillColor;
        fillImg.raycastTarget = false;

        var handleArea = Rect("Handle Slide Area", rt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        handleArea.offsetMin = new Vector2(8f, 0f);
        handleArea.offsetMax = new Vector2(-8f, 0f);
        var handle = Rect("Handle", handleArea, new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 0f));
        var handleImg = handle.gameObject.AddComponent<Image>();
        handleImg.color = Color.white;

        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImg;
        return slider;
    }

    // Bar sederhana (untuk HP/XP) — lebar diisi dihitung dari rasio.
    // Sekarang punya garis tepi + kilau di bagian atas supaya terlihat lebih rapi.
    public static Image Bar(Transform parent, string name, Vector2 anchored, Vector2 size, Color bg, out RectTransform fillRt)
    {
        var outer = Rect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero,
            anchored, size);
        var oi = outer.gameObject.AddComponent<Image>();
        oi.color = new Color(0.55f, 0.63f, 0.76f, 0.5f);   // garis tepi
        oi.raycastTarget = false;

        var innerBg = Rect("Bg", outer, Vector2.zero, Vector2.zero, Vector2.zero,
            new Vector2(2f, 2f), new Vector2(size.x - 4f, size.y - 4f));
        var ib = innerBg.gameObject.AddComponent<Image>();
        ib.color = new Color(0.04f, 0.05f, 0.08f, 0.95f);
        ib.raycastTarget = false;

        var inner = Rect("Fill", outer, Vector2.zero, Vector2.zero, Vector2.zero,
            new Vector2(3f, 3f), new Vector2(size.x - 6f, size.y - 6f));
        var ii = inner.gameObject.AddComponent<Image>();
        ii.color = bg;
        ii.raycastTarget = false;

        var gloss = Rect("Gloss", inner, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 4f));
        var gi = gloss.gameObject.AddComponent<Image>();
        gi.color = new Color(1f, 1f, 1f, 0.22f);
        gi.raycastTarget = false;

        fillRt = inner;
        return ii;
    }

    public static void SetBarFill(RectTransform fill, float ratio, float fullWidth, float fullHeight)
    {
        if (fill == null) return;
        fill.sizeDelta = new Vector2((fullWidth - 6f) * Mathf.Clamp01(ratio), fullHeight - 6f);
    }

    // Sprite gradasi vertikal untuk latar (dibuat dari kode).
    static Sprite _gradient;
    public static Sprite GradientSprite(Color bottom, Color top)
    {
        if (_gradient != null) return _gradient;

        int h = 128;
        var tex = new Texture2D(2, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < h; y++)
        {
            var c = Color.Lerp(bottom, top, y / (float)(h - 1));
            tex.SetPixel(0, y, c);
            tex.SetPixel(1, y, c);
        }
        tex.Apply();
        _gradient = Sprite.Create(tex, new Rect(0, 0, 2, h), new Vector2(0.5f, 0.5f), 1f);
        return _gradient;
    }

    public static Sprite CircleSprite()
    {
        return SpriteFactory.Circle;
    }

    // Gradasi horizontal (untuk lapisan gelap di atas artwork menu).
    static Sprite _gradientH;
    public static Sprite GradientSpriteH(Color left, Color right)
    {
        if (_gradientH != null) return _gradientH;

        int wpx = 128;
        var tex = new Texture2D(wpx, 2, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int x = 0; x < wpx; x++)
        {
            var c = Color.Lerp(left, right, x / (float)(wpx - 1));
            tex.SetPixel(x, 0, c);
            tex.SetPixel(x, 1, c);
        }
        tex.Apply();
        _gradientH = Sprite.Create(tex, new Rect(0, 0, wpx, 2), new Vector2(0.5f, 0.5f), 1f);
        return _gradientH;
    }

    public static InputField NameField(Transform parent, string name, Vector2 anchored, Vector2 size,
        string placeholder)
    {
        var rt = Rect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), anchored, size);

        var bg = rt.gameObject.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.08f, 0.13f, 1f);

        var field = rt.gameObject.AddComponent<InputField>();

        var textRt = Stretch("Text", rt);
        textRt.offsetMin = new Vector2(14f, 6f);
        textRt.offsetMax = new Vector2(-14f, -6f);
        var text = textRt.gameObject.AddComponent<Text>();
        text.font = UIFont;
        text.fontSize = 26;
        text.color = TextMain;
        text.alignment = TextAnchor.MiddleLeft;
        text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;

        var phRt = Stretch("Placeholder", rt);
        phRt.offsetMin = new Vector2(14f, 6f);
        phRt.offsetMax = new Vector2(-14f, -6f);
        var ph = phRt.gameObject.AddComponent<Text>();
        ph.font = UIFont;
        ph.fontSize = 26;
        ph.color = new Color(1f, 1f, 1f, 0.35f);
        ph.fontStyle = FontStyle.Italic;
        ph.text = placeholder;
        ph.alignment = TextAnchor.MiddleLeft;

        field.textComponent = text;
        field.placeholder = ph;
        field.targetGraphic = bg;
        field.characterLimit = 14;
        field.lineType = InputField.LineType.SingleLine;
        return field;
    }
}
