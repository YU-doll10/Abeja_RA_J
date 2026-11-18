using UnityEngine;

public class CamaraSeguir : MonoBehaviour
{
    public Transform objetivo;
    public float distancia = 10f;
    public float altura = 5f;
    public float suavidad = 5f;
    [Range(1.5f, 5f)] public float escalaEncuadre = 2.5f;
    [Range(20f, 75f)] public float inclinacionOrbita = 40f;
    [Range(20f, 160f)] public float anguloLateralOrbita = 55f;
    public float margenSobreTerreno = 1f;

    private Vector3 centroVisualLocal;
    private float distanciaEncuadre;
    private float alturaEncuadre;
    private float anguloCamara;
    private float ultimoGiroObjetivo;
    private Camera camara;
    private Terrain terreno;

    void Awake()
    {
        camara = GetComponent<Camera>();
        if (camara == null) enabled = false;
    }

    void Start()
    {
        if (!enabled) return;

        if (objetivo == null)
        {
            GameObject abeja = GameObject.Find("Meshy_AI_Crear_un_modelo_3D_de_0925233536_texture");
            if (abeja != null) objetivo = abeja.transform;
        }
        if (objetivo == null || objetivo == transform)
        {
            Debug.LogError("CamaraSeguir no encontró el objeto raíz de la abeja.", this);
            enabled = false;
            return;
        }

        terreno = Terrain.activeTerrain != null ? Terrain.activeTerrain : FindFirstObjectByType<Terrain>();
        CalcularEncuadre();

        ultimoGiroObjetivo = objetivo.eulerAngles.y;
        // Coloca la cámara arriba y en un lateral de la abeja al iniciar.
        anguloCamara = anguloLateralOrbita - ultimoGiroObjetivo;
        Vector3 centroVisual = objetivo.TransformPoint(centroVisualLocal);
        transform.position = PosicionDeseada(centroVisual);
        transform.LookAt(centroVisual);
    }

    void CalcularEncuadre()
    {
        Bounds limites = new Bounds(objetivo.position, Vector3.zero);
        Renderer[] renderizadores = objetivo.GetComponentsInChildren<Renderer>();
        bool encontroRenderizador = false;
        foreach (Renderer renderizador in renderizadores)
        {
            if (!renderizador.enabled) continue;
            if (!encontroRenderizador)
            {
                limites = renderizador.bounds;
                encontroRenderizador = true;
            }
            else limites.Encapsulate(renderizador.bounds);
        }

        Vector3 centroVisual = encontroRenderizador ? limites.center : objetivo.position;
        centroVisualLocal = objetivo.InverseTransformPoint(centroVisual);
        float radio = encontroRenderizador ? limites.extents.magnitude : 1f;
        distanciaEncuadre = Mathf.Max(distancia, radio * escalaEncuadre);
        float alturaPorAngulo = Mathf.Tan(inclinacionOrbita * Mathf.Deg2Rad) * distanciaEncuadre;
        alturaEncuadre = Mathf.Max(altura, alturaPorAngulo);
        camara.farClipPlane = Mathf.Max(camara.farClipPlane, distanciaEncuadre * 2f + radio * 2f);
    }

    void LateUpdate()
    {
        if (objetivo == null || !enabled) return;

        float giroActual = objetivo.eulerAngles.y;
        float diferenciaGiro = Mathf.DeltaAngle(ultimoGiroObjetivo, giroActual);
        // Cuando la abeja gira a la derecha, la cámara gira la misma cantidad
        // hacia la izquierda alrededor de ella, y viceversa.
        anguloCamara -= diferenciaGiro;
        ultimoGiroObjetivo = giroActual;

        Vector3 centroVisual = objetivo.TransformPoint(centroVisualLocal);
        Vector3 posicionDeseada = PosicionDeseada(centroVisual);
        float factor = 1f - Mathf.Exp(-suavidad * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, posicionDeseada, factor);
        if (terreno != null && terreno.terrainData != null)
        {
            float sueloBajoCamara = terreno.SampleHeight(transform.position) + terreno.transform.position.y;
            transform.position = new Vector3(transform.position.x,
                Mathf.Max(transform.position.y, sueloBajoCamara + margenSobreTerreno), transform.position.z);
        }

        Quaternion giroDeseado = Quaternion.LookRotation(centroVisual - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, giroDeseado, factor);
    }

    Vector3 PosicionDeseada(Vector3 centroVisual)
    {
        Vector3 offset = new Vector3(0f, alturaEncuadre, -distanciaEncuadre);
        Vector3 posicion = centroVisual + Quaternion.Euler(0f, anguloCamara, 0f) * offset;

        // Puede moverse sobre cualquier parte del terreno con la abeja, y sube
        // por encima de la superficie local cuando hay una montaña debajo.
        if (terreno != null && terreno.terrainData != null)
        {
            float suelo = terreno.SampleHeight(posicion) + terreno.transform.position.y;
            posicion.y = Mathf.Max(posicion.y, suelo + margenSobreTerreno);
        }
        return posicion;
    }
}
