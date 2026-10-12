#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Tool editor: menampilkan struktur game di Hierarchy supaya bisa dilihat & dipelajari.
// Isi game sendiri dibangun OTOMATIS dari kode saat Play (bukan disimpan di scene),
// jadi scene sengaja kosong dan tidak ada referensi yang bisa "missing".
public static class GameTools
{
    const string SceneDir = "Assets/Scenes";
    const string ScenePath = "Assets/Scenes/Main.unity";
    const string PreviewName = "PREVIEW_World";
    const string AutoPref = "SEAChan.AutoPreview";

    [InitializeOnLoadMethod]
    static void Hook()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.delayCall += InitialBuild;
    }

    // pastikan struktur pratinjau selalu dibersihkan sebelum masuk Play mode
    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode) ClearPreview();
    }

    static bool AutoPreview
    {
        get { return EditorPrefs.GetBool(AutoPref, true); }
        set { EditorPrefs.SetBool(AutoPref, value); }
    }

    static void InitialBuild()
    {
        if (!AutoPreview || EditorApplication.isPlayingOrWillChangePlaymode) return;
        BuildPreview();
    }

    static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (!AutoPreview || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (scene.path != ScenePath) return;
        EditorApplication.delayCall += BuildPreview;
    }

    [MenuItem("SEA Chan/1. Buka Scene Main", false, 1)]
    public static void OpenMainScene()
    {
        ProjectSetup.EnsureScene();

        if (!File.Exists(ScenePath))
        {
            Debug.LogError("[SEA Chan] Scene utama gagal dibuat. Coba simpan scene yang terbuka dulu " +
                           "(File > Save As), lalu jalankan menu ini lagi.");
            return;
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Debug.Log("[SEA Chan] Scene dibuka: " + ScenePath + " — tekan Play untuk menjalankan game.");
    }

    // Build & Run langsung dari Unity, tanpa perlu atur Build Settings manual.
    [MenuItem("SEA Chan/4. Build dan Run (Windows)", false, 4)]
    public static void BuildAndRun()
    {
        ProjectSetup.EnsureScene();

        string root = Directory.GetParent(Application.dataPath).FullName;
        string dir = Path.Combine(root, "Build");
        Directory.CreateDirectory(dir);
        string exe = Path.Combine(dir, "SEAChanAdventure.exe");

        var opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = exe,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.AutoRunPlayer
        };

        var report = BuildPipeline.BuildPlayer(opts);
        var sum = report.summary;

        if (sum.result == BuildResult.Succeeded)
            Debug.Log("[SEA Chan] Build BERHASIL -> " + exe);
        else
            Debug.LogError("[SEA Chan] Build GAGAL: " + sum.result + " (" + sum.totalErrors + " error)");
    }

    [MenuItem("SEA Chan/2. Tampilkan Struktur di Hierarchy", false, 2)]
    public static void BuildPreview()
    {
        ClearPreview();

        var root = new GameObject(PreviewName);
        GameBootstrap.BuildStructurePreview(root.transform);
        SetDontSave(root); // tidak disimpan ke scene & hilang saat Play

        Selection.activeGameObject = root;
        if (!Application.isBatchMode) EditorGUIUtility.PingObject(root);

        Debug.Log("[SEA Chan] Struktur game ditampilkan di Hierarchy (" + PreviewName + "). " +
                  "Pelajari susunan objek & komponennya; saat Play, game dibangun ulang oleh kode.");
    }

    [MenuItem("SEA Chan/3. Hapus Struktur Pratinjau", false, 3)]
    public static void ClearPreview()
    {
        var existing = GameObject.Find(PreviewName);
        while (existing != null)
        {
            Object.DestroyImmediate(existing);
            existing = GameObject.Find(PreviewName);
        }
    }

    [MenuItem("SEA Chan/6. Buat Scene Authoring (gampang diedit)", false, 6)]
    public static void CreateAuthoringScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        const string path = "Assets/Scenes/Authoring.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // struktur game yang bisa diedit bebas di Hierarchy / Inspector
        var world = GameBootstrap.BuildStructurePreview(null);
        world.name = GameBootstrap.AuthoringRootName;
        world.transform.SetParent(null);

        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(scene, path);
        AssetDatabase.Refresh();

        Debug.Log("[SEA Chan] Scene authoring dibuat: " + path +
                  " — tekan Play di scene INI. Nilai yang kamu ubah di Inspector " +
                  "(Move Speed, Pickup Radius, Dash, posisi Player) akan dipakai game.");
    }

    [MenuItem("SEA Chan/Tampilkan otomatis di Hierarchy", false, 20)]
    static void ToggleAuto()
    {
        AutoPreview = !AutoPreview;
        Debug.Log("[SEA Chan] Pratinjau otomatis di Hierarchy: " + (AutoPreview ? "AKTIF" : "MATI"));
        if (AutoPreview) BuildPreview();
        else ClearPreview();
    }

    [MenuItem("SEA Chan/Tampilkan otomatis di Hierarchy", true)]
    static bool ToggleAutoValidate()
    {
        Menu.SetChecked("SEA Chan/Tampilkan otomatis di Hierarchy", AutoPreview);
        return true;
    }

    [MenuItem("SEA Chan/Panduan: di mana mengubah nilai", false, 40)]
    public static void Info()
    {
        EditorUtility.DisplayDialog(
            "Cara mengedit SEA Chan Adventure",
            "Game ini Unity + C# (BUKAN Python). Isi game dibuat otomatis dari kode saat Play,\n" +
            "jadi scene memang kosong dan tidak ada objek yang ditulis manual — ini disengaja\n" +
            "supaya tidak ada referensi 'missing'.\n\n" +
            "Objek & komponennya bisa dilihat di Hierarchy lewat menu\n" +
            "'SEA Chan > 2. Tampilkan Struktur di Hierarchy' (atau otomatis saat editor dibuka).\n\n" +
            "Untuk mengubah permainan, edit nilainya di Assets/Scripts:\n" +
            "• Gerak & HP player ..... Weapons.cs  (PlayerController)\n" +
            "• Damage/kecepatan senjata  Weapons.cs\n" +
            "• Jumlah & kekuatan musuh . Enemies.cs (EnemySpawner)\n" +
            "• Peluang drop kotak obat . MonsterArt.cs (HealthDropChance)\n" +
            "• Tampilan & isi UI ....... UIManager.cs\n" +
            "• Lantai (grid) ............ GroundLayer.cs\n" +
            "• Dinding / obstacle ...... WallLayer.cs\n" +
            "• Dekorasi ................. PropsLayer.cs\n" +
            "• Gambar & audio ........... Assets/Resources",
            "Mengerti");
    }

    static void SetDontSave(GameObject go)
    {
        go.hideFlags = HideFlags.DontSave;
        foreach (Transform t in go.transform)
            SetDontSave(t.gameObject);
    }
}
#endif
