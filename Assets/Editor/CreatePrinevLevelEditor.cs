using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CreatePrinevLevelEditor
{
    private const string SourceScene = "Assets/Scenes/jose1.unity";
    private const string TargetScene = "Assets/Scenes/prinev.unity";
    private const string MaplePrefab = "Assets/Waldemarst/JapaneseGardenPackage/Prefabs/JapaneseMapleTree_D.prefab";

    static CreatePrinevLevelEditor()
    {
        EditorApplication.delayCall += CreateIfMissing;
    }

    private static void CreateIfMissing()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneAssetExists(TargetScene)) return;
        CreatePrinev();
    }

    [MenuItem("Abeja RA/Crear nivel prinev (3 flores, fácil)")]
    public static void CreatePrinev()
    {
        if (!SceneAssetExists(TargetScene) && !AssetDatabase.CopyAsset(SourceScene, TargetScene))
            throw new InvalidOperationException("No se pudo crear prinev copiando la escena jose1.");
        AssetDatabase.Refresh();

        Scene scene = EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);
        SceneManager.SetActiveScene(scene);

        MovimientoAbeja3D jugador = FindInScene<MovimientoAbeja3D>(scene).FirstOrDefault(m => m.controlJugador);
        MovimientoAbejaEnemiga enemigo = FindInScene<MovimientoAbejaEnemiga>(scene).FirstOrDefault();
        MeshCollider suelo = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<MeshCollider>(true))
            .FirstOrDefault(c => c.enabled && c.name == "RoundedTerrainVisual");
        FlorRecolectable[] flores = FindInScene<FlorRecolectable>(scene)
            .OrderBy(f => f.name, StringComparer.Ordinal).ToArray();

        if (jugador == null || enemigo == null || suelo == null || flores.Length < 3)
            throw new InvalidOperationException("prinev necesita la abeja del jugador, el enemigo, el terreno redondeado y al menos tres flores de jose1.");

        GameObject grupoFlores = GameObject.Find("Flores_Plant07C");
        for (int i = flores.Length - 1; i >= 3; i--)
            UnityEngine.Object.DestroyImmediate(flores[i].gameObject);

        Vector3 centro = suelo.bounds.center;
        Vector3 haciaCentro = Vector3.ProjectOnPlane(centro - jugador.transform.position, Vector3.up).normalized;
        if (haciaCentro.sqrMagnitude < 0.001f) haciaCentro = jugador.transform.forward;
        Vector3 lateral = Vector3.Cross(Vector3.up, haciaCentro).normalized;
        Vector3 origen = jugador.transform.position;
        Vector3[] puntosFlores =
        {
            origen + haciaCentro * 10f,
            origen + haciaCentro * 22f + lateral * 5f,
            origen + haciaCentro * 34f - lateral * 5f
        };
        for (int i = 0; i < 3; i++)
        {
            flores[i].name = "Plant07C_0" + (i + 1);
            Vector3 point = DentroDeTerreno(puntosFlores[i], suelo.bounds);
            if (TryRaycastSurface(suelo, point, out Vector3 surface))
                flores[i].transform.position = surface + Vector3.up * 0.04f;
            else
                Debug.LogWarning("No se encontró superficie para Plant07C_0" + (i + 1) + "; se conservó su posición anterior.");
        }

        enemigo.velocidad = 3.5f;
        enemigo.velocidadGiro = 115f;
        enemigo.distanciaMinima = 1.2f;
        enemigo.cantidadAlMorir = 0;
        enemigo.vidasEnemigo = 5;
        enemigo.objetivo = jugador.transform;
        jugador.velocidad = 19f;
        jugador.radioEsquinasTerreno = 0f;

        CamaraSeguir camara = FindInScene<CamaraSeguir>(scene).FirstOrDefault();
        if (camara != null) camara.objetivo = jugador.transform;

        AddGardenTrees(scene, suelo);
        AddPrinevToBuildSettings();

        if (grupoFlores != null) EditorUtility.SetDirty(grupoFlores);
        EditorUtility.SetDirty(jugador);
        EditorUtility.SetDirty(enemigo);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene, TargetScene);
        Debug.Log("prinev quedó creado con tres flores, abeja rápida, enemigo lento, cámara de seguimiento y árboles del jardín YULI.");
    }

    private static System.Collections.Generic.IEnumerable<T> FindInScene<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));
    }

    private static Vector3 DentroDeTerreno(Vector3 point, Bounds bounds)
    {
        return new Vector3(
            Mathf.Clamp(point.x, bounds.min.x + 3f, bounds.max.x - 3f),
            bounds.max.y + 100f,
            Mathf.Clamp(point.z, bounds.min.z + 3f, bounds.max.z - 3f));
    }

    private static bool TryRaycastSurface(MeshCollider surface, Vector3 point, out Vector3 hitPoint)
    {
        Ray ray = new Ray(point, Vector3.down);
        if (surface.Raycast(ray, out RaycastHit hit, 1000f))
        {
            hitPoint = hit.point;
            return true;
        }
        hitPoint = default;
        return false;
    }

    private static void AddGardenTrees(Scene scene, MeshCollider ground)
    {
        if (scene.GetRootGameObjects().Any(root => root.name == "Arboles_Jardin_YULI")) return;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MaplePrefab);
        if (prefab == null)
        {
            Debug.LogWarning("No se encontró el prefab JapaneseMapleTree_D; prinev se creó sin esos árboles.");
            return;
        }

        GameObject group = new GameObject("Arboles_Jardin_YULI");
        SceneManager.MoveGameObjectToScene(group, scene);
        Vector2[] positions =
        {
            new Vector2(-0.72f, -0.72f), new Vector2(0.72f, -0.72f),
            new Vector2(-0.72f, 0.72f), new Vector2(0.72f, 0.72f)
        };
        Bounds bounds = ground.bounds;
        for (int i = 0; i < positions.Length; i++)
        {
            Vector3 point = new Vector3(
                bounds.center.x + bounds.extents.x * positions[i].x,
                bounds.max.y + 100f,
                bounds.center.z + bounds.extents.z * positions[i].y);
            if (!TryRaycastSurface(ground, point, out Vector3 hit)) continue;
            GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            tree.name = "Arbol_Japones_0" + (i + 1);
            tree.transform.SetParent(group.transform, true);
            tree.transform.position = hit;
            tree.transform.rotation = Quaternion.Euler(0f, i * 73f, 0f);
            tree.transform.localScale = Vector3.one * 0.8f;
        }
    }

    private static void AddPrinevToBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes
            .Where(s => AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path) != null)
            .ToList();
        foreach (string path in new[] { "Assets/Scenes/YULI Scene.unity", "Assets/Scenes/tercerniv.unity", TargetScene })
        {
            if (scenes.All(s => s.path != path)) scenes.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static bool SceneAssetExists(string path)
    {
        return AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null;
    }
}
