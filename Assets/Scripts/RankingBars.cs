using UnityEngine;

/// Ranking of the 10 highest-energy fireballs (Q1).
/// The reader stands at 'viewer' and turns their head to compare bars (egocentric reading).
/// Attach to an empty GameObject and assign top10.json to 'dataJson'.
public class RankingBars : MonoBehaviour
{
    public TextAsset dataJson;          // top10.json
    public Transform viewer;            // reader's eye position (home position)
    public float radius = 5f;           // distance from the reader to the bars (m)
    public float arcDegrees = 170f;     // angular spread; wider = more spacing between bars
    public float maxHeight = 2.5f;      // height of the #1 bar (m)
    public float barWidth = 0.3f;
    public float baseY = 0f;            // floor level of the bar bases (PIC 2: RoomLayout sets it)
    public bool useLog = false;         // false = linear (honest), true = log
    public float labelCharSize = 0.014f;
    public float baseLift = 0.35f;      // gap between the bar top and the lowest label level (m)
    public float levelHeight = 0.6f;    // height difference between label levels (m)
    public int levels = 3;              // labels cycle through this many heights so neighbours never share one

    static readonly string[] Months =
        { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    void Start() { Rebuild(); } // always rebuilds, so objects saved in Edit mode never duplicate

    // Right-click the component header -> "Rebuild" to preview in Edit mode
    [ContextMenu("Rebuild")]
    public void Rebuild()
    {
        Clear();
        Build();
    }

    // Right-click the component header -> "Clear" to remove all generated children
    // (use this before saving the prefab, so only the empty object + script are stored)
    [ContextMenu("Clear")]
    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            FireballPalette.Kill(transform.GetChild(i).gameObject);
    }

    // "2013-02-15 03:20:26" -> "15 Feb" and "2013" (short lines keep each label narrow)
    static void SplitDate(string date, out string dayMonth, out string year)
    {
        year = date.Substring(0, 4);
        int month = int.Parse(date.Substring(5, 2));
        int day = int.Parse(date.Substring(8, 2));
        dayMonth = day + " " + Months[Mathf.Clamp(month, 1, 12) - 1];
    }

    void Build()
    {
        if (dataJson == null) { Debug.LogError("RankingBars: assign top10.json to Data Json"); return; }
        var file = JsonUtility.FromJson<FireballFile>(dataJson.text);
        var ev = file.events;
        if (ev == null || ev.Length == 0) { Debug.LogError("top10.json is empty"); return; }

        float maxVal = 0f;
        foreach (var e in ev) maxVal = Mathf.Max(maxVal, e.impact_e);

        Vector3 center = viewer != null ? viewer.position : transform.position;
        Vector3 eye = viewer != null ? viewer.position : center + Vector3.up * 1.6f;
        // baseY is a public field now (see above)

        for (int i = 0; i < ev.Length; i++)
        {
            var e = ev[i];
            // Spread the bars along an arc around the reader
            float t = ev.Length == 1 ? 0.5f : (float)i / (ev.Length - 1);
            float ang = Mathf.Lerp(-arcDegrees / 2f, arcDegrees / 2f, t) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));
            Vector3 basePos = new Vector3(center.x, baseY, center.z) + dir * radius;

            float h = useLog
                ? maxHeight * Mathf.Log10(1f + e.impact_e) / Mathf.Log10(1f + maxVal)
                : maxHeight * e.impact_e / maxVal;
            h = Mathf.Max(h, 0.05f); // keep the smallest bar visible

            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = $"Bar_{e.rank}_{e.event_id}";
            bar.transform.SetParent(transform, false);
            bar.transform.localScale = new Vector3(barWidth, h, barWidth);
            bar.transform.position = basePos + Vector3.up * (h / 2f);
            bar.transform.rotation = Quaternion.LookRotation(basePos - new Vector3(center.x, baseY, center.z));
            FireballPalette.Kill(bar.GetComponent<Collider>()); // no interaction allowed
            FireballPalette.Tint(bar.GetComponent<Renderer>(), FireballPalette.ForRank(e.rank));

            // Compact 4-line label: rank / day+month / year / energy
            SplitDate(e.date, out string dayMonth, out string year);
            string txt = $"#{e.rank}\n{dayMonth}\n{year}\n{e.impact_e:0.#} kt";

            // Labels cycle through several heights, so neighbours are never at the same height
            int level = (e.rank - 1) % Mathf.Max(1, levels);
            Vector3 barTop = basePos + Vector3.up * h;
            Vector3 labelPos = barTop + Vector3.up * (baseLift + level * levelHeight);

            var tm = FireballPalette.MakeLabel(txt, labelPos, transform, labelCharSize);
            tm.transform.rotation = Quaternion.LookRotation(labelPos - eye);

            // Thin leader line from the bar top to the bottom of its label
            MakeLeader(barTop, labelPos - Vector3.up * 0.28f);
        }
    }

    void MakeLeader(Vector3 from, Vector3 to)
    {
        var go = new GameObject("Leader");
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
        lr.startWidth = lr.endWidth = 0.01f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = new Color(1f, 1f, 1f, 0.7f);
    }
}