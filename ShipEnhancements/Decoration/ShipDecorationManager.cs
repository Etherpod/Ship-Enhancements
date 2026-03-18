using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements;
using static ShipEnhancements.Settings;
using ShipEnhancements.Utils;

namespace ShipEnhancements.Decoration;

public static class ShipDecorationManager
{
    private static readonly Dictionary<Material, ShipTextureBlender> _textureBlenders = new();
    public static Material textureBlendMat;
    
    private static Material _defaultInteriorHullMat;
    private static Material _defaultExteriorHullMat;
    private static Material _defaultInteriorWoodMat;
    private static Material _defaultExteriorWoodMat;
    private static Material _defaultGlassMat;
    private static Material _customGlassMat;
    private static Material _defaultSEInteriorMat1;
    private static Material _defaultSEInteriorMat2;
    
	public static void Initialize()
	{
        textureBlendMat = LoadMaterial("Assets/ShipEnhancements/ShipSkins/ShipTextureBlend.mat");
        
		if (_defaultInteriorHullMat == null)
        {
            MeshRenderer suppliesRenderer = SELocator.GetShipTransform().
                Find("Module_Supplies/Geo_Supplies/Supplies_Geometry/Supplies_Interior").GetComponent<MeshRenderer>();
            _defaultInteriorHullMat = suppliesRenderer.sharedMaterials[0];
        }
        
        if (_defaultExteriorHullMat == null)
        {
            MeshRenderer cabinRenderer = SELocator.GetShipTransform().
                Find("Module_Cabin/Geo_Cabin/Cabin_Geometry/Cabin_Exterior").GetComponent<MeshRenderer>();
            _defaultExteriorHullMat = cabinRenderer.sharedMaterials[3];
        }

        if (_defaultGlassMat == null)
        {
            MeshRenderer cockpitRenderer = SELocator.GetShipTransform()
                .Find("Module_Cockpit/Geo_Cockpit/Cockpit_Geometry/Cockpit_Exterior/CockpitExterior_GoldGlass")
                .GetComponent<MeshRenderer>();
            _defaultGlassMat = cockpitRenderer.sharedMaterial;
        }
        _customGlassMat = new Material(_defaultGlassMat);
        
        if (_defaultInteriorWoodMat == null)
        {
            MeshRenderer suppliesRenderer = SELocator.GetShipTransform().
                            Find("Module_Supplies/Geo_Supplies/Supplies_Geometry/Supplies_Interior").GetComponent<MeshRenderer>();
            _defaultInteriorWoodMat = suppliesRenderer.sharedMaterials[2];
        }
        
        if (_defaultExteriorWoodMat == null)
        {
            MeshRenderer cabinRenderer = SELocator.GetShipTransform().
                Find("Module_Cabin/Geo_Cabin/Cabin_Geometry/Cabin_Exterior").GetComponent<MeshRenderer>();
            _defaultExteriorWoodMat = cabinRenderer.sharedMaterials[2];
        }

        if (_defaultSEInteriorMat1 == null)
        {
            _defaultSEInteriorMat1 = LoadMaterial("Assets/ShipEnhancements/ShipInterior_HEA_VillageCabin_Recolored_mat.mat");
        }

        if (_defaultSEInteriorMat2 == null)
        {
            _defaultSEInteriorMat2 = LoadMaterial("Assets/ShipEnhancements/ShipInterior_SE_VillageCabin_mat.mat");
        }

        CustomMatManager.ClearMaterials(true);
        
        Material[] lightmapMaterials =
        {
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_SE_VillageCabin_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_HEA_VillageCabin_Recolored_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_SE_VillageMetal_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_HEA_VillageMetal_Recolored_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_HEA_VillagePlanks_Recolored_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_SE_CampsiteProps_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_HEA_CampsiteProps_Recolored_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_SE_SignsDecal_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_HEA_SignsDecal_Recolored_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_HEA_VillageCloth_Recolored_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_NOM_CopperOld_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_NOM_Sandstone_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/CockpitWindowFrost_Material.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_HEA_WaterGaugeMetal_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_HEA_ShipCurtain_Cloth_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_HEA_ShipCurtain_Metal_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_HEA_ShipCurtain_CampsiteProps_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipPlants/ShipInterior_Cactus_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipPlants/ShipInterior_CactusFlower_mat.mat"),
            _customGlassMat,
        };

        foreach (var mat in lightmapMaterials)
        {
            LightmapManager.AddMaterial(mat);
        }

        MeshRenderer chassisRenderer = SELocator.GetShipTransform().Find("Module_Cockpit/Geo_Cockpit/Cockpit_Geometry/Cockpit_Interior/Cockpit_Interior_Chassis")
            .GetComponent<MeshRenderer>();
        Texture2D blackTex = LoadAsset<Texture2D>("Assets/ShipEnhancements/Black_d.png");
        chassisRenderer.sharedMaterials[6].SetTexture("_OcclusionMap", blackTex);
        chassisRenderer.sharedMaterials[6].SetFloat("_OcclusionStrength", 0.75f);
        
        SetUpShipLogSplashScreen();
        ApplyHullDecoration();
        SetGlassMaterial();
        SetShipPlantDecoration();
        SetStringLightDecoration();
        SetDamageColors();
	}
    
