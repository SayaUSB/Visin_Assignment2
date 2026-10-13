using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// Event scatter plot (Q3): WHEN did the fireballs happen, and how energetic were they?
///   x = date of the event (years),  y = calculated impact energy (kt TNT, log scale).
/// Same visual language as the altitude scatter: grey dots, the top 10 in their rank colour
/// (FireballPalette) and a row of callouts above the plot, white axes, one reference line.
/// The plot area is x in [-width/2, +width/2], y in [0, height]; the origin is the bottom centre
/// of the plot, like the altitude scatter, so both can be placed and scaled the same way.
/// Attach to the ScatterPlotEvent object and assign AllEvents.json to 'dataJson'.
public class ScatterPlotEvent : MonoBehaviour
{
    public TextAsset dataJson;           // AllEvents.json (all events, 10 flagged is_top10)
    public float width = 4f;             // plot width (m)
    public float height = 2.5f;          // plot height (m)
    public float minEnergy = 0.01f;      // bottom of the log axis (kt)
    public float maxEnergy = 1000f;      // top of the log axis (kt)
    public int yearStep = 5;             // one year label every N years
    public float dotSize = 0.05f;
    public float topDotSize = 0.12f;
    public float labelCharSize = 0.02f;  // tick labels
    public float titleCharSize = 0.024f; // axis titles and callouts
    public float calloutSpacing = 0.32f; // minimum distance between neighbouring callouts (m)
    public float calloutRise = 0.3f;     // callout row height above the top of the plot (m)

    static readonly Color Grey = new Color(0.65f, 0.65f, 0.65f, 1f);
    static readonly Color AxisColor = Color.white;
    static readonly Color RefColor = new Color(1f, 1f, 1f, 0.6f);

    void Start() { Rebuild(); } // always rebuilds, so objects saved in Edit mode never duplicate

    // Right-click the component header -> "Rebuild" to preview in Edit mode
    [ContextMenu("Rebuild")]
    public void Rebuild()
    {
        Clear();
        Build();
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            FireballPalette.Kill(transform.GetChild(i).gameObject);
    }

    static string Fmt(float v, string f) { return v.ToString(f, CultureInfo.InvariantCulture); }

    // "2013-02-15 03:20:26" -> 2013.12 (decimal year)
    static float DecimalYear(string date)
    {
        int y = int.Parse(date.Substring(0, 4), CultureInfo.InvariantCulture);
        int m = int.Parse(date.Substring(5, 2), CultureInfo.InvariantCulture);
        int d = int.Parse(date.Substring(8, 2), CultureInfo.InvariantCulture);
        return y + (new DateTime(y, m, d).DayOfYear - 1) / 365.25f;
    }

