// Layout der Karte (Portierung von buildGround/buildCity der Browser-Version).
using UnityEngine;

namespace LU
{
    public partial class CityLayout
    {
        readonly City c; Rng R => c.R;
        public CityLayout(City city) { c = city; }
        static readonly int[] CC = { 0x7d8a96, 0x8f1f18, 0x22324e, 0xd8d6d0, 0x2f4a32, 0x9a8a6a, 0x18181c, 0xb9b6ad, 0x5a6670, 0x6b2a3a, 0xe8e6e0, 0x3a3d42 };
        public static readonly (float a, float b)[] INTERS = { (112, 124), (286, 300), (392, 404) };
        public static readonly (string id, string name, float x, float z)[] CPDEF = {
            ("hof","Hemshof-Gasse",22,102.4f), ("bism","Bismarckstraße",190.6f,108), ("platz","Kiosk am Berliner Platz",219.6f,229), ("bau","Bauzaun",236.5f,69.4f),
            ("hoch","Unter der Brücke",165,60), ("bahn","Bahnsteig LU-Mitte",178,277.4f), ("gal","Galerie-Vorplatz",305.4f,99), ("park","Parkplatz",318,168.6f) };
        static bool NearCP(float x, float z, float r) { foreach (var p in CPDEF) if ((p.x - x) * (p.x - x) + (p.z - z) * (p.z - z) < r * r) return true; return false; }

