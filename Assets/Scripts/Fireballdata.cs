using System;
using UnityEngine;

[Serializable]
public class FireballEvent
{
    public string event_id;
    public int rank;
    public string date;
    public float impact_e;   // kilotons of TNT
    public float energy;     // 10^10 J
    public float alt;        // km
    public float latitude;
    public float longitude;
    public bool is_top10;
    public bool low_alt;     // altitude below the dataset median
}

[Serializable]
public class FireballFile
{
    public float median_alt_km;
    public FireballEvent[] events;
}

public static class FireballPalette
{
    // One color per rank (1-10). The world map (PIC 2) must use the SAME colors.
    // Color is never the only cue: the rank number is always shown in the label.
    public static readonly Color[] Top10 = new Color[]
    {
        new Color32(230, 159,   0, 255), // 1
        new Color32( 86, 180, 233, 255), // 2
        new Color32(  0, 158, 115, 255), // 3
        new Color32(240, 228,  66, 255), // 4
        new Color32(  0, 114, 178, 255), // 5
        new Color32(213,  94,   0, 255), // 6
        new Color32(204, 121, 167, 255), // 7
        new Color32(148, 103, 189, 255), // 8
        new Color32(140,  86,  75, 255), // 9
        new Color32(127, 127, 127, 255), // 10
    };

    public static Color ForRank(int rank) => Top10[Mathf.Clamp(rank, 1, 10) - 1];

    // Tints a renderer using the default material of the active render pipeline
    // (URP/HDRP/Built-in), so it does not depend on Shader.Find("Standard"),
    // which renders magenta in URP.
    public static void Tint(Renderer r, Color c)
    {
        var m = new Material(r.sharedMaterial);
        m.color = c;                                                   // Built-in (_Color)
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);  // URP/HDRP
        r.sharedMaterial = m; // sharedMaterial also works in Edit mode (no instancing warning)
    }

    // Destroys an object in both Play mode and Edit mode
    // (Destroy is not allowed in Edit mode, DestroyImmediate is not safe at runtime).
    public static void Kill(UnityEngine.Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) UnityEngine.Object.Destroy(o);
        else UnityEngine.Object.DestroyImmediate(o);
    }

    public static TextMesh MakeLabel(string text, Vector3 pos, Transform parent,
                                     float charSize = 0.03f, Color? color = null)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 100;            // large font size + small characterSize = sharp text
        tm.characterSize = charSize;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color ?? Color.white;
        return tm;
    }
}