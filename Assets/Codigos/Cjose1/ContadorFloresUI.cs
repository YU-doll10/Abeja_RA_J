using UnityEngine;

public class ContadorFloresUI : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CrearContador()
    {
        // These levels already own a collectible counter; keep the Jose1 HUD in its own levels.
        if (FindFirstObjectByType<AbejaRA.BeeGardenGame>(FindObjectsInactive.Include) != null ||
            FindFirstObjectByType<AbejaRA.FinalBeeTrial>(FindObjectsInactive.Include) != null) return;
        if (FindFirstObjectByType<ContadorFloresUI>() != null) return;
        GameObject hud = new GameObject("ContadorFloresUI");
        hud.AddComponent<ContadorFloresUI>();
    }

    private void OnGUI()
    {
        FlorRecolectable[] flores = FindObjectsByType<FlorRecolectable>(FindObjectsSortMode.None);
        int restantes = 0;
        foreach (FlorRecolectable flor in flores)
        {
            if (flor != null && flor.isActiveAndEnabled && flor.gameObject.activeInHierarchy) restantes++;
        }

        GUIStyle estilo = new GUIStyle(GUI.skin.box)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(12, 8, 4, 4)
        };
        GUI.Box(new Rect(12, 96, 250, 36), "Flores por recoger: " + restantes, estilo);
    }
}