    private static void SetUpShipLogSplashScreen()
    {
        GameObject go = SELocator.GetShipBody().GetComponentInChildren<ShipLogSplashScreen>().gameObject;
        MeshRenderer rend = go.GetComponent<MeshRenderer>();

        Texture2D tex = null;
        
        List<string> files = [];
        files.AddRange(Directory.GetFiles(Path.Combine(Instance.ModHelper.Manifest.ModFolderPath, "ShipLogIcons"), 
            "*.jpg", SearchOption.AllDirectories));
        files.AddRange(Directory.GetFiles(Path.Combine(Instance.ModHelper.Manifest.ModFolderPath, "ShipLogIcons"),
            "*.png", SearchOption.AllDirectories));
        
        if (files.Count > 0)
        {
            var rand = new System.Random();
            byte[] fileData = File.ReadAllBytes(files[rand.Next(0, files.Count)]);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }

        if (tex != null)
        {
            rend.sharedMaterial.SetTexture("_MainTex", tex);
        }
    }
    
    private static void ApplyHullDecoration()
    {
        string interiorHull = (string)interiorHullColor1.GetProperty();
        string exteriorHull = (string)exteriorHullColor1.GetProperty();
        string interiorWood = (string)interiorWoodColor1.GetProperty();
        string exteriorWood = (string)exteriorWoodColor1.GetProperty();
        
        WriteDebugMessage("interior hull setting: " + interiorHull);
        WriteDebugMessage("exterior hull setting: " + exteriorHull);
        
        bool interiorHullTex = (string)interiorHullTexture.GetProperty() != "None";
        bool exteriorHullTex = (string)exteriorHullTexture.GetProperty() != "None";
        bool interiorWoodTex = (string)interiorWoodTexture.GetProperty() != "None";
        bool exteriorWoodTex = (string)exteriorWoodTexture.GetProperty() != "None";

        bool blendInteriorHull = ((bool)enableColorBlending.GetProperty()
            && int.Parse((string)interiorHullColorOptions.GetProperty()) > 1)
            || interiorHull == "Rainbow";
        bool blendExteriorHull = ((bool)enableColorBlending.GetProperty()
                && int.Parse((string)exteriorHullColorOptions.GetProperty()) > 1)
            || exteriorHull == "Rainbow";
        bool blendInteriorWood = ((bool)enableColorBlending.GetProperty()
                && int.Parse((string)interiorWoodColorOptions.GetProperty()) > 1)
            || interiorWood == "Rainbow";
        bool blendExteriorWood = ((bool)enableColorBlending.GetProperty()
                && int.Parse((string)exteriorWoodColorOptions.GetProperty()) > 1)
            || exteriorWood == "Rainbow";
        
        var customizeInteriorHull = blendInteriorHull || interiorHull != "Default" || interiorHullTex;
        var customizeExteriorHull = blendExteriorHull || exteriorHull != "Default" || exteriorHullTex;
        var customizeInteriorWood = blendInteriorWood || interiorWood != "Default" || interiorWoodTex;
        var customizeExteriorWood = blendExteriorWood || exteriorWood != "Default" || exteriorWoodTex;

        Material[] interiorMats =
        [
            _defaultInteriorHullMat,
            _defaultSEInteriorMat1,
            _defaultSEInteriorMat2
        ];

        foreach (var blender in _textureBlenders.Values) blender.Dispose();
        _textureBlenders.Clear();

        foreach (var mat in interiorMats)
        {
            AddBlender(mat, false);
        }
        AddBlender(_defaultExteriorHullMat, false);
        AddBlender(_defaultInteriorWoodMat, true);
        AddBlender(_defaultExteriorWoodMat, true);
        
        // DumpMats("D:/misc/files/mats_02.json");

        foreach (MeshRenderer rend in SELocator.GetShipTransform().GetComponentsInChildren<MeshRenderer>())
        {
            rend.sharedMaterials = rend.sharedMaterials.Select(mat =>
            {
                if (mat == null) return mat;

                var isCustom = new[]
                {
                    interiorMats.Contains(mat) && customizeInteriorHull,
                    mat == _defaultExteriorHullMat && customizeExteriorHull,
                    mat == _defaultInteriorWoodMat && customizeInteriorWood,
                    mat == _defaultExteriorWoodMat && customizeExteriorWood
                }.Any(b => b);
                if (isCustom) return _textureBlenders[mat].BlendedMaterial;

                return mat;
            }).ToArray();
        }
        
        // DumpMats("D:/misc/files/mats_03.json");

        var intHullTex = LoadCustomTexture(interiorHullTex, interiorHullTexture, true);
        var extHullTex = LoadCustomTexture(exteriorHullTex, exteriorHullTexture, true);
        var intWoodTex = LoadCustomTexture(interiorWoodTex, interiorWoodTexture, false);
        var extWoodTex = LoadCustomTexture(exteriorWoodTex, exteriorWoodTexture, false);
        
        var intHullBlendController = SELocator.GetShipBody().gameObject.GetAddComponent<InteriorHullBlendController>();
        var extHullBlendController = SELocator.GetShipBody().gameObject.GetAddComponent<ExteriorHullBlendController>();
        var intWoodBlendController = SELocator.GetShipBody().gameObject.GetAddComponent<InteriorWoodBlendController>();
        var extWoodBlendController = SELocator.GetShipBody().gameObject.GetAddComponent<ExteriorWoodBlendController>();
        
        foreach (var mat in interiorMats)
        {
            ConfigureBlender(
                mat,
                interiorHullTex,
                intHullTex,
                blendInteriorHull,
                intHullBlendController,
                interiorHull
            );
        }

        ConfigureBlender(
            _defaultExteriorHullMat,
            exteriorHullTex,
            extHullTex,
            blendExteriorHull,
            extHullBlendController,
            exteriorHull
        );
        
        ConfigureBlender(
            _defaultInteriorWoodMat,
            interiorWoodTex,
            intWoodTex,
            blendInteriorWood,
            intWoodBlendController,
            interiorWood
        );
        
        ConfigureBlender(
            _defaultExteriorWoodMat,
            exteriorWoodTex,
            extWoodTex,
            blendExteriorWood,
            extWoodBlendController,
            exteriorWood
        );
        
        foreach (var blender in _textureBlenders.Values)
        {
            // $"[Q6J] try to full update {blender.BlendedMaterial.name}".Log();
            blender.UpdateFullTexture();
        }
        
        // DumpMats("D:/misc/files/mats_04.json");
    }

