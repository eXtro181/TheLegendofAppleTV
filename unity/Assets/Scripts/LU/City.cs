// Ludwigshafen-Mitte: Portierung des Stadtgenerators der Browser-Version (gleiche Zahlen, gleiche Zufallsfolge).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LU
{
    public class Rng
    {
        uint s; public Rng(int seed) { s = (uint)seed; }
        /// mulberry32 wie in der Browser-Version
        public float Next() { unchecked { s += 0x6D2B79F5; uint t = s; t = (t ^ (t >> 15)) * (t | 1u); t ^= t + (t ^ (t >> 7)) * (t | 61u); return (t ^ (t >> 14)) / 4294967296f; } }
        public float R(float a, float b) => a + Next() * (b - a);
        public T Pick<T>(IList<T> a) => a[(int)(Next() * a.Count)];
    }

    public struct Box2 { public float x, z, w, d, y0, y1; public bool fence; public bool Inside(float px, float pz) => px >= x && px < x + w && pz >= z && pz < z + d; }
    public class Zone { public string id, name; public float x, z, w, d; }
    public class Yard { public float x, z, w, d; }

    public class City
    {
        public const float WW = 420, WD = 320, WATERX = 412, CELL = 64;
        public static readonly float[] RAILZ = { 284, 292 };
        public const float TRAMX = 198;
        public static readonly Zone[] Zones = {
            new Zone{id="hems",name="Hemshof",x=0,z=0,w=112,d=262}, new Zone{id="hoch",name="Unter der Hochstraße",x=124,z=0,w=82,d=66},
            new Zone{id="bau",name="Baustelle Hochstraße Nord",x=206,z=0,w=80,d=66}, new Zone{id="bism",name="Bismarckstraße",x=124,z=66,w=162,d=84},
            new Zone{id="platz",name="Berliner Platz",x=124,z=164,w=162,d=98}, new Zone{id="bahn",name="S-Bahnhof LU-Mitte",x=0,z=262,w=WW,d=58},
            new Zone{id="gal",name="Rhein-Galerie",x=300,z=0,w=120,d=150}, new Zone{id="park",name="Parkplatz Rhein-Galerie",x=300,z=164,w=120,d=98} };
        public static readonly Box2[] Roads = { new Box2{x=-60,z=150,w=452,d=14}, new Box2{x=112,z=-60,w=12,d=322}, new Box2{x=286,z=-60,w=14,d=322}, new Box2{x=392,z=-60,w=12,d=420} };
        public static readonly int[] PLASTER = { 0xffffff, 0xfff0d8, 0xf8d8c8, 0xffe8a8, 0xe0ecd0, 0xd8e4f0, 0xf0d0d8, 0xfff4e8 };

        public readonly Batcher B = new();
        public readonly List<Box2> Cols = new();
        public readonly List<Yard> Yards = new();
        public readonly List<Vector3> Trees = new();
        public readonly List<Vector3> Fires = new();
        public Rng R = new(7331);
        int kint = 1;

        public static bool OnRoad(float x, float z) { foreach (var r in Roads) if (r.Inside(x, z)) return true; return false; }
        public static float H(float x, float z) => OnRoad(x, z) ? -.15f : 0;
        public static string CellOf(float x, float z) => Mathf.FloorToInt(x / CELL) + "_" + Mathf.FloorToInt(z / CELL);
        public static Zone ZoneAt(float x, float z) { foreach (var zz in Zones) if (x >= zz.x && x < zz.x + zz.w && z >= zz.z && z < zz.z + zz.d) return zz; return null; }

        // Raster für Kollisionsabfragen (12 m): O(1) statt O(n) über alle ~4000 Kästen
        public const float GS = 12;
        readonly Dictionary<int, List<int>> grid = new();
        static readonly List<int> empty = new();
        static int GKey(int gx, int gz) => gx * 4096 + gz + 2048 * 4097;
        public void AddCol(float x, float z, float w, float d, float y1, float y0 = 0, bool fence = false)
        {
            int idx = Cols.Count; Cols.Add(new Box2 { x = x, z = z, w = w, d = d, y0 = y0, y1 = y1, fence = fence });
            for (int gx = Mathf.FloorToInt(x / GS); gx <= Mathf.FloorToInt((x + w) / GS); gx++)
                for (int gz = Mathf.FloorToInt(z / GS); gz <= Mathf.FloorToInt((z + d) / GS); gz++)
                { int k = GKey(gx, gz); if (!grid.TryGetValue(k, out var l)) grid[k] = l = new List<int>(4); l.Add(idx); }
        }
        /// Kasten-Indizes der Rasterzelle von (x,z); Kästen über mehrere Zellen erscheinen in jeder davon.
        public List<int> ColsAt(int gx, int gz) => grid.TryGetValue(GKey(gx, gz), out var l) ? l : empty;
        public readonly List<LodBox> Lods = new();
        public struct LodBox { public float x, z, w, d, h, y0; public string style; public int tint; public string cell; }

        public bool FreeAt(float x, float z, float r)
        {
            if (x < r || z < r || x > WW - r || z > WD - r) return false;
            for (int gx = Mathf.FloorToInt((x - r) / GS); gx <= Mathf.FloorToInt((x + r) / GS); gx++)
                for (int gz = Mathf.FloorToInt((z - r) / GS); gz <= Mathf.FloorToInt((z + r) / GS); gz++)
                    foreach (int i in ColsAt(gx, gz)) { var c = Cols[i]; if (c.y0 > 1.5f) continue; if (x > c.x - r && x < c.x + c.w + r && z > c.z - r && z < c.z + c.d + r) return false; }
            foreach (var rd in Roads) if (x > rd.x - r && x < rd.x + rd.w + r && z > rd.z - r && z < rd.z + rd.d + r) return false;
            if (z > 280.8f && z < 295.6f) return false; if (x > WATERX - 1) return false; return true;
        }

        // ---------- Baukasten ----------
        static readonly Dictionary<string, (string, int)> MERGE = new() { { "MI_RedBrick_Pale", ("MI_RedBrick", 0xf2e4d6) }, { "MI_Trim_Green", ("MI_Trim", 0x6a8a64) }, { "MI_Trim_Dark", ("MI_Trim", 0x505050) } };
        public void Kit(string name, float x, float y, float z, float ry, int col, string cell = null, float sx = 1, float sy = 1, float sz = 1)
        {
            if (!LU.Kit.Has(name)) { Debug.LogWarning("Bauteil fehlt " + name); return; }
            cell ??= CellOf(x, z); var m = M.T(x, y, z, ry, 0, 0, sx, sy, sz); var ps = LU.Kit.Piece(name);
            for (int k = 0; k < ps.Length; k++)
            {
                string mat = ps[k].m; Color c = M.Hex(col);
                if (mat.StartsWith("MI_FakeInterior")) mat = "INT" + (mat == "MI_FakeInterior_4" ? 3 : mat == "MI_FakeInterior" ? kint : (int.Parse(mat.Substring(mat.Length - 1)) % 3 + 1));
                if (MERGE.TryGetValue(mat, out var mg)) { mat = mg.Item1; var t = M.Hex(mg.Item2); c = new Color(c.r * t.r, c.g * t.g, c.b * t.b); }
                B.Add(mat, cell, LU.Kit.GeoOf(name, k), m, c);
            }
        }
        public void KitBox(string mat, float x, float y, float z, float w, float h, float d, int col, string cell = null, float tile = 3)
            => B.Add(mat, cell ?? CellOf(x + w / 2, z + d / 2), Geo.BoxUV(w, h, d, x + w / 2, y + h / 2, z + d / 2, tile, true), M.T(x + w / 2, y + h / 2, z + d / 2), M.Hex(col));
        /// box() der Browser-Version (Eckpunkt + Maße) in einen Stapel
        public void Box(string mat, float x, float y, float z, float w, float h, float d, int col, float tile = 2)
            => B.Add(mat, CellOf(x + w / 2, z + d / 2), Geo.BoxUV(w, h, d, x + w / 2, y + h / 2, z + d / 2, tile, true), M.T(x + w / 2, y + h / 2, z + d / 2), M.Hex(col));
        public void Add(string mat, Geo g, float x, float y, float z, int col, float ry = 0, float rx = 0, float rz = 0, float sx = 1, float sy = 1, float sz = 1)
            => B.Add(mat, CellOf(x, z), g, M.T(x, y, z, ry, rx, rz, sx, sy, sz), M.Hex(col));

        // ---------- Häuser ----------
        class Sty { public string[] w2, w4; public string plain, gw, cor, col; }
        static readonly Dictionary<string, Sty> STY = new() {
            { "brick", new Sty{ w2=new[]{"Brick_Window_Square_Single","Brick_Window_Trim_Single"}, w4=new[]{"Brick_Inset_Window","Brick_Inset_Window","Brick_RedWhite_DoubleWindow"}, plain="Brick_Plain_3", gw="Brick_Window_Square_Single", cor="Cornice_Brick_Center", col="Brick_Column_Small"} },
            { "plaster", new Sty{ w2=new[]{"Trim_Window"}, w4=new string[0], plain="Trim_Plain_3", gw="Trim_Window", cor="Cornice_Trim_Center", col="Trim_Column_Center"} },
            { "office", new Sty{ w2=new[]{"Metal_FullWindow","Metal_Window_Half"}, w4=new[]{"Metal_Window"}, plain="Metal_Plain_3", gw="Metal_FirstFloor_Window", cor="Cornice_Metal_Center", col="Metal_Column_Center"} } };
        static readonly Dictionary<char, (float ry, float nx, float nz, float ux, float uz)> SIDE = new() {
            { 'S', (0, 0, 1, 1, 0) }, { 'N', (Mathf.PI, 0, -1, -1, 0) }, { 'E', (Mathf.PI / 2, 1, 0, 0, -1) }, { 'W', (-Mathf.PI / 2, -1, 0, 0, 1) } };

        public class HouseOpt
        {
            public float x, z, w, d; public int floors = 3; public string style = "plaster"; public int tint = -1; public string roof = "flat";
            public Dictionary<char, string> open; public string shops = ""; public char esc = ' '; public float? gateX0; public bool col = true, collide = true;
            public Dictionary<char, Func<int, int, bool>> skip; public int seed;
        }
        class HB { public Rng rng; public string style, w2, w4, roof, cell; public int floors, tint; }

        void Facade(HB b, char side, float ox, float oz, float L, bool cheap, bool shops, int door, Func<int, int, bool> skip, bool esc)
        {
            var sd = SIDE[side]; var st = STY[b.style]; int n = Mathf.RoundToInt(L / 2), F = b.floors;
            void Put(string name, int i, float w, int f) { float c = i * 2 + w / 2; Kit(name, ox + sd.ux * c, f * 3, oz + sd.uz * c, sd.ry, b.tint, b.cell); }
            skip ??= (_, _) => false;
            for (int f = 0; f < F; f++)
            {
                int i = 0;
                while (i < n)
                {
                    if (skip(i, f)) { i++; continue; }
                    if (f == 0)
                    {
                        bool isDoor = i == door;
                        Put(isDoor ? (b.style == "office" ? "DoorFrame_Metal_Single" : shops ? "DoorFrame_Trim" : "DoorFrame_Wooden") : shops ? (b.style == "office" ? "Metal_FirstFloor_Window" : "Trim_FirstFloor_Window_001") : (cheap ? st.plain : (b.style == "plaster" ? "Trim_FirstFloor_Wall" : st.gw)), i, 2, 0);
                        if (isDoor && !shops && b.style != "office") Put("Door_2", i, 2, 0);
                        i++; continue;
                    }
                    if (cheap) { Put(i % 2 == 1 ? st.w2[0] : st.plain, i, 2, f); i++; continue; }
                    if (b.w4 != null && i + 1 < n && i % 2 == 0 && !skip(i + 1, f)) { Put(b.w4, i, 4, f); i += 2; continue; }
                    Put(b.w2, i, 2, f); i++;
                }
            }
            if (b.roof != "slate") for (int i = 0; i < n; i++) Put(st.cor, i, 2, F);
            else for (int i = 0; i < n; i++) Put(!cheap && b.rng.Next() < .3f ? "Roof_Slate_Window_1" : "Roof_Slate_Center", i, 2, F);
            if (shops) Signs.Add((ox + sd.ux * L / 2 + sd.nx * .12f, 3.35f, oz + sd.uz * L / 2 + sd.nz * .12f, Mathf.Min(L - 1, 6), sd.ry));
            if (esc && b.style == "brick" && F >= 3) FireEscape(ox + sd.ux * (L / 2) + sd.nx * .05f, oz + sd.uz * (L / 2) + sd.nz * .05f, sd.ry, F);
        }
        public readonly List<(float x, float y, float z, float w, float ry)> Signs = new();

        public void House(HouseOpt o)
        {
            var b = new HB { rng = new Rng(o.seed != 0 ? o.seed : (int)(o.x * 73 + o.z * 31 + o.w * 7) + 1), style = o.style, roof = o.roof, floors = o.floors, cell = CellOf(o.x + o.w / 2, o.z + o.d / 2) };
            var st = STY[b.style];
            b.w2 = st.w2[(int)(b.rng.Next() * st.w2.Length)]; b.w4 = st.w4.Length > 0 && b.rng.Next() < .6f ? st.w4[(int)(b.rng.Next() * st.w4.Length)] : null;
            float H = b.floors * 3; kint = 1 + (int)(b.rng.Next() * 3);
            b.tint = o.tint >= 0 ? o.tint : b.style == "plaster" ? PLASTER[(int)(b.rng.Next() * PLASTER.Length)] : b.style == "brick" ? R.Pick(new[] { 0xffffff, 0xf0e0d8, 0xe4d4cc }) : 0xffffff;
            var S = o.open ?? new Dictionary<char, string> { { 'N', "street" }, { 'S', "street" }, { 'E', "street" }, { 'W', "street" } };
            var sides = new Dictionary<char, (float, float, float)> { { 'S', (o.x, o.z + o.d, o.w) }, { 'N', (o.x + o.w, o.z, o.w) }, { 'E', (o.x + o.w, o.z + o.d, o.d) }, { 'W', (o.x, o.z, o.d) } };
            Func<int, int, bool> gS = null, gN = null;
            if (o.gateX0.HasValue) { float g0 = o.gateX0.Value; bool Ovl(float a) => a + 2 > g0 + .01f && a < g0 + 3.99f; gS = (i, f) => f == 0 && Ovl(o.x + 2 * i); gN = (i, f) => f == 0 && Ovl(o.x + o.w - 2 * i - 2); }
            Func<int, int, bool> Both(Func<int, int, bool> a, Func<int, int, bool> c) => a != null && c != null ? (i, f) => a(i, f) || c(i, f) : a ?? c;
            foreach (var k in new[] { 'S', 'N', 'E', 'W' })
            {
                if (!S.TryGetValue(k, out var kind) || kind == "hidden") continue; var (sx, sz, L) = sides[k];
                bool shop = kind == "street" && o.shops.IndexOf(k) >= 0;
                var gk = k == 'S' ? gS : k == 'N' ? gN : null; Func<int, int, bool> os = null; o.skip?.TryGetValue(k, out os);
                Facade(b, k, sx, sz, L, kind == "yard", shop, kind == "street" ? Mathf.FloorToInt(L / 4) : kind == "yard" ? 1 : -1, Both(gk, os), o.esc == k);
            }
            float inset = b.roof == "slate" ? 2 : 0, rh = b.roof == "slate" ? H + 3 : H + .6f;
            KitBox("MI_Asphalt", o.x + inset, rh - .25f, o.z + inset, o.w - inset * 2, .25f, o.d - inset * 2, 0x8a8a8a, b.cell, 4);
            if (b.roof == "slate") foreach (var k in new[] { 'S', 'N', 'E', 'W' }) if (!S.ContainsKey(k) || S[k] == "hidden") { float t = .3f; KitBox("MI_Trim", k == 'E' ? o.x + o.w - t : o.x, H, k == 'S' ? o.z + o.d - t : o.z, k == 'E' || k == 'W' ? t : o.w, 3, k == 'S' || k == 'N' ? t : o.d, b.tint, b.cell, 3); }
            if (o.col && b.style != "office")
                foreach (var (cx, cz, k1, k2) in new[] { (o.x, o.z, 'N', 'W'), (o.x + o.w, o.z, 'N', 'E'), (o.x, o.z + o.d, 'S', 'W'), (o.x + o.w, o.z + o.d, 'S', 'E') })
                    if (S.TryGetValue(k1, out var a1) && a1 == "street" && S.TryGetValue(k2, out var a2) && a2 == "street") for (int f = 0; f < b.floors; f++) Kit(st.col, cx, f * 3, cz, 0, b.tint, b.cell);
            if (b.rng.Next() < .5f) Kit("Prop_ACUnit", o.x + 2 + (o.w - 4) * b.rng.Next(), rh - .05f, o.z + o.d * .5f, (int)(b.rng.Next() * 4) * Mathf.PI / 2, 0xffffff, b.cell);
            if (b.rng.Next() < .5f) KitBox("MI_RedBrick", o.x + 1 + (o.w - 3) * b.rng.Next(), rh - .1f, o.z + 1 + (o.d - 3) * b.rng.Next(), .8f, 1.6f, .8f, 0xffffff, b.cell, 2);
            if (o.gateX0.HasValue)
            {
                float gx0 = o.gateX0.Value, gx1 = gx0 + 4; AddCol(o.x, o.z, gx0 - o.x, o.d, rh); AddCol(gx1, o.z, o.x + o.w - gx1, o.d, rh); AddCol(gx0, o.z, 4, o.d, rh, 3);
                KitBox("MI_Trim", gx0, 0, o.z, .2f, 3, o.d, b.tint, b.cell, 3); KitBox("MI_Trim", gx1 - .2f, 0, o.z, .2f, 3, o.d, b.tint, b.cell, 3); KitBox("MI_Concrete", gx0, 2.9f, o.z, 4, .12f, o.d, 0xbbbbbb, b.cell, 3);
            }
            else if (o.collide) AddCol(o.x, o.z, o.w, o.d, rh);
            AoRing(o.x, o.z, o.w, o.d);
            // Fernansicht (wie Browser-Version): Quader je Haus, 0,35 m eingerückt
            float hb = b.roof == "slate" ? H + .4f : rh - .3f;
            void Lod(float lx, float lz, float lw, float ld, float lh, float ly0 = 0) => Lods.Add(new LodBox { x = lx, z = lz, w = lw, d = ld, h = lh, y0 = ly0, style = b.style, tint = b.tint, cell = b.cell });
            if (o.gateX0.HasValue) { float g0 = o.gateX0.Value; Lod(o.x, o.z, g0 - o.x, o.d, hb); Lod(g0 + 4, o.z, o.x + o.w - g0 - 4, o.d, hb); Lod(g0, o.z, 4, o.d, hb, 3); }
            else Lod(o.x, o.z, o.w, o.d, hb);
            if (b.roof == "slate") Lod(o.x + 2, o.z + 2, o.w - 4, o.d - 4, rh - .3f);
        }

        public void FireEscape(float cx, float cz, float ry, int F)
        {
            float c = Mathf.Cos(ry), s = Mathf.Sin(ry); const int col = 0x2a2b2c;
            void P(float lx, float y, float lz, float w, float h, float d, float rz = 0) => Add("Iron", Geo.Box(w, h, d), cx + lx * c + lz * s, y, cz - lx * s + lz * c, col, ry, 0, rz);
            for (int f = 1; f < F; f++)
            {
                float y = f * 3; P(0, y, .62f, 2.6f, .06f, 1.2f); P(0, y + .5f, 1.2f, 2.6f, .04f, .04f); P(0, y + 1, 1.2f, 2.6f, .04f, .04f); P(1.3f, y + .5f, .62f, .04f, 1, 1.2f); P(-1.3f, y + .5f, .62f, .04f, 1, 1.2f);
                for (int k = -6; k <= 6; k++) P(k * .2f, y + .5f, 1.2f, .02f, 1, .02f);
                if (f < F - 1) { float L = Mathf.Sqrt(2.2f * 2.2f + 9); P(.1f, y + 1.5f, .62f, L, .05f, .5f, Mathf.Atan2(3, 2.2f)); }
            }
        }

        public void AoRing(float x, float z, float w, float d)
        {
            const float s = 1.8f;
            void P(float len, float cx, float cz, float ry) => B.Add("AO", CellOf(cx, cz), Geo.Plane(len, s, 1, 0, 0), M.T(cx, .022f, cz, ry), Color.white);
            P(w + s * 2, x + w / 2, z + d + s / 2, 0); P(w + s * 2, x + w / 2, z - s / 2, Mathf.PI); P(d, x + w + s / 2, z + d / 2, Mathf.PI / 2); P(d, x - s / 2, z + d / 2, -Mathf.PI / 2);
        }

        // ---------- Blockrandbebauung ----------
        public void Block(float x0, float z0, float x1, float z1, char gate = 'S', string shops = "", float brick = .42f, int fMin = 3, int fMax = 5, float D = 10, bool allShops = false)
        {
            var br = new Rng((int)(x0 * 13 + z0 * 7));
            List<float> Split(float len) { var o = new List<float>(); float rem = len; while (rem > 0) { float w = 2 * Mathf.Round(4 + br.Next() * 3); if (rem - w < 6) w = rem; o.Add(w); rem -= w; } return o; }
            string Sty() => br.Next() < brick ? "brick" : "plaster";
            int Fl() => fMin + (int)(br.Next() * (fMax - fMin + 1));
            float yx0 = x0 + D, yx1 = x1 - D, yz0 = z0 + D, yz1 = z1 - D, mid = (x0 + x1) / 2;
            var rows = new List<HouseOpt>();
            HouseOpt Mk(float hx, float hz, float w, float d, Dictionary<char, string> open, char esc)
            {
                var st = Sty(); var h = new HouseOpt { x = hx, z = hz, w = w, d = d, floors = Fl(), style = st, open = open, roof = st == "plaster" && br.Next() < .45f ? "slate" : "flat", esc = esc };
                foreach (var k in "NSEW") if (open.TryGetValue(k, out var v) && v == "street" && shops.IndexOf(k) >= 0 && (br.Next() < .6f || allShops)) h.shops += k;
                return h;
            }
            float x = x0;
            foreach (var w in Split(x1 - x0))
            {
                var h = Mk(x, z0, w, D, new() { { 'N', "street" }, { 'S', "yard" }, { 'W', x == x0 ? "street" : "hidden" }, { 'E', x + w == x1 ? "street" : "hidden" } }, br.Next() < .5f ? 'S' : ' ');
                float hx = x; h.skip = new() { { 'S', (i, f) => { float a = hx + 2 * i; return a < yx0 - .01f || a + 2 > yx1 + .01f; } } }; rows.Add(h); x += w;
            }
            x = x0;
            foreach (var w in Split(x1 - x0))
            {
                var h = Mk(x, z1 - D, w, D, new() { { 'S', "street" }, { 'N', "yard" }, { 'W', x == x0 ? "street" : "hidden" }, { 'E', x + w == x1 ? "street" : "hidden" } }, br.Next() < .5f ? 'N' : ' ');
                float hx = x, ww = w; h.skip = new() { { 'N', (i, f) => { float a = hx + ww - 2 * i - 2; return a < yx0 - .01f || a + 2 > yx1 + .01f; } } }; rows.Add(h); x += w;
            }
            float z = yz0;
            foreach (var d in Split(yz1 - yz0))
            {
                rows.Add(Mk(x0, z, D, d, new() { { 'W', "street" }, { 'E', "yard" }, { 'N', "hidden" }, { 'S', "hidden" } }, br.Next() < .5f ? 'E' : ' '));
                rows.Add(Mk(x1 - D, z, D, d, new() { { 'E', "street" }, { 'W', "yard" }, { 'N', "hidden" }, { 'S', "hidden" } }, br.Next() < .5f ? 'W' : ' ')); z += d;
            }
            bool InRow(HouseOpt h) => gate == 'S' ? h.z == z1 - D : h.z == z0;
            var gr = rows.Find(h => InRow(h) && h.x <= mid && h.x + h.w > mid && h.w >= 6) ?? rows.Find(h => InRow(h) && h.w >= 6 && h.x >= yx0 && h.x + h.w <= yx1);
            if (gr != null) { gr.gateX0 = Mathf.Clamp(2 * Mathf.Round((mid - 2) / 2), Mathf.Max(gr.x, yx0), Mathf.Min(gr.x + gr.w, yx1) - 4); gr.shops = gr.shops.Replace(gate.ToString(), ""); gr.esc = ' '; }
            foreach (var h in rows) House(h);
            var Y = new Yard { x = yx0, z = yz0, w = yx1 - yx0, d = yz1 - yz0 }; Yards.Add(Y);
            Ground("G_Conc", Y.x, Y.z, Y.w, Y.d, .008f, 4);
            float cx = Y.x + Y.w / 2, cz = Y.z + Y.d / 2;
            for (int i = 0; i < 4; i++) { float bx = Y.x + 1.2f + i * 1.1f, bz = Y.z + .9f; Add("Props", Geo.Box(.75f, 1.15f, .75f), bx, .58f, bz, R.Pick(new[] { 0x2a5a3a, 0x5a5a5a, 0x2a4a8a, 0xc8a03a })); Add("Props", Geo.Box(.8f, .06f, .8f), bx, 1.18f, bz, 0x222222); }
            AddCol(Y.x + .8f, Y.z + .5f, 4.6f, .9f, 1.2f);
            if (br.Next() < .6f) Tree(cx + R.R(-3, 3), cz + R.R(-2, 2), R.R(.7f, .95f));
            if (br.Next() < .55f) FireBarrel(Y.x + Y.w - 2, Y.z + Y.d - 2);
        }

        public void RimRow(float x0, float z0, float x1, float z1, char face, int fMin = 3, int fMax = 5)
        {
            var br = new Rng((int)(x0 * 7 + z0 * 11)); bool alongX = face == 'N' || face == 'S'; float p = alongX ? x0 : z0, end = alongX ? x1 : z1, D = alongX ? z1 - z0 : x1 - x0;
            while (p < end - .5f)
            {
                float w = 2 * Mathf.Round(4 + br.Next() * 3); if (end - p - w < 6) w = end - p;
                var o = new HouseOpt { x = alongX ? p : x0, z = alongX ? z0 : p, w = alongX ? w : D, d = alongX ? D : w, floors = fMin + (int)(br.Next() * (fMax - fMin + 1)), style = br.Next() < .45f ? "brick" : "plaster", open = new() { { face, "street" } } };
                o.roof = o.style == "plaster" && br.Next() < .4f ? "slate" : "flat"; if (br.Next() < .35f) o.shops = face.ToString(); House(o); p += w;
            }
        }

        // ---------- Boden ----------
        public void Ground(string mat, float x, float z, float w, float d, float y, float tile)
        {
            // in Kacheln von höchstens 32 m, damit Zellen und Culling greifen
            for (float a = x; a < x + w - .01f; a += 32) for (float c = z; c < z + d - .01f; c += 32)
                {
                    float ww = Mathf.Min(32, x + w - a), dd = Mathf.Min(32, z + d - c);
                    B.Add(mat, CellOf(a + ww / 2, c + dd / 2), Geo.Plane(ww, dd, tile, a + ww / 2, c + dd / 2), M.T(a + ww / 2, y, c + dd / 2), Color.white);
                }
        }

        // ---------- Straßenmöbel ----------
        public void Tree(float x, float z, float s) { Trees.Add(new Vector3(x, z, s)); AddCol(x - .3f, z - .3f, .6f, .6f, 4); }
        public void FireBarrel(float x, float z) { Add("Rust", Geo.Cylinder(.32f, .32f, .95f, 16, true), x, .47f, z, 0x6a4a38); Fires.Add(new Vector3(x, 1, z)); AddCol(x - .33f, z - .33f, .66f, .66f, 1); }
        public void Lamp(float x, float z, float ry)
        {
            float y0 = H(x, z), c = Mathf.Cos(ry), s = Mathf.Sin(ry);
            Add("Metal", Geo.Cylinder(.08f, .12f, 7, 10, false), x, 3.5f + y0, z, 0x6a6e72); Add("Metal", Geo.Box(1.4f, .08f, .08f), x + .7f * c, 6.95f, z - .7f * s, 0x6a6e72, ry);
            Add("Props", Geo.Box(.6f, .14f, .3f), x + 1.35f * c, 6.85f, z - 1.35f * s, 0x3a3a3a, ry); Add("Lamp", Geo.Box(.5f, .04f, .22f), x + 1.35f * c, 6.77f, z - 1.35f * s, 0xffffff, ry); AddCol(x - .15f, z - .15f, .3f, .3f, 7);
        }
        public void Bench(float x, float z, float ry)
        {
            float c = Mathf.Cos(ry), s = Mathf.Sin(ry);
            void P(float ox, float oy, float oz, float w, float h, float d, int col, string m) => Add(m, Geo.Box(w, h, d), x + ox * c + oz * s, oy, z - ox * s + oz * c, col, ry);
            P(0, .45f, 0, 1.9f, .06f, .45f, 0x8a6a4a, "Props"); P(0, .72f, -.22f, 1.9f, .3f, .05f, 0x8a6a4a, "Props"); P(-.85f, .22f, 0, .06f, .45f, .4f, 0x2a2a2a, "Iron"); P(.85f, .22f, 0, .06f, .45f, .4f, 0x2a2a2a, "Iron");
            if (Mathf.Abs(c) > .5f) AddCol(x - .95f, z - .3f, 1.9f, .6f, .8f); else AddCol(x - .3f, z - .95f, .6f, 1.9f, .8f);
        }
        public void Bin(float x, float z) { Add("Props", Geo.Cylinder(.24f, .21f, .75f, 14, false), x, .42f, z, 0xe0661e); AddCol(x - .25f, z - .25f, .5f, .5f, .85f); }
        public void Dumpster(float x, float z, float ry, int col)
        {
            Add("Props", Geo.Box(1.9f, 1.2f, 1.1f), x, .65f, z, col, ry); Add("Props", Geo.Box(1.95f, .1f, 1.15f), x, 1.3f, z, 0x1a1a1a, ry, .15f);
            if (Mathf.Abs(Mathf.Cos(ry)) > .5f) AddCol(x - 1, z - .6f, 2, 1.2f, 1.3f); else AddCol(x - .6f, z - 1, 1.2f, 2, 1.3f);
        }
        /// Parkendes Auto (vereinfachte Karosserie mit Glas, Rädern und Lichtern)
        public void Car(float x, float z, float ry, int col)
        {
            float y = H(x, z), c = Mathf.Cos(ry), s = Mathf.Sin(ry);
            void P(string m, Geo g, float ox, float oy, float oz, int cl, float rx = 0, float rz = 0) => Add(m, g, x + ox * c + oz * s, y + oy, z - ox * s + oz * c, cl, ry, rx, rz);
            P("Paint", Geo.Box(4.4f, .62f, 1.78f), 0, .62f, 0, col); P("Paint", Geo.Box(2.3f, .55f, 1.6f), -.25f, 1.2f, 0, col); P("Glass", Geo.Box(2.15f, .46f, 1.64f), -.25f, 1.2f, 0, 0xffffff);
            P("Props", Geo.Box(4.46f, .18f, 1.82f), 0, .38f, 0, 0x1c1c1e);
            foreach (var wx in new[] { 1.36f, -1.36f }) foreach (var wz in new[] { .78f, -.78f }) P("Tire", Geo.Cylinder(.34f, .34f, .24f, 16, false), wx, .34f, wz, 0x151515, Mathf.PI / 2);
            foreach (var wz in new[] { .6f, -.6f }) { P("Lamp", Geo.Box(.06f, .14f, .32f), 2.2f, .78f, wz, 0xffffff); P("Tail", Geo.Box(.06f, .14f, .3f), -2.2f, .82f, wz, 0xffffff); }
            if (Mathf.Abs(c) > .5f) AddCol(x - 2.3f, z - 1, 4.6f, 2, 1.5f); else AddCol(x - 1, z - 2.3f, 2, 4.6f, 1.5f);
        }
        public void Fence(float x1, float z1, float x2, float z2, float h)
        {
            float L = Mathf.Sqrt((x2 - x1) * (x2 - x1) + (z2 - z1) * (z2 - z1)), ry = -Mathf.Atan2(z2 - z1, x2 - x1), cx = (x1 + x2) / 2, cz = (z1 + z2) / 2;
            var g = new Geo { P = new[] { new Vector3(-L / 2, -h / 2, 0), new Vector3(L / 2, -h / 2, 0), new Vector3(L / 2, h / 2, 0), new Vector3(-L / 2, h / 2, 0) }, N = new[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward }, U = new[] { new Vector2(0, 0), new Vector2(L / 1.2f, 0), new Vector2(L / 1.2f, h / 1.2f), new Vector2(0, h / 1.2f) }, I = new[] { 0, 1, 2, 0, 2, 3, 0, 2, 1, 0, 3, 2 } };
            B.Add("Fence", CellOf(cx, cz), g, M.T(cx, h / 2, cz, ry), Color.white);
            int n = Mathf.Max(1, Mathf.RoundToInt(L / 3)); for (int i = 0; i <= n; i++) { float t = (float)i / n; Add("Metal", Geo.Cylinder(.04f, .04f, h + .1f, 6, false), Mathf.Lerp(x1, x2, t), (h + .1f) / 2, Mathf.Lerp(z1, z2, t), 0x9a9c9e); }
            const float th = .15f; if (Mathf.Abs(x2 - x1) > Mathf.Abs(z2 - z1)) AddCol(Mathf.Min(x1, x2), cz - th, Mathf.Abs(x2 - x1), th * 2, h, 0, true); else AddCol(cx - th, Mathf.Min(z1, z2), th * 2, Mathf.Abs(z2 - z1), h, 0, true);
        }
        public void Container(float x, float z, float ry, int color, int stack = 1)
        {
            bool along = Mathf.Abs(Mathf.Cos(ry)) > .5f; float w = along ? 12.2f : 2.44f, d = along ? 2.44f : 12.2f;
            for (int k = 0; k < stack; k++) Box("Corr", x, k * 2.6f, z, w, 2.6f, d, k == 0 ? color : R.Pick(new[] { 0x8e3b2c, 0x2f5577, 0x3e6b45, 0xc07a2a, 0x7a7f86 }), 2);
            AddCol(x, z, w, d, stack * 2.6f); AoRing(x, z, w, d);
        }
        public void Truck(float x, float z, float ry, int col)
        {
            float c = Mathf.Cos(ry), s = Mathf.Sin(ry);
            void P(string m, float ox, float oy, float oz, float w, float h, float d, int cl) => Add(m, Geo.Box(w, h, d), x + ox * c + oz * s, oy, z - ox * s + oz * c, cl, ry);
            P("Paint", 2.6f, 1.55f, 0, 1.9f, 2, 2.4f, col); P("Glass", 3.35f, 2, 0, .4f, .8f, 2.2f, 0xffffff); P("Paint", -1.2f, 1.3f, 0, 5.4f, 1.4f, 2.45f, col); P("Props", -1.2f, .55f, 0, 6.8f, .3f, 2.3f, 0x2a2a2a);
            foreach (var ox in new[] { 2.6f, -.4f, -2.4f }) foreach (var oz in new[] { -1.1f, 1.1f }) Add("Tire", Geo.Cylinder(.5f, .5f, .4f, 16, false), x + ox * c + oz * s, .5f, z - ox * s + oz * c, 0x151515, ry, Mathf.PI / 2);
            if (Mathf.Abs(c) > .5f) AddCol(x - 4.4f, z - 1.3f, 8.8f, 2.6f, 2.8f); else AddCol(x - 1.3f, z - 4.4f, 2.6f, 8.8f, 2.8f);
        }
        public void Crane(float x, float z, float h)
        {
            const int Y = 0xe2b524;
            for (float y = 0; y < h; y += 3)
            {
                foreach (var (a, bb) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) }) Add("Paint", Geo.Box(.14f, 3, .14f), x + a * .9f, y + 1.5f, z + bb * .9f, Y);
                Add("Paint", Geo.Box(1.9f, .1f, .1f), x, y + .1f, z - .9f, Y); Add("Paint", Geo.Box(1.9f, .1f, .1f), x, y + .1f, z + .9f, Y); Add("Paint", Geo.Box(.1f, .1f, 1.9f), x - .9f, y + .1f, z, Y); Add("Paint", Geo.Box(.1f, .1f, 1.9f), x + .9f, y + .1f, z, Y);
                Add("Paint", Geo.Box(.08f, 3.3f, .08f), x, y + 1.5f, z - .9f, Y, 0, 0, .56f); Add("Paint", Geo.Box(.08f, 3.3f, .08f), x, y + 1.5f, z + .9f, Y, 0, 0, -.56f);
            }
            Add("Paint", Geo.Box(46, 1.2f, 1.4f), x - 12, h + .6f, z, Y); Add("Props", Geo.Box(3, 2, 2.2f), x, h - 1, z + 1.6f, 0xd8d8d0); Add("Conc", Geo.Box(4, 3, 2.4f), x + 9, h - 1, z, 0x9a9a92);
            Add("Iron", Geo.Cylinder(.02f, .02f, h - 6, 4, true), x - 30, h / 2 + 2, z, 0x222222); Add("Conc", Geo.Box(5, 1.2f, 5), x, .6f, z, 0xb8b4aa); AddCol(x - 2.5f, z - 2.5f, 5, 5, h);
        }
        public void Scaffold(float x0, float x1, float z, float h)
        {
            for (float x = x0; x <= x1 + .01f; x += 2.5f) foreach (var dz in new[] { 0, 1.2f }) Add("Metal", Geo.Cylinder(.03f, .03f, h, 5, true), x, h / 2, z + dz, 0x9a9c9e);
            for (float y = 2; y <= h; y += 2) { Add("Metal", Geo.Cylinder(.025f, .025f, x1 - x0, 5, true), (x0 + x1) / 2, y, z, 0x9a9c9e, 0, 0, Mathf.PI / 2); Add("Metal", Geo.Cylinder(.025f, .025f, x1 - x0, 5, true), (x0 + x1) / 2, y, z + 1.2f, 0x9a9c9e, 0, 0, Mathf.PI / 2); Box("Wood", x0, y - .04f, z, x1 - x0, .05f, 1.2f, 0xc8a878, 1); }
            AddCol(x0 - .1f, z - .1f, x1 - x0 + .2f, 1.4f, h);
        }
        public void Jersey(float x, float z, float ry) { Add("Conc", Geo.Box(3, .35f, .62f), x, .17f, z, 0xd2cec4, ry); Add("Conc", Geo.Box(3, .5f, .3f), x, .6f, z, 0xd2cec4, ry); if (Mathf.Abs(Mathf.Cos(ry)) > .5f) AddCol(x - 1.5f, z - .31f, 3, .62f, .85f); else AddCol(x - .31f, z - 1.5f, .62f, 3, .85f); }
    }
}
