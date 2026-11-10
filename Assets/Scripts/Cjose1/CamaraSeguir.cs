using UnityEngine;

public class CamaraSeguir : MonoBehaviour
{
    public Transform objetivo;
    public float distancia = 10f;
    public float altura = 5f;
    public float suavidad = 5f;
    [Range(1.5f, 5f)] public float escalaEncuadre = 2.5f;

    private Vector3 centroVisualLocal;
    private float distanciaEncuadre;
    private float alturaEncuadre;
    private Camera camara;

    void Awake()
    {
        camara = GetComponent<Camera>();
        if (camara == null)
        {
            // This component was also attached to the bee by mistake. It must only run on a camera.
            enabled = false;
        }
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

        CalcularEncuadre();
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
            else
            {
                limites.Encapsulate(renderizador.bounds);
            }
        }

        Vector3 centroVisual = encontroRenderizador ? limites.center : objetivo.position;
        centroVisualLocal = objetivo.InverseTransformPoint(centroVisual);
        float radio = encontroRenderizador ? limites.extents.magnitude : 1f;
        distanciaEncuadre = Mathf.Max(distancia, radio * escalaEncuadre);
        alturaEncuadre = Mathf.Max(altura, radio * 0.8f);
        camara.farClipPlane = Mathf.Max(camara.farClipPlane, distanciaEncuadre + radio * 2f);
    }

    void LateUpdate()
    {
        if (objetivo == null || !enabled) return;

        Vector3 centroVisual = objetivo.TransformPoint(centroVisualLocal);
        Vector3 posicionDeseada = centroVisual - objetivo.forward * distanciaEncuadre + Vector3.up * alturaEncuadre;
        float factorSuavidad = 1f - Mathf.Exp(-suavidad * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, posicionDeseada, factorSuavidad);
        transform.LookAt(centroVisual);
    }
}
