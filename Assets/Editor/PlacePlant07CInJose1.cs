using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PlacePlant07CInJose1
{
    private const string ScenePath = "Assets/Scenes/jose1.unity";
    private const string PrefabPath = "Assets/Low Poly Nature Pack/Prefabs/Plants/Plant07C.prefab";
    private const string MaterialPath = "Assets/Low Poly Nature Pack/Materials/Atlas_Mat.mat";
    private static bool attempted;

    static PlacePlant07CInJose1()
    {
        EditorApplication.delayCall += PlaceIfJose1IsOpen;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (scene.path.Replace('\\', '/') == ScenePath)
            EditorApplication.delayCall += () => AlinearFloresExistentes(scene);
    }

    [MenuItem("Abeja RA/Colocar cinco flores Plant07C")]
    private static void PlaceFromMenu()
    {
        attempted = false;
        PlaceIfJose1IsOpen();
    }

    private static void PlaceIfJose1IsOpen()
    {
        if (attempted || EditorApplication.isPlaying) return;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path.Replace('\\', '/') != ScenePath) return;
        attempted = true;

        if (GameObject.Find("Flores_Plant07C") != null)
        {
            AlinearFloresExistentes(scene);
            Debug.Log("Ya existe el grupo Flores_Plant07C; no se duplicaron las flores.");
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        GameObject terreno = GameObject.Find("Terrain");
        MeshCollider mallaTerreno = terreno != null ? terreno.GetComponentInChildren<MeshCollider>() : null;
        if (prefab == null || material == null || mallaTerreno == null || !mallaTerreno.enabled)
        {
            Debug.LogError("No se pudo colocar Plant07C: falta el prefab, Atlas_Mat o la malla activa del terreno.");
            return;
        }

        GameObject grupo = new GameObject("Flores_Plant07C");
        Undo.RegisterCreatedObjectUndo(grupo, "Crear grupo de flores Plant07C");
        Undo.SetTransformParent(grupo.transform, terreno.transform, "Agrupar flores en el terreno");

        Vector3 centro = mallaTerreno.bounds.center;
        Vector3 ext = mallaTerreno.bounds.extents;
        Vector2[] posiciones =
        {
            new Vector2(-0.68f, -0.68f),
            new Vector2( 0.68f, -0.68f),
            new Vector2(-0.68f,  0.68f),
            new Vector2( 0.68f,  0.68f),
            new Vector2( 0.00f, -0.72f)
        };
        float[] giros = { 18f, 94f, 161f, 238f, 311f };
        for (int i = 0; i < posiciones.Length; i++)
        {
            Vector3 origen = new Vector3(centro.x + ext.x * posiciones[i].x, mallaTerreno.bounds.max.y + 300f, centro.z + ext.z * posiciones[i].y);
            Ray rayo = new Ray(origen, Vector3.down);
            if (!mallaTerreno.Raycast(rayo, out RaycastHit impacto, 1000f))
            {
                Debug.LogWarning("No encontré superficie del terreno para la flor " + (i + 1) + ".");
                continue;
            }

            GameObject flor = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(flor, "Colocar flor Plant07C");
            flor.name = "Plant07C_0" + (i + 1);
            flor.transform.position = impacto.point + Vector3.up * 0.04f;
            flor.transform.rotation = Quaternion.Euler(0f, giros[i], 0f);
            flor.transform.localScale = Vector3.one * 2.3f;
            Undo.SetTransformParent(flor.transform, grupo.transform, "Agrupar flores Plant07C");
            foreach (Renderer renderizador in flor.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materiales = renderizador.sharedMaterials;
                for (int j = 0; j < materiales.Length; j++) materiales[j] = material;
                renderizador.sharedMaterials = materiales;
            }
            AlinearBaseAlTerreno(flor.transform, terreno.GetComponent<Terrain>());
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Se colocaron cinco flores Plant07C sobre RoundedTerrainVisual con la textura Color Atlas del paquete Low Poly Nature Pack.");
    }

    private static void AlinearFloresExistentes(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || EditorApplication.isPlayingOrWillChangePlaymode) return;
        Terrain terreno = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            terreno = root.GetComponentInChildren<Terrain>(true);
            if (terreno != null) break;
        }
        if (terreno == null || terreno.terrainData == null) return;

        int ajustadas = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!EsInstanciaFlor(t.name)) continue;
                if (AlinearBaseAlTerreno(t, terreno)) ajustadas++;
            }
        }

        if (ajustadas > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Se asentaron " + ajustadas + " flores Plant07C en el suelo plano de jose1.");
        }
    }

    private static bool EsInstanciaFlor(string nombre)
    {
        return nombre.StartsWith("Plant07C_") && nombre.Length == 11 &&
               char.IsDigit(nombre[9]) && char.IsDigit(nombre[10]);
    }

    private static bool AlinearBaseAlTerreno(Transform flor, Terrain terreno)
    {
        if (flor == null || terreno == null || terreno.terrainData == null) return false;
        bool cambioLOD = false;
        foreach (Transform hijo in flor.GetComponentsInChildren<Transform>(true))
        {
            if (hijo == flor || !hijo.name.StartsWith("Plant07C_LOD")) continue;
            Vector3 local = hijo.localPosition;
            if (Mathf.Abs(local.y) < 0.005f) continue;
            local.y = 0f;
            hijo.localPosition = local;
            EditorUtility.SetDirty(hijo);
            cambioLOD = true;
        }

        Renderer[] renderizadores = flor.GetComponentsInChildren<Renderer>(true);
        bool encontro = false;
        Bounds bounds = default;
        foreach (Renderer renderizador in renderizadores)
        {
            if (!renderizador.enabled || !renderizador.gameObject.activeInHierarchy) continue;
            if (!encontro) { bounds = renderizador.bounds; encontro = true; }
            else bounds.Encapsulate(renderizador.bounds);
        }
        if (!encontro)
        {
            foreach (Renderer renderizador in renderizadores)
            {
                if (!renderizador.enabled) continue;
                if (!encontro) { bounds = renderizador.bounds; encontro = true; }
                else bounds.Encapsulate(renderizador.bounds);
            }
        }
        if (!encontro) return false;

        float suelo = terreno.SampleHeight(flor.position) + terreno.transform.position.y;
        float diferencia = suelo - bounds.min.y;
        if (Mathf.Abs(diferencia) >= 0.005f)
        {
            Undo.RecordObject(flor, "Alinear flor con el terreno");
            flor.position += Vector3.up * diferencia;
            EditorUtility.SetDirty(flor);
            Debug.Log($"Flor {flor.name}: base visual alineada con el suelo ({diferencia:0.00} m).", flor);
        }
        return cambioLOD || Mathf.Abs(diferencia) >= 0.005f;
    }
}