    private static void ConfigureBlender(
        Material material,
        bool textureCondition,
        ShipTextureInfo sourceTexture,
        bool blendCondition,
        ShipHullBlendController blendController,
        string themeName
    )
    {
        var blender = _textureBlenders[material];
        if (textureCondition)
            blender.SourceTexture = sourceTexture;
        else
            blender.SourceTexture = blender.BaseTexture;

        if (blendCondition)
            blendController.AddTextureBlender(blender);
        else if (themeName != "Default")
            blender.OverlayColor = ShipEnhancements.ThemeManager.GetHullTheme(themeName).HullColor / 255f;
    }

    private static void AddBlender(Material baseMaterial, bool isWood)
    {
        var woodZone = new Vector4(0, 0, 1, 1);
        var nonWoodZone = new Vector4(.5f, 0f, 1f, .5f);
        var exclusionZone = new Vector4(.9f, 0f, 1f, .2f);
        CustomMatManager.InitializeMaterial(baseMaterial);
        _textureBlenders[baseMaterial] = new ShipTextureBlender(
            textureBlendMat,
            baseMaterial,
            isWood ? woodZone : nonWoodZone,
            destExclusionZone: isWood ? null : exclusionZone
        );
    }

    private static ShipTextureInfo LoadCustomTexture(bool condition, Settings textureSetting, bool hasGloss)
    {
        if (!condition) return null;

        return new ShipTextureInfo(
            ShipEnhancements.ThemeManager.GetHullTexturePath((string)textureSetting.GetProperty()).path,
            hasGloss
        );
    }