        public void BuildGround()
        {
            float[] XS = { 0, 112, 124, 286, 300, 392, 404, City.WATERX }, ZS = { 0, 150, 164, 262, City.WD };
            for (int i = 0; i < XS.Length - 1; i++) for (int j = 0; j < ZS.Length - 1; j++)
                {
                    float x = XS[i], z = ZS[j], w = XS[i + 1] - x, d = ZS[j + 1] - z;
                    if (City.OnRoad(x + w / 2, z + d / 2)) c.Ground("G_Asph", x, z, w, d, -.15f, 7); else c.Ground("G_Pav", x, z, w, d, 0, 3);
                }
            foreach (var r in City.Roads) { if (r.z < 0 && r.w < 20) c.Ground("G_Asph", r.x, -60, r.w, 60, .01f, 7); if (r.x < 0) c.Ground("G_Asph", -60, r.z, 60, r.d, .01f, 7); }
            c.Ground("G_Out", -600, -600, 1600, 600, -.02f, 9); c.Ground("G_Out", -600, City.WD, 1600, 600, -.02f, 9); c.Ground("G_Out", -600, 0, 600, City.WD, -.02f, 9);
            void Ov(float x, float z, float w, float d, string m, float tile, float y = .006f) => c.Ground(m, x, z, w, d, y, tile);
            foreach (var (x, z, w, d) in new[] { (52f, 0f, 8f, 150f), (52, 164, 8, 98), (0, 44, 112, 8), (0, 96, 112, 8), (0, 210, 112, 8) }) Ov(x, z, w, d, "G_Asph", 7, .005f);
            Ov(124, 0, 82, 66, "G_Asph", 7); Ov(206, 0, 80, 66, "G_Dirt", 6, .007f);
            Ov(186, 66, 24, 84, "G_Plaza", 3.2f); Ov(186, 0, 20, 66, "G_Plaza", 3.2f, .009f); Ov(124, 164, 162, 98, "G_Plaza", 3.2f);
            Ov(300, 0, 12, 150, "G_Plaza", 3.2f); Ov(312, 0, 80, 40, "G_Asph", 7); Ov(300, 164, 92, 98, "G_Asph", 7); Ov(404, -40, 8, City.WD + 80, "G_Plaza", 3.2f);
            Ov(124, 262, 176, 10, "G_Plaza", 3.2f); Ov(130, 272, 160, 8.6f, "G_Conc", 4, .12f); Ov(-60, 280.6f, City.WATERX + 60, 15.2f, "G_Grav", 4, .004f);
            foreach (var (x, z, w, d) in new[] { (0f, 262f, 124f, 18.6f), (300, 262, 112, 18.6f), (0, 295.8f, City.WATERX, 24.2f) }) Ov(x, z, w, d, "G_Dirt", 6, .005f);
            // Bordsteine
            foreach (var r in City.Roads)
            {
                bool ax = r.w > r.d;
                if (ax) foreach (var zz in new[] { r.z, r.z + r.d }) { float? s = null; float end = Mathf.Min(City.WATERX, r.x + r.w); for (float x = Mathf.Max(0, r.x); x <= end; x += 1) { bool o = City.OnRoad(x + .5f, zz + (zz == r.z ? -1 : 1)); if (!o && x < end) { s ??= x; } else if (s != null) { c.Box("Conc", s.Value, -.15f, zz - .11f, x - s.Value, .17f, .22f, 0xc8c2b6); s = null; } } }
                else foreach (var xx in new[] { r.x, r.x + r.w }) { float? s = null; float end = Mathf.Min(262, r.z + r.d); for (float z = Mathf.Max(0, r.z); z <= end; z += 1) { bool o = City.OnRoad(xx + (xx == r.x ? -1 : 1), z + .5f); if (!o && z < end) { s ??= z; } else if (s != null) { c.Box("Conc", xx - .11f, -.15f, s.Value, .22f, .17f, z - s.Value, 0xc8c2b6); s = null; } } }
            }
            // Markierungen
            void Mark(float x, float z, float w, float d, int col = 0xe2ddcf) => c.Box("Mark", x, -.135f, z, w, .012f, d, col);
            for (float x = -50; x < 392; x += 7) { if (City.OnRoad(x + 1.5f, 140) || City.OnRoad(x + 1.5f, 170)) continue; Mark(x, 153.4f, 3.2f, .15f); Mark(x, 160.45f, 3.2f, .15f); }
            Mark(-50, 156.85f, 442, .12f, 0xe8d23a); Mark(-50, 157.03f, 442, .12f, 0xe8d23a);
            foreach (var r in City.Roads) { if (r.w > r.d) continue; float cx = r.x + r.w / 2; for (float z = -50; z < 262; z += 7) { if (z > 146 && z < 166) continue; Mark(cx - .09f, z, .18f, 3.2f); } }
            void Zebra(float x0, float x1, float z0, float z1, bool alongX) { if (alongX) for (float x = x0; x < x1; x += 1.05f) Mark(x, z0, .55f, z1 - z0, 0xeeeae0); else for (float z = z0; z < z1; z += 1.05f) Mark(x0, z, x1 - x0, .55f, 0xeeeae0); }
            Zebra(188, 208, 150.6f, 163.4f, true);
            foreach (var (a, b) in INTERS) { Zebra(a + .5f, b - .5f, 145.5f, 149.5f, true); Zebra(a + .5f, b - .5f, 164.5f, 168.5f, true); Zebra(a - 4.5f, a - .5f, 150.5f, 163.5f, false); Zebra(b + .5f, b + 4.5f, 150.5f, 163.5f, false); }
            // Gleise
            foreach (var dx in new[] { -.72f, .72f }) c.Box("Metal", City.TRAMX + dx - .04f, -.01f, -60, .08f, .04f, 322, 0x9a9c9e);
            c.Box("Conc", City.TRAMX - 1.4f, 0, 0, 2.8f, .012f, 262, 0x8a8680);
            foreach (var zc in City.RAILZ) { foreach (var dz in new[] { -.72f, .72f }) c.Box("Metal", -60, .14f, zc + dz - .04f, City.WATERX + 120, .1f, .08f, 0x9a9c9e); for (float x = -58; x < City.WATERX + 58; x += .75f) c.Add("Wood", Geo.Box(.22f, .12f, 2.5f), x, .08f, zc, 0x6a5a4a); }
            // Rhein
            c.Ground("Water", City.WATERX, -600, 600, 1500, -1.3f, 14);
            c.Box("Conc", City.WATERX - 1.4f, -2, -60, 1.4f, 2.3f, City.WD + 120, 0xbcb7ab);
            for (float z = -56; z < City.WD + 60; z += 2.4f) c.Add("Metal", Geo.Cylinder(.03f, .03f, 1, 6, true), City.WATERX - .3f, .8f, z, 0x9a9c9e);
            c.Box("Metal", City.WATERX - .34f, 1.25f, -60, .08f, .08f, City.WD + 120, 0x9a9c9e); c.AddCol(City.WATERX - 1, -60, 40, City.WD + 120, 3);
        }

