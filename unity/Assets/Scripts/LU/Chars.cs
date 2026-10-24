// Figuren: Quaternius Universal Base Characters + Universal Animation Library (CC0), geladen mit glTFast.
using System.Collections.Generic;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;

namespace LU
{
    public class Look
    {
        public bool female; public int skin, top, leg, shoe, hairC; public string hair; public bool beard, helmet, crown; public int cap = -1;
        public Look(bool f, int skin, int top, int leg, int shoe, int hairC, string hair, bool beard = false) { female = f; this.skin = skin; this.top = top; this.leg = leg; this.shoe = shoe; this.hairC = hairC; this.hair = hair; this.beard = beard; }
        public static readonly Look Kalle = new(false, 0xe8c2a6, 0x2f4a32, 0x25365a, 0xf0f0ec, 0xdcb35a, "Hair_SimpleParted");
        static readonly int[] TOPS = { 0x5c6066, 0xa3332c, 0xb8a684, 0x4a5a3a, 0x2e7a78, 0x2f4a7a, 0xd8b03a, 0xe8e6e0, 0x7a4a8a, 0xc8803a, 0x3a6a9a, 0x8a8a8a, 0xe0c8b0, 0x1a1a1a };
        static readonly int[] LEGS = { 0x2e3b55, 0x26262c, 0x5a4a3a, 0x7a7c80, 0x3a3440, 0x1c1c22, 0x4a5a6a, 0xc8b89a };
        static readonly int[] SKINS = { 0xf0d0b8, 0xe8c2a6, 0xe0b896, 0xd8a886, 0xc99673, 0xb58463, 0x8a5a3c, 0xefcfb4 };
        static readonly int[] HAIRS = { 0x1a1410, 0x2a1d14, 0x5a3e26, 0x9a4a26, 0xd8b46a, 0x6a6a6a, 0x3a2a1a, 0xc8c4bc };
        static readonly int[] GANG = { 0x1f1f25, 0x2a2f3a, 0x3a3a40, 0x4a1a1a, 0x1f3a2a, 0x2a2a5a };
        public static Look Random(Rng R, bool gang)
        {
            bool f = !gang && R.Next() < .5f;
            return new Look(f, R.Pick(SKINS), R.Pick(gang ? GANG : TOPS), R.Pick(gang ? new[] { 0x1d1d22, 0x262a33, 0x2a2a30 } : LEGS), R.Pick(new[] { 0xe9e9e9, 0x1a1a1a, 0x3a2c22, 0x5a3a2a }), R.Pick(HAIRS),
                f ? R.Pick(new[] { "Hair_Long", "Hair_Buns", "Hair_BuzzedFemale" }) : R.Pick(new[] { "Hair_SimpleParted", "Hair_Buzzed", "Hair_Buzzed" }), !f && R.Next() < (gang ? .6f : .3f));
        }
    }

    public class Char
    {
        public GameObject holder, model; public Animation anim; public string cur = "", ucur = ""; public bool upper; public Look look;
        public SkinnedMeshRenderer body; public readonly List<Renderer> renderers = new();
        public Transform Bone(string n) => Find(model.transform, n);
        static Transform Find(Transform t, string n) { if (t.name == n) return t; foreach (Transform c in t) { var r = Find(c, n); if (r) return r; } return null; }

        public void Place(float x, float z, float yaw) { holder.transform.SetPositionAndRotation(M.U(x, City.H(x, z), z), M.Yaw(yaw)); }
        public void State(string s, float fade = .2f)
        {
            if (s == cur || anim == null || anim[s] == null) return; cur = s; anim.CrossFade(s, fade, PlayMode.StopSameLayer);
        }
        public void Upper(bool on, string s = null, float fade = .15f)
        {
            if (anim == null) return;
            if (!on) { if (upper) { foreach (AnimationState st in anim) if (st.layer == 1) anim.Blend(st.name, 0, fade); upper = false; ucur = ""; } return; }
            string n = "U_" + s; if (anim[n] == null) return;
            if (!upper || ucur != n) { anim.CrossFade(n, fade, PlayMode.StopSameLayer); upper = true; ucur = n; }
        }
        public void Visible(bool v) { if (holder.activeSelf != v) holder.SetActive(v); }
        public void Shadows(bool on) { foreach (var r in renderers) r.shadowCastingMode = on ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off; }
        public void Flash(float k)
        {
            var b = new MaterialPropertyBlock(); body.GetPropertyBlock(b);
            b.SetVectorArray("_RegionColors", Chars.Regions(look, k)); body.SetPropertyBlock(b);
        }
    }

    public class Chars
    {
        GltfImport male, female; readonly Dictionary<string, AnimationClip> clips = new();
        Material bodyMat, hairMat;
        public bool Ready;
        static readonly (string state, string clip, float speed, bool loop)[] BASE = {
            ("idle","Idle_Loop",1,true), ("walk","Walk_Loop",1,true), ("jog","Jog_Fwd_Loop",1.3f,true), ("sprint","Sprint_Loop",1.2f,true), ("roll","Roll",1.8f,false), ("death","Death01",1,false),
            ("hit","Hit_Chest",1.4f,false), ("swing","Sword_Attack",1.1f,false), ("punch","Punch_Cross",1,false), ("throw","Spell_Simple_Shoot",1,false), ("talk","Idle_Talking_Loop",1,true),
            ("sit","Sitting_Idle_Loop",1,true), ("aim","Pistol_Idle_Loop",1,true) };
        static readonly (string state, string clip, float speed, bool loop)[] UP = { ("carry", "Pistol_Idle_Loop", 1, true), ("aimU", "Pistol_Aim_Neutral", 1, true), ("reload", "Pistol_Reload", 1.2f, false), ("stab", "Punch_Jab", 1.6f, false) };

