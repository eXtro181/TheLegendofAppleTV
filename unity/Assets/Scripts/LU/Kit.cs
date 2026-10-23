// Baukasten (Quaternius Downtown City MegaKit, CC0) und Stapel-Meshes.
// Alle Welt-Koordinaten sind wie in der Browser-Version (rechtshändig, +Z nach Süden);
// erst beim Erzeugen der Unity-Meshes wird Z gespiegelt (W(x,y,z) => Unity(x,y,-z)).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LU
{
    /// Einfache Geometrie im JS-Raum (rechtshändig, Dreiecke gegen den Uhrzeigersinn).
    public class Geo
    {
        public Vector3[] P; public Vector3[] N; public Vector2[] U; public int[] I;
        public int Count => P.Length;

        public static Geo Box(float w, float h, float d) => BoxUV(w, h, d, 0, 0, 0, 1, false);

        /// Quader mit Welt-skalierten UVs (wie boxGeo/boxW der Browser-Version).
        public static Geo BoxUV(float w, float h, float d, float ox, float oy, float oz, float tile, bool world)
        {
            var P = new List<Vector3>(); var N = new List<Vector3>(); var U = new List<Vector2>(); var I = new List<int>();
            void Face(Vector3 n, Vector3 u, Vector3 v, float su, float sv)
            {
                int b = P.Count; Vector3 c = new Vector3(n.x * w / 2, n.y * h / 2, n.z * d / 2);
                Vector3[] corners = { c - u * su / 2 - v * sv / 2, c + u * su / 2 - v * sv / 2, c + u * su / 2 + v * sv / 2, c - u * su / 2 + v * sv / 2 };
                foreach (var q in corners)
                {
                    P.Add(q); N.Add(n);
                    Vector3 wq = q + new Vector3(ox, oy, oz);
                    float uu, vv;
                    if (Mathf.Abs(n.y) > .5f) { uu = (world ? wq.x : q.x + w / 2) / tile; vv = (world ? wq.z : q.z + d / 2) / tile; }
                    else if (Mathf.Abs(n.x) > .5f) { uu = (world ? wq.z * -n.x : (n.x > 0 ? d / 2 - q.z : q.z + d / 2)) / tile; vv = (world ? wq.y : q.y + h / 2) / tile; }
                    else { uu = (world ? wq.x * n.z : (n.z > 0 ? q.x + w / 2 : w / 2 - q.x)) / tile; vv = (world ? wq.y : q.y + h / 2) / tile; }
                    U.Add(new Vector2(uu, vv));
                }
                I.Add(b); I.Add(b + 1); I.Add(b + 2); I.Add(b); I.Add(b + 2); I.Add(b + 3);
            }
            Face(Vector3.forward, Vector3.right, Vector3.up, w, h);
            Face(Vector3.back, Vector3.left, Vector3.up, w, h);
            Face(Vector3.right, Vector3.back, Vector3.up, d, h);
            Face(Vector3.left, Vector3.forward, Vector3.up, d, h);
            Face(Vector3.up, Vector3.right, Vector3.back, w, d);
            Face(Vector3.down, Vector3.right, Vector3.forward, w, d);
            return new Geo { P = P.ToArray(), N = N.ToArray(), U = U.ToArray(), I = I.ToArray() };
        }

        public static Geo Plane(float w, float d, float tile, float x0, float z0) // waagrecht, nach oben
        {
            var g = new Geo();
            g.P = new[] { new Vector3(-w / 2, 0, -d / 2), new Vector3(w / 2, 0, -d / 2), new Vector3(w / 2, 0, d / 2), new Vector3(-w / 2, 0, d / 2) };
            g.N = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            g.U = new Vector2[4]; for (int i = 0; i < 4; i++) g.U[i] = new Vector2((g.P[i].x + x0 + w / 2) / tile, -(g.P[i].z + z0 + d / 2) / tile);
            g.I = new[] { 0, 2, 1, 0, 3, 2 };
            return g;
        }

        public static Geo Cylinder(float rt, float rb, float h, int seg, bool open, float t0 = 0, float tl = Mathf.PI * 2)
        {
            var P = new List<Vector3>(); var N = new List<Vector3>(); var U = new List<Vector2>(); var I = new List<int>();
            float slope = (rb - rt) / h;
            for (int y = 0; y <= 1; y++)
                for (int x = 0; x <= seg; x++)
                {
                    float u = (float)x / seg, th = t0 + u * tl, r = y == 0 ? rt : rb, s = Mathf.Sin(th), c = Mathf.Cos(th);
                    P.Add(new Vector3(r * s, y == 0 ? h / 2 : -h / 2, r * c)); N.Add(new Vector3(s, slope, c).normalized); U.Add(new Vector2(u, 1 - y));
                }
            for (int x = 0; x < seg; x++) { int a = x, b = x + seg + 1, cc = x + seg + 2, dd = x + 1; I.Add(a); I.Add(b); I.Add(dd); I.Add(b); I.Add(cc); I.Add(dd); }
            if (!open)
                foreach (bool top in new[] { true, false })
                {
                    float r = top ? rt : rb; if (r <= 0) continue; float sy = top ? 1 : -1; int c0 = P.Count;
                    P.Add(new Vector3(0, sy * h / 2, 0)); N.Add(new Vector3(0, sy, 0)); U.Add(new Vector2(.5f, .5f));
                    for (int x = 0; x <= seg; x++) { float th = t0 + (float)x / seg * tl; P.Add(new Vector3(r * Mathf.Sin(th), sy * h / 2, r * Mathf.Cos(th))); N.Add(new Vector3(0, sy, 0)); U.Add(new Vector2(Mathf.Cos(th) * .5f + .5f, Mathf.Sin(th) * .5f + .5f)); }
                    for (int x = 0; x < seg; x++) { if (top) { I.Add(c0 + 1 + x); I.Add(c0 + 2 + x); I.Add(c0); } else { I.Add(c0 + 2 + x); I.Add(c0 + 1 + x); I.Add(c0); } }
                }
            return new Geo { P = P.ToArray(), N = N.ToArray(), U = U.ToArray(), I = I.ToArray() };
        }

        public static Geo Cone(float r, float h, int seg) => Cylinder(0, r, h, seg, false);

        public static Geo Sphere(float r, int ws, int hs)
        {
            var P = new List<Vector3>(); var N = new List<Vector3>(); var U = new List<Vector2>(); var I = new List<int>();
            for (int iy = 0; iy <= hs; iy++) for (int ix = 0; ix <= ws; ix++)
                {
                    float u = (float)ix / ws, v = (float)iy / hs;
                    var p = new Vector3(-r * Mathf.Cos(u * Mathf.PI * 2) * Mathf.Sin(v * Mathf.PI), r * Mathf.Cos(v * Mathf.PI), r * Mathf.Sin(u * Mathf.PI * 2) * Mathf.Sin(v * Mathf.PI));
                    P.Add(p); N.Add(p.normalized); U.Add(new Vector2(u, 1 - v));
                }
            for (int iy = 0; iy < hs; iy++) for (int ix = 0; ix < ws; ix++)
                {
                    int a = iy * (ws + 1) + ix + 1, b = iy * (ws + 1) + ix, c = (iy + 1) * (ws + 1) + ix, d = (iy + 1) * (ws + 1) + ix + 1;
                    if (iy != 0) { I.Add(a); I.Add(b); I.Add(d); }
                    if (iy != hs - 1) { I.Add(b); I.Add(c); I.Add(d); }
                }
            return new Geo { P = P.ToArray(), N = N.ToArray(), U = U.ToArray(), I = I.ToArray() };
        }
    }

    public static class M
    {
        /// mtx() der Browser-Version: Position, Euler YXZ (Bogenmaß), Skalierung — im JS-Raum.
        public static Matrix4x4 T(float x, float y, float z, float ry = 0, float rx = 0, float rz = 0, float sx = 1, float sy = 1, float sz = 1)
        {
            Quaternion q = Quaternion.AngleAxis(ry * Mathf.Rad2Deg, Vector3.up) * Quaternion.AngleAxis(rx * Mathf.Rad2Deg, Vector3.right) * Quaternion.AngleAxis(rz * Mathf.Rad2Deg, Vector3.forward);
            return Matrix4x4.TRS(new Vector3(x, y, z), q, new Vector3(sx == 0 ? 1 : sx, sy == 0 ? 1 : sy, sz == 0 ? 1 : sz));
        }
        // Die Rotationsmatrix eines Quaternions ist unabhängig von der Händigkeit: TRS liefert dieselben Zahlen wie compose() in JS.
        public static Vector3 U(float x, float y, float z) => new Vector3(x, y, -z);
        public static Vector3 U(Vector3 v) => new Vector3(v.x, v.y, -v.z);
        public static Quaternion Yaw(float jsYaw) => Quaternion.Euler(0, 180f - jsYaw * Mathf.Rad2Deg, 0);
        public static Color Hex(int h, float k = 1) => new Color(((h >> 16) & 255) / 255f * k, ((h >> 8) & 255) / 255f * k, (h & 255) / 255f * k, 1);
    }

    /// Sammelt Geometrie je Material und Zelle und baut daraus große Meshes (wenige Draw Calls).
    public class Batcher
    {
        class Part { public Geo g; public Matrix4x4 m; public Color c; }
        readonly Dictionary<string, Dictionary<string, List<Part>>> buckets = new();
        public readonly Dictionary<string, List<Renderer>> Cells = new();
        public readonly HashSet<string> Grime = new();

        public void Add(string mat, string cell, Geo g, Matrix4x4 m, Color c)
        {
            if (!buckets.TryGetValue(mat, out var b)) buckets[mat] = b = new();
            if (!b.TryGetValue(cell, out var l)) b[cell] = l = new();
            l.Add(new Part { g = g, m = m, c = c });
        }

        public void Build(Transform parent, System.Func<string, Material> mat, System.Func<string, bool> shadows)
        {
            foreach (var mb in buckets)
            {
                Material material = mat(mb.Key); if (material == null) { Debug.LogWarning("Material fehlt: " + mb.Key); continue; }
                bool grime = Grime.Contains(mb.Key);
                foreach (var cb in mb.Value)
                {
                    var parts = cb.Value; int i = 0;
                    while (i < parts.Count)
                    {
                        int nv = 0, j = i; while (j < parts.Count && nv < 400000) { nv += parts[j].g.Count; j++; }
                        var mesh = Merge(parts, i, j, grime);
                        var go = new GameObject(mb.Key + "@" + cb.Key);
                        go.transform.SetParent(parent, false);
                        go.AddComponent<MeshFilter>().sharedMesh = mesh;
                        var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = material;
                        r.shadowCastingMode = shadows(mb.Key) ? ShadowCastingMode.On : ShadowCastingMode.Off;
                        if (!Cells.TryGetValue(cb.Key, out var cl)) Cells[cb.Key] = cl = new();
                        cl.Add(r); i = j;
                    }
                }
            }
            buckets.Clear();
        }

        static Mesh Merge(List<Part> parts, int a, int b, bool grime)
        {
            int nv = 0, ni = 0; for (int k = a; k < b; k++) { nv += parts[k].g.Count; ni += parts[k].g.I.Length; }
            var P = new Vector3[nv]; var N = new Vector3[nv]; var UV = new Vector2[nv]; var C = new Color32[nv]; var I = new int[ni];
            int vo = 0, io = 0;
            for (int k = a; k < b; k++)
            {
                var p = parts[k]; var m = p.m; var nm = m.inverse.transpose; var g = p.g;
                for (int v = 0; v < g.Count; v++)
                {
                    Vector3 w = m.MultiplyPoint3x4(g.P[v]); Vector3 n = nm.MultiplyVector(g.N[v]).normalized;
                    float gk = 1;
                    if (grime) { float t = Mathf.Clamp01(w.y / 2.6f); gk = (.7f + .3f * t * t * (3 - 2 * t)) * (.9f + .1f * Mathf.PerlinNoise(w.x * .21f + w.z * .13f, w.y * .35f)); }
                    P[vo + v] = new Vector3(w.x, w.y, -w.z); N[vo + v] = new Vector3(n.x, n.y, -n.z); UV[vo + v] = g.U[v];
                    C[vo + v] = new Color(p.c.r * gk, p.c.g * gk, p.c.b * gk, 1);
                }
                bool flip = m.determinant < 0; // gespiegelte Teile: Reihenfolge bleibt, Z-Spiegelung dreht ohnehin
                for (int t = 0; t < g.I.Length; t += 3)
                {
                    // Z-Spiegelung kehrt den Umlaufsinn um -> Indizes tauschen
                    if (!flip) { I[io + t] = g.I[t] + vo; I[io + t + 1] = g.I[t + 2] + vo; I[io + t + 2] = g.I[t + 1] + vo; }
                    else { I[io + t] = g.I[t] + vo; I[io + t + 1] = g.I[t + 1] + vo; I[io + t + 2] = g.I[t + 2] + vo; }
                }
                vo += g.Count; io += g.I.Length;
            }
            var mesh = new Mesh { indexFormat = nv > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.vertices = P; mesh.normals = N; mesh.uv = UV; mesh.colors32 = C; mesh.triangles = I;
            mesh.RecalculateTangents(); mesh.RecalculateBounds();
            return mesh;
        }
    }

    /// Lädt kit_index.txt + kit_bin.bytes aus Resources/Kit.
    public static class Kit
    {
        public struct Prim { public string m; public int v, vc, i, ic; }
        static Dictionary<string, Prim[]> pieces;
        static float[] Pos, Nor, UV; static ushort[] Idx;
        static readonly Dictionary<string, Geo> cache = new();

        public static void Load()
        {
            if (pieces != null) return;
            pieces = new();
            foreach (var line in Resources.Load<TextAsset>("Kit/kit_index").text.Split('\n'))
            {
                var s = line.Trim(); if (s.Length == 0) continue; var a = s.Split('|');
                var ps = new List<Prim>();
                foreach (var q in a[1].Split(';')) { var f = q.Split(','); ps.Add(new Prim { m = f[0], v = int.Parse(f[1]), vc = int.Parse(f[2]), i = int.Parse(f[3]), ic = int.Parse(f[4]) }); }
                pieces[a[0]] = ps.ToArray();
            }
            int nv = 0, ni = 0; foreach (var p in pieces.Values) foreach (var q in p) { nv = Mathf.Max(nv, q.v + q.vc); ni = Mathf.Max(ni, q.i + q.ic); }
            byte[] b = Resources.Load<TextAsset>("Kit/kit_bin").bytes;
            Pos = new float[nv * 3]; Nor = new float[nv * 3]; UV = new float[nv * 2]; Idx = new ushort[ni];
            System.Buffer.BlockCopy(b, 0, Pos, 0, nv * 12);
            System.Buffer.BlockCopy(b, nv * 12, Nor, 0, nv * 12);
            System.Buffer.BlockCopy(b, nv * 24, UV, 0, nv * 8);
            System.Buffer.BlockCopy(b, nv * 32, Idx, 0, ni * 2);
        }

        public static bool Has(string name) => pieces.ContainsKey(name);
        public static Prim[] Piece(string name) => pieces[name];

        public static Geo GeoOf(string name, int k)
        {
            string key = name + "#" + k; if (cache.TryGetValue(key, out var g)) return g;
            var p = pieces[name][k]; g = new Geo { P = new Vector3[p.vc], N = new Vector3[p.vc], U = new Vector2[p.vc], I = new int[p.ic] };
            for (int i = 0; i < p.vc; i++)
            {
                int o = p.v + i;
                g.P[i] = new Vector3(Pos[o * 3], Pos[o * 3 + 1], Pos[o * 3 + 2]); g.N[i] = new Vector3(Nor[o * 3], Nor[o * 3 + 1], Nor[o * 3 + 2]);
                g.U[i] = new Vector2(UV[o * 2], 1 - UV[o * 2 + 1]);
            }
            for (int i = 0; i < p.ic; i++) g.I[i] = Idx[p.i + i];
            cache[key] = g; return g;
        }
    }
}
