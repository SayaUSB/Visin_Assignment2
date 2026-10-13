using UnityEngine;
using UnityEngine.Rendering;

/// Small helpers shared by the PIC 2 scripts (VirtualRoom, WorldMapTable).
public static class Pic2Util
{
    // Flat (unlit) material: colors stay exactly as specified, independent of scene lighting.
    public static Material MakeUnlit(Color c, Texture tex = null)
    {
        var sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default"); // fallback, also renders in URP
        var m = new Material(sh);
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (tex != null)
        {
            m.mainTexture = tex;
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        }
        return m;
    }

    // Primitive without collider (no interaction allowed) and without shadows.
    public static GameObject Prim(PrimitiveType type, string name, Transform parent,
                                  Vector3 localPos, Vector3 localScale, Material mat,
                                  Quaternion? localRot = null)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot ?? Quaternion.identity;
        go.transform.localScale = localScale;
        FireballPalette.Kill(go.GetComponent<Collider>());
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        return go;
    }

    public static void ClearChildren(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
            FireballPalette.Kill(t.GetChild(i).gameObject);
    }
}
