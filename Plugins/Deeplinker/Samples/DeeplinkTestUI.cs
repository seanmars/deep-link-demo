using System;
using System.Text;
using Deeplink;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Deeplink tester UI — self-contained.
/// Attach to any GameObject; the full Canvas is built in Awake().
/// No Inspector wiring needed.
/// </summary>
[DefaultExecutionOrder(-50)]
public class DeeplinkTestUI : MonoBehaviour
{
    private const string appName = "devapp";

    // ── Runtime references (built in Awake) ──────────────────────────────
    private TMP_Text _statusLabel;
    private TMP_Text _rawUrlText;
    private TMP_Text _schemeText;
    private TMP_Text _hostText;
    private TMP_Text _pathText;
    private TMP_Text _fragmentText;
    private TMP_Text _paramsText;
    private TMP_InputField _testUrlInput;
    private Button _sendButton;
    private Button _clearButton;

    private int _receiveCount;

    // ── Colours ──────────────────────────────────────────────────────────
    private static readonly Color BgDark = new(0.08f, 0.09f, 0.12f, 0.97f);
    private static readonly Color BgSection = new(0.13f, 0.15f, 0.20f, 1f);
    private static readonly Color BgInput = new(0.18f, 0.20f, 0.26f, 1f);
    private static readonly Color AccentBlue = new(0.25f, 0.55f, 1.00f, 1f);
    private static readonly Color AccentGreen = new(0.30f, 0.90f, 0.50f, 1f);
    private static readonly Color TextWhite = new(0.95f, 0.95f, 0.95f, 1f);
    private static readonly Color TextGrey = new(0.55f, 0.60f, 0.68f, 1f);

    // ════════════════════════════════════════════════════════════════════
    // Lifecycle
    // ════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        BuildUI();

