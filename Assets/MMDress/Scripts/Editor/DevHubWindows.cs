#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// MMDress Dev Hub (panel pusat tooling Editor)
/// Menu: Tools/MMDress/Dev Hub
/// </summary>
public sealed class MMDressDevHubWindow : EditorWindow
{
    // ================== EditorPrefs Keys ==================
    const string PREF_ROOT = "MMDress/DevHub/Root";
    const string PREF_DATAASSETS = "MMDress/DevHub/DataAssets";
    const string PREF_PREFABS = "MMDress/DevHub/Prefabs";
    const string PREF_SCRIPTS_R = "MMDress/DevHub/ScriptsRuntime";
    const string PREF_SCRIPTS_E = "MMDress/DevHub/ScriptsEditor";

    // ================== Paths (editable dari UI) ==================
    string rootPath;       // ex: Assets/MMDress
    string dataAssetsPath; // ex: Assets/MMDress/DataAssets
    string prefabsPath;    // ex: Assets/MMDress/Prefabs
    string scriptsRuntime; // ex: Assets/MMDress/Scripts/Runtime
    string scriptsEditor;  // ex: Assets/MMDress/Scripts/Editor
    string itemsFolder;    // ex: Assets/MMDress/DataAssets/Items

    // ================== Tabs ==================
    enum Tab { Setup, GenerateItems, CreatePrefabs, Validators }
    Tab current = Tab.Setup;

    // ================== Generate Items options ==================
    int genTop = 3, genBottom = 3;
    bool createCatalog = true;
    bool createInventory = true;

    // ================== Prefab helpers ==================
    string customerPrefabName = "Customer";
    string spawnerGoName = "[Spawner]";
    string bootstrapGoName = "[_Bootstrap]";

    // ================== Menu Entry (Satu-satunya) ==================
    [MenuItem("Tools/MMDress/Dev Hub", false, 0)]
    public static void OpenMenu() => OpenWindow();

    /// <summary>Buka jendela Dev Hub secara terprogram.</summary>
    public static MMDressDevHubWindow OpenWindow()
    {
        var w = GetWindow<MMDressDevHubWindow>("MMDress Dev Hub");
        w.minSize = new Vector2(720, 420);
        w.Show();
        return w;
    }

    /// <summary>Pindah tab (mis. dari menu legacy/shortcut).</summary>
    // jadikan private agar tidak bentrok akses
    private void SetTab(Tab tab) { current = tab; Repaint(); }

    // API publik yang dipanggil dari menu lain
    public void SetTabGenerateItems() => SetTab(Tab.GenerateItems);


    // ================== Lifecycle ==================
    void OnEnable()
    {
        rootPath = EditorPrefs.GetString(PREF_ROOT, "Assets/MMDress");
        dataAssetsPath = EditorPrefs.GetString(PREF_DATAASSETS, "Assets/MMDress/DataAssets");
        prefabsPath = EditorPrefs.GetString(PREF_PREFABS, "Assets/MMDress/Prefabs");
        scriptsRuntime = EditorPrefs.GetString(PREF_SCRIPTS_R, "Assets/MMDress/Scripts/Runtime");
        scriptsEditor = EditorPrefs.GetString(PREF_SCRIPTS_E, "Assets/MMDress/Scripts/Editor");
        itemsFolder = $"{dataAssetsPath}/Items";
    }

    void OnDisable()
    {
        EditorPrefs.SetString(PREF_ROOT, rootPath);
        EditorPrefs.SetString(PREF_DATAASSETS, dataAssetsPath);
        EditorPrefs.SetString(PREF_PREFABS, prefabsPath);
        EditorPrefs.SetString(PREF_SCRIPTS_R, scriptsRuntime);
        EditorPrefs.SetString(PREF_SCRIPTS_E, scriptsEditor);
    }

