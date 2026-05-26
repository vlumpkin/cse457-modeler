using UnityEngine;

// 3D progress meter that sits above a Pickupable while it's being cut.
// Transparent capsule "shell" filled by a green cylinder whose Y-scale = progress.
public class CutProgressMeter : MonoBehaviour
{
    [Header("Dimensions")]
    public float height = 0.6f;
    public float radius = 0.12f;
    [Tooltip("How much of the shell's vertical extent is usable for the fill (caps eat some).")]
    [Range(0.5f, 1f)] public float usableFraction = 0.85f;

    [Header("Colors")]
    public Color shellColor = new Color(1f, 1f, 1f, 0.25f);
    public Color fillColor = new Color(0.3f, 1f, 0.4f, 1f);

    private Transform shell;
    private Transform fill;
    private MeshRenderer shellRenderer;
    private MeshRenderer fillRenderer;
    private float progress;

    public static CutProgressMeter AttachTo(Pickupable item)
    {
        GameObject go = new GameObject("CutProgressMeter");
        go.transform.SetParent(item.transform, false);
        go.transform.localPosition = new Vector3(0f, item.cutMeterYOffset, 0f);
        go.transform.localRotation = Quaternion.identity;
        var meter = go.AddComponent<CutProgressMeter>();
        meter.BuildVisuals();
        meter.SetProgress(0f);
        return meter;
    }

    private void BuildVisuals()
    {
        // Shell: a translucent capsule the same height as the meter.
        GameObject shellGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        shellGo.name = "Shell";
        Destroy(shellGo.GetComponent<Collider>());
        shellGo.transform.SetParent(transform, false);
        shellGo.transform.localPosition = Vector3.zero;
        shellGo.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
        shell = shellGo.transform;
        shellRenderer = shellGo.GetComponent<MeshRenderer>();
        shellRenderer.sharedMaterial = MakeTransparentMaterial(shellColor);
        shellRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        shellRenderer.receiveShadows = false;

        // Fill: a green cylinder that grows from the bottom.
        GameObject fillGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fillGo.name = "Fill";
        Destroy(fillGo.GetComponent<Collider>());
        fillGo.transform.SetParent(transform, false);
        fill = fillGo.transform;
        fillRenderer = fillGo.GetComponent<MeshRenderer>();
        fillRenderer.sharedMaterial = MakeOpaqueMaterial(fillColor);
        fillRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        fillRenderer.receiveShadows = false;
    }

    public void SetProgress(float p)
    {
        progress = Mathf.Clamp01(p);
        if (fill == null) return;

        float usableHeight = height * usableFraction;
        float fillHeight = usableHeight * progress;
        // Unity's primitive cylinder is 2 units tall; localScale.y = height * 0.5
        fill.localScale = new Vector3(radius * 1.6f, fillHeight * 0.5f, radius * 1.6f);
        // Anchor the bottom at -usableHeight/2 so it grows upward.
        float bottom = -usableHeight * 0.5f;
        fill.localPosition = new Vector3(0f, bottom + fillHeight * 0.5f, 0f);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        // Billboard so the capsule always reads upright relative to the main camera.
        if (Camera.main == null) return;
        Vector3 toCam = Camera.main.transform.position - transform.position;
        toCam.y = 0f;
        if (toCam.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
    }

    private static Material MakeTransparentMaterial(Color c)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Transparent")
            ?? Shader.Find("Sprites/Default");
        Material m = new Material(sh);
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
        if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return m;
    }

    private static Material MakeOpaqueMaterial(Color c)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Standard");
        Material m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        return m;
    }
}
