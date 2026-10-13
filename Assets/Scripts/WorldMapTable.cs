using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// World map (PIC 2, Q2 and the location part of Q5).
/// A horizontal miniature world map (allocentric reading): either lying on the floor
/// (tableHeight = 0, showTable = false) or on a virtual table.
/// The map is an equirectangular texture, so  x = longitude / 180 * (width / 2)
///                                            z = latitude  /  90 * (depth / 2).
/// RoomLayout sets direction / distance / width / height from the Room Builder table;
/// the fields can also be set by hand. Assign AllEvents.json and the viewer (Main Camera).
public class WorldMapTable : MonoBehaviour
{
    [Header("Data")]
    public TextAsset allEventsJson;      // AllEvents.json (358 events, 10 flagged is_top10)
    public Texture2D mapTexture;         // optional; falls back to Resources/world_map.png
    public Transform viewer;             // reader's eye position (home position)

    [Header("Placement (RoomLayout normally sets these)")]
    public float directionDegrees = 0f;   // clockwise from straight ahead (+Z): 0 = 12 o'clock, 60 = 2 o'clock
    public float distance = 2f;           // viewer -> map centre (m)
    public float mapWidth = 3f;           // map width (m); depth = width / 2 (2:1 map)
    public float tableHeight = 0f;        // map surface above the floor (m); 0 = lies on the floor
    public bool showTable = false;        // true = table legs under the map, false = flat frame only
    public float framePadding = 0.1f;     // frame / table edge around the map (m)

    [Header("Markers & labels")]
    public float topMarkerSize = 0.075f;      // diameter of a top-10 marker (m)
    public float contextMarkerSize = 0.02f;   // diameter of the other events (m)
    public float rankLabelCharSize = 0.03f;   // "#rank" label above each top-10 marker
    public bool showCoordinateLabels = true;  // lat/lon text above each marker
    public float coordLabelCharSize = 0.018f; // size of that coordinates line, if shown

    static readonly Color FrameColor = new Color32(92, 102, 116, 255);
    static readonly Color LegColor   = new Color32(66, 75, 88, 255);
    static readonly Color OceanDark  = new Color32(23, 33, 43, 255);
    static readonly Color ContextDot = new Color32(143, 163, 184, 255);
    static readonly Color TextMain   = new Color32(235, 240, 245, 255);
    static readonly Color TextDim    = new Color32(190, 202, 216, 255);

    readonly List<Transform> faceReader = new List<Transform>(); // labels that turn towards the reader

    void Start() { Rebuild(); } // always rebuilds, so objects saved in Edit mode never duplicate

    // Labels stay upright and keep facing the reader while the reader turns the head.
    void LateUpdate()
    {
        if (!Application.isPlaying || viewer == null) return;
        Vector3 eye = viewer.position;
        foreach (var t in faceReader)
        {
            if (t == null) continue;
            Vector3 f = t.position - eye; f.y = 0f;
            if (f.sqrMagnitude > 1e-4f) t.rotation = Quaternion.LookRotation(f, Vector3.up);
        }
    }

    [ContextMenu("Rebuild")]
    public void Rebuild() { Clear(); faceReader.Clear(); Build(); }

    [ContextMenu("Clear")]
    public void Clear() { Pic2Util.ClearChildren(transform); }

    static string Fmt(float v, string f) { return v.ToString(f, CultureInfo.InvariantCulture); }
    static string Coord(float lat, float lon)
    {
        return Fmt(Mathf.Abs(lat), "0.0") + "°" + (lat >= 0 ? "N" : "S") + " " +
               Fmt(Mathf.Abs(lon), "0.0") + "°" + (lon >= 0 ? "E" : "W");
    }

    void AddLabel(string text, Vector3 worldPos, float charSize, Color color, Vector3 eye)
    {
        var tm = FireballPalette.MakeLabel(text, worldPos, transform, charSize, color);
        Vector3 f = worldPos - eye; f.y = 0f;
        if (f.sqrMagnitude < 1e-4f) f = transform.forward;
        tm.transform.rotation = Quaternion.LookRotation(f, Vector3.up);
        faceReader.Add(tm.transform);
    }

    // x = lon/180 * W/2 , z = lat/90 * D/2  (local to the map), y = height above the floor
    static Vector3 Geo(float lat, float lon, float W, float D, float y)
    {
        return new Vector3(lon / 180f * W * 0.5f, y, lat / 90f * D * 0.5f);
    }