    private static void SetGlassMaterial()
    {
        string tex = (string)shipGlassTexture.GetProperty();
        string[] paths =
        [
            "Module_Cockpit/Geo_Cockpit/Cockpit_Geometry/Cockpit_Exterior/CockpitExterior_GoldGlass",
            "Module_Cockpit/Geo_Cockpit/Cockpit_Geometry/Cockpit_Exterior/CockpitExterior_Chassis",
            "Module_Cabin/Geo_Cabin/Cabin_Tech/Cabin_Tech_Exterior/HatchPivot/Hatch_GoldGlass"
        ];

        if (tex == "None")
        {
            foreach (string child in paths)
            {
                MeshRenderer rend = SELocator.GetShipTransform().Find(child).GetComponent<MeshRenderer>();
                for (int i = 0; i < rend.sharedMaterials.Length; i++)
                {
                    if (rend.sharedMaterials[i] == null) continue;
                    
                    if (rend.sharedMaterials[i] == _customGlassMat)
                    {
                        List<Material> mats = new List<Material>();
                        mats.AddRange(rend.sharedMaterials);
                        mats[i] = _defaultGlassMat;
                        rend.sharedMaterials = mats.ToArray();
                    }
                }
            }
            
            _customGlassMat = new Material(_defaultGlassMat);
        }
        else
        {
            string path = ShipEnhancements.ThemeManager.GetGlassMaterialPath((string)shipGlassTexture.GetProperty());
            Material newMat = LoadMaterial(path);
            _customGlassMat = new Material(newMat);
            
            foreach (string child in paths)
            {
                MeshRenderer rend = SELocator.GetShipTransform().Find(child).GetComponent<MeshRenderer>();
                for (int i = 0; i < rend.sharedMaterials.Length; i++)
                {
                    if (rend.sharedMaterials[i] == null) continue;
                    
                    if (rend.sharedMaterials[i] == _defaultGlassMat)
                    {
                        List<Material> mats = new List<Material>();
                        mats.AddRange(rend.sharedMaterials);
                        mats[i] = _customGlassMat;
                        rend.sharedMaterials = mats.ToArray();
                    }
                }
            }
        }
    }

    private static void SetShipPlantDecoration()
    {
        string plantType = (string)shipPlantType.GetProperty();
        if (plantType == "Default") return;

        Transform parent = SELocator.GetShipTransform().Find("Module_Cockpit/Props_Cockpit");
        parent.Find("Props_HEA_ShipFoliage").gameObject.SetActive(false);
        
        if (plantType == "None") return;

        GameObject prefab = LoadPrefab(ShipEnhancements.ThemeManager.GetPlantTypePath(plantType));
        CreateObject(prefab, parent);
    }

    private static void SetStringLightDecoration()
    {
        string stringLights = (string)shipStringLights.GetProperty();
        if (stringLights == "None") return;

        GameObject prefab = LoadPrefab(ShipEnhancements.ThemeManager.GetStringLightPath(stringLights));
        Transform parent = CreateObject(prefab).transform;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            string path;
            if (child.name.Contains("Cabin"))
            {
                path = "Module_Cabin/Lights_Cabin";
            }
            else if (child.name.Contains("Supplies"))
            {
                path = "Module_Supplies/Lights_Supplies";
            }
            else if (child.name.Contains("Engine"))
            {
                path = "Module_Engine";
            }
            else
            {
                continue;
            }
            
            child.SetParent(SELocator.GetShipTransform().Find(path));
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
        }
        
