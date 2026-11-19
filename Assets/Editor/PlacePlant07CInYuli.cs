using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PlacePlant07CInYuli
{
    private const string ScenePath = "Assets/Scenes/YULI Scene.unity";
    private const string PrefabPath = "Assets/Low Poly Nature Pack/Prefabs/Plants/Plant07C.prefab";
    private const string MaterialPath = "Assets/Low Poly Nature Pack/Materials/Atlas_Mat.mat";
    private const string GroupName = "Flores_Plant07C_YULI";
    private static bool attempted;

    static PlacePlant07CInYuli()
    {
        EditorApplication.delayCall += PlaceIfYuliIsOpen;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (Normalize(scene.path) == ScenePath)
            EditorApplication.delayCall += PlaceIfYuliIsOpen;
    }

    [MenuItem("Abeja RA/Colocar cinco flores Plant07C en YULI Scene")]
    private static void PlaceFromMenu()
    {
        attempted = false;
        PlaceIfYuliIsOpen();
    }

    public static void PlaceForYuliSceneBatch()
    {
        attempted = false;
        if (Normalize(SceneManager.GetActiveScene().path) != ScenePath)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        PlaceIfYuliIsOpen();
    }

    private static void PlaceIfYuliIsOpen()
    {
        if (attempted || EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        if (Normalize(scene.path) != ScenePath) return;
        attempted = true;

        if (GameObject.Find(GroupName) != null) return;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        Terrain terrain = FindTerrain(scene);
        if (prefab == null || material == null || terrain == null || terrain.terrainData == null)
        {
            Debug.LogError("No se pudieron colocar las flores Plant07C en YULI Scene: falta el prefab, Atlas_Mat o el Terrain.");
            return;
        }

        GameObject group = new GameObject(GroupName);
        SceneManager.MoveGameObjectToScene(group, scene);
        Undo.RegisterCreatedObjectUndo(group, "Crear flores Plant07C para YULI Scene");

        Vector2[] normalizedPositions =
        {
            new Vector2(0.28f, 0.28f),
            new Vector2(0.72f, 0.28f),
            new Vector2(0.28f, 0.72f),
            new Vector2(0.72f, 0.72f),
            new Vector2(0.50f, 0.50f)
        };
        float[] rotations = { 18f, 94f, 161f, 238f, 311f };
        Vector3 size = terrain.terrainData.size;

        for (int i = 0; i < normalizedPositions.Length; i++)
        {
            Vector3 local = new Vector3(normalizedPositions[i].x * size.x, 0f, normalizedPositions[i].y * size.z);
            Vector3 position = terrain.transform.TransformPoint(local);
            position.y = terrain.SampleHeight(position) + terrain.transform.position.y;

            GameObject flower = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(flower, "Colocar flor Plant07C");
            flower.name = "Plant07C_0" + (i + 1);
            flower.transform.SetParent(group.transform, true);
            flower.transform.position = position;
            flower.transform.rotation = Quaternion.Euler(0f, rotations[i], 0f);
            flower.transform.localScale = Vector3.one * 2.3f;

            FlorRecolectable collectible = flower.GetComponent<FlorRecolectable>();
            if (collectible == null) flower.AddComponent<FlorRecolectable>();
            CapsuleCollider trigger = flower.GetComponent<CapsuleCollider>();
            if (trigger == null) trigger = flower.AddComponent<CapsuleCollider>();
            trigger.isTrigger = true;
            trigger.direction = 1;
            trigger.center = new Vector3(0f, 0.7f, 0f);
            trigger.radius = 0.32f;
            trigger.height = 1.4f;

            foreach (MeshCollider meshCollider in flower.GetComponentsInChildren<MeshCollider>(true))
                meshCollider.enabled = false;

            foreach (Renderer renderer in flower.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int j = 0; j < materials.Length; j++) materials[j] = material;
                renderer.sharedMaterials = materials;
            }

            AlignFlowerToTerrain(flower.transform, terrain);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Se colocaron cinco flores Plant07C recolectables en YULI Scene.");
    }

    private static Terrain FindTerrain(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Terrain terrain = root.GetComponentInChildren<Terrain>(true);
            if (terrain != null) return terrain;
        }
        return null;
    }

    private static void AlignFlowerToTerrain(Transform flower, Terrain terrain)
    {
        Renderer[] renderers = flower.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        Bounds bounds = default;
        foreach (Renderer renderer in renderers)
        {
            if (!renderer.enabled) continue;
            if (!found) { bounds = renderer.bounds; found = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        if (!found) return;

        float ground = terrain.SampleHeight(flower.position) + terrain.transform.position.y;
        flower.position += Vector3.up * (ground - bounds.min.y);
    }

    private static string Normalize(string path) => path.Replace('\\', '/');
}
