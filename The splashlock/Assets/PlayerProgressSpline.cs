using UnityEngine;

public class PlayerProgressSpline : MonoBehaviour
{
    public LineRenderer[] raceLines; // Sleep hier alle LineRenderers van het parkour
    private float[] totalLengths;
    private float[][] segmentLengths;

    private void Start()
    {
        if (raceLines == null || raceLines.Length == 0) return;

        totalLengths = new float[raceLines.Length];
        segmentLengths = new float[raceLines.Length][];

        for (int l = 0; l < raceLines.Length; l++)
        {
            LineRenderer lr = raceLines[l];
            int count = lr.positionCount;
            segmentLengths[l] = new float[count - 1];
            float total = 0f;

            for (int i = 0; i < count - 1; i++)
            {
                float segLength = Vector3.Distance(lr.GetPosition(i), lr.GetPosition(i + 1));
                segmentLengths[l][i] = segLength;
                total += segLength;
            }

            totalLengths[l] = total;
        }
    }

    public float GetProgress()
    {
        if (raceLines == null || raceLines.Length == 0) return 0f;

        // Zoek het segment over alle lijnen waar de speler het dichtstbij is
        int closestLine = 0;
        int closestSegIndex = 0;
        float closestDist = float.MaxValue;

        for (int l = 0; l < raceLines.Length; l++)
        {
            LineRenderer lr = raceLines[l];
            for (int i = 0; i < lr.positionCount - 1; i++)
            {
                Vector3 start = lr.GetPosition(i);
                Vector3 end = lr.GetPosition(i + 1);
                Vector3 projected = ProjectPointOnLineSegment(start, end, transform.position);
                float dist = Vector3.Distance(transform.position, projected);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestLine = l;
                    closestSegIndex = i;
                }
            }
        }

        // Bereken totale afstand afgelegd tot dit segment
        float progress = 0f;
        for (int l = 0; l < closestLine; l++)
            progress += totalLengths[l];

        // Voeg segmenten in deze lijn toe
        for (int i = 0; i < closestSegIndex; i++)
            progress += segmentLengths[closestLine][i];

        // Voeg fractioneel deel van het huidige segment toe
        LineRenderer line = raceLines[closestLine];
        Vector3 startSeg = line.GetPosition(closestSegIndex);
        Vector3 endSeg = line.GetPosition(closestSegIndex + 1);
        float segLength = segmentLengths[closestLine][closestSegIndex];
        float fraction = Vector3.Distance(startSeg, ProjectPointOnLineSegment(startSeg, endSeg, transform.position)) / segLength;
        progress += fraction * segLength;

        // Normaliseer over totale lengte van alle lijnen
        float totalLength = 0f;
        foreach (var l in totalLengths) totalLength += l;

        return progress / totalLength; // 0-1
    }

    private Vector3 ProjectPointOnLineSegment(Vector3 a, Vector3 b, Vector3 point)
    {
        Vector3 ab = b - a;
        float t = Vector3.Dot(point - a, ab) / Vector3.Dot(ab, ab);
        t = Mathf.Clamp01(t);
        return a + ab * t;
    }
}