        DeeplinkHandler.SetHandler(OnDeeplink);
    }

    private void Start()
    {
        _sendButton.onClick.AddListener(OnSendClicked);
        _clearButton.onClick.AddListener(OnClearClicked);

        if (!string.IsNullOrEmpty(Application.absoluteURL))
            ProcessUrl(Application.absoluteURL);
        else
        {
            SetStatus("Waiting for deeplink...", new Color(0.6f, 0.8f, 1f));
            ClearDisplayFields();
        }

        _testUrlInput.text = $"{appName}://item/id/42?source=shop#details";
    }

    private void OnDestroy()
    {
        DeeplinkHandler.SetHandler(null);
    }

    // ════════════════════════════════════════════════════════════════════
    // Deeplink logic
    // ════════════════════════════════════════════════════════════════════

    private void OnDeeplink(DeeplinkData data) => DisplayData(data);

    private void ProcessUrl(string url)
    {
        try
        {
            DeeplinkHandler.Handle(url);
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}", Color.red);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // Display
    // ════════════════════════════════════════════════════════════════════

    private void DisplayData(DeeplinkData data)
    {
        _receiveCount++;
        SetStatus($"Received #{_receiveCount} - deeplink handled", AccentGreen);

        _rawUrlText.text = data.RawUrl;
        _schemeText.text = data.Scheme;
        _hostText.text = data.Host;
        _pathText.text = string.IsNullOrEmpty(data.Path) ? "(root)" : "/" + data.Path;

        _fragmentText.text = string.IsNullOrEmpty(data.Fragment) ? "(none)" : "#" + data.Fragment;

        if (data.Query.Count == 0)
        {
            _paramsText.text = "  (none)";
        }
        else
        {
            var sb = new StringBuilder();
            foreach (var kv in data.Query)
                sb.AppendLine($"  <b>{Esc(kv.Key)}</b>  =>  {Esc(kv.Value)}");
            _paramsText.text = sb.ToString().TrimEnd();
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // Button handlers
    // ════════════════════════════════════════════════════════════════════

    private void OnSendClicked()
    {
        var url = _testUrlInput.text.Trim();
        if (string.IsNullOrEmpty(url)) { SetStatus("Enter a URL first.", Color.red); return; }
        ProcessUrl(url);
    }

    private void OnClearClicked()
    {
        _receiveCount = 0;
        ClearDisplayFields();
        SetStatus("Cleared.", TextGrey);
    }

    private void ClearDisplayFields()
    {
        _rawUrlText.text = _schemeText.text = _hostText.text =
        _pathText.text = _fragmentText.text = _paramsText.text = "-";
    }

    private void SetStatus(string msg, Color color)
    {
        _statusLabel.text = msg;
        _statusLabel.color = color;
    }

    private static string Esc(string s) =>
        s?.Replace("<", "\u003c").Replace(">", "\u003e") ?? string.Empty;

    // ════════════════════════════════════════════════════════════════════
    // UI Builder
    // ════════════════════════════════════════════════════════════════════

    private void BuildUI()
    {
        // ── EventSystem ──────────────────────────────────────────────────
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // ── Canvas ───────────────────────────────────────────────────────
        var canvasGo = new GameObject("DeeplinkTestCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // ── Root panel ───────────────────────────────────────────────────
        var root = MakePanel(canvasGo, "Root", BgDark,
            new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        // full-screen

        // Vertical layout on root
        var vlRoot = root.AddComponent<VerticalLayoutGroup>();
        vlRoot.padding = new RectOffset(24, 24, 24, 24);
        vlRoot.spacing = 14;
        vlRoot.childControlWidth = true;
        vlRoot.childForceExpandWidth = true;
        vlRoot.childControlHeight = true;
        vlRoot.childForceExpandHeight = false;

        // ── Title ────────────────────────────────────────────────────────
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(root.transform, false);
        var title = titleGo.AddComponent<TextMeshProUGUI>();
        title.text = "Deeplink Tester";
        title.fontSize = 52;
        title.fontStyle = FontStyles.Bold;
        title.color = AccentBlue;
        title.alignment = TextAlignmentOptions.Center;
        FixedHeight(titleGo, 68);

        // ── Status ───────────────────────────────────────────────────────
        var statusGo = new GameObject("Status");
        statusGo.transform.SetParent(root.transform, false);
        _statusLabel = statusGo.AddComponent<TextMeshProUGUI>();
        _statusLabel.text = "Initialising...";
        _statusLabel.fontSize = 30;
        _statusLabel.color = TextGrey;
        _statusLabel.alignment = TextAlignmentOptions.Center;
        FixedHeight(statusGo, 44);

        // ── Test input bar (single compact row) ──────────────────────────
        var inputPanel = MakeSectionPanel(root, "TestInput");
        var inputRowHl = inputPanel.AddComponent<HorizontalLayoutGroup>();
        inputRowHl.padding = new RectOffset(10, 10, 8, 8);
        inputRowHl.spacing = 8;
        inputRowHl.childControlWidth = true;
        inputRowHl.childControlHeight = true;
        inputRowHl.childForceExpandHeight = true;
        FixedHeight(inputPanel, 76);

        _testUrlInput = MakeTMPInputField(inputPanel, BgInput);
        var inputLg = _testUrlInput.gameObject.AddComponent<LayoutElement>();
        inputLg.flexibleWidth = 1;

        _sendButton = MakeButton(inputPanel, "Send", AccentGreen, Color.black, 110);
        _clearButton = MakeButton(inputPanel, "Clear", new Color(0.5f, 0.25f, 0.25f, 1f), TextWhite, 90);

        // ── Raw URL panel ────────────────────────────────────────────────
        var rawPanel = MakeSectionPanel(root, "Raw URL");
        var rawVl = rawPanel.AddComponent<VerticalLayoutGroup>();
        SetVL(rawVl, 6);
        FixedHeight(rawPanel, 100);
        MakeLabel(rawPanel, "Raw URL", TextGrey, 26);
        _rawUrlText = MakeValueText(rawPanel, "-", 28);

        // ── Parsed data panel ────────────────────────────────────────────
        var parsedPanel = MakeSectionPanel(root, "Parsed");
        var parsedVl = parsedPanel.AddComponent<VerticalLayoutGroup>();
        SetVL(parsedVl, 6);
        var parsedLe = parsedPanel.AddComponent<LayoutElement>();
        parsedLe.flexibleHeight = 1;
        parsedLe.minHeight = 200;

        MakeLabel(parsedPanel, "Parsed Data", TextGrey, 26);
        MakeRow(parsedPanel, "Scheme", out _schemeText);
        MakeRow(parsedPanel, "Host", out _hostText);
        MakeRow(parsedPanel, "Path", out _pathText);
        MakeRow(parsedPanel, "Fragment", out _fragmentText);
        MakeLabel(parsedPanel, "Parameters", TextGrey, 22);
        _paramsText = MakeExpandableValueText(parsedPanel, "-", 24);
    }


    // ════════════════════════════════════════════════════════════════════
    // UI helper methods
    // ════════════════════════════════════════════════════════════════════

    private static GameObject MakePanel(GameObject parent, string name, Color bg,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = bg;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return go;
    }

    private static GameObject MakeSectionPanel(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = BgSection;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        return go;
    }

    private static TMP_Text MakeLabel(GameObject parent, string text, Color color, float size)
    {
        var go = new GameObject(text.Replace(" ", "") + "Label");
        go.transform.SetParent(parent.transform, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.fontStyle = FontStyles.Bold;
        FixedHeight(go, size + 8);
        return t;
    }

    private static TMP_Text MakeValueText(GameObject parent, string text, float size)
    {
        var go = new GameObject("Value");
        go.transform.SetParent(parent.transform, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = TextWhite;
        t.richText = true;
        t.overflowMode = TextOverflowModes.Ellipsis;
        FixedHeight(go, size + 10);
        return t;
    }

    private static TMP_Text MakeExpandableValueText(GameObject parent, string text, float size)
    {
        var go = new GameObject("Value");
        go.transform.SetParent(parent.transform, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = TextWhite;
        t.richText = true;
        t.overflowMode = TextOverflowModes.Overflow;
        t.enableWordWrapping = true;
        var le = go.AddComponent<LayoutElement>();
        le.flexibleHeight = 1;
        le.minHeight = size + 10;
        return t;
    }

    private static void MakeRow(GameObject parent, string label, out TMP_Text value)
    {
        var row = new GameObject(label + "Row");
        row.transform.SetParent(parent.transform, false);
        var hl = row.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 8;
        hl.childControlWidth = true;
        hl.childControlHeight = true;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = true;
        FixedHeight(row, 40);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(row.transform, false);
        var lt = labelGo.AddComponent<TextMeshProUGUI>();
        lt.text = label + ":";
        lt.fontSize = 24;
        lt.color = TextGrey;
        var lLe = labelGo.AddComponent<LayoutElement>();
        lLe.minWidth = 150;
        lLe.preferredWidth = 150;
        lLe.flexibleWidth = 0;

        var valGo = new GameObject("Value");
        valGo.transform.SetParent(row.transform, false);
        var vt = valGo.AddComponent<TextMeshProUGUI>();
        vt.text = "-";
        vt.fontSize = 24;
        vt.color = TextWhite;
        vt.richText = true;
        var vLe = valGo.AddComponent<LayoutElement>();
        vLe.flexibleWidth = 1;

        value = vt;
    }

    private static TMP_InputField MakeTMPInputField(GameObject parent, Color bg)
    {
        var go = new GameObject("InputField");
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = bg;

        // Text Area
        var taGo = new GameObject("Text Area");
        taGo.transform.SetParent(go.transform, false);
        var taMask = taGo.AddComponent<RectMask2D>();
        _ = taMask;
        var taRt = taGo.GetComponent<RectTransform>();
        taRt.anchorMin = Vector2.zero;
        taRt.anchorMax = Vector2.one;
        taRt.offsetMin = new Vector2(8, 4);
        taRt.offsetMax = new Vector2(-8, -4);

        // Placeholder
        var phGo = new GameObject("Placeholder");
        phGo.transform.SetParent(taGo.transform, false);
        var ph = phGo.AddComponent<TextMeshProUGUI>();
        ph.text = $"{appName}://…";
        ph.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        ph.fontSize = 26;
        var phRt = phGo.GetComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero;
        phRt.anchorMax = Vector2.one;
        phRt.offsetMin = phRt.offsetMax = Vector2.zero;

        // Text
        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(taGo.transform, false);
        var txt = txtGo.AddComponent<TextMeshProUGUI>();
        txt.color = Color.white;
        txt.fontSize = 26;
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = txtRt.offsetMax = Vector2.zero;

        // TMP_InputField
        var field = go.AddComponent<TMP_InputField>();
        field.textViewport = taRt;
        field.textComponent = txt;
        field.placeholder = ph;
        field.caretColor = Color.white;
        field.selectionColor = new Color(0.3f, 0.6f, 1f, 0.5f);

        return field;
    }

    private static Button MakeButton(GameObject parent, string label, Color bg, Color textColor, float width)
    {
        var go = new GameObject(label + "Btn");
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = bg;
        var btn = go.AddComponent<Button>();

        var cs = btn.colors;
        cs.normalColor = bg;
        cs.highlightedColor = Color.Lerp(bg, Color.white, 0.25f);
        cs.pressedColor = Color.Lerp(bg, Color.black, 0.25f);
        btn.colors = cs;

        var le = go.AddComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;

        var txtGo = new GameObject("Label");
        txtGo.transform.SetParent(go.transform, false);
        var txt = txtGo.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 28;
        txt.fontStyle = FontStyles.Bold;
        txt.color = textColor;
        txt.alignment = TextAlignmentOptions.Center;
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = txtRt.offsetMax = Vector2.zero;

        return btn;
    }

    private static void FixedHeight(GameObject go, float h)
    {
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.minHeight = h;
        le.preferredHeight = h;
    }

    private static void SetVL(VerticalLayoutGroup vl, float spacing)
    {
        vl.padding = new RectOffset(12, 12, 10, 10);
        vl.spacing = spacing;
        vl.childControlWidth = true;
        vl.childForceExpandWidth = true;
        vl.childControlHeight = true;
        vl.childForceExpandHeight = false;
    }
}
