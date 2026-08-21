using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileHud : MonoBehaviour
{
    Canvas canvas;
    RectTransform movePad, lookPad;
    Image moveKnob;
    int moveId = -1, lookId = -1;
    Vector2 moveOrigin;

    void Start()
    {
        canvas = GetComponent<Canvas>();
        if (!canvas)
        {
            var go = gameObject;
            canvas = go.GetComponent<Canvas>() ?? go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            if (!go.GetComponent<CanvasScaler>()) go.AddComponent<CanvasScaler>();
            if (!go.GetComponent<GraphicRaycaster>()) go.AddComponent<GraphicRaycaster>();
        }
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        Build();
    }

    void Build()
    {
        lookPad = MakeHit("LookPad", new Vector2(0.42f, 0f), Vector2.one, Color.clear, 0);
        Bind(lookPad.gameObject, OnLookDown, OnLookDrag, OnLookUp);

        movePad = MakeHit("MovePad", new Vector2(0.03f, 0.06f), new Vector2(0.28f, 0.42f), new Color(1, 1, 1, 0.07f), 1);
        var ring = MakeImage(movePad, "Ring", Vector2.zero, new Vector2(180, 180), new Color(1, 1, 1, 0.18f));
        ring.GetComponent<RectTransform>().anchorMin = ring.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
        moveKnob = MakeImage(movePad, "Knob", Vector2.zero, new Vector2(72, 72), new Color(0.2f, 0.85f, 1f, 0.85f));
        moveKnob.rectTransform.anchorMin = moveKnob.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        Bind(movePad.gameObject, OnMoveDown, OnMoveDrag, OnMoveUp);

        MakeHoldButton("FIRE", new Vector2(-110, 90), new Vector2(150, 150), new Color(0.85f, 0.12f, 0.18f, 0.92f),
            v => { if (CombatInput.Instance) CombatInput.Instance.TouchFire = v; });
        MakeHoldButton("ADS", new Vector2(-110, 250), new Vector2(92, 64), new Color(0.08f, 0.12f, 0.18f, 0.88f),
            v => { if (CombatInput.Instance) CombatInput.Instance.TouchAds = v; });
        MakeTapButton("R", new Vector2(-230, 90), new Vector2(72, 72), new Color(0.08f, 0.12f, 0.18f, 0.88f),
            () => { if (CombatInput.Instance) CombatInput.Instance.TouchReload = true; });
        var brief = transform.Find("BriefingPanel");
        if (brief) brief.SetAsLastSibling();
        var result = transform.Find("ResultPanel");
        if (result) result.SetAsLastSibling();
    }

    void LateUpdate()
    {
        if (CombatInput.Instance) CombatInput.Instance.TouchReload = false;
    }

    void OnMoveDown(PointerEventData e)
    {
        moveId = e.pointerId;
        moveOrigin = e.position;
        SetMove(Vector2.zero);
    }

    void OnMoveDrag(PointerEventData e)
    {
        if (e.pointerId != moveId) return;
        Vector2 d = Vector2.ClampMagnitude(e.position - moveOrigin, 80f);
        moveKnob.rectTransform.anchoredPosition = d;
        SetMove(new Vector2(d.x / 80f, d.y / 80f));
    }

    void OnMoveUp(PointerEventData e)
    {
        if (e.pointerId != moveId) return;
        moveId = -1;
        moveKnob.rectTransform.anchoredPosition = Vector2.zero;
        SetMove(Vector2.zero);
    }

    void OnLookDown(PointerEventData e) { lookId = e.pointerId; }

    void OnLookDrag(PointerEventData e)
    {
        if (e.pointerId != lookId) return;
        if (CombatInput.Instance)
            CombatInput.Instance.TouchLook += e.delta * 0.14f;
    }

    void OnLookUp(PointerEventData e)
    {
        if (e.pointerId == lookId) lookId = -1;
    }

    void SetMove(Vector2 m)
    {
        if (CombatInput.Instance) CombatInput.Instance.TouchMove = m;
    }

    RectTransform MakeHit(string name, Vector2 min, Vector2 max, Color col, int sibling)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = col; img.raycastTarget = true; img.sprite = White();
        go.transform.SetSiblingIndex(Mathf.Min(sibling, transform.childCount - 1));
        return rt;
    }

    Image MakeImage(RectTransform parent, string name, Vector2 pos, Vector2 size, Color col)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = col; img.raycastTarget = false; img.sprite = White();
        var rt = img.rectTransform;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return img;
    }

    void MakeHoldButton(string label, Vector2 pos, Vector2 size, Color col, System.Action<bool> on)
    {
        var btn = MakeTextButton(label, pos, size, col);
        var hold = btn.gameObject.AddComponent<HoldRelay>();
        hold.on = on;
    }

    void MakeTapButton(string label, Vector2 pos, Vector2 size, Color col, System.Action tap)
    {
        var btn = MakeTextButton(label, pos, size, col);
        var relay = btn.gameObject.AddComponent<TapRelay>();
        relay.tap = tap;
    }

    Image MakeTextButton(string label, Vector2 pos, Vector2 size, Color col)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.color = col; img.raycastTarget = true; img.sprite = White();
        var tgo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        tgo.transform.SetParent(go.transform, false);
        var t = tgo.GetComponent<Text>();
        t.text = label; t.alignment = TextAnchor.MiddleCenter; t.color = Color.white;
        t.fontSize = 22; t.fontStyle = FontStyle.Bold;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.raycastTarget = false;
        var trt = t.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
        return img;
    }

    static Sprite whiteSprite;
    static Sprite White()
    {
        if (whiteSprite) return whiteSprite;
        var tex = Texture2D.whiteTexture;
        whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 4f);
        return whiteSprite;
    }

    static void Bind(GameObject go, System.Action<PointerEventData> down, System.Action<PointerEventData> drag, System.Action<PointerEventData> up)
    {
        var relay = go.AddComponent<PointerRelay>();
        relay.down = down; relay.drag = drag; relay.up = up;
    }
}

public class PointerRelay : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public System.Action<PointerEventData> down, drag, up;
    public void OnPointerDown(PointerEventData e) => down?.Invoke(e);
    public void OnDrag(PointerEventData e) => drag?.Invoke(e);
    public void OnPointerUp(PointerEventData e) => up?.Invoke(e);
}

public class HoldRelay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public System.Action<bool> on;
    public void OnPointerDown(PointerEventData e) => on?.Invoke(true);
    public void OnPointerUp(PointerEventData e) => on?.Invoke(false);
    public void OnPointerExit(PointerEventData e) => on?.Invoke(false);
}

public class TapRelay : MonoBehaviour, IPointerClickHandler
{
    public System.Action tap;
    public void OnPointerClick(PointerEventData e) => tap?.Invoke();
}
