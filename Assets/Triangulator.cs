// Adapted from https://forum.unity.com/threads/polygon-triangulation-code.27223/#post-1274726

using UnityEngine;
using System.Collections.Generic;

public class Triangulator
{
    private List<Vector2> m_points;

    public Triangulator(List<Vector2> points)
    {
        m_points = new List<Vector2>(points);
    }

    public Triangulator(List<Vector3> points)
    {
        m_points = new List<Vector2>();
        foreach(var point in points) {
            m_points.Add(new Vector2(point.x, point.y));
        }
    }

    public int[] Triangulate() {
        var indices = new List<int>();
        
        var n = m_points.Count;
        if (n < 3)
            return indices.ToArray();
        
        var V = new int[n];
        if (Area() > 0) 
        {
            for (var v = 0; v < n; v++)
                V[v] = v;
        }
        else {
            for (var v = 0; v < n; v++)
                V[v] = (n - 1) - v;
        }
        
        var nv = n;
        var count = 2 * nv;
        var m = 0;
        for (var v = nv - 1; nv > 2; ) 
        {
            if ((count--) <= 0)
                return indices.ToArray();
            
            var u = v;
            if (nv <= u)
                u = 0;
            v = u + 1;
            if (nv <= v)
                v = 0;
            var w = v + 1;
            if (nv <= w)
                w = 0;
            
            if (Snip(u, v, w, nv, V)) 
            {
                int a = V[u];
                int b = V[v];
                int c = V[w];
                indices.Add(a);
                indices.Add(c);
                indices.Add(b);
                m++;
                int s = v;
                for (int t = v + 1; t < nv; t++)
                {
                    V[s] = V[t];
                    s++;
                }
                nv--;
                count = 2 * nv;
            }
        }
        
        return indices.ToArray();
    }
    
    private float Area () {
        int n = m_points.Count;
        float A = 0.0f;
        int q = 0;
        for (var p = n - 1; q < n; p = q++) {
            var pval = m_points[p];
            var qval = m_points[q];
            A += pval.x * qval.y - qval.x * pval.y;
        }
        return (A * 0.5f);
    }
    
    private bool Snip (int u, int v, int w, int n, int[] V) {
        var A = m_points[V[u]];
        var B = m_points[V[v]];
        var C = m_points[V[w]];

        if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
            return false;
        for (int p = 0; p < n; p++) {
            if ((p == u) || (p == v) || (p == w))
                continue;
            var P = m_points[V[p]];
            if (InsideTriangle(A, B, C, P))
                return false;
        }
        return true;
    }
    
    private bool InsideTriangle (Vector2 A, Vector2 B, Vector2 C, Vector2 P) {
        float ax = C.x - B.x;
        float ay = C.y - B.y;
        float bx = A.x - C.x;
        float by = A.y - C.y;
        float cx = B.x - A.x;
        float cy = B.y - A.y;

        float apx = P.x - A.x;
        float apy = P.y - A.y;
        float bpx = P.x - B.x;
        float bpy = P.y - B.y;
        float cpx = P.x - C.x;
        float cpy = P.y - C.y;
        
        float aCROSSbp = ax * bpy - ay * bpx;
        float cCROSSap = cx * apy - cy * apx;
        float bCROSScp = bx * cpy - by * cpx;
        
        return ((aCROSSbp > 0.0) && (bCROSScp > 0.0) && (cCROSSap > 0.0));
    }
}