        public void BuildCity()
        {
            // Hemshof
            (float, float)[] XB = { (4, 52), (60, 108) }, ZN = { (4, 44), (52, 96), (104, 146) }, ZSb = { (168, 210), (218, 258) };
            for (int j = 0; j < ZN.Length; j++) for (int i = 0; i < XB.Length; i++)
                    c.Block(XB[i].Item1, ZN[j].Item1, XB[i].Item2, ZN[j].Item2, (i + j) % 2 == 1 ? 'N' : 'S', (j == 2 ? "S" : "") + (i == 1 ? "E" : "") + ((i + j) % 2 == 1 ? "W" : ""));
            for (int j = 0; j < ZSb.Length; j++) for (int i = 0; i < XB.Length; i++)
                    c.Block(XB[i].Item1, ZSb[j].Item1, XB[i].Item2, ZSb[j].Item2, (i + j) % 2 == 1 ? 'S' : 'N', (j == 0 ? "N" : "") + (i == 1 ? "E" : ""), .5f);
            c.RimRow(-10, 0, 0, 150, 'E'); c.RimRow(-10, 164, 0, 262, 'E'); c.RimRow(0, -10, 112, 0, 'S'); c.RimRow(124, -10, 206, 0, 'S', 4, 6); c.RimRow(300, -10, 392, 0, 'S', 5, 6);
            foreach (var (z, x0, x1) in new[] { (44f, 4f, 108f), (96, 4, 108), (210, 4, 108) })
                for (float x = x0 + 3; x < x1 - 3; x += 6) { if (Mathf.Abs(x - 56) < 6) continue; float r = R.Next(); if (NearCP(x, z + 3, 7)) continue; if (r < .3f) c.Car(x, z + 1.25f, R.Next() < .5f ? 0 : Mathf.PI, R.Pick(CC)); else if (r < .42f) c.Dumpster(x, z + 7.1f, 0, R.Pick(new[] { 0x2f5a3a, 0x5a5a5a, 0x8a3a2a })); }
            foreach (var (x, z0, z1) in new[] { (52f, 0f, 150f), (52, 164, 262) })
                for (float z = z0 + 4; z < z1 - 4; z += 6) { bool skip = false; foreach (var a in new[] { 44f, 96, 210 }) if (z > a - 4 && z < a + 12) skip = true; if (skip) continue; float r = R.Next(); if (NearCP(x + 3, z, 7)) continue; if (r < .25f) c.Car(x + 1.25f, z, R.Next() < .5f ? Mathf.PI / 2 : -Mathf.PI / 2, R.Pick(CC)); else if (r < .37f) c.Dumpster(x + 7.1f, z, Mathf.PI / 2, R.Pick(new[] { 0x2f5a3a, 0x5a5a5a })); }
            // Bismarckstraße
            c.Block(124, 70, 186, 104, 'N', "EWS", .35f, 4, 5, 12, true); c.Block(124, 112, 186, 146, 'N', "EWNS", .35f, 4, 5, 12, true);
            c.Block(210, 70, 286, 104, 'N', "WES", .4f, 4, 6, 12, true); c.Block(210, 112, 286, 146, 'N', "WNSE", .4f, 4, 6, 12, true);
            for (float z = 72; z < 148; z += 12) { if (z > 100 && z < 114) continue; c.Tree(188.5f, z + 3, .9f); c.Tree(207.5f, z + 9, .9f); c.Kit("Sidewalk_Planter", 188.5f, 0, z + 3, 0, 0xffffff); c.Kit("Sidewalk_Planter", 207.5f, 0, z + 9, 0, 0xffffff); }
            for (float z = 74; z < 148; z += 16) { c.Bench(191.5f, z, Mathf.PI / 2); c.Bench(204.5f, z + 6, -Mathf.PI / 2); c.Bin(192, z + 4); }
            for (float z = 68; z < 150; z += 3.2f) { c.Kit("Prop_Bollard", 186.6f, 0, z, 0, 0xffffff); c.Kit("Prop_Bollard", 209.4f, 0, z, 0, 0xffffff); }
            for (float z = 78; z < 148; z += 24) { c.Lamp(193, z, 0); c.Lamp(203, z + 12, Mathf.PI); }
            // Hochstraße Nord und Baustelle
            Hochstrasse();
            for (float x = 128; x < 204; x += 5.4f) { if (Mathf.Abs(x - City.TRAMX) < 4) continue; if (R.Next() < .55f) c.Car(x, 30, Mathf.PI / 2, R.Pick(CC)); if (R.Next() < .45f) c.Car(x, 45, -Mathf.PI / 2, R.Pick(CC)); }
            foreach (var (x, z) in new[] { (140f, 56f), (170, 60), (150, 4) }) c.FireBarrel(x, z);
            foreach (var (a, b) in new[] { (206f, 240f), (254, 286) }) c.Fence(a, 66, b, 66, 2.3f);
            c.Fence(206, 4, 206, 66, 2.3f); c.Fence(286, 4, 286, 66, 2.3f); c.Fence(206, 4, 286, 4, 2.3f);
            c.Crane(266, 40, 44);
            c.Container(214, 48, 0, 0xe8e6e0, 2); c.Container(214, 56, 0, 0xe8e6e0); c.Container(228, 14, Mathf.PI / 2, 0x8e3b2c);
            c.Truck(250, 48, 0, 0xe07020); c.Truck(272, 58, Mathf.PI, 0xe07020);
            for (int i = 0; i < 6; i++) { float x = 232 + i * 5, z = 30 + (i % 2) * 6; c.Add("Wood", Geo.Cylinder(.9f, .9f, .9f, 18, false), x, .9f, z, 0x8a5a32, 0, Mathf.PI / 2); c.Add("Paint", Geo.Cylinder(.6f, .6f, .95f, 18, false), x, .9f, z, 0xc06a3a, 0, Mathf.PI / 2); c.AddCol(x - .5f, z - .9f, 1, 1.8f, 1.8f); }
            for (int k = 0; k < 30; k++) { float x = R.R(212, 282), z = R.R(8, 62); if (!c.FreeAt(x, z, 1.3f)) continue; float s = R.R(.4f, 1.2f); c.Add("Conc", Geo.Box(s, s * .8f, s), x, .25f, z, 0xb8b2a6, R.Next() * 3, R.Next() * 3); }
            foreach (var (x, z, ry) in new[] { (242f, 62f, 0f), (230, 40, Mathf.PI / 2), (276, 20, 0) }) c.Jersey(x, z, ry);
            for (float x = 210; x < 284; x += 1.4f) if (R.Next() < .85f) c.Add("Paint", Geo.Box(1.2f, .25f, .06f), x, 1.0f, 65.6f, R.Next() < .5f ? 0xc4231c : 0xf2f2ee);
            // Berliner Platz
            c.House(new City.HouseOpt { x = 216, z = 170, w = 64, d = 24, floors = 7, style = "office", shops = "NW" });
            c.House(new City.HouseOpt { x = 136, z = 230, w = 6, d = 6, floors = 10, style = "brick", col = false, open = new() { { 'N', "street" }, { 'S', "hidden" }, { 'E', "street" }, { 'W', "street" } } });
            c.House(new City.HouseOpt { x = 152, z = 230, w = 6, d = 6, floors = 10, style = "brick", col = false, open = new() { { 'N', "street" }, { 'S', "hidden" }, { 'E', "street" }, { 'W', "street" } } });
            c.House(new City.HouseOpt { x = 138, z = 236, w = 18, d = 22, floors = 5, style = "brick", roof = "slate", col = false });
            foreach (var x in new[] { 139f, 155 }) c.Add("Slate", Geo.Cylinder(0, 4.5f, 9, 4, false), x, 35.1f, 233, 0x6a6e72, Mathf.PI / 4);
            foreach (var px in new[] { 194.6f, 201.4f })
            {
                c.Box("Conc", px - 1.6f, 0, 176, 3.2f, .22f, 58, 0xcfcac0); float side = px < 198 ? -1 : 1;
                for (float z = 178; z < 234; z += 8) { c.Add("Metal", Geo.Cylinder(.07f, .07f, 3.3f, 8, false), px + side, 1.65f, z, 0x9a9c9e); c.AddCol(px + side - .1f, z - .1f, .2f, .2f, 3.3f); }
                c.Box("Iron", px - 1.9f, 3.3f, 176, 3.8f, .12f, 58, 0x3a3e42); c.Box("Glass", px - 1.8f, 3.38f, 176.2f, 3.6f, .04f, 57.6f, 0xffffff);
                c.Bench(px - side * -.6f, 190, px < 198 ? Mathf.PI / 2 : -Mathf.PI / 2); c.Bench(px - side * -.6f, 214, px < 198 ? Mathf.PI / 2 : -Mathf.PI / 2);
            }
            c.Box("Props", 213, 0, 222, 4.4f, 2.9f, 3.6f, 0xe8e2d4); c.Box("Iron", 212.8f, 2.9f, 221.8f, 4.8f, .18f, 4, 0x2e3236); c.Box("Glass", 213.5f, 1, 225.62f, 3.4f, 1.2f, .06f, 0xffffff); c.AddCol(213, 222, 4.4f, 3.6f, 3);
            c.Fence(134, 172, 180, 172, 2.2f); c.Fence(134, 220, 180, 220, 2.2f); c.Fence(134, 172, 134, 220, 2.2f); c.Fence(180, 172, 180, 220, 2.2f); c.AddCol(134, 172, 46, 48, 2.2f, 0, true);
            c.Ground("G_Dirt", 134, 172, 46, 48, .012f, 5);
            for (int k = 0; k < 40; k++) { float s = R.R(.5f, 1.6f); c.Add("Conc", Geo.Box(s, s * .7f, s), R.R(138, 176), .2f, R.R(176, 216), R.Pick(new[] { 0xb8b2a6, 0xa8a296, 0xc8c0b0 }), R.Next() * 3, R.Next() * 3); }
            foreach (var (x, z) in new[] { (140f, 178f), (150, 178), (170, 214) }) c.Add("Paint", Geo.Box(2.6f, 1.4f, 1.4f), x, .7f, z, 0x2a5aa8);
            foreach (var (x, z) in new[] { (132f, 166f), (184, 168), (184, 244), (216, 246), (244, 252), (282, 206), (282, 236), (126, 200), (166, 226) }) c.Tree(x, z, R.R(.85f, 1.25f));
            foreach (var (x, z, ry) in new[] { (170f, 236f, 0f), (176, 236, 0), (230, 206, Mathf.PI), (250, 206, Mathf.PI), (266, 226, Mathf.PI / 2) }) c.Bench(x, z, ry);
            foreach (var (x, z) in new[] { (130f, 176f), (186, 240), (250, 214), (280, 254), (226, 248) }) c.Lamp(x, z, 0);
            foreach (var (x, z) in new[] { (183f, 180f), (211, 236), (260, 212), (136, 226) }) c.Bin(x, z);
            // S-Bahnhof
            c.Box("Conc", 130, 0, 272, 160, .12f, 8.6f, 0xd0ccc4); c.Box("Mark", 130, .125f, 279.25f, 160, .012f, .25f, 0xf2d23a);
            for (float x = 136; x < 288; x += 12) { c.Add("Metal", Geo.Cylinder(.1f, .1f, 4, 8, false), x, 2.1f, 275.5f, 0x9a9c9e); c.AddCol(x - .12f, 275.4f, .24f, .24f, 4); }
            c.Box("Iron", 132, 4.1f, 273, 156, .2f, 6, 0x3a3e42); c.Box("Glass", 132.5f, 4.3f, 273.2f, 155, .05f, 5.6f, 0xffffff);
            foreach (var x in new[] { 150f, 190, 240, 276 }) c.Bench(x, 273.6f, 0);
            c.House(new City.HouseOpt { x = 126, z = 263, w = 16, d = 8, floors = 2, style = "brick", shops = "N" });
            foreach (var (a, b) in new[] { (-60f, 130f), (290, City.WATERX) }) c.Fence(a, 280.4f, b, 280.4f, 1.8f);
            c.Fence(-60, 296.4f, City.WATERX, 296.4f, 1.8f); c.AddCol(-60, 296.3f, City.WATERX + 60, .3f, 2);
            for (float x = -40; x < City.WATERX; x += 30) { c.Add("Metal", Geo.Cylinder(.12f, .15f, 7.5f, 8, false), x, 3.75f, 288, 0x9a9c9e); c.Add("Metal", Geo.Box(.1f, .1f, 12), x, 7.2f, 288, 0x9a9c9e); }
            c.Box("Conc", -60, 0, 297, City.WATERX + 60, 3.2f, .5f, 0xb0aca2);
            // Rhein-Galerie
            const int WH = 0xf2f2ee;
            c.AddCol(312, 40, 76, 44, 14); c.AddCol(312, 102, 76, 44, 14); c.AddCol(324, 84, 64, 18, 14);
            foreach (var (z0, z1) in new[] { (40f, 84f), (102f, 146f) })
            {
                c.Box("Glass", 312, 0, z0, .1f, 12, z1 - z0, 0xffffff); c.Box("Props", 312.2f, 0, z0, .4f, 12, z1 - z0, 0x9a9a96);
                for (float z = z0; z <= z1; z += 6) c.Box("Props", 311.8f, 0, z - .15f, .4f, 12.6f, .3f, WH);
                foreach (var y in new[] { 4f, 8f }) c.Box("Props", 311.85f, y, z0, .3f, .15f, z1 - z0, WH);
            }
            c.Box("Props", 312, 12, 40, 76, 2.2f, 106, WH); c.Box("Props", 388, 0, 40, .4f, 12, 106, 0xe2e2de); c.Box("Props", 312, 0, 40, 76, 12, .4f, 0xe2e2de); c.Box("Props", 312, 0, 145.6f, 76, 12, .4f, 0xe2e2de);
            for (int k = 0; k < 5; k++)
            {
                float z0 = 40 + k * 21.2f; var g = Geo.Cylinder(10.6f, 10.6f, 76, 28, true, 0, Mathf.PI);
                c.B.Add("Props", City.CellOf(350, z0 + 10.6f), g, Matrix4x4.Translate(new Vector3(350, 14.2f, z0 + 10.6f)) * ScaleY(.42f) * M.T(0, 0, 0, 0, 0, Mathf.PI / 2), M.Hex(WH));
            }
            for (float x = 316; x < 388; x += 8) { c.Box("Props", x - .3f, 0, 145.7f, .6f, 12.2f, .6f, 0xe8e8e4); c.Box("Props", x - .3f, 0, 39.7f, .6f, 12.2f, .6f, 0xe8e8e4); }
            for (float z = 44; z < 146; z += 8) c.Box("Props", 388.1f, 0, z - .3f, .6f, 12.2f, .6f, 0xe8e8e4);
            c.Box("Glass", 312, 9.3f, 145.4f, 76, 2.2f, .6f, 0xffffff); c.Box("Glass", 312, 9.3f, 39.8f, 76, 2.2f, .6f, 0xffffff);
            c.Box("Props", 312, 4.2f, 145.9f, 76, .35f, .5f, 0x1d3f7a); c.Box("Props", 312, 4.2f, 39.6f, 76, .35f, .5f, 0x1d3f7a);
            // Eingangshalle mit Apple Store
            c.Box("Props", 312, 8, 84, 12, .3f, 18, WH); c.Box("Props", 312, 0, 83.7f, 12, 8, .3f, WH); c.Box("Props", 312, 0, 102, 12, 8, .3f, WH); c.AddCol(312, 83.6f, 12, .4f, 8); c.AddCol(312, 102, 12, .4f, 8);
            c.Box("Props", 323.7f, 0, 84, .3f, 8, 18, 0xf6f6f4); c.Box("Glass", 323.5f, .1f, 88, .06f, 5, 10, 0xffffff); c.Ground("G_Floor", 312, 84, 12, 18, .02f, 4);
            for (int i = 0; i < 3; i++) c.Box("Wood", 315 + i * 3, 0, 92, 1.6f, .9f, 2, 0xe8c8a0);
            foreach (var (x, z) in new[] { (304f, 60f), (304, 126), (304, 30), (306, 140) }) c.Tree(x, z, .95f);
            foreach (var (x, z) in new[] { (305f, 72f), (305, 114) }) c.Bench(x, z, Mathf.PI / 2);
            c.Truck(340, 22, 0, 0xd8d8d0); c.Truck(372, 30, Mathf.PI, 0x2a4a7a); foreach (var (x, z) in new[] { (326f, 14f), (356, 10) }) c.Container(x, z, 0, 0x7a7f86);
            // Parkplatz
            for (float x = 304; x < 390; x += 2.8f) foreach (var z in new[] { 170f, 186, 206, 222, 242, 256 }) { c.Box("Mark", x - .06f, .01f, z - 2.4f, .12f, .012f, 4.8f, 0xe8e4d8); if (R.Next() < .3f) c.Car(x + 1.4f, z, (z == 186 || z == 222 || z == 256) ? Mathf.PI / 2 : -Mathf.PI / 2, R.Pick(CC)); }
            foreach (var (x, z) in new[] { (306f, 196f), (306, 232), (388, 196), (388, 232), (346, 196), (346, 232) }) c.Lamp(x, z, 0);
            foreach (var (x, z) in new[] { (302f, 180f), (302, 216), (302, 250), (390, 180), (390, 216), (390, 250) }) c.Tree(x, z, 1.05f);
            // Promenade
            for (float z = -30; z < City.WD + 30; z += 14) { c.Tree(407.5f, z, 1.15f); if (z > 0 && z < City.WD) c.Bench(410.2f, z + 7, -Mathf.PI / 2); }
            // Straßenraum
            for (float x = 4; x < 392; x += 26) { bool sk = Mathf.Abs(x - 198) < 14; foreach (var (a, b) in INTERS) if (x > a - 4 && x < b + 4) sk = true; if (sk) continue; c.Lamp(x, 148.6f, -Mathf.PI / 2); c.Lamp(x + 13, 165.4f, Mathf.PI / 2); }
            foreach (var (a, b) in INTERS) for (float z = 6; z < 262; z += 30) { if (z > 140 && z < 172) continue; c.Lamp(a - 1.3f, z, 0); c.Lamp(b + 1.3f, z + 15, Mathf.PI); }
            foreach (var (x, z) in new[] { (30f, 148.4f), (90, 148.4f), (150, 148.4f), (240, 148.4f), (330, 148.4f), (60, 165.6f), (170, 165.6f), (260, 165.6f), (360, 165.6f), (110.6f, 40), (125.4f, 100), (285, 220), (301.4f, 60) }) c.Bin(x, z);
            foreach (var cp in CPDEF) c.FireBarrel(cp.x, cp.z);
        }

