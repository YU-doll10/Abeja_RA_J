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
            flor.transform.localScale = Vector3.one * 2f;
            Undo.SetTransformParent(flor.transform, grupo.transform, "Agrupar flores Plant07C");
            foreach (Renderer renderizador in flor.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materiales = renderizador.sharedMaterials;
                for (int j = 0; j < materiales.Length; j++) materiales[j] = material;
                renderizador.sharedMaterials = materiales;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Se colocaron cinco flores Plant07C sobre RoundedTerrainVisual con la textura Color Atlas del paquete Low Poly Nature Pack.");
    }
}