        public async Task Load()
        {
            string b = Application.streamingAssetsPath + "/chars/";
            var set = new ImportSettings { AnimationMethod = AnimationMethod.Legacy };
            var an = new GltfImport(); if (!await an.Load(b + "anims.glb", set)) { Debug.LogError("anims.glb nicht geladen"); return; }
            foreach (var c in an.GetAnimationClips()) { c.legacy = true; clips[c.name] = c; }
            male = new GltfImport(); female = new GltfImport();
            bool ok = await male.Load(b + "male.glb", set) & await female.Load(b + "female.glb", set);
            if (!ok) { Debug.LogError("Figuren nicht geladen"); return; }
            bodyMat = Resources.Load<Material>("Materials/CharBody"); hairMat = Resources.Load<Material>("Materials/CharHair");
            Ready = true;
        }

        public static List<Vector4> Regions(Look L, float flash = 0)
        {
            Vector4 C(int h, bool fl = true) { Color c = M.Hex(h).linear; if (flash > 0 && fl) c = Color.Lerp(c, new Color(1, .12f, .08f), flash); return c; }
            return new List<Vector4> { C(L.skin), C(L.top), C(L.leg), C(L.shoe), new Vector4(.5f, .5f, .5f, 1), C(L.hairC, false) };
        }

        public async Task<Char> Spawn(Look L, float scale, Transform parent, bool shadows)
        {
            var ch = new Char { look = L, holder = new GameObject("Figur") }; ch.holder.transform.SetParent(parent, false);
            var src = L.female ? female : male;
            await src.InstantiateMainSceneAsync(ch.holder.transform);
            ch.model = ch.holder.transform.GetChild(ch.holder.transform.childCount - 1).gameObject; ch.model.transform.localScale = Vector3.one * scale;
            foreach (var r in ch.model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                string n = r.gameObject.name; bool isBody = n.StartsWith("Body");
                if (isBody) { var mats = new Material[r.sharedMaterials.Length]; for (int i = 0; i < mats.Length; i++) mats[i] = bodyMat; r.sharedMaterials = mats; ch.body = r; }
                else
                {
                    bool on = n.StartsWith(L.hair) || (L.beard && n.StartsWith("Hair_Beard")); r.gameObject.SetActive(on);
                    var mats = new Material[r.sharedMaterials.Length]; for (int i = 0; i < mats.Length; i++) mats[i] = hairMat; r.sharedMaterials = mats;
                    var hb = new MaterialPropertyBlock(); hb.SetColor("_BaseColor", M.Hex(L.hairC).linear * 2f); r.SetPropertyBlock(hb);
                }
                r.updateWhenOffscreen = false; ch.renderers.Add(r);
            }
            if (ch.body) { var b = new MaterialPropertyBlock(); b.SetVectorArray("_RegionColors", Regions(L)); ch.body.SetPropertyBlock(b); }
            ch.Shadows(shadows);
            var anim = ch.model.GetComponent<Animation>() ?? ch.model.AddComponent<Animation>(); anim.playAutomatically = false; anim.cullingType = AnimationCullingType.BasedOnRenderers;
            foreach (var (s, c, sp, loop) in BASE) if (clips.TryGetValue(c, out var clip)) { anim.AddClip(clip, s); var st = anim[s]; st.speed = sp; st.wrapMode = loop ? WrapMode.Loop : WrapMode.ClampForever; st.layer = 0; }
            var spine = ch.Bone("spine_01");
            foreach (var (s, c, sp, loop) in UP) if (clips.TryGetValue(c, out var clip)) { string n = "U_" + s; anim.AddClip(clip, n); var st = anim[n]; st.speed = sp; st.wrapMode = loop ? WrapMode.Loop : WrapMode.ClampForever; st.layer = 1; if (spine) st.AddMixingTransform(spine, true); }
            ch.anim = anim; ch.State("idle", 0);
            AddExtras(ch, L);
            return ch;
        }

        static void AddExtras(Char ch, Look L)
        {
            var head = ch.Bone("Head"); if (!head) return;
            if (L.helmet) { var h = Prim(PrimitiveType.Sphere, head, new Vector3(0, .1f, .01f), new Vector3(.3f, .2f, .32f), 0xf2f2f2); }
            if (L.cap >= 0) { Prim(PrimitiveType.Sphere, head, new Vector3(0, .1f, .01f), new Vector3(.25f, .16f, .27f), L.cap); Prim(PrimitiveType.Cube, head, new Vector3(0, .07f, .14f), new Vector3(.2f, .015f, .13f), L.cap); }
            if (L.crown) for (int i = 0; i < 8; i++) { float a = i / 8f * Mathf.PI * 2; Prim(PrimitiveType.Cube, head, new Vector3(Mathf.Cos(a) * .1f, .2f, Mathf.Sin(a) * .1f), new Vector3(.03f, .08f, .03f), 0xd8b04a); }
        }

        public static GameObject Prim(PrimitiveType t, Transform parent, Vector3 pos, Vector3 scale, int col, string mat = "Props")
        {
            var g = GameObject.CreatePrimitive(t); Object.Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localScale = scale;
            var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = Solid(mat, col);
            return g;
        }
        static readonly Dictionary<string, Material> solids = new();
        /// Material ohne Vertexfarbe (für Unity-Grundkörper)
        public static Material Solid(string baseMat, int col)
        {
            string k = baseMat + col; if (solids.TryGetValue(k, out var m)) return m;
            m = new Material(Resources.Load<Material>("Materials/" + baseMat)); m.SetFloat("_VCStrength", 0); m.SetColor("_BaseColor", M.Hex(col)); solids[k] = m; return m;
        }
    }
}