        static Matrix4x4 ScaleY(float k) => Matrix4x4.Scale(new Vector3(1, k, 1));

        void Hochstrasse()
        {
            void Deck(float x0, float x1)
            {
                for (float x = x0; x < x1; x += 24) { float w = Mathf.Min(24, x1 - x); c.KitBox("MI_Concrete", x, 8.5f, 8, w, 1.4f, 14, 0xd8d4ca, null, 4); c.KitBox("MI_Concrete", x, 9.9f, 8, w, .9f, .4f, 0xc8c4bc, null, 3); c.KitBox("MI_Concrete", x, 9.9f, 21.6f, w, .9f, .4f, 0xc8c4bc, null, 3); }
                c.AddCol(x0, 8, x1 - x0, 14, 10.8f, 8.5f);
            }
            Deck(124, 206); Deck(296, City.WATERX + 2);
            for (float x = 132; x < City.WATERX; x += 20) { if (x > 200 && x < 296) continue; foreach (var zz in new[] { 11f, 19 }) { c.Add("Conc", Geo.Cylinder(.8f, .8f, 8.5f, 18, false), x, 4.25f, zz, 0xd2cdc2); c.AddCol(x - .8f, zz - .8f, 1.6f, 1.6f, 8.5f); } c.Add("Conc", Geo.Box(2.2f, .9f, 12), x, 8.05f, 15, 0xd2cdc2); }
            foreach (var x in new[] { 222f, 248, 272 }) { foreach (var zz in new[] { 11f, 19 }) { float h = x == 248 ? 8.5f : R.R(3, 6); c.Add("Conc", Geo.Cylinder(.8f, .8f, h, 18, false), x, h / 2, zz, 0xc8c3b8); } c.AddCol(x - 1, 10, 2, 10, 6); }
            c.KitBox("MI_Concrete", 236, 8.5f, 8, 24, 1.4f, 14, 0xc8c4bc, null, 4); c.AddCol(236, 8, 24, 14, 10, 8.5f); c.Scaffold(236, 260, 6.4f, 8.4f); c.Scaffold(236, 260, 22.2f, 8.4f);
            c.Box("Conc", City.WATERX + 2, 8.5f, 8, 380, 1.4f, 14, 0xd8d4ca, 6); for (float x = City.WATERX + 40; x < 780; x += 48) c.Box("Conc", x - 1.5f, -6, 9, 3, 14.5f, 12, 0xcfcac0, 4);
        }

