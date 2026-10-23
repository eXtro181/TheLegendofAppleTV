// Richtet das Projekt komplett per Skript ein (URP, Materialien, Szene) und baut WebGL.
// Aufruf in CI: -executeMethod LU.Builder.BuildWebGL   (GameCI: buildMethod: LU.Builder.BuildWebGL)
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LU
{
    public static class Builder
    {
        const string MatDir = "Assets/Resources/Materials";
        const string KitDir = "Assets/Resources/Kit";
        const string GenDir = "Assets/Generated";
        const string ScenePath = "Assets/Scenes/LU.unity";

        [MenuItem("LU/Projekt einrichten")]
        public static void Setup()
        {
            Directory.CreateDirectory(MatDir); Directory.CreateDirectory(GenDir); Directory.CreateDirectory("Assets/Scenes");
            ConfigureTextures();
            var pipeline = CreatePipeline();
            GraphicsSettings.defaultRenderPipeline = pipeline;
            for (int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = pipeline; }
            CreateMaterials();
            CreateScene();
            PlayerSettings.companyName = "eXtro181"; PlayerSettings.productName = "The Legend of Apple TV";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;   // GitHub Pages liefert kein Content-Encoding
            PlayerSettings.WebGL.memorySize = 512;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.stripEngineCode = false;
            EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.ASTC;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        public static void BuildWebGL()
        {
            Setup();
            string outDir = Environment.GetEnvironmentVariable("LU_BUILD_DIR") ?? "build/WebGL";
            var opts = new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = outDir, target = BuildTarget.WebGL, options = BuildOptions.None };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("LU-Build: " + report.summary.result + " " + report.summary.totalSize / 1e6 + " MB, Fehler: " + report.summary.totalErrors);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        // ---------- Texturen ----------
        static void ConfigureTextures()
        {
            foreach (var path in Directory.GetFiles(KitDir).Where(p => p.EndsWith(".png") || p.EndsWith(".jpg")))
            {
                var ti = AssetImporter.GetAtPath(path.Replace('\\', '/')) as TextureImporter; if (ti == null) continue;
                string n = Path.GetFileNameWithoutExtension(path);
                ti.maxTextureSize = 2048; ti.mipmapEnabled = true; ti.anisoLevel = 8; ti.wrapMode = TextureWrapMode.Repeat;
                ti.textureCompression = TextureImporterCompression.CompressedHQ;
                if (n.Contains("Normal")) { ti.textureType = TextureImporterType.NormalMap; }
                else if (n.Contains("MetalSmooth") || n.Contains("Occlusion")) { ti.textureType = TextureImporterType.Default; ti.sRGBTexture = false; ti.maxTextureSize = 1024; }
                else { ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true; ti.alphaIsTransparency = n.Contains("Decal"); }
                ti.SaveAndReimport();
            }
        }

        static Texture2D SaveTex(string name, int w, int h, Func<int, int, Color> f, bool srgb = true)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true, !srgb);
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) t.SetPixel(x, y, f(x, y));
            t.Apply(); string p = GenDir + "/" + name + ".png"; File.WriteAllBytes(p, t.EncodeToPNG()); AssetDatabase.ImportAsset(p);
            var ti = (TextureImporter)AssetImporter.GetAtPath(p); ti.sRGBTexture = srgb; ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Repeat; ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        }

        // ---------- Render-Pipeline ----------
        static UniversalRenderPipelineAsset CreatePipeline()
        {
            string rdPath = GenDir + "/LU_Renderer.asset", rpPath = GenDir + "/LU_URP.asset";
            var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rdPath);
            if (rd == null)
            {
                rd = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rd, rdPath);
                var ppd = AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
                if (ppd != null) rd.postProcessData = ppd; else Debug.LogWarning("PostProcessData nicht gefunden");
                AddSSAO(rd);
            }
            var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(rpPath);
            if (rp == null) { rp = UniversalRenderPipelineAsset.Create(rd); AssetDatabase.CreateAsset(rp, rpPath); }
            var so = new SerializedObject(rp);
            void F(string n, float v) { var p = so.FindProperty(n); if (p == null) { Debug.LogWarning("URP-Feld fehlt " + n); return; } if (p.propertyType == SerializedPropertyType.Float) p.floatValue = v; else if (p.propertyType == SerializedPropertyType.Boolean) p.boolValue = v != 0; else p.intValue = (int)v; }
            F("m_ShadowDistance", 90); F("m_ShadowCascadeCount", 2); F("m_MainLightShadowmapResolution", 4096); F("m_SoftShadowsSupported", 1);
            F("m_AdditionalLightsRenderingMode", 1); F("m_AdditionalLightsPerObjectLimit", 4); F("m_SupportsHDR", 1); F("m_MSAA", 4); F("m_RenderScale", 1);
            F("m_RequireDepthTexture", 1); F("m_SupportsTerrainHoles", 0); F("m_Cascade2Split", .28f);
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(rp);
            return rp;
        }

        static void AddSSAO(UniversalRendererData rd)
        {
            var t = typeof(UniversalRendererData).Assembly.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion");
            if (t == null) { Debug.LogWarning("SSAO-Feature nicht gefunden"); return; }
            var f = (ScriptableRendererFeature)ScriptableObject.CreateInstance(t); f.name = "SSAO";
            AssetDatabase.AddObjectToAsset(f, rd); rd.rendererFeatures.Add(f);
            AssetDatabase.SaveAssets();
            var so = new SerializedObject(rd); var map = so.FindProperty("m_RendererFeatureMap");
            if (map != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(f, out _, out long id)) { map.arraySize = rd.rendererFeatures.Count; map.GetArrayElementAtIndex(map.arraySize - 1).longValue = id; so.ApplyModifiedPropertiesWithoutUndo(); }
            // SSAO etwas kräftiger, kleiner Radius (Kanten, Ecken, Fensterlaibungen)
            var fso = new SerializedObject(f); var st = fso.FindProperty("m_Settings");
            if (st != null) { void S(string n, float v) { var p = st.FindPropertyRelative(n); if (p != null) { if (p.propertyType == SerializedPropertyType.Float) p.floatValue = v; else p.intValue = (int)v; } } S("Intensity", 1.6f); S("Radius", .4f); S("DirectLightingStrength", .3f); fso.ApplyModifiedPropertiesWithoutUndo(); }
            EditorUtility.SetDirty(rd);
        }

        // ---------- Materialien ----------
        static Texture2D K(string n) { foreach (var e in new[] { ".jpg", ".png" }) { var t = AssetDatabase.LoadAssetAtPath<Texture2D>(KitDir + "/" + n + e); if (t != null) return t; } Debug.LogWarning("Textur fehlt " + n); return null; }

        static Material Vc(string name, Texture2D albedo, Texture2D normal, Texture2D ms, Texture2D occ, float smooth = .5f, float metal = 1, float bump = 1, float vc = 1, Color? col = null)
        {
            var m = new Material(Shader.Find("LU/LitVC")) { name = name };
            m.SetTexture("_BaseMap", albedo); m.SetColor("_BaseColor", col ?? Color.white); m.SetFloat("_VCStrength", vc);
            if (normal) { m.SetTexture("_BumpMap", normal); m.SetFloat("_BumpScale", bump); m.EnableKeyword("_NORMALMAP"); m.SetFloat("_UseNormal", 1); }
            if (ms) { m.SetTexture("_MetallicGlossMap", ms); m.EnableKeyword("_METALLICSPECGLOSSMAP"); m.SetFloat("_UseMS", 1); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal); }
            else { m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal == 1 ? 0 : metal); }
            if (occ) { m.SetTexture("_OcclusionMap", occ); m.EnableKeyword("_OCCLUSIONMAP"); m.SetFloat("_UseOcc", 1); }
            return Save(m);
        }
        static Material Transparent(string shader, string name, Color col, Texture2D tex = null, float smooth = .9f, float metal = 0, bool zwrite = false)
        {
            var m = new Material(Shader.Find(shader)) { name = name };
            m.SetColor("_BaseColor", col); if (tex) m.SetTexture("_BaseMap", tex);
            m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0); m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One); m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", zwrite ? 1 : 0); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)RenderQueue.Transparent;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth); if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
            return Save(m);
        }
        static Material Save(Material m) { string p = MatDir + "/" + m.name + ".mat"; AssetDatabase.DeleteAsset(p); AssetDatabase.CreateAsset(m, p); return m; }

        static void CreateMaterials()
        {
            foreach (var (name, tex, sm) in new[] { ("MI_RedBrick", "T_RedBrick", .6f), ("MI_Trim", "T_Trim", .6f), ("MI_Trim_MetalConcrete", "T_MetalConcrete", .7f), ("MI_Concrete", "T_Concrete", .6f), ("MI_Asphalt", "T_Concrete", .6f), ("MI_Ornaments", "T_Ornaments", .8f), ("MI_Dirt", "T_Dirt", .5f), ("MI_Roof_Slate", "T_RoofSlate", .7f) })
                Vc(name, K(name == "MI_Asphalt" ? "T_Concrete_Asphalt_BaseColor" : tex + "_BaseColor"), K(tex + "_Normal"), K(tex + "_MetalSmooth"), K(tex + "_Occlusion"), sm, 1, 1.2f);
            Transparent("Universal Render Pipeline/Lit", "MI_Glass", new Color(.12f, .16f, .19f, .55f), null, .96f, .6f, true);
            Transparent("Universal Render Pipeline/Lit", "Glass", new Color(.12f, .16f, .19f, .5f), null, .96f, .6f, true);
            Transparent("Universal Render Pipeline/Lit", "MI_StreetDecals", Color.white, K("T_Street_Decals"), .3f);
            int k = 1; foreach (var n in new[] { "T_lit_interior_1", "T_lit_interior_2", "T_dark_interior" })
            {
                var m = Vc("INT" + k++, K(n), null, null, null, .2f, 0, 1, 0); m.SetTexture("_EmissionMap", K(n)); m.SetColor("_EmissionColor", Color.white * .35f); m.EnableKeyword("_EMISSION"); m.SetFloat("_UseEmission", 1); EditorUtility.SetDirty(m);
            }
            // Boden (Kit-Texturen in voller Auflösung)
            Vc("G_Asph", K("T_Concrete_Asphalt_BaseColor"), K("T_Concrete_Normal"), K("T_Concrete_MetalSmooth"), K("T_Concrete_Occlusion"), .55f, 1, 1.3f);
            Vc("G_Pav", K("T_MetalConcrete_BaseColor"), K("T_MetalConcrete_Normal"), K("T_MetalConcrete_MetalSmooth"), K("T_MetalConcrete_Occlusion"), .5f, 1, 1.4f, 1, new Color(.86f, .84f, .8f));
            Vc("G_Plaza", K("T_Concrete_BaseColor"), K("T_Concrete_Normal"), K("T_Concrete_MetalSmooth"), K("T_Concrete_Occlusion"), .55f, 1, 1.4f, 1, new Color(.92f, .88f, .82f));
            Vc("G_Conc", K("T_Concrete_BaseColor"), K("T_Concrete_Normal"), K("T_Concrete_MetalSmooth"), K("T_Concrete_Occlusion"), .5f, 1, 1.2f);
            Vc("G_Dirt", K("T_Dirt_BaseColor"), K("T_Dirt_Normal"), K("T_Dirt_MetalSmooth"), K("T_Dirt_Occlusion"), .4f, 1, 1.5f);
            Vc("G_Grav", K("T_Dirt_BaseColor"), K("T_Dirt_Normal"), K("T_Dirt_MetalSmooth"), K("T_Dirt_Occlusion"), .3f, 1, 2f, 1, new Color(.62f, .6f, .58f));
            Vc("G_Out", K("T_Concrete_BaseColor"), null, null, null, .2f, 0, 1, 1, new Color(.62f, .6f, .56f));
            Vc("G_Floor", null, null, null, null, .85f, 0, 1, 1, new Color(.85f, .83f, .8f));
            // Requisiten (Farbe aus Vertexfarbe)
            Vc("Props", null, null, null, null, .35f, 0); Vc("Paint", null, null, null, null, .8f, .35f); Vc("Iron", null, null, null, null, .35f, .8f); Vc("Metal", null, null, null, null, .7f, .95f);
            Vc("Rust", K("T_Dirt_BaseColor"), K("T_Dirt_Normal"), null, null, .2f, .4f, 1, 1, new Color(.6f, .42f, .32f)); Vc("Tire", null, null, null, null, .15f, 0);
            Vc("Wood", K("T_Dirt_BaseColor"), null, null, null, .25f, 0, 1, 1, new Color(.85f, .7f, .55f)); Vc("Conc", K("T_Concrete_BaseColor"), K("T_Concrete_Normal"), K("T_Concrete_MetalSmooth"), K("T_Concrete_Occlusion"), .5f, 1, 1);
            Vc("Mark", null, null, null, null, .4f, 0); Vc("Corr", K("T_MetalConcrete_BaseColor"), K("T_MetalConcrete_Normal"), null, null, .5f, .5f, 2);
            Vc("Slate", K("T_RoofSlate_BaseColor"), K("T_RoofSlate_Normal"), null, null, .5f, 0); Vc("Far", null, null, null, null, .3f, 0);
            Vc("Foliage", null, null, null, null, .25f, 0); Vc("Bark", K("T_Dirt_BaseColor"), K("T_Dirt_Normal"), null, null, .2f, 0, 1.5f, 1, new Color(.45f, .38f, .3f));
            var lamp = Vc("Lamp", null, null, null, null, .9f, 0); lamp.SetColor("_EmissionColor", new Color(1, .95f, .85f) * 1.5f); lamp.EnableKeyword("_EMISSION"); lamp.SetFloat("_UseEmission", 1);
            var tail = Vc("Tail", null, null, null, null, .9f, 0, 1, 1, new Color(.6f, .05f, .05f)); tail.SetColor("_EmissionColor", new Color(.5f, .02f, .02f)); tail.EnableKeyword("_EMISSION"); tail.SetFloat("_UseEmission", 1);
            var water = Vc("Water", null, null, null, null, .96f, 0, 1, 0, new Color(.2f, .27f, .3f));
            var ao = SaveTex("ao", 4, 64, (x, y) => new Color(0, 0, 0, .55f * Mathf.Pow(1 - y / 63f, 2)));
            Transparent("Universal Render Pipeline/Unlit", "AO", Color.white, ao);
            var chain = SaveTex("chain", 64, 64, (x, y) => { int a = (x + y) % 16, b = (x - y + 64) % 16; bool wire = a < 2 || b < 2; return wire ? new Color(.55f, .57f, .58f, 1) : new Color(0, 0, 0, 0); });
            var fence = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Fence" }; fence.SetTexture("_BaseMap", chain); fence.SetFloat("_AlphaClip", 1); fence.SetFloat("_Cutoff", .5f); fence.EnableKeyword("_ALPHATEST_ON"); fence.SetFloat("_Cull", 0); fence.SetFloat("_Metallic", .7f); fence.SetFloat("_Smoothness", .5f); Save(fence);
            // Figuren
            var body = new Material(Shader.Find("LU/LitVC")) { name = "CharBody" }; body.EnableKeyword("_REGIONS"); body.SetFloat("_Regions", 1); body.SetFloat("_Smoothness", .35f); body.SetFloat("_Metallic", 0); Save(body);
            var hair = new Material(Shader.Find("LU/LitVC")) { name = "CharHair" }; hair.SetFloat("_Smoothness", .45f); hair.SetFloat("_Metallic", 0); hair.SetFloat("_VCStrength", 0); Save(hair);
            var skyM = new Material(Shader.Find("Skybox/Procedural")) { name = "Sky" }; skyM.SetFloat("_SunSize", .03f); skyM.SetFloat("_AtmosphereThickness", 1.15f); skyM.SetColor("_SkyTint", new Color(.55f, .6f, .66f)); skyM.SetColor("_GroundColor", new Color(.42f, .4f, .37f)); skyM.SetFloat("_Exposure", 1.25f); Save(skyM);
            AssetDatabase.SaveAssets();
        }

        // ---------- Szene ----------
        static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Sky.mat");
            RenderSettings.ambientMode = AmbientMode.Skybox; RenderSettings.ambientIntensity = 1.1f;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .0026f; RenderSettings.fogColor = new Color(.66f, .64f, .6f);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            var go = new GameObject("Game"); go.AddComponent<Game>();
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
    }
}
