using UnityEngine;

/// Simple virtual room (PIC 2): floor, walls, ceiling and a home-position marker.
/// Surfaces are flat/unlit on purpose: the focus is the information, not decoration.
/// The floor is at y = 0, the same level RankingBars uses for the bar bases.
public class VirtualRoom : MonoBehaviour
{
    public Transform viewer;                 // home position (Main Camera)
    public float minX = -5f, maxX = 5f;      // room extent on X (m)
    public float minZ = -15f, maxZ = -4f;     // room extent on Z (m)
    public float height = 5f;                // ceiling height (m)
    public Color floorColor   = new Color32(46, 54, 66, 255);
    public Color ceilingColor = new Color32(26, 31, 40, 255);
    public Color frontWall    = new Color32(40, 48, 60, 255);  // +Z side (bar chart)
    public Color backWall     = new Color32(33, 40, 51, 255);  // -Z side (behind the reader)
    public Color sideWall     = new Color32(36, 43, 55, 255);
    public Color homeMarker   = new Color32(95, 110, 130, 255);

    void Start() { Rebuild(); }

    [ContextMenu("Rebuild")]
    public void Rebuild() { Clear(); Build(); }

    [ContextMenu("Clear")]
    public void Clear() { Pic2Util.ClearChildren(transform); }

    void Build()
    {
        float w = maxX - minX, d = maxZ - minZ, cx = (minX + maxX) * 0.5f, cz = (minZ + maxZ) * 0.5f;
        const float t = 0.2f; // wall thickness (outside the room)

        Pic2Util.Prim(PrimitiveType.Cube, "Floor", transform,
            new Vector3(cx, -0.05f, cz), new Vector3(w, 0.1f, d), Pic2Util.MakeUnlit(floorColor));
        Pic2Util.Prim(PrimitiveType.Cube, "Ceiling", transform,
            new Vector3(cx, height + 0.05f, cz), new Vector3(w + 2 * t, 0.1f, d + 2 * t), Pic2Util.MakeUnlit(ceilingColor));

        Pic2Util.Prim(PrimitiveType.Cube, "Wall_Front", transform,
            new Vector3(cx, height * 0.5f, maxZ + t * 0.5f), new Vector3(w + 2 * t, height, t), Pic2Util.MakeUnlit(frontWall));
        Pic2Util.Prim(PrimitiveType.Cube, "Wall_Back", transform,
            new Vector3(cx, height * 0.5f, minZ - t * 0.5f), new Vector3(w + 2 * t, height, t), Pic2Util.MakeUnlit(backWall));
        var side = Pic2Util.MakeUnlit(sideWall);
        Pic2Util.Prim(PrimitiveType.Cube, "Wall_Left", transform,
            new Vector3(minX - t * 0.5f, height * 0.5f, cz), new Vector3(t, height, d), side);
        Pic2Util.Prim(PrimitiveType.Cube, "Wall_Right", transform,
            new Vector3(maxX + t * 0.5f, height * 0.5f, cz), new Vector3(t, height, d), side);

        // Home-position marker: a flat ring on the floor under the reader's feet
        Vector3 p = viewer != null ? viewer.position : new Vector3(0f, 1.6f, -10f);
        var ring = Pic2Util.MakeUnlit(homeMarker);
        var dark = Pic2Util.MakeUnlit(floorColor);
        Pic2Util.Prim(PrimitiveType.Cylinder, "HomeRing", transform,
            new Vector3(p.x, 0.004f, p.z), new Vector3(0.9f, 0.002f, 0.9f), ring);
        Pic2Util.Prim(PrimitiveType.Cylinder, "HomeRingInner", transform,
            new Vector3(p.x, 0.006f, p.z), new Vector3(0.74f, 0.002f, 0.74f), dark);
    }
}