        /// Stadtsilhouette jenseits der Karte (einfache Quader).
        public void BuildOuter()
        {
            float Near(float x, float z) { float dx = Mathf.Max(0, Mathf.Max(-x, x - City.WATERX)), dz = Mathf.Max(0, Mathf.Max(-z, z - City.WD)); return Mathf.Sqrt(dx * dx + dz * dz); }
            bool River(float x) => x > City.WATERX - 4 && x < 780;
            const float P = 44;
            for (float gx = -640; gx < 1200; gx += P) for (float gz = -660; gz < 960; gz += P)
                {
                    float bx = gx + 6, bz = gz + 6, bw = P - 12, bd = P - 12, cx = bx + bw / 2, cz = bz + bd / 2, dn = Near(cx, cz);
                    if (dn < 70 || River(bx) || River(bx + bw) || River(cx)) continue; if (dn > 620 && R.Next() < .5f) continue; if (R.Next() < .09f) continue;
                    bool man = cx > 780; float h = man ? R.R(18, 60) : R.R(11, 30); if (man && R.Next() < .3f) h = R.R(40, 95);
                    int tint = R.Pick(new[] { 0xffffff, 0xf0e8d8, 0xe6ebee, 0xd8d0c4, 0xc8c4bc, 0xe8dccc });
                    c.B.Add("Far", "far", Geo.BoxUV(bw - R.R(0, 6), h, bd - R.R(0, 6), cx, h / 2, cz, 11.2f, true), M.T(cx, h / 2 - 1, cz), M.Hex(tint));
                }
            for (float x = -60; x < City.WATERX - 10; x += R.R(14, 26)) { float w = R.R(12, 22), h = R.R(12, 24), z = 304 + R.R(0, 6), d = R.R(14, 20); c.B.Add("Far", "far", Geo.BoxUV(w, h, d, x + w / 2, h / 2, z + d / 2, 11.2f, true), M.T(x + w / 2, h / 2, z + d / 2), M.Hex(R.Pick(new[] { 0xf0e8d8, 0xe6ebee, 0xd8d0c4 }))); }
            // Fernmeldeturm Mannheim, BASF-Kühltürme
            c.B.Add("Conc", "far", Geo.Cylinder(3, 5, 212, 24, false), M.T(1060, 106, 420), M.Hex(0xd8d6d0)); c.B.Add("Conc", "far", Geo.Cylinder(15, 12, 11, 32, false), M.T(1060, 128, 420), M.Hex(0xe2e0da));
            foreach (var (x, z, r, h) in new[] { (-360f, -430f, 32f, 95f), (-280, -465, 30, 90) }) c.B.Add("Conc", "far", Geo.Cylinder(r * .7f, r, h, 40, true), M.T(x, h / 2, z), M.Hex(0xd6d2ca));
        }
    }
}
