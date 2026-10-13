using System.Collections;
using UnityEngine;

public enum ViewHeight { Floor, Waist, Eye }

/// One row of the Room Builder table (place up to four views from home).
[System.Serializable]
public class ViewSpec
{
    public string label = "View";
    [Range(1, 12)] public int clock = 12;   // 12 = straight ahead, 3 = right, 6 = behind, 9 = left
    public float distanceM = 2f;            // from home to the centre of the view
    public float widthM = 1f;               // width of the view
    public ViewHeight height = ViewHeight.Eye;
    public float labelCm = 3f;              // smallest label (capital-letter height) the view must have

    public ViewSpec() { }
    public ViewSpec(string label, int clock, float distanceM, float widthM, ViewHeight height, float labelCm)
    {
        this.label = label; this.clock = clock; this.distanceM = distanceM;
        this.widthM = widthM; this.height = height; this.labelCm = labelCm;
    }

    // degrees clockwise from straight ahead: 12 -> 0, 2 -> 60, 3 -> 90, 10 -> 300 (= -60)
    public float Yaw { get { return (clock % 12) * 30f; } }
}

/// PIC 2: puts every visualization where the Room Builder table says (clock / distance /
/// width / height / label size, all measured from the home position = the viewer).
///   V1 Bar chart  : 12 o'clock, 3 m, 3 m wide, eye height
///   V2 World map  : 12 o'clock, 2 m, 3 m wide, floor
///   V3 Event      : 2 o'clock,  2 m, 1 m wide, waist   (timeline)
///   V4 Altitude   : 10 o'clock, 2 m, 1 m wide, waist   (energy vs altitude scatter)
/// Runs in Awake, i.e. before RankingBars / WorldMapTable build themselves in Start.
/// Right-click the component -> "Apply (rebuild now)" to preview in Edit mode.
public class RoomLayout : MonoBehaviour
{
    [Header("Targets")]
    public Transform viewer;              // home position (Main Camera)
    public RankingBars rankingBars;       // V1
    public WorldMapTable worldMap;        // V2
    public Transform timelineRoot;        // V3 (PIC 1's timeline root; optional until it exists)
    public Transform scatterRoot;         // V4 (PIC 1's ScatterPlot root)

    [Header("Room Builder table")]
    public ViewSpec v1BarChart  = new ViewSpec("V1 Bar Chart", 12, 3f, 3f, ViewHeight.Eye,   3f);
    public ViewSpec v2WorldMap  = new ViewSpec("V2 World Map", 12, 2f, 3f, ViewHeight.Floor, 3f);
    public ViewSpec v3Event     = new ViewSpec("V3 Event",      2, 2f, 1f, ViewHeight.Waist, 3f);
    public ViewSpec v4Altitude  = new ViewSpec("V4 Altitude",  10, 2f, 1f, ViewHeight.Waist, 3f);

    [Header("Heights & native panel sizes")]
    public float waistHeight = 1.0f;                                  // m above the floor
    public Vector2 scatterNativeSize = new Vector2(4f, 2.5f);         // ScatterPlot width x height as built
    public Vector2 timelineNativeSize = new Vector2(4f, 2.5f);        // timeline width x height as built (adjust)
    public float minReadableDmm = 20f;                                // warn below this (dmm = mm per metre of distance)

    void Awake() { Apply(false); }

    IEnumerator Start()
    {
        yield return null;            // wait until every Start() has built its labels
        EnforceLabelMinimum();
        ReportReadability();
    }

    [ContextMenu("Apply (rebuild now)")]
    void ApplyAndRebuild() { Apply(true); EnforceLabelMinimum(); ReportReadability(); }

    float HeightY(ViewHeight h)
    {
        switch (h)
        {
            case ViewHeight.Floor: return 0f;
            case ViewHeight.Waist: return waistHeight;
            default: return viewer != null ? viewer.position.y : 1.6f;
        }
    }

