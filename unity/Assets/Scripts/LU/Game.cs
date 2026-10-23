// Einstieg: baut Ludwigshafen auf, lädt die Figuren und steuert Kalle (Phase 1: Welt, Licht, Bewegung, Passanten).
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LU
{
    public class Game : MonoBehaviour
    {
        City city; Chars chars; Camera cam; Light sun;
        Char kalle; readonly List<Civ> civs = new();
        float px = 30, pz = 100.2f, pyaw = 1.6f, pspeed; bool moving;
        float camYaw = -1.95f, camPitch = .2f; Vector3 camPos;
        string status = "Ludwigshafen wird gebaut …"; float fps = 60;
        readonly Dictionary<string, Material> mats = new();
        Transform world;

        class Civ { public Char c; public float x, z, ax, az, bx, bz, yaw, sp, wait; public int dir = 1; public bool walk; }

        IEnumerator Start()
        {
            Application.targetFrameRate = 60;
            SetupCamera(); SetupLight(); SetupPost();
            yield return null;
            Kit.Load();
            city = new City(); var L = new CityLayout(city);
            L.BuildGround(); yield return null;
            L.BuildCity(); yield return null;
            L.BuildOuter();
            BuildTrees();
            foreach (var g in new[] { "MI_RedBrick", "MI_Trim", "MI_Trim_MetalConcrete", "MI_Concrete" }) city.B.Grime.Add(g);
            world = new GameObject("Welt").transform;
            city.B.Build(world, Mat, m => !(m.StartsWith("G_") || m == "AO" || m == "MI_StreetDecals" || m == "Mark" || m == "Water" || m.StartsWith("INT") || m == "Far" || m == "MI_Glass" || m == "Glass"));
            yield return null;
            DynamicGI.UpdateEnvironment();
            var probe = new GameObject("Reflexionen").AddComponent<ReflectionProbe>();
            probe.transform.position = M.U(200, 18, 150); probe.size = new Vector3(900, 200, 900); probe.mode = ReflectionProbeMode.Realtime; probe.refreshMode = ReflectionProbeRefreshMode.OnAwake; probe.resolution = 256; probe.boxProjection = false; probe.importance = 1;
            status = "Figuren werden geladen …"; yield return null;
            chars = new Chars(); var t = chars.Load(); while (!t.IsCompleted) yield return null;
            if (chars.Ready)
            {
                var tk = chars.Spawn(Look.Kalle, 1, transform, true); while (!tk.IsCompleted) yield return null; kalle = tk.Result;
                AttachAK(kalle);
                var R = new Rng(99);
                var areas = new[] { (186f, 66f, 24f, 84f, 14), (124, 164, 162, 98, 16), (0, 96, 112, 8, 5), (52, 0, 8, 262, 5), (124, 262, 176, 18, 8), (300, 0, 12, 150, 6), (404, 0, 8, City.WD, 4) };
                foreach (var (ax, az, aw, ad, n) in areas)
                    for (int k = 0, tries = 0; k < n && tries < 300; tries++)
                    {
                        float x = ax + R.Next() * aw, z = az + R.Next() * ad; if (!city.FreeAt(x, z, .9f)) continue;
                        var tc = chars.Spawn(Look.Random(R, false), R.R(.94f, 1.04f), transform, false); while (!tc.IsCompleted) yield return null;
                        var cv = new Civ { c = tc.Result, x = x, z = z, ax = x, az = z, yaw = R.Next() * 6.28f, sp = R.R(1.1f, 1.5f), walk = R.Next() < .6f };
                        float a = R.Next() * 6.28f, len = R.R(6, 16); cv.bx = x + Mathf.Cos(a) * len; cv.bz = z + Mathf.Sin(a) * len;
                        for (int q = 1; q <= 8 && cv.walk; q++) if (!city.FreeAt(Mathf.Lerp(x, cv.bx, q / 8f), Mathf.Lerp(z, cv.bz, q / 8f), .6f)) cv.walk = false;
                        cv.c.Place(x, z, cv.yaw); if (!cv.walk) cv.c.State(R.Next() < .4f ? "talk" : "idle"); civs.Add(cv); k++;
                    }
            }
            camPos = M.U(px - 3, 2, pz - 3);
            status = null;
        }

        Material Mat(string name)
        {
            if (mats.TryGetValue(name, out var m)) return m;
            m = Resources.Load<Material>("Materials/" + name); mats[name] = m; return m;
        }

        void SetupCamera()
        {
            var go = new GameObject("Kamera"); cam = go.AddComponent<Camera>(); go.tag = "MainCamera";
            cam.fieldOfView = 58; cam.nearClipPlane = .1f; cam.farClipPlane = 1600; cam.clearFlags = CameraClearFlags.Skybox;
            var d = go.AddComponent<UniversalAdditionalCameraData>(); d.renderPostProcessing = true; d.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; d.antialiasingQuality = AntialiasingQuality.High; d.renderShadows = true;
        }
        void SetupLight()
        {
            var go = new GameObject("Sonne"); sun = go.AddComponent<Light>(); sun.type = LightType.Directional;
            Vector3 sd = M.U(new Vector3(-.55f, .42f, .5f).normalized); go.transform.rotation = Quaternion.LookRotation(-sd);
            sun.color = new Color(1, .9f, .76f); sun.intensity = 2.4f; sun.shadows = LightShadows.Soft; sun.shadowStrength = .92f; sun.shadowBias = .04f; sun.shadowNormalBias = .3f;
            RenderSettings.sun = sun;
        }
        void SetupPost()
        {
            var v = new GameObject("Nachbearbeitung").AddComponent<Volume>(); v.isGlobal = true; var p = ScriptableObject.CreateInstance<VolumeProfile>();
            var tm = p.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES);
            var bl = p.Add<Bloom>(true); bl.intensity.Override(.45f); bl.threshold.Override(1.05f); bl.scatter.Override(.65f);
            var ca = p.Add<ColorAdjustments>(true); ca.saturation.Override(-10); ca.contrast.Override(10); ca.postExposure.Override(.15f); ca.colorFilter.Override(new Color(1, .985f, .95f));
            var vg = p.Add<Vignette>(true); vg.intensity.Override(.28f); vg.smoothness.Override(.45f);
            var wb = p.Add<WhiteBalance>(true); wb.temperature.Override(6);
            v.sharedProfile = p;
        }

        void BuildTrees()
        {
            var trunk = Geo.Cylinder(.15f, .3f, 4.8f, 9, false); var blob = Geo.Sphere(1.4f, 10, 7);
            var R = new Rng(5);
            foreach (var t in city.Trees)
            {
                float x = t.x, z = t.y, s = t.z;
                city.B.Add("Bark", City.CellOf(x, z), trunk, M.T(x, 2.4f * s, z, R.Next() * 6, 0, 0, s, s, s), Color.white);
                for (int k = 0; k < 6; k++)
                {
                    float a = k / 6f * 6.28f + R.Next(), d = R.R(.4f, 1.6f) * s, y = R.R(4.2f, 6.2f) * s, sc = R.R(.7f, 1.15f) * s;
                    Color c = Color.HSVToRGB(R.R(.2f, .28f), R.R(.35f, .55f), R.R(.32f, .5f));
                    city.B.Add("Foliage", City.CellOf(x, z), blob, M.T(x + Mathf.Cos(a) * d, y, z + Mathf.Sin(a) * d, R.Next() * 6, 0, 0, sc, sc * .8f, sc), c);
                }
            }
        }

        void AttachAK(Char ch)
        {
            var hand = ch.Bone("hand_r"); if (!hand) return;
            var ak = new GameObject("AK47").transform; ak.SetParent(hand, false);
            // Lage aus der Kalibrierung der Browser-Version (glTF-Raum), in Unity-Raum gespiegelt (x)
            var q = new Quaternion(0.0066f, 0.7695f, 0.6381f, -0.0249f); ak.localRotation = q;
            Vector3 grip = q * new Vector3(0, -.085f, -.1f); ak.localPosition = new Vector3(.004f, .07f, .004f) - grip;
            Chars.Prim(PrimitiveType.Cube, ak, new Vector3(0, 0, 0), new Vector3(.055f, .075f, .36f), 0x24252a, "Iron");
            Chars.Prim(PrimitiveType.Cube, ak, new Vector3(0, .052f, -.01f), new Vector3(.05f, .035f, .33f), 0x3a3b40, "Iron");
            var br = Chars.Prim(PrimitiveType.Cylinder, ak, new Vector3(0, .012f, .38f), new Vector3(.022f, .21f, .022f), 0x24252a, "Iron"); br.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Chars.Prim(PrimitiveType.Cube, ak, new Vector3(0, -.005f, .27f), new Vector3(.068f, .062f, .21f), 0xa0602a, "Wood");
            var st = Chars.Prim(PrimitiveType.Cube, ak, new Vector3(0, -.045f, -.33f), new Vector3(.05f, .078f, .34f), 0xa0602a, "Wood"); st.transform.localRotation = Quaternion.Euler(-9, 0, 0);
            var mg = Chars.Prim(PrimitiveType.Cube, ak, new Vector3(0, -.13f, .16f), new Vector3(.045f, .2f, .08f), 0x24252a, "Iron"); mg.transform.localRotation = Quaternion.Euler(-20, 0, 0);
            Chars.Prim(PrimitiveType.Cube, ak, new Vector3(0, -.085f, -.1f), new Vector3(.044f, .112f, .052f), 0xa0602a, "Wood");
        }

        // ---------- Steuerung ----------
        int stickId = -1, lookId = -1; Vector2 stickO, stick, lookLast;
        void ReadInput(out float sx, out float sy)
        {
            sx = 0; sy = 0;
            foreach (var t in Input.touches)
            {
                if (t.phase == TouchPhase.Began) { if (t.position.x < Screen.width * .45f && stickId < 0) { stickId = t.fingerId; stickO = t.position; } else if (lookId < 0) { lookId = t.fingerId; lookLast = t.position; } }
                if (t.fingerId == stickId) { stick = Vector2.ClampMagnitude((t.position - stickO) / (Screen.height * .12f), 1); if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) { stickId = -1; stick = Vector2.zero; } }
                if (t.fingerId == lookId) { var dlt = t.position - lookLast; lookLast = t.position; camYaw -= dlt.x * .0068f * 900f / Screen.height; camPitch = Mathf.Clamp(camPitch - dlt.y * .0045f * 900f / Screen.height, -.25f, .95f); if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) lookId = -1; }
            }
            sx = stick.x; sy = -stick.y;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) sx -= 1; if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) sx += 1;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) sy -= 1; if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) sy += 1;
            if (Input.GetMouseButton(1)) { camYaw -= Input.GetAxis("Mouse X") * .08f; camPitch = Mathf.Clamp(camPitch + Input.GetAxis("Mouse Y") * .05f, -.25f, .95f); }
            if (Input.GetKey(KeyCode.Q)) camYaw += Time.deltaTime * 2; if (Input.GetKey(KeyCode.E)) camYaw -= Time.deltaTime * 2;
        }

        void Collide(ref float x, ref float z, float r)
        {
            foreach (var c in city.Cols)
            {
                if (c.y0 > 1.5f) continue; if (x < c.x - r || x > c.x + c.w + r || z < c.z - r || z > c.z + c.d + r) continue;
                float nx = Mathf.Clamp(x, c.x, c.x + c.w), nz = Mathf.Clamp(z, c.z, c.z + c.d), dx = x - nx, dz = z - nz, d2 = dx * dx + dz * dz;
                if (d2 < r * r)
                {
                    if (d2 > 1e-6f) { float d = Mathf.Sqrt(d2); x = nx + dx / d * r; z = nz + dz / d * r; }
                    else { float l = x - c.x, rr = c.x + c.w - x, t = z - c.z, b = c.z + c.d - z, m = Mathf.Min(Mathf.Min(l, rr), Mathf.Min(t, b)); if (m == l) x = c.x - r; else if (m == rr) x = c.x + c.w + r; else if (m == t) z = c.z - r; else z = c.z + c.d + r; }
                }
            }
            x = Mathf.Clamp(x, r, City.WATERX - r); z = Mathf.Clamp(z, r, City.WD - r);
        }

        void Update()
        {
            fps = Mathf.Lerp(fps, 1f / Mathf.Max(.001f, Time.unscaledDeltaTime), .05f);
            if (city == null || kalle == null) { OrbitCamera(); return; }
            float dt = Mathf.Min(.05f, Time.deltaTime);
            ReadInput(out float sx, out float sy); float ml = Mathf.Min(1, new Vector2(sx, sy).magnitude);
            float fx = -Mathf.Sin(camYaw), fz = -Mathf.Cos(camYaw), rx = Mathf.Cos(camYaw), rz = -Mathf.Sin(camYaw);
            float wx = rx * sx + fx * -sy, wz = rz * sx + fz * -sy;
            bool run = Input.GetKey(KeyCode.LeftShift);
            if (ml > .12f)
            {
                float l = Mathf.Sqrt(wx * wx + wz * wz), spd = (run ? 7.5f : 5.4f) * ml; px += wx / l * spd * dt; pz += wz / l * spd * dt; moving = true; pspeed = spd;
                float ty = Mathf.Atan2(wx, wz), d = Mathf.DeltaAngle(pyaw * Mathf.Rad2Deg, ty * Mathf.Rad2Deg) * Mathf.Deg2Rad; pyaw += d * Mathf.Min(1, dt * 12);
            }
            else { moving = false; pspeed = 0; }
            Collide(ref px, ref pz, .38f);
            kalle.Place(px, pz, pyaw);
            kalle.State(moving ? (pspeed > 6.5f ? "sprint" : pspeed > 3.6f ? "jog" : "walk") : "idle");
            kalle.Upper(!run || !moving, "carry");
            foreach (var cv in civs)
            {
                float dp = Mathf.Abs(cv.x - px) + Mathf.Abs(cv.z - pz); cv.c.Visible(dp < 70); if (dp > 70 || !cv.walk) continue;
                float tx = cv.dir > 0 ? cv.bx : cv.ax, tz = cv.dir > 0 ? cv.bz : cv.az, dx = tx - cv.x, dz = tz - cv.z, dd = Mathf.Sqrt(dx * dx + dz * dz);
                if (cv.wait > 0) { cv.wait -= dt; cv.c.State("idle"); }
                else if (dd < .3f) { cv.dir *= -1; cv.wait = Random.Range(1f, 4f); }
                else { cv.x += dx / dd * cv.sp * dt; cv.z += dz / dd * cv.sp * dt; cv.yaw = Mathf.LerpAngle(cv.yaw * Mathf.Rad2Deg, Mathf.Atan2(dx, dz) * Mathf.Rad2Deg, dt * 6) * Mathf.Deg2Rad; cv.c.State("walk"); }
                cv.c.Place(cv.x, cv.z, cv.yaw);
            }
            FollowCamera(dt);
        }

        void OrbitCamera()
        {
            float a = Time.time * .035f; cam.transform.position = M.U(205 + Mathf.Cos(a) * 150, 46, 150 + Mathf.Sin(a) * 120); cam.transform.LookAt(M.U(200, 10, 145));
        }

        void FollowCamera(float dt)
        {
            float ty = 1.55f + City.H(px, pz), sh = .6f, rx = Mathf.Cos(camYaw), rz = -Mathf.Sin(camYaw);
            float ox = Mathf.Sin(camYaw) * Mathf.Cos(camPitch), oy = Mathf.Sin(camPitch), oz = Mathf.Cos(camYaw) * Mathf.Cos(camPitch);
            float hx = px + rx * sh, hz = pz + rz * sh, dist = 3.9f;
            // Kamera nicht in Wände schieben
            float dd = dist;
            foreach (var c in city.Cols)
            {
                if (Mathf.Abs(c.x + c.w / 2 - hx) > c.w / 2 + 6 || Mathf.Abs(c.z + c.d / 2 - hz) > c.d / 2 + 6) continue;
                float t = RayBox(hx, ty, hz, ox, oy, oz, c); if (t < dd) dd = Mathf.Max(.5f, t - .25f);
            }
            Vector3 want = M.U(hx + ox * dd, Mathf.Max(.35f, ty + oy * dd), hz + oz * dd);
            camPos = Vector3.Lerp(camPos, want, Mathf.Min(1, dt * 14)); if ((camPos - want).magnitude > 6) camPos = want;
            cam.transform.position = camPos;
            cam.transform.LookAt(M.U(hx - ox * 10, ty + .15f - Mathf.Sin(camPitch) * 2.2f, hz - oz * 10));
        }

        static float RayBox(float ox, float oy, float oz, float dx, float dy, float dz, Box2 c)
        {
            float tmin = 0, tmax = 1e9f; float[] o = { ox, oy, oz }, d = { dx, dy, dz }, lo = { c.x, c.y0, c.z }, hi = { c.x + c.w, c.y1, c.z + c.d };
            for (int i = 0; i < 3; i++)
            {
                if (Mathf.Abs(d[i]) < 1e-8f) { if (o[i] < lo[i] || o[i] > hi[i]) return float.PositiveInfinity; continue; }
                float t1 = (lo[i] - o[i]) / d[i], t2 = (hi[i] - o[i]) / d[i]; if (t1 > t2) (t1, t2) = (t2, t1);
                tmin = Mathf.Max(tmin, t1); tmax = Mathf.Min(tmax, t2); if (tmin > tmax) return float.PositiveInfinity;
            }
            return tmin;
        }

        void OnGUI()
        {
            var st = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(Screen.height * .032f), fontStyle = FontStyle.Bold }; st.normal.textColor = Color.white;
            if (status != null) { GUI.Label(new Rect(20, Screen.height * .45f, Screen.width - 40, 60), status, st); return; }
            var small = new GUIStyle(st) { fontSize = Mathf.RoundToInt(Screen.height * .024f) };
            GUI.Label(new Rect(16, 12, Screen.width, 40), "The Legend of Apple TV · Unity-Vorschau (Phase 1: Stadt & Figuren)", small);
            GUI.Label(new Rect(16, 12 + Screen.height * .035f, 400, 40), Mathf.RoundToInt(fps) + " fps", small);
            if (stickId >= 0) { var c = GUI.color; GUI.color = new Color(1, 1, 1, .35f); float r = Screen.height * .12f; GUI.Box(new Rect(stickO.x - r, Screen.height - stickO.y - r, r * 2, r * 2), ""); GUI.color = c; }
        }
    }
}