    void Build()
    {
        if (allEventsJson == null) { Debug.LogError("WorldMapTable: assign AllEvents.json to All Events Json"); return; }
        var file = JsonUtility.FromJson<FireballFile>(allEventsJson.text);
        if (file == null || file.events == null || file.events.Length == 0) { Debug.LogError("AllEvents.json is empty"); return; }

        // ---- placement: local +Z points away from the viewer, local +X to the viewer's right
        Vector3 eye = viewer != null ? viewer.position : new Vector3(0f, 1.6f, -10f);
        float a = directionDegrees * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
        Vector3 center = new Vector3(eye.x, 0f, eye.z) + dir * distance;
        transform.SetPositionAndRotation(center, Quaternion.LookRotation(dir, Vector3.up));
        transform.localScale = Vector3.one;

        float W = mapWidth, D = mapWidth * 0.5f, pad = framePadding;
        float plateTop = tableHeight;             // top of the frame / table
        float mapY = tableHeight + 0.004f;        // the map plane
        float surface = tableHeight + 0.006f;     // markers start here

        // ---- frame (flat plate) or table
        var plateMat = Pic2Util.MakeUnlit(FrameColor);
        if (showTable && tableHeight > 0.2f)
        {
            Pic2Util.Prim(PrimitiveType.Cube, "TableTop", transform,
                new Vector3(0f, plateTop - 0.03f, 0f), new Vector3(W + 2 * pad, 0.06f, D + 2 * pad), plateMat);
            float legH = plateTop - 0.06f;
            var legMat = Pic2Util.MakeUnlit(LegColor);
            foreach (int sx in new[] { -1, 1 })
                foreach (int sz in new[] { -1, 1 })
                    Pic2Util.Prim(PrimitiveType.Cube, "TableLeg", transform,
                        new Vector3(sx * (W * 0.5f + pad - 0.08f), legH * 0.5f, sz * (D * 0.5f + pad - 0.08f)),
                        new Vector3(0.09f, legH, 0.09f), legMat);
        }
        else
        {
            // flat plate: top surface 1 mm under the map plane
            Pic2Util.Prim(PrimitiveType.Cube, "MapFrame", transform,
                new Vector3(0f, plateTop - 0.004f, 0f), new Vector3(W + 2 * pad, 0.01f, D + 2 * pad), plateMat);
        }

        // ---- map plane (thin vector-style texture: flat colors, no photo detail)
        Texture2D tex = mapTexture != null ? mapTexture : Resources.Load<Texture2D>("world_map");
        if (tex == null) Debug.LogWarning("WorldMapTable: world_map texture not found (Assets/Resources/world_map.png)");
        else { tex.wrapMode = TextureWrapMode.Clamp; tex.filterMode = FilterMode.Trilinear; tex.anisoLevel = 8; }
        Pic2Util.Prim(PrimitiveType.Quad, "MapPlane", transform,
            new Vector3(0f, mapY, 0f), new Vector3(W, D, 1f),
            Pic2Util.MakeUnlit(tex != null ? Color.white : OceanDark, tex),
            Quaternion.Euler(90f, 0f, 0f)); // quad faces up; texture top = north (+Z), right = east (+X)

        // ---- other events: small, quiet dots (context only)
        var dotMat = Pic2Util.MakeUnlit(ContextDot);
        var top = new List<FireballEvent>();
        foreach (var e in file.events)
        {
            if (e.is_top10) { top.Add(e); continue; }
            float s = contextMarkerSize;
            Pic2Util.Prim(PrimitiveType.Sphere, "Dot_" + e.event_id, transform,
                Geo(e.latitude, e.longitude, W, D, surface + s * 0.2f), new Vector3(s, s * 0.5f, s), dotMat);
        }

        // ---- top-10 markers: rank colour (same palette as the ranking bars) + white halo.
        // A halo with a dark gap (a ring) = peak altitude below the dataset median.
        var white = Pic2Util.MakeUnlit(Color.white);
        var gap = Pic2Util.MakeUnlit(OceanDark);
        var leaderMat = Pic2Util.MakeUnlit(Color.white);
        float m = topMarkerSize;

        // x-sorted order -> labels cycle through 3 heights, so neighbours never collide
        top.Sort((p, q) => p.longitude.CompareTo(q.longitude));
        for (int i = 0; i < top.Count; i++)
        {
            var e = top[i];
            Vector3 c = Geo(e.latitude, e.longitude, W, D, surface);
            if (e.low_alt)
            {
                Pic2Util.Prim(PrimitiveType.Cylinder, "Ring_" + e.event_id, transform,
                    c + Vector3.up * 0.002f, new Vector3(m * 1.95f, 0.002f, m * 1.95f), white);
                Pic2Util.Prim(PrimitiveType.Cylinder, "RingGap_" + e.event_id, transform,
                    c + Vector3.up * 0.004f, new Vector3(m * 1.5f, 0.002f, m * 1.5f), gap);
            }
            else
            {
                Pic2Util.Prim(PrimitiveType.Cylinder, "Halo_" + e.event_id, transform,
                    c + Vector3.up * 0.002f, new Vector3(m * 1.45f, 0.002f, m * 1.45f), white);
            }
            Pic2Util.Prim(PrimitiveType.Sphere, "Marker_" + e.rank + "_" + e.event_id, transform,
                c + Vector3.up * (m * 0.35f), new Vector3(m, m, m),
                Pic2Util.MakeUnlit(FireballPalette.ForRank(e.rank)));

            // "#rank" + coordinates above the marker, with a thin leader line
            float rankH = rankLabelCharSize * 4f;
            float coordH = showCoordinateLabels ? coordLabelCharSize * 4f : 0f;
            float lift = 0.10f + (i % 3) * (rankH + coordH + 0.04f);
            Vector3 markTop = c + Vector3.up * (m * 0.6f);
            Vector3 coordPos = markTop + Vector3.up * (lift + coordH * 0.5f);
            Vector3 rankPos = coordPos + Vector3.up * (coordH * 0.5f + rankH * 0.5f);
            float leaderLen = coordPos.y - coordH * 0.5f - markTop.y;
            if (leaderLen > 0.01f)
                Pic2Util.Prim(PrimitiveType.Cube, "Leader_" + e.rank, transform,
                    markTop + Vector3.up * (leaderLen * 0.5f), new Vector3(0.004f, leaderLen, 0.004f), leaderMat);
            // positions above are LOCAL to the map; MakeLabel wants WORLD positions
            AddLabel("#" + e.rank, transform.TransformPoint(rankPos), rankLabelCharSize, TextMain, eye);
            if (showCoordinateLabels)
                AddLabel(Coord(e.latitude, e.longitude), transform.TransformPoint(coordPos), coordLabelCharSize, TextDim, eye);
        }
    }
}
