using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CrearTercerNivelEditor
{
    const string SceneSource = "Assets/Scenes/jose1.unity";
    const string SceneTarget = "Assets/Scenes/tercerniv.unity";
    const string TerrainTarget = "Assets/Scenes/tercerniv_Terrain.asset";
    const string TerrainMaterialTarget = "Assets/Scenes/tercerniv_TerrainLit.mat";
    const string PathMaterialTarget = "Assets/Scenes/tercerniv_Camino.mat";
    const string GrassLayerTarget = "Assets/Scenes/tercerniv_Grass.terrainlayer";
    const string SoilLayerTarget = "Assets/Scenes/tercerniv_Soil.terrainlayer";
    const string MazeMapTarget = "Assets/Scenes/tercerniv_MazeMap.asset";
    const string MazeLayerTarget = "Assets/Scenes/tercerniv_Maze.terrainlayer";

    [MenuItem("Abeja RA/Crear laberinto como referencia")]
    public static void CrearLaberintoReferencia()
    {
        Scene scene = SceneManager.GetSceneByPath(SceneTarget);
        if (!scene.IsValid() || !scene.isLoaded)
            scene = EditorSceneManager.OpenScene(SceneTarget, OpenSceneMode.Single);

        Terrain terrain = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogError("No se encontró el terreno de tercerniv.");
            return;
        }

        const int mazeCells = 17;
        const int textureSize = 1024;
        bool[,] maze = GenerateMaze(mazeCells, 24019);
        List<Vector2Int> solution = SolveMaze(maze);
        if (solution.Count < 6)
        {
            Debug.LogError("No se pudo generar una ruta conectada para el laberinto.");
            return;
        }

        Color32[] pixels = DrawMazeTexture(maze, textureSize, 24019);
        Texture2D map = AssetDatabase.LoadAssetAtPath<Texture2D>(MazeMapTarget);
        if (map == null)
        {
            map = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false, false)
            {
                name = "tercerniv Maze Map",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 4
            };
            map.SetPixels32(pixels);
            map.Apply(false, false);
            AssetDatabase.CreateAsset(map, MazeMapTarget);
        }
        else
        {
            map.Reinitialize(textureSize, textureSize, TextureFormat.RGBA32, false);
            map.SetPixels32(pixels);
            map.Apply(false, false);
            EditorUtility.SetDirty(map);
        }

        TerrainLayer mazeLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(MazeLayerTarget);
        if (mazeLayer == null)
        {
            mazeLayer = new TerrainLayer { name = "tercerniv Laberinto Verde y Arena" };
            AssetDatabase.CreateAsset(mazeLayer, MazeLayerTarget);
        }
        mazeLayer.diffuseTexture = map;
        mazeLayer.tileSize = new Vector2(400f, 400f);
        EditorUtility.SetDirty(mazeLayer);

        TerrainData data = terrain.terrainData;
        data.size = new Vector3(400f, 10f, 400f);
        data.SetHeights(0, 0, new float[data.heightmapResolution, data.heightmapResolution]);
        data.terrainLayers = new[] { mazeLayer };
        int alpha = data.alphamapResolution;
        float[,,] weights = new float[alpha, alpha, 1];
        for (int z = 0; z < alpha; z++)
        for (int x = 0; x < alpha; x++) weights[z, x, 0] = 1f;
        data.SetAlphamaps(0, 0, weights);
        terrain.drawHeightmap = true;
        terrain.Flush();
        TerrainCollider terrainCollider = terrain.GetComponent<TerrainCollider>();
        if (terrainCollider == null) terrainCollider = terrain.gameObject.AddComponent<TerrainCollider>();
        terrainCollider.terrainData = data;

        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == "Camino_Laberinto") UnityEngine.Object.DestroyImmediate(root);
        GameObject mazeRoot = new GameObject("Camino_Laberinto");
        SceneManager.MoveGameObjectToScene(mazeRoot, scene);

        MovimientoAbeja3D playerControl = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<MovimientoAbeja3D>(true))
            .FirstOrDefault(c => c.controlJugador);
        MovimientoAbejaEnemiga enemyControl = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<MovimientoAbejaEnemiga>(true)).FirstOrDefault();
        if (playerControl == null || enemyControl == null)
        {
            Debug.LogError("No se encontraron los controles de las abejas en tercerniv.");
            return;
        }

        List<Vector3> routeWorld = new List<Vector3>(solution.Count);
        float margin = 8f;
        float gridUnit = (400f - margin * 2f) / (maze.GetLength(0) - 1);
        foreach (Vector2Int p in solution)
            routeWorld.Add(new Vector3(margin + p.x * gridUnit, 0.1f, margin + p.y * gridUnit));
        playerControl.transform.position = new Vector3(routeWorld[0].x, 5f, routeWorld[0].z);
        playerControl.radioEsquinasTerreno = 0f;
        int enemyIndex = Mathf.Min(5, routeWorld.Count - 1);
        enemyControl.transform.position = new Vector3(routeWorld[enemyIndex].x, 5f, routeWorld[enemyIndex].z);
        enemyControl.objetivo = playerControl.transform;

        FlorRecolectable[] flowers = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<FlorRecolectable>(true)).ToArray();
        Array.Sort(flowers, (a, b) => string.CompareOrdinal(a.name, b.name));
        if (flowers.Length != 5)
        {
            Debug.LogError("Se esperaban cinco flores; se encontraron " + flowers.Length + ".");
            return;
        }
        int[] fractions = { 18, 34, 50, 66, 82 };
        for (int i = 0; i < flowers.Length; i++)
        {
            int index = Mathf.Clamp((routeWorld.Count - 1) * fractions[i] / 100, 0, routeWorld.Count - 1);
            flowers[i].transform.position = routeWorld[index];
        }

        Camera camera = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true))
            .FirstOrDefault(c => c.CompareTag("MainCamera"));
        if (camera != null)
        {
            CamaraSeguir follow = camera.GetComponent<CamaraSeguir>();
            if (follow == null) follow = camera.gameObject.AddComponent<CamaraSeguir>();
            follow.objetivo = playerControl.transform;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene, SceneTarget);
        SceneView view = SceneView.lastActiveSceneView;
        if (view != null)
        {
            view.LookAt(new Vector3(200f, 0f, 200f), Quaternion.Euler(55f, 0f, 0f), 340f);
            view.Repaint();
        }
        Debug.Log("Laberinto de referencia guardado: pasillos arena, bosque verde, cinco flores y ambas abejas en la ruta.");
    }

    [MenuItem("Abeja RA/Restaurar Grass.png en tercerniv")]
    public static void RestaurarGrassEnTercerNivel()
    {
        Scene scene = SceneManager.GetSceneByPath(SceneTarget);
        if (!scene.IsValid() || !scene.isLoaded)
            scene = EditorSceneManager.OpenScene(SceneTarget, OpenSceneMode.Single);

        Terrain terrain = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
        TerrainData data = terrain != null ? terrain.terrainData : null;
        Texture2D map = AssetDatabase.LoadAssetAtPath<Texture2D>(MazeMapTarget);
        TerrainLayer mazeLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(MazeLayerTarget);
        string grassPath = "Assets/Low Poly Nature Pack/Textures/Grass.png";
        TextureImporter importer = AssetImporter.GetAtPath(grassPath) as TextureImporter;
        if (data == null || map == null || mazeLayer == null || importer == null)
        {
            Debug.LogError("No se encontró el terreno, el mapa del laberinto o Grass.png para restaurar la textura.");
            return;
        }

        bool wasReadable = importer.isReadable;
        if (!wasReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        Texture2D grass = AssetDatabase.LoadAssetAtPath<Texture2D>(grassPath);
        Color32[] grassPixels = grass.GetPixels32();
        int grassWidth = grass.width;
        int grassHeight = grass.height;
        Color32[] mapPixels = map.GetPixels32();
        int mapSize = map.width;
        const float grassTileWorldSize = 28f;
        for (int y = 0; y < mapSize; y++)
        for (int x = 0; x < mapSize; x++)
        {
            int index = y * mapSize + x;
            Color32 original = mapPixels[index];
            bool greenGround =
                (original.r == 9 && original.g == 137 && original.b == 49) ||
                (original.r == 12 && original.g == 148 && original.b == 52) ||
                (original.r == 17 && original.g == 158 && original.b == 55);
            if (!greenGround) continue;

            float u = (x / (float)mapSize * data.size.x / grassTileWorldSize) % 1f;
            float v = (y / (float)mapSize * data.size.z / grassTileWorldSize) % 1f;
            int gx = Mathf.Clamp(Mathf.FloorToInt(u * grassWidth), 0, grassWidth - 1);
            int gy = Mathf.Clamp(Mathf.FloorToInt(v * grassHeight), 0, grassHeight - 1);
            mapPixels[index] = grassPixels[gy * grassWidth + gx];
        }

        map.SetPixels32(mapPixels);
        map.Apply(false, false);
        EditorUtility.SetDirty(map);
        mazeLayer.diffuseTexture = map;
        mazeLayer.tileSize = new Vector2(data.size.x, data.size.z);
        EditorUtility.SetDirty(mazeLayer);
        if (!wasReadable)
        {
            importer.isReadable = false;
            importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, SceneTarget);
        terrain.Flush();
        Debug.Log("Grass.png restaurada en el fondo verde de tercerniv; los caminos arena y los árboles del laberinto se conservaron.");
    }

    static bool[,] GenerateMaze(int cells, int seed)
    {
        int size = cells * 2 + 1;
        bool[,] open = new bool[size, size];
        bool[,] visited = new bool[cells, cells];
        System.Random random = new System.Random(seed);
        List<Vector2Int> stack = new List<Vector2Int> { new Vector2Int(0, 0) };
        visited[0, 0] = true;
        open[1, 1] = true;
        Vector2Int[] directions = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
        while (stack.Count > 0)
        {
            Vector2Int current = stack[stack.Count - 1];
            List<Vector2Int> choices = new List<Vector2Int>();
            foreach (Vector2Int direction in directions)
            {
                Vector2Int next = current + direction;
                if (next.x >= 0 && next.y >= 0 && next.x < cells && next.y < cells && !visited[next.x, next.y])
                    choices.Add(direction);
            }
            if (choices.Count == 0)
            {
                stack.RemoveAt(stack.Count - 1);
                continue;
            }
            Vector2Int step = choices[random.Next(choices.Count)];
            Vector2Int nextCell = current + step;
            int x = 1 + current.x * 2, y = 1 + current.y * 2;
            open[x + step.x, y + step.y] = true;
            open[1 + nextCell.x * 2, 1 + nextCell.y * 2] = true;
            visited[nextCell.x, nextCell.y] = true;
            stack.Add(nextCell);
        }
        return open;
    }

    static List<Vector2Int> SolveMaze(bool[,] open)
    {
        int size = open.GetLength(0);
        Vector2Int start = new Vector2Int(1, 1);
        Vector2Int goal = new Vector2Int(size - 2, size - 2);
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        bool[,] seen = new bool[size, size];
        Vector2Int[,] previous = new Vector2Int[size, size];
        queue.Enqueue(start);
        seen[start.x, start.y] = true;
        Vector2Int[] dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
        while (queue.Count > 0)
        {
            Vector2Int p = queue.Dequeue();
            if (p == goal) break;
            foreach (Vector2Int dir in dirs)
            {
                Vector2Int next = p + dir;
                if (next.x < 0 || next.y < 0 || next.x >= size || next.y >= size || !open[next.x, next.y] || seen[next.x, next.y]) continue;
                seen[next.x, next.y] = true;
                previous[next.x, next.y] = p;
                queue.Enqueue(next);
            }
        }
        if (!seen[goal.x, goal.y]) return new List<Vector2Int>();
        List<Vector2Int> path = new List<Vector2Int>();
        for (Vector2Int p = goal; p != start; p = previous[p.x, p.y]) path.Add(p);
        path.Add(start);
        path.Reverse();
        return path;
    }

    static Color32[] DrawMazeTexture(bool[,] open, int textureSize, int seed)
    {
        int size = open.GetLength(0);
        int margin = 20;
        float cell = (textureSize - margin * 2f) / size;
        Color32[] pixels = new Color32[textureSize * textureSize];
        System.Random random = new System.Random(seed + 1);
        Color32[] greens = { new Color32(9, 137, 49, 255), new Color32(12, 148, 52, 255), new Color32(17, 158, 55, 255) };
        for (int cy = 0; cy < textureSize; cy += 16)
        for (int cx = 0; cx < textureSize; cx += 16)
        {
            Color32 color = greens[random.Next(greens.Length)];
            FillRect(pixels, textureSize, cx, cy, 16, 16, color);
        }

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            if (!open[x, y]) DrawTree(pixels, textureSize, margin + (x + 0.5f) * cell, margin + (y + 0.5f) * cell, cell, (x * 31 + y * 71 + seed) % 7);

        int road = Mathf.RoundToInt(cell * 0.78f);
        Color32 edge = new Color32(205, 163, 62, 255);
        Color32 sand = new Color32(250, 222, 109, 255);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float px = margin + (x + 0.5f) * cell;
            float py = margin + (y + 0.5f) * cell;
            if (open[x, y])
            {
                DrawCircle(pixels, textureSize, Mathf.RoundToInt(px), Mathf.RoundToInt(py), road / 2 + 3, edge);
                DrawCircle(pixels, textureSize, Mathf.RoundToInt(px), Mathf.RoundToInt(py), road / 2, sand);
                if (x + 1 < size && open[x + 1, y]) DrawRoad(pixels, textureSize, px, py, px + cell, py, road, edge, sand);
                if (y + 1 < size && open[x, y + 1]) DrawRoad(pixels, textureSize, px, py, px, py + cell, road, edge, sand);
            }
        }
        return pixels;
    }

    static void DrawRoad(Color32[] pixels, int size, float x1, float y1, float x2, float y2, int width, Color32 edge, Color32 sand)
    {
        int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(new Vector2(x1, y1), new Vector2(x2, y2))));
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(x1, x2, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(y1, y2, t));
            DrawCircle(pixels, size, x, y, width / 2 + 3, edge);
            DrawCircle(pixels, size, x, y, width / 2, sand);
        }
    }

    static void DrawTree(Color32[] pixels, int size, float cx, float cy, float cell, int variant)
    {
        int x = Mathf.RoundToInt(cx), y = Mathf.RoundToInt(cy);
        int radius = Mathf.Max(4, Mathf.RoundToInt(cell * 0.30f));
        Color32 shadow = new Color32(6, 94, 36, 255);
        Color32[] leaves = { new Color32(48, 170, 46, 255), new Color32(65, 185, 50, 255), new Color32(39, 157, 42, 255) };
        Color32 trunk = new Color32(121, 83, 42, 255);
        FillRect(pixels, size, x - 2, y - radius / 2, 5, radius, trunk);
        DrawCircle(pixels, size, x, y + 2, radius, shadow);
        DrawCircle(pixels, size, x - radius / 3, y + radius / 4, radius * 2 / 3, leaves[variant % leaves.Length]);
        DrawCircle(pixels, size, x + radius / 3, y + radius / 4, radius * 2 / 3, leaves[(variant + 1) % leaves.Length]);
        DrawCircle(pixels, size, x, y + radius / 2, radius * 2 / 3, leaves[(variant + 2) % leaves.Length]);
        DrawCircle(pixels, size, x - radius / 3, y + radius / 2, Mathf.Max(2, radius / 5), new Color32(111, 207, 62, 255));
    }

    static void DrawCircle(Color32[] pixels, int size, int cx, int cy, int radius, Color32 color)
    {
        int r2 = radius * radius;
        for (int y = -radius; y <= radius; y++)
        for (int x = -radius; x <= radius; x++)
            if (x * x + y * y <= r2) SetPixel(pixels, size, cx + x, cy + y, color);
    }

    static void FillRect(Color32[] pixels, int size, int x, int y, int width, int height, Color32 color)
    {
        for (int py = Mathf.Max(0, y); py < Mathf.Min(size, y + height); py++)
        for (int px = Mathf.Max(0, x); px < Mathf.Min(size, x + width); px++) SetPixel(pixels, size, px, py, color);
    }

    static void SetPixel(Color32[] pixels, int size, int x, int y, Color32 color)
    {
        if (x >= 0 && y >= 0 && x < size && y < size) pixels[y * size + x] = color;
    }

    [MenuItem("Abeja RA/Reparar terreno visible de tercerniv")]
    public static void RepararTerreno()
    {
        Scene scene = SceneManager.GetSceneByPath(SceneTarget);
        if (!scene.IsValid() || !scene.isLoaded)
            scene = EditorSceneManager.OpenScene(SceneTarget, OpenSceneMode.Single);

        Terrain terrain = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogError("No se encontró el Terrain o su TerrainData en tercerniv.");
            return;
        }

        TerrainData data = terrain.terrainData;
        data.size = new Vector3(400f, 10f, 400f);
        data.SetHeights(0, 0, new float[data.heightmapResolution, data.heightmapResolution]);
        Texture2D grassTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Low Poly Nature Pack/Textures/Grass.png");
        Texture2D soilTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Low Poly Nature Pack/Textures/Soil.png");
        TerrainLayer green = LoadOrCreateLayer(GrassLayerTarget, "tercerniv Grass Texture", grassTexture, 24f);
        TerrainLayer brown = LoadOrCreateLayer(SoilLayerTarget, "tercerniv Soil Texture", soilTexture, 18f);
        if (green != null && brown != null)
        {
            data.terrainLayers = new[] { green, brown };
            int alpha = data.alphamapResolution;
            float[,,] maps = new float[alpha, alpha, 2];
            Vector2[][] routes = GetMazeRoutes();
            const float pathWidth = 18f;
            for (int z = 0; z < alpha; z++)
            for (int x = 0; x < alpha; x++)
            {
                Vector2 point = new Vector2(x * 400f / (alpha - 1), z * 400f / (alpha - 1));
                float nearest = float.MaxValue;
                foreach (Vector2[] route in routes)
                    for (int i = 0; i < route.Length - 1; i++) nearest = Mathf.Min(nearest, DistanceToSegment(point, route[i], route[i + 1]));
                float path = 1f - Mathf.SmoothStep(pathWidth * .5f - 1.5f, pathWidth * .5f + 1.5f, nearest);
                maps[z, x, 0] = 1f - path;
                maps[z, x, 1] = path;
            }
            data.SetAlphamaps(0, 0, maps);
        }

        Material terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>(TerrainMaterialTarget);
        if (terrainMaterial == null)
        {
            Shader terrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if (terrainShader == null) terrainShader = Shader.Find("Nature/Terrain/Standard");
            if (terrainShader == null)
            {
                Debug.LogError("No se encontró un shader compatible para renderizar el terreno.");
                return;
            }
            terrainMaterial = new Material(terrainShader) { name = "tercerniv Terrain Lit" };
            AssetDatabase.CreateAsset(terrainMaterial, TerrainMaterialTarget);
        }
        terrain.materialTemplate = terrainMaterial;
        TerrainCollider terrainCollider = terrain.GetComponent<TerrainCollider>();
        if (terrainCollider == null) terrainCollider = terrain.gameObject.AddComponent<TerrainCollider>();
        terrainCollider.terrainData = data;
        terrain.drawHeightmap = true;
        terrain.drawTreesAndFoliage = true;
        terrain.Flush();

        GameObject existingPath = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Camino_Laberinto");
        if (existingPath != null) UnityEngine.Object.DestroyImmediate(existingPath);
        Shader pathShader = Shader.Find("Universal Render Pipeline/Lit");
        if (pathShader == null) pathShader = Shader.Find("Standard");
        if (pathShader == null)
        {
            Debug.LogError("No se encontró un shader compatible para el camino.");
            return;
        }
        Material pathMaterial = AssetDatabase.LoadAssetAtPath<Material>(PathMaterialTarget);
        if (pathMaterial == null)
        {
            pathMaterial = new Material(pathShader) { name = "tercerniv Camino Marron", color = new Color32(126, 82, 46, 255) };
            AssetDatabase.CreateAsset(pathMaterial, PathMaterialTarget);
        }
        if (soilTexture != null)
        {
            pathMaterial.color = Color.white;
            pathMaterial.SetTexture("_BaseMap", soilTexture);
            pathMaterial.SetTextureScale("_BaseMap", new Vector2(3f, 8f));
        }
        Vector2[][] routesForVisual = GetMazeRoutes();
        GameObject pathRoot = new GameObject("Camino_Laberinto");
        SceneManager.MoveGameObjectToScene(pathRoot, scene);
        int segmentNumber = 1;
        foreach (Vector2[] routePoints in routesForVisual)
        for (int i = 0; i < routePoints.Length - 1; i++)
        {
            Vector3 start = new Vector3(routePoints[i].x, 0.16f, routePoints[i].y);
            Vector3 end = new Vector3(routePoints[i + 1].x, 0.16f, routePoints[i + 1].y);
            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = "Tramo_" + (segmentNumber++).ToString("00");
            UnityEngine.Object.DestroyImmediate(segment.GetComponent<Collider>());
            segment.transform.SetParent(pathRoot.transform, true);
            segment.transform.position = (start + end) * 0.5f;
            segment.transform.rotation = Quaternion.LookRotation(end - start, Vector3.up);
            segment.transform.localScale = new Vector3(20f, 0.22f, Vector3.Distance(start, end) + 1f);
            segment.GetComponent<Renderer>().sharedMaterial = pathMaterial;
        }

        FlorRecolectable[] flowers = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<FlorRecolectable>(true)).ToArray();
        foreach (FlorRecolectable flower in flowers)
        {
            flower.transform.position = new Vector3(flower.transform.position.x, 0.1f, flower.transform.position.z);
            flower.transform.localScale = Vector3.one * 8f;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene, SceneTarget);
        SceneView view = SceneView.lastActiveSceneView;
        if (view != null)
        {
            view.LookAt(new Vector3(200f, 0f, 200f), Quaternion.Euler(55f, 0f, 0f), 340f);
            view.Repaint();
        }
        Debug.Log("Terreno tercerniv reparado: 400 x 400 visible, camino marrón, 5 flores ampliadas y colisión activa.");
    }

    static TerrainLayer LoadOrCreateLayer(string path, string layerName, Texture2D texture, float tileSize)
    {
        if (texture == null) return null;
        TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
        if (layer == null)
        {
            layer = new TerrainLayer { name = layerName };
            AssetDatabase.CreateAsset(layer, path);
        }
        layer.diffuseTexture = texture;
        layer.tileSize = new Vector2(tileSize, tileSize);
        EditorUtility.SetDirty(layer);
        return layer;
    }

    static Vector2[][] GetMazeRoutes()
    {
        return new[]
        {
            new[] {
                new Vector2(22, 12), new Vector2(22, 115), new Vector2(50, 115), new Vector2(50, 70),
                new Vector2(78, 70), new Vector2(78, 145), new Vector2(92, 145), new Vector2(92, 305),
                new Vector2(125, 305), new Vector2(125, 185), new Vector2(162, 185), new Vector2(162, 105),
                new Vector2(190, 105), new Vector2(190, 70), new Vector2(220, 70), new Vector2(220, 160),
                new Vector2(232, 160), new Vector2(232, 278), new Vector2(270, 278), new Vector2(270, 230),
                new Vector2(290, 230), new Vector2(290, 170), new Vector2(302, 170), new Vector2(342, 170),
                new Vector2(342, 250), new Vector2(365, 250), new Vector2(365, 335), new Vector2(340, 335),
                new Vector2(340, 380), new Vector2(300, 380), new Vector2(300, 350), new Vector2(250, 350),
                new Vector2(250, 380), new Vector2(200, 380), new Vector2(200, 350), new Vector2(145, 350),
                new Vector2(145, 380), new Vector2(90, 380), new Vector2(90, 350), new Vector2(45, 350),
                new Vector2(45, 320), new Vector2(22, 320)
            },
            new[] { new Vector2(22, 60), new Vector2(42, 60), new Vector2(42, 40) },
            new[] { new Vector2(50, 115), new Vector2(50, 145), new Vector2(65, 145) },
            new[] { new Vector2(92, 220), new Vector2(108, 220), new Vector2(108, 245) },
            new[] { new Vector2(162, 145), new Vector2(178, 145), new Vector2(178, 165) },
            new[] { new Vector2(220, 200), new Vector2(205, 200), new Vector2(205, 220) },
            new[] { new Vector2(270, 278), new Vector2(270, 315), new Vector2(290, 315) },
            new[] { new Vector2(342, 195), new Vector2(325, 195), new Vector2(325, 215) },
            new[] { new Vector2(300, 380), new Vector2(300, 365), new Vector2(275, 365) }
        };
    }

    [MenuItem("Abeja RA/Completar tercerniv (400 x 400)")]
    public static void Completar()
    {
        Scene target = SceneManager.GetSceneByPath(SceneTarget);
        if (!target.IsValid() || !target.isLoaded)
            target = EditorSceneManager.OpenScene(SceneTarget, OpenSceneMode.Single);

        if (target.GetRootGameObjects().Any(g => g.GetComponent<Terrain>() != null))
        {
            Debug.LogError("tercerniv ya contiene un Terrain. Se detuvo para no duplicarlo.");
            return;
        }

        Scene source = EditorSceneManager.OpenScene(SceneSource, OpenSceneMode.Additive);
        GameObject[] roots = source.GetRootGameObjects();
        GameObject playerSource = roots.FirstOrDefault(g => g.GetComponentInChildren<MovimientoAbeja3D>(true)?.controlJugador == true);
        GameObject enemySource = roots.FirstOrDefault(g => g.GetComponentInChildren<MovimientoAbejaEnemiga>(true) != null);
        GameObject flowersSource = roots.SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .Select(t => t.gameObject).FirstOrDefault(g => g.name == "Flores_Plant07C");
        if (playerSource == null || enemySource == null || flowersSource == null)
            throw new InvalidOperationException("No se encontraron las abejas y el grupo Plant07C de jose1.");

        SceneManager.SetActiveScene(target);
        GameObject player = UnityEngine.Object.Instantiate(playerSource);
        player.name = playerSource.name;
        SceneManager.MoveGameObjectToScene(player, target);
        GameObject enemy = UnityEngine.Object.Instantiate(enemySource);
        enemy.name = enemySource.name;
        SceneManager.MoveGameObjectToScene(enemy, target);
        GameObject flowers = UnityEngine.Object.Instantiate(flowersSource, Vector3.zero, Quaternion.identity);
        flowers.name = "Flores_Plant07C";
        flowers.transform.SetParent(null, true);
        SceneManager.MoveGameObjectToScene(flowers, target);

        MovimientoAbeja3D playerControl = player.GetComponentInChildren<MovimientoAbeja3D>(true);
        MovimientoAbejaEnemiga enemyControl = enemy.GetComponentInChildren<MovimientoAbejaEnemiga>(true);
        if (playerControl == null || enemyControl == null) throw new InvalidOperationException("No se pudieron copiar los controles de las abejas.");

        TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainTarget);
        if (data == null)
        {
            data = new TerrainData { name = "tercerniv_Terrain", heightmapResolution = 513, alphamapResolution = 512, baseMapResolution = 1024 };
            AssetDatabase.CreateAsset(data, TerrainTarget);
        }
        data.size = new Vector3(400f, 10f, 400f);
        data.SetHeights(0, 0, new float[data.heightmapResolution, data.heightmapResolution]);

        TerrainLayer green = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Scenes/Jose1_FlagGreen.terrainlayer");
        TerrainLayer brown = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Scenes/Jose1_Brown.terrainlayer");
        if (green == null || brown == null) throw new InvalidOperationException("No se encontraron las texturas de suelo de jose1.");
        data.terrainLayers = new[] { green, brown };

        int alpha = data.alphamapResolution;
        float[,,] maps = new float[alpha, alpha, 2];
        Vector2[] route = {
            new Vector2(22, 22), new Vector2(22, 378), new Vector2(92, 378), new Vector2(92, 42),
            new Vector2(162, 42), new Vector2(162, 358), new Vector2(232, 358), new Vector2(232, 22),
            new Vector2(302, 22), new Vector2(302, 378), new Vector2(378, 378)
        };
        const float pathWidth = 22f;
        for (int z = 0; z < alpha; z++)
        for (int x = 0; x < alpha; x++)
        {
            Vector2 point = new Vector2(x * 400f / (alpha - 1), z * 400f / (alpha - 1));
            float nearest = float.MaxValue;
            for (int i = 0; i < route.Length - 1; i++) nearest = Mathf.Min(nearest, DistanceToSegment(point, route[i], route[i + 1]));
            float path = 1f - Mathf.SmoothStep(pathWidth * .5f - 1.5f, pathWidth * .5f + 1.5f, nearest);
            maps[z, x, 0] = 1f - path;
            maps[z, x, 1] = path;
        }
        data.SetAlphamaps(0, 0, maps);

        GameObject terrainObject = new GameObject("Terrain");
        SceneManager.MoveGameObjectToScene(terrainObject, target);
        Terrain terrain = terrainObject.AddComponent<Terrain>();
        terrain.terrainData = data;
        terrain.drawInstanced = true;
        terrain.drawHeightmap = true;
        TerrainCollider collider = terrainObject.AddComponent<TerrainCollider>();
        collider.terrainData = data;
        terrain.Flush();

        player.transform.SetPositionAndRotation(new Vector3(22f, 5f, 12f), Quaternion.identity);
        playerControl.radioEsquinasTerreno = 0f;
        enemy.transform.SetPositionAndRotation(new Vector3(34f, 5f, 12f), Quaternion.Euler(0f, 180f, 0f));
        enemyControl.objetivo = playerControl.transform;

        Vector2[] flowerPoints = { new Vector2(22, 115), new Vector2(92, 305), new Vector2(162, 105), new Vector2(232, 278), new Vector2(302, 170) };
        FlorRecolectable[] flowerComponents = flowers.GetComponentsInChildren<FlorRecolectable>(true);
        if (flowerComponents.Length != 5) throw new InvalidOperationException("Se esperaban cinco flores Plant07C; se encontraron " + flowerComponents.Length + ".");
        Array.Sort(flowerComponents, (a, b) => string.CompareOrdinal(a.name, b.name));
        for (int i = 0; i < flowerComponents.Length; i++)
        {
            Transform t = flowerComponents[i].transform;
            t.position = new Vector3(flowerPoints[i].x, 0.1f, flowerPoints[i].y);
            t.rotation = Quaternion.Euler(0f, i * 67f, 0f);
        }

        Camera camera = target.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)).FirstOrDefault(c => c.CompareTag("MainCamera"));
        if (camera == null) camera = target.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)).FirstOrDefault();
        if (camera == null) throw new InvalidOperationException("No se encontró la cámara de tercerniv.");
        CamaraSeguir follow = camera.GetComponent<CamaraSeguir>();
        if (follow == null) follow = camera.gameObject.AddComponent<CamaraSeguir>();
        follow.objetivo = playerControl.transform;

        EditorSceneManager.MarkSceneDirty(target);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(target, SceneTarget);
        EditorSceneManager.CloseScene(source, true);
        SceneManager.SetActiveScene(target);

        var buildScenes = EditorBuildSettings.scenes.ToList();
        if (!buildScenes.Any(s => s.path == SceneTarget))
        {
            buildScenes.Add(new EditorBuildSettingsScene(SceneTarget, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
        }
        Debug.Log("tercerniv completado: terreno plano 400 x 400, camino en laberinto, 5 flores Plant07C, abeja enemiga y 30 disparos.");
    }

    static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + t * ab);
    }
}