    void Build()
    {
        if (dataJson == null) { Debug.LogError("ScatterPlotEvent: assign AllEvents.json to Data Json"); return; }
        var file = JsonUtility.FromJson<FireballFile>(dataJson.text);
        var ev = file != null ? file.events : null;
        if (ev == null || ev.Length == 0) { Debug.LogError("ScatterPlotEvent: the data file is empty"); return; }

        // ---- axes ranges
        float tMin = float.MaxValue, tMax = float.MinValue;
        foreach (var e in ev)
        {
            float t = DecimalYear(e.date);
            tMin = Mathf.Min(tMin, t); tMax = Mathf.Max(tMax, t);
        }
        int step = Mathf.Max(1, yearStep);
        int yearMin = Mathf.FloorToInt(tMin / step) * step;
        int yearMax = Mathf.CeilToInt(tMax / step) * step;
        if (yearMax <= yearMin) yearMax = yearMin + step;
        float logMin = Mathf.Log10(minEnergy), logMax = Mathf.Log10(maxEnergy);

        Func<float, float> X = t => -width * 0.5f + (t - yearMin) / (yearMax - yearMin) * width;
        Func<float, float> Y = en => (Mathf.Log10(Mathf.Max(en, minEnergy)) - logMin) / (logMax - logMin) * height;

        // ---- dots: all events grey, the top 10 larger, in front, in their rank colour
        var top = new List<FireballEvent>();
        var topX = new Dictionary<string, float>();
        foreach (var e in ev)
        {
            float x = X(DecimalYear(e.date)), y = Y(e.impact_e);
            if (e.is_top10) { top.Add(e); topX[e.event_id] = x; }
            MakeDot("Dot_" + e.event_id, new Vector3(x, y, e.is_top10 ? -0.02f : 0f),
                    e.is_top10 ? topDotSize : dotSize, e.is_top10 ? FireballPalette.ForRank(e.rank) : Grey);
        }

        // ---- axes
        MakeLine("Line", new[] { new Vector3(-width * 0.5f, 0f, 0f), new Vector3(width * 0.5f, 0f, 0f) }, AxisColor);
        MakeLine("Line", new[] { new Vector3(-width * 0.5f, 0f, 0f), new Vector3(-width * 0.5f, height, 0f) }, AxisColor);

        for (int yr = yearMin; yr <= yearMax; yr += step)
            MakeLabel(yr.ToString(CultureInfo.InvariantCulture), new Vector3(X(yr), -0.15f, 0f), labelCharSize);
        for (int p = Mathf.RoundToInt(logMin); p <= Mathf.RoundToInt(logMax); p++)
        {
            float en = Mathf.Pow(10f, p);
            MakeLabel(Fmt(en, "0.##"), new Vector3(-width * 0.5f - 0.2f, Y(en), 0f), labelCharSize);
        }
        MakeLabel("Date of the event (year)", new Vector3(0f, -0.4f, 0f), titleCharSize);
        MakeLabel("Impact energy (kt TNT, log scale)", new Vector3(-width * 0.5f - 0.6f, height + 0.2f, 0f), titleCharSize);

        // ---- reference line: the energy of the 10th-ranked event (everything above is in the top 10)
        float tenth = float.MaxValue;
        foreach (var e in top) tenth = Mathf.Min(tenth, e.impact_e);
        if (tenth < float.MaxValue)
        {
            float yRef = Y(tenth);
            MakeLine("Line", new[] { new Vector3(-width * 0.5f, yRef, 0f), new Vector3(width * 0.5f, yRef, 0f) }, RefColor);
            MakeLabel("top 10: " + Fmt(tenth, "0.#") + " kt and up", new Vector3(width * 0.5f + 0.5f, yRef, 0f), labelCharSize);
        }

        // ---- callouts above the plot: "#rank / year", ordered by date, spread so they never overlap
        top.Sort((a, b) => topX[a.event_id].CompareTo(topX[b.event_id]));
        int n = top.Count;
        var cx = new float[n];
        for (int i = 0; i < n; i++)
        {
            cx[i] = topX[top[i].event_id];
            if (i > 0) cx[i] = Mathf.Max(cx[i], cx[i - 1] + calloutSpacing);
        }
        if (n > 0) cx[n - 1] = Mathf.Min(cx[n - 1], width * 0.5f);
        for (int i = n - 2; i >= 0; i--) cx[i] = Mathf.Min(cx[i], cx[i + 1] - calloutSpacing);

        float labelY = height + calloutRise;
        for (int i = 0; i < n; i++)
        {
            var e = top[i];
            Color c = FireballPalette.ForRank(e.rank);
            MakeLine("Line", new[] { new Vector3(topX[e.event_id], Y(e.impact_e), -0.02f),
                                     new Vector3(cx[i], labelY - 0.12f, 0f) }, c);
            MakeLabel("#" + e.rank + "\n" + e.date.Substring(0, 4), new Vector3(cx[i], labelY, 0f), titleCharSize);
        }
    }

    void MakeDot(string name, Vector3 localPos, float size, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = Vector3.one * size;
        FireballPalette.Kill(go.GetComponent<Collider>()); // no interaction allowed
        FireballPalette.Tint(go.GetComponent<Renderer>(), color);
    }

    void MakeLine(string name, Vector3[] localPoints, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = localPoints.Length;
        lr.SetPositions(localPoints);
        lr.startWidth = lr.endWidth = 0.008f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = color;
    }

    void MakeLabel(string text, Vector3 localPos, float charSize)
    {
        var tm = FireballPalette.MakeLabel(text, Vector3.zero, transform, charSize);
        tm.transform.localPosition = localPos;
        tm.transform.localRotation = Quaternion.identity; // readable when looking along the plot's +Z
    }
}