    public void Apply(bool rebuild)
    {
        if (viewer == null) { Debug.LogError("RoomLayout: assign the viewer (Main Camera)"); return; }

        // ---- V1 bar chart: radius = distance, arc from the width, centre at the requested height
        if (rankingBars != null)
        {
            var v = v1BarChart;
            if (v.clock != 12) Debug.LogWarning("RoomLayout: RankingBars is always centred on 12 o'clock; clock " + v.clock + " is ignored for V1");
            rankingBars.viewer = viewer;
            rankingBars.staticAnchor = true;
            rankingBars.anchorPosition = viewer.position;
            rankingBars.radius = v.distanceM;
            float span = Mathf.Max(0.1f, v.widthM - rankingBars.barWidth); // bar centres, so the outer edges reach widthM
            rankingBars.arcDegrees = 2f * Mathf.Asin(Mathf.Clamp01(span / (2f * v.distanceM))) * Mathf.Rad2Deg;
            rankingBars.baseY = v.height == ViewHeight.Floor ? 0f
                                : Mathf.Max(0f, HeightY(v.height) - rankingBars.maxHeight * 0.5f);
            if (rebuild) rankingBars.Rebuild();
        }

        // ---- V2 world map
        if (worldMap != null)
        {
            var v = v2WorldMap;
            worldMap.viewer = viewer;
            worldMap.directionDegrees = v.Yaw;
            worldMap.distance = v.distanceM;
            worldMap.mapWidth = v.widthM;
            worldMap.tableHeight = HeightY(v.height);
            worldMap.showTable = v.height != ViewHeight.Floor;
            if (rebuild) worldMap.Rebuild();
        }

        // ---- V3 / V4 panels built by PIC 1 (placed and scaled as a whole)
        Place(timelineRoot, v3Event, timelineNativeSize);
        Place(scatterRoot, v4Altitude, scatterNativeSize);
    }

    // Origin of the panel = bottom centre of its plot, content faces the viewer (local +Z points away).
    void Place(Transform root, ViewSpec v, Vector2 nativeSize)
    {
        if (root == null) return;
        float s = v.widthM / nativeSize.x;
        float a = v.Yaw * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
        Vector3 pos = new Vector3(viewer.position.x, 0f, viewer.position.z) + dir * v.distanceM;
        pos.y = v.height == ViewHeight.Floor ? 0f : Mathf.Max(0f, HeightY(v.height) - nativeSize.y * s * 0.5f);
        root.SetPositionAndRotation(pos, Quaternion.LookRotation(dir, Vector3.up));
        root.localScale = Vector3.one * s;
    }

    // ---- label size: TextMesh capital-letter height in cm (fontSize 100: ~2.8 x characterSize m)
    static float CapCm(TextMesh tm)
    {
        float fs = tm.fontSize > 0 ? tm.fontSize : 100f;
        return 0.028f * tm.characterSize * fs * tm.transform.lossyScale.y * 100f;
    }

    // Labels smaller than the table's "label cm" are enlarged (never shrunk).
    void EnforceLabelMinimum()
    {
        Enforce(rankingBars != null ? rankingBars.transform : null, v1BarChart);
        Enforce(worldMap != null ? worldMap.transform : null, v2WorldMap);
        Enforce(timelineRoot, v3Event);
        Enforce(scatterRoot, v4Altitude);
    }

    static void Enforce(Transform root, ViewSpec v)
    {
        if (root == null) return;
        foreach (var tm in root.GetComponentsInChildren<TextMesh>())
        {
            float cm = CapCm(tm);
            if (cm > 0.0001f && cm < v.labelCm) tm.characterSize *= v.labelCm / cm;
        }
    }

    void ReportReadability()
    {
        Report(rankingBars != null ? rankingBars.transform : null, v1BarChart);
        Report(worldMap != null ? worldMap.transform : null, v2WorldMap);
        Report(timelineRoot, v3Event);
        Report(scatterRoot, v4Altitude);
    }

    void Report(Transform root, ViewSpec v)
    {
        if (root == null) { Debug.Log("RoomLayout: " + v.label + " - not assigned yet"); return; }
        float min = float.MaxValue;
        foreach (var tm in root.GetComponentsInChildren<TextMesh>()) min = Mathf.Min(min, CapCm(tm));
        if (min == float.MaxValue) { Debug.Log("RoomLayout: " + v.label + " - no labels found"); return; }
        float dmm = min * 10f / v.distanceM; // 1 dmm = 1 mm at 1 m
        string msg = "RoomLayout: " + v.label + " - smallest label " + min.ToString("0.0") + " cm at " +
                     v.distanceM.ToString("0.0") + " m = " + dmm.ToString("0.0") + " dmm";
        if (dmm < minReadableDmm) Debug.LogWarning(msg + "  (below " + minReadableDmm + " dmm: hard to read)");
        else Debug.Log(msg);
    }
}
