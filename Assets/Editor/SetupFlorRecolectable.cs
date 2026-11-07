using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SetupFlorRecolectable
{
    private const string PrefabPath = "Assets/Low Poly Nature Pack/Prefabs/Plants/Plant07C.prefab";
    private static bool enCola;

    static SetupFlorRecolectable()
    {
        if (!enCola)
        {
            enCola = true;
            EditorApplication.delayCall += ConfigurarPrefab;
        }
    }

    private static void ConfigurarPrefab()
    {
        enCola = false;
        if (EditorApplication.isPlaying) return;

        GameObject raiz = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (raiz == null)
        {
            Debug.LogError("No se encontró el prefab Plant07C.");
            return;
        }

        FlorRecolectable recolectable = raiz.GetComponent<FlorRecolectable>();
        CapsuleCollider trigger = raiz.GetComponent<CapsuleCollider>();
        bool cambio = recolectable == null || trigger == null || !trigger.isTrigger;
        if (recolectable == null) raiz.AddComponent<FlorRecolectable>();
        if (trigger == null) trigger = raiz.AddComponent<CapsuleCollider>();
        trigger.isTrigger = true;
        trigger.direction = 1;
        trigger.center = new Vector3(0f, 0.7f, 0f);
        trigger.radius = 0.32f;
        trigger.height = 1.4f;

        foreach (MeshCollider malla in raiz.GetComponentsInChildren<MeshCollider>(true))
        {
            if (malla.enabled) cambio = true;
            malla.enabled = false;
        }

        if (cambio)
        {
            PrefabUtility.SaveAsPrefabAsset(raiz, PrefabPath);
            Debug.Log("Plant07C ahora se recoge al tocarla con la abeja del jugador.");
        }
        PrefabUtility.UnloadPrefabContents(raiz);
    }
}