        GameObject.Destroy(parent.gameObject);
    }
    
    private static void SetDamageColors()
    {
        string color = (string)indicatorColor1.GetProperty();
        bool indicatorBlend = ((bool)enableColorBlending.GetProperty()
            && int.Parse((string)indicatorColorOptions.GetProperty()) > 1)
            || color == "Rainbow";

        if (indicatorBlend)
        {
            SELocator.GetShipTransform().gameObject.AddComponent<ShipIndicatorBlendController>();
        }
        else if (color != "Default")
        {
            var damageScreenMat = SELocator.GetShipTransform().Find("Module_Cockpit/Systems_Cockpit/ShipCockpitUI/DamageScreen/HUD_ShipDamageDisplay")
                .GetComponent<MeshRenderer>().material;
            var masterAlarmMat = SELocator.GetShipTransform().Find("Module_Cockpit/Geo_Cockpit/Cockpit_Geometry/Cockpit_Interior/Cockpit_Interior_Chassis")
                .GetComponent<MeshRenderer>().sharedMaterials[6];
            var masterAlarmLight = SELocator.GetShipTransform().Find("Module_Cabin/Lights_Cabin/PointLight_HEA_MasterAlarm").GetComponent<Light>();
            var reactorLight = SELocator.GetShipTransform().Find("Module_Engine/Systems_Engine/ReactorComponent/ReactorDamageLight").GetComponent<Light>();
            var reactorGlow = SELocator.GetShipTransform().Find("Module_Engine/Systems_Engine/ReactorComponent/Structure_HEA_PlayerShip_ReactorDamageDecal")
                .GetComponent<MeshRenderer>().material;

            DamageTheme theme = ShipEnhancements.ThemeManager.GetDamageTheme(color);

            damageScreenMat.SetColor("_DamagedHullFill", theme.HullColor / 255f * Mathf.Pow(2, theme.HullIntensity));
            damageScreenMat.SetColor("_DamagedComponentFill", theme.CompColor / 255f * theme.CompIntensity);

            masterAlarmMat.SetColor("_Color", theme.AlarmColor / 255f);
            SELocator.GetShipCockpitController().transform.parent.GetComponentInChildren<ShipCockpitUI>()._damageLightColor = theme.AlarmLitColor / 255f
                * Mathf.Pow(2, theme.AlarmLitIntensity);
            masterAlarmLight.color = theme.IndicatorLight / 255f;

            Color reactorColor = theme.ReactorColor;
            reactorColor /= 191f;
            reactorColor.a = 1;
            reactorGlow.SetColor("_EmissionColor", reactorColor * Mathf.Pow(2, theme.ReactorIntensity));
            reactorLight.color = theme.ReactorLight / 255f;

            foreach (DamageEffect effect in SELocator.GetShipTransform().GetComponentsInChildren<DamageEffect>())
            {
                if (effect._damageLight)
                {
                    effect._damageLight.GetLight().color = theme.IndicatorLight / 255f;
                }
                if (effect._damageLightRenderer)
                {
                    effect._damageLightRendererColor = theme.AlarmLitColor / 255f * Mathf.Pow(2, theme.AlarmLitIntensity);
                }
            }
        }
    }
    
    public static void DumpMats(string filepath)
    {
        var q = new List<Dictionary<string, object>>();
        foreach (var r in SELocator.GetShipBody().GetComponentsInChildren<MeshRenderer>())
        {
            var p = new Dictionary<string, object>();
            q.Add(p);
            p.Add("GOID", r.gameObject.GetInstanceID());
            var ms = new List<Dictionary<string, object>>();
            p.Add("materials", ms);
            foreach (var m in r.sharedMaterials)
            {
                var md = new Dictionary<string, object>();
                ms.Add(md);
                md.Add("matID", m?.GetInstanceID());
                md.Add("hash", m?.GetHashCode());
                md.Add("name", m?.name);
            }
        }

        File.WriteAllText(filepath, JsonConvert.SerializeObject(q));
        
        WriteDebugMessage("[X0A] mats dumped");
    }
}