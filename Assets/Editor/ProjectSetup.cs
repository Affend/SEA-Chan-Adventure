#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Menyiapkan hal-hal kecil supaya project langsung bisa dipakai:
//   1) membuat scene Assets/Scenes/Main.unity (kalau belum ada)
//   2) mendaftarkannya ke Build Settings (WAJIB supaya Build & Run jalan)
//   3) memastikan "Active Input Handling" mengizinkan input lama (Input Manager)
[InitializeOnLoad]
public static class ProjectSetup
{
    const string SceneDir = "Assets/Scenes";
    const string ScenePath = "Assets/Scenes/Main.unity";

    static ProjectSetup()
    {
        EditorApplication.delayCall += Run;
    }

    static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        try
        {
            EnsureActiveInput();
            EnsureScene();
            SetNaming();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[SEA Chan] ProjectSetup dilewati: " + e.Message);
        }
    }

    // Pastikan folder "Assets/Scenes" dikenali Unity, lalu buat scene-nya.
    // Aman walau scene yang sedang terbuka masih "Untitled" (belum disimpan).
    public static void EnsureScene()
    {
        if (!AssetDatabase.IsValidFolder(SceneDir))
        {
            if (!AssetDatabase.IsValidFolder("Assets"))
            {
                Directory.CreateDirectory("Assets");
                AssetDatabase.Refresh();
            }
            AssetDatabase.CreateFolder("Assets", "Scenes");
            AssetDatabase.Refresh();
        }

        if (!File.Exists(ScenePath))
        {
            var active = SceneManager.GetActiveScene();

            if (!string.IsNullOrEmpty(active.path))
            {
                // scene aktif sudah punya file -> aman dibuat additive
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                EditorSceneManager.SaveScene(scene, ScenePath);
                EditorSceneManager.CloseScene(scene, true);
            }
            else
            {
                // scene aktif masih "Untitled" -> minta simpan dulu, lalu jadikan scene utama
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }

            AssetDatabase.Refresh();
            Debug.Log("[SEA Chan] Scene dibuat: " + ScenePath);
        }

        RegisterScene();
    }

    // Daftarkan scene utama ke Build Settings (tanpa ini, Build jadi kosong).
    public static void RegisterScene()
    {
        var scenes = EditorBuildSettings.scenes;
        for (int i = 0; i < scenes.Length; i++)
            if (scenes[i].path == ScenePath) return;

        var list = new List<EditorBuildSettingsScene>(scenes);
        list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log("[SEA Chan] Scene didaftarkan ke Build Settings.");
    }

    static void EnsureActiveInput()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (assets == null || assets.Length == 0) return;

        var so = new SerializedObject(assets[0]);
        var prop = so.FindProperty("activeInputHandler");
        if (prop == null) return;

        // 0 = Input Manager (Old), 1 = Input System (New), 2 = Both
        if (prop.intValue == 1)
        {
            prop.intValue = 2;
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log("[SEA Chan] Active Input Handling diubah ke 'Both'. " +
                      "Jika kontrol belum jalan, restart Unity sekali.");
        }
    }

    static void SetNaming()
    {
        if (PlayerSettings.productName != "SEA Chan Adventure")
            PlayerSettings.productName = "SEA Chan Adventure";
        if (PlayerSettings.companyName == "DefaultCompany")
            PlayerSettings.companyName = "SEA GEN 11";
    }
}
#endif