    // ================== GUI ==================
    void OnGUI()
    {
        DrawHeader();
        EditorGUILayout.Space(4);

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawSidebar();
            using (new EditorGUILayout.VerticalScope("box", GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true)))
            {
                switch (current)
                {
                    case Tab.Setup: DrawSetupTab(); break;
                    case Tab.GenerateItems: DrawGenerateItemsTab(); break;
                    case Tab.CreatePrefabs: DrawCreatePrefabsTab(); break;
                    case Tab.Validators: DrawValidatorsTab(); break;
                }
            }
        }
    }

    void DrawHeader()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("MMDress Dev Hub", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Docs (Paths)", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                EditorUtility.DisplayDialog("MMDress Paths",
                    $"{rootPath}\n├─ Scripts\n│  ├─ Runtime\n│  └─ Editor\n├─ Prefabs\n└─ DataAssets\n", "OK");
            }
        }
    }

    void DrawSidebar()
    {
        using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(190)))
        {
            GUILayout.Space(4);
            current = SidebarButton("Setup Project", Tab.Setup, current);
            current = SidebarButton("Generate Items", Tab.GenerateItems, current);
            current = SidebarButton("Create Prefabs", Tab.CreatePrefabs, current);
            current = SidebarButton("Validators", Tab.Validators, current);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Open DataAssets", GUILayout.Height(22))) OpenPath(dataAssetsPath);
            if (GUILayout.Button("Open Prefabs", GUILayout.Height(22))) OpenPath(prefabsPath);
        }
    }

    Tab SidebarButton(string label, Tab tab, Tab cur)
    {
        var style = new GUIStyle(EditorStyles.miniButtonLeft)
        { alignment = TextAnchor.MiddleLeft, fixedHeight = 26 };
        bool on = cur == tab;
        if (GUILayout.Toggle(on, label, style) != on) cur = tab;
        return cur;
    }

    // ------------------ TAB: Setup ------------------
    void DrawSetupTab()
    {
        EditorGUILayout.LabelField("Project Paths", EditorStyles.boldLabel);
        rootPath = PathField("Root", rootPath);
        dataAssetsPath = PathField("DataAssets", dataAssetsPath);
        prefabsPath = PathField("Prefabs", prefabsPath);
        scriptsRuntime = PathField("Scripts/Runtime", scriptsRuntime);
        scriptsEditor = PathField("Scripts/Editor", scriptsEditor);
        itemsFolder = PathField("Items Folder", itemsFolder);

        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Create Standard Folders", GUILayout.Height(28)))
            {
                CreateFoldersStandard();
                EditorUtility.DisplayDialog("MMDress", "Standard folders ensured.", "OK");
            }
            if (GUILayout.Button("Create asmdef (Runtime & Editor)", GUILayout.Height(28)))
            {
                CreateAsmdefs();
                EditorUtility.DisplayDialog("MMDress", "asmdef created (or updated).", "OK");
            }
        }

        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add GameBootstrap to Scene", GUILayout.Height(26)))
                AddBootstrapToScene();

            if (GUILayout.Button("Reset PlayerPrefs (All)", GUILayout.Height(26)))
            {
                if (EditorUtility.DisplayDialog("Reset PlayerPrefs",
                    "Hapus SEMUA PlayerPrefs project ini? (tidak bisa di-undo)",
                    "Reset", "Batal"))
                {
                    PlayerPrefs.DeleteAll();
                    PlayerPrefs.Save();
                    Debug.Log("[MMDress] PlayerPrefs di-reset dari Dev Hub.");
                }
            }
        }
    }

    // ------------------ TAB: Generate Items ------------------
    void DrawGenerateItemsTab()
    {
        EditorGUILayout.LabelField("Generate Dummy ItemSO", EditorStyles.boldLabel);
        genTop = EditorGUILayout.IntField("Top (Baju)", Mathf.Max(0, genTop));
        genBottom = EditorGUILayout.IntField("Bottom (Celana)", Mathf.Max(0, genBottom));
        itemsFolder = PathField("Save To", itemsFolder);
        createCatalog = EditorGUILayout.ToggleLeft("Create/Update Catalog.asset", createCatalog);
        createInventory = EditorGUILayout.ToggleLeft("Create Inventory.asset (optional)", createInventory);

        if (GUILayout.Button("Generate", GUILayout.Height(28)))
        {
            EnsureFolder(itemsFolder);
            var catalog = EnsureCatalog($"{itemsFolder}/Catalog.asset");
            int idSeed = UnityEngine.Random.Range(1000, 9999);

            // Tops
            for (int i = 0; i < genTop; i++)
            {
                var item = ScriptableObject.CreateInstance<MMDress.Data.ItemSO>();
                item.id = $"top_{idSeed + i}";
                item.displayName = $"Top {i + 1}";
                item.slot = MMDress.Data.OutfitSlot.Top;
                item.localPos = Vector3.zero;
                item.localScale = Vector3.one;
                item.localRotZ = 0f;
                var path = $"{itemsFolder}/Top_{i + 1}.asset";
                AssetDatabase.CreateAsset(item, path);
                if (createCatalog) catalog.items.Add(item);
            }

            // Bottoms
            for (int i = 0; i < genBottom; i++)
            {
                var item = ScriptableObject.CreateInstance<MMDress.Data.ItemSO>();
                item.id = $"bottom_{idSeed + genTop + i}";
                item.displayName = $"Bottom {i + 1}";
                item.slot = MMDress.Data.OutfitSlot.Bottom;
                item.localPos = Vector3.zero;
                item.localScale = Vector3.one;
                item.localRotZ = 0f;
                var path = $"{itemsFolder}/Bottom_{i + 1}.asset";
                AssetDatabase.CreateAsset(item, path);
                if (createCatalog) catalog.items.Add(item);
            }

            if (createCatalog)
            {
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            if (createInventory)
            {
                EnsureFolder($"{dataAssetsPath}/Inventory");
                var inv = ScriptableObject.CreateInstance<MMDress.Data.InventorySO>();
                AssetDatabase.CreateAsset(inv, $"{dataAssetsPath}/Inventory/Inventory.asset");
            }

            EditorUtility.DisplayDialog("MMDress", "Dummy items created.", "OK");
        }
    }

    // ------------------ TAB: Create Prefabs ------------------
    void DrawCreatePrefabsTab()
    {
        EditorGUILayout.LabelField("Create Prefabs / Scene Helpers", EditorStyles.boldLabel);

        if (GUILayout.Button("Create Customer.prefab (with Anchors & Controller)", GUILayout.Height(26)))
            CreateCustomerPrefab();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Create Spawner in Scene", GUILayout.Height(26)))
                CreateSpawnerInScene();
            if (GUILayout.Button("Create Bootstrap in Scene", GUILayout.Height(26)))
                AddBootstrapToScene();
        }
    }

    // ------------------ TAB: Validators ------------------
    void DrawValidatorsTab()
    {
        EditorGUILayout.LabelField("Validation Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("Scan Duplicate ItemSO IDs", GUILayout.Height(26)))
        {
            var guids = AssetDatabase.FindAssets("t:MMDress.Data.ItemSO");
            var map = new Dictionary<string, string>();
            var dup = new List<(string id, string path)>();

            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                var item = AssetDatabase.LoadAssetAtPath<MMDress.Data.ItemSO>(path);
                if (item == null) continue;

                if (string.IsNullOrEmpty(item.id)) { dup.Add(("(EMPTY)", path)); continue; }
                if (map.ContainsKey(item.id)) dup.Add((item.id, path));
                else map[item.id] = path;
            }

            if (dup.Count == 0) EditorUtility.DisplayDialog("MMDress", "No duplicate IDs found.", "OK");
            else
            {
                string report = "Duplicates:\n";
                foreach (var d in dup) report += $"- {d.id} @ {d.path}\n";
                EditorUtility.DisplayDialog("MMDress", report, "OK");
            }
        }
    }

    // ================== Helpers ==================
    string PathField(string label, string path)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            path = EditorGUILayout.TextField(label, path);
            if (GUILayout.Button("…", GUILayout.Width(28)))
            {
                string picked = EditorUtility.OpenFolderPanel("Pick folder under project", Absolute(rootPath), "");
                if (!string.IsNullOrEmpty(picked))
                {
                    string rel = ToRelative(picked);
                    if (!string.IsNullOrEmpty(rel)) path = rel;
                }
            }
        }
        return path;
    }

    void CreateFoldersStandard()
    {
        EnsureFolder(rootPath);
        EnsureFolder($"{rootPath}/Scripts/Runtime/Core");
        EnsureFolder($"{rootPath}/Scripts/Runtime/Data");
        EnsureFolder($"{rootPath}/Scripts/Runtime/Services");
        EnsureFolder($"{rootPath}/Scripts/Runtime/Gameplay");
        EnsureFolder($"{rootPath}/Scripts/Runtime/Character");
        EnsureFolder($"{rootPath}/Scripts/Runtime/Customer");
        EnsureFolder($"{rootPath}/Scripts/Runtime/UI");
        EnsureFolder($"{rootPath}/Scripts/Editor/Menu");
        EnsureFolder($"{rootPath}/Scripts/Editor/Validators");
        EnsureFolder(prefabsPath);
        EnsureFolder($"{prefabsPath}/Character");
        EnsureFolder($"{prefabsPath}/UI");
        EnsureFolder($"{rootPath}/Scenes");
        EnsureFolder(dataAssetsPath);
        EnsureFolder($"{dataAssetsPath}/Items");
        EnsureFolder($"{dataAssetsPath}/Catalogs");
        EnsureFolder($"{dataAssetsPath}/Inventory");
    }

    void CreateAsmdefs()
    {
        EnsureFolder($"{rootPath}/Scripts/Runtime");
        EnsureFolder($"{rootPath}/Scripts/Editor");

        string runtimeAsm = $"{rootPath}/Scripts/Runtime/MMDress.Runtime.asmdef";
        string editorAsm = $"{rootPath}/Scripts/Editor/MMDress.Editor.asmdef";

        WriteTextAsset(runtimeAsm,
@"{
  ""name"": ""MMDress.Runtime"",
  ""autoReferenced"": true
}");
        WriteTextAsset(editorAsm,
@"{
  ""name"": ""MMDress.Editor"",
  ""references"": [ ""MMDress.Runtime"" ],
  ""includePlatforms"": [ ""Editor"" ],
  ""autoReferenced"": true
}");
        AssetDatabase.ImportAsset(runtimeAsm);
        AssetDatabase.ImportAsset(editorAsm);
    }

    void AddBootstrapToScene()
    {
        var type = Type.GetType("MMDress.Gameplay.GameBootstrap, MMDress.Runtime");
        if (type == null)
        {
            EditorUtility.DisplayDialog("MMDress", "GameBootstrap type not found. Pastikan script runtime sudah ada & compile OK.", "OK");
            return;
        }
        var go = new GameObject(bootstrapGoName);
        go.AddComponent(type);
        Undo.RegisterCreatedObjectUndo(go, "Create Bootstrap");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = go;
    }

    void CreateCustomerPrefab()
    {
        EnsureFolder($"{prefabsPath}/Character");

        var root = new GameObject(customerPrefabName);
        var col = root.AddComponent<BoxCollider2D>();
        col.isTrigger = false; col.size = new Vector2(1, 2);

        var customerType = Type.GetType("MMDress.Customer.CustomerController, MMDress.Runtime");
        if (customerType == null) { Cleanup(root); EditorUtility.DisplayDialog("MMDress", "CustomerController not found.", "OK"); return; }
        root.AddComponent(customerType);

        var character = new GameObject("Character");
        character.transform.SetParent(root.transform, false);

        var outfitType = Type.GetType("MMDress.Character.CharacterOutfitController, MMDress.Runtime");
        if (outfitType == null) { Cleanup(root); EditorUtility.DisplayDialog("MMDress", "CharacterOutfitController not found.", "OK"); return; }
        var outfit = character.AddComponent(outfitType) as MonoBehaviour;

        var top = new GameObject("TopAnchor").transform; top.SetParent(character.transform, false);
        var bot = new GameObject("BottomAnchor").transform; bot.SetParent(character.transform, false);
        top.localPosition = new Vector3(0f, 0.6f, 0f);
        bot.localPosition = new Vector3(0f, 0.0f, 0f);

        var so = new SerializedObject(outfit);
        so.FindProperty("topAnchor").objectReferenceValue = top;
        so.FindProperty("bottomAnchor").objectReferenceValue = bot;
        so.ApplyModifiedPropertiesWithoutUndo();

        string savePath = $"{prefabsPath}/Character/{customerPrefabName}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, savePath, out bool success);
        Cleanup(root);

        if (success)
        {
            EditorGUIUtility.PingObject(prefab);
            Selection.activeObject = prefab;
            EditorUtility.DisplayDialog("MMDress", $"Prefab saved:\n{savePath}", "OK");
        }
        else EditorUtility.DisplayDialog("MMDress", "Failed to save prefab.", "OK");
    }

    void CreateSpawnerInScene()
    {
        var spawnerType = Type.GetType("MMDress.Customer.CustomerSpawner, MMDress.Runtime");
        if (spawnerType == null) { EditorUtility.DisplayDialog("MMDress", "CustomerSpawner not found.", "OK"); return; }

        var go = new GameObject(spawnerGoName);
        var spawnPoint = new GameObject("SpawnPoint").transform;
        spawnPoint.SetParent(go.transform, false);

        var spawner = go.AddComponent(spawnerType) as MonoBehaviour;
        var so = new SerializedObject(spawner);
        so.FindProperty("spawnPoint").objectReferenceValue = spawnPoint;
        so.ApplyModifiedPropertiesWithoutUndo();

        Undo.RegisterCreatedObjectUndo(go, "Create Spawner");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = go;
    }

    // -------- Data helpers --------
    MMDress.Data.CatalogSO EnsureCatalog(string unityPath)
    {
        var existing = AssetDatabase.LoadAssetAtPath<MMDress.Data.CatalogSO>(unityPath);
        if (existing != null) return existing;

        int slash = unityPath.LastIndexOf('/');
        if (slash > 0) EnsureFolder(unityPath.Substring(0, slash));

        var catalog = ScriptableObject.CreateInstance<MMDress.Data.CatalogSO>();
        AssetDatabase.CreateAsset(catalog, unityPath);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        return catalog;
    }

    // -------- File & Path helpers --------
    void EnsureFolder(string unityPath)
    {
        if (AssetDatabase.IsValidFolder(unityPath)) return;

        var parts = unityPath.Split('/');
        if (parts.Length <= 1 || parts[0] != "Assets")
            throw new Exception("Path must start with 'Assets/' : " + unityPath);

        string cur = "Assets";
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{cur}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }

    void WriteTextAsset(string unityPath, string contents)
    {
        string abs = Absolute(unityPath);
        Directory.CreateDirectory(Path.GetDirectoryName(abs));
        File.WriteAllText(abs, contents);
    }

    string Absolute(string unityPath)
    {
        string proj = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
        return Path.GetFullPath(Path.Combine(proj, unityPath));
    }

    string ToRelative(string absolutePath)
    {
        absolutePath = absolutePath.Replace("\\", "/");
        string proj = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length).Replace("\\", "/");
        if (!absolutePath.StartsWith(proj)) return null;
        return absolutePath.Substring(proj.Length);
    }

    void OpenPath(string unityPath)
    {
        var obj = AssetDatabase.LoadAssetAtPath<Object>(unityPath);
        if (obj) EditorGUIUtility.PingObject(obj);
        else EditorUtility.RevealInFinder(Absolute(unityPath));
    }

    void Cleanup(GameObject go) { if (go) DestroyImmediate(go); }
}
#endif
