using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MovimientoAbeja3D))]
public class MovimientoAbejaEnemiga : MonoBehaviour
{
    public Transform objetivo;
    public float velocidad = 8f;
    public float velocidadGiro = 150f;
    public float distanciaMinima = 0.5f;
    public float alturaSobreTerreno = 1.5f;

    private Rigidbody rb;
    private MovimientoAbeja3D controlAbeja;
    private CapsuleCollider colision;
    private Terrain terreno;
    private bool persecucionIniciada;
    private static Texture2D texturaEnemigaRoja;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        controlAbeja = GetComponent<MovimientoAbeja3D>();
        controlAbeja.controlJugador = false;
        AplicarColorEnemigo();
        VidasAbeja vidas = GetComponent<VidasAbeja>();
        if (vidas == null) vidas = gameObject.AddComponent<VidasAbeja>();
        vidas.Inicializar(false);
        colision = GetComponent<CapsuleCollider>();
        terreno = Terrain.activeTerrain != null ? Terrain.activeTerrain : FindFirstObjectByType<Terrain>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void Start()
    {
        if (objetivo == null)
        {
            MovimientoAbeja3D[] abejas = FindObjectsByType<MovimientoAbeja3D>(FindObjectsSortMode.None);
            foreach (MovimientoAbeja3D abeja in abejas)
            {
                if (abeja != controlAbeja && abeja.controlJugador)
                {
                    objetivo = abeja.transform;
                    break;
                }
            }
        }
        if (objetivo == null) Debug.LogWarning("Abeja enemiga: no se encontró una abeja de jugador para perseguir.", this);
    }

    void AplicarColorEnemigo()
    {
        if (texturaEnemigaRoja == null)
            texturaEnemigaRoja = Resources.Load<Texture2D>("Textures/abeja_enemiga_roja");

        if (texturaEnemigaRoja == null)
        {
            Debug.LogWarning("No se encontró la textura roja selectiva de la abeja enemiga.", this);
            return;
        }

        foreach (Renderer renderizador in GetComponentsInChildren<Renderer>(true))
        {
            Material[] materiales = renderizador.materials;
            foreach (Material material in materiales)
            {
                if (material == null) continue;
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texturaEnemigaRoja);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texturaEnemigaRoja);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
                if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            }
            renderizador.materials = materiales;
        }
    }

    void FixedUpdate()
    {
        if (objetivo == null || rb == null) return;
        if (!persecucionIniciada)
        {
            MovimientoAbeja3D jugador = objetivo.GetComponent<MovimientoAbeja3D>();
            if (jugador == null || !jugador.SeMovio) return;
            persecucionIniciada = true;
        }

        Vector3 diferencia = objetivo.position - rb.position;
        Vector3 direccion = new Vector3(diferencia.x, 0f, diferencia.z);
        if (direccion.magnitude <= distanciaMinima) return;
        direccion = BuscarDireccionLibre(direccion.normalized);
        if (direccion.sqrMagnitude < 0.001f) return;

        Quaternion rotacion = Quaternion.LookRotation(direccion, Vector3.up);
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, rotacion, velocidadGiro * Time.fixedDeltaTime));
        Vector3 siguiente = rb.position + direccion * velocidad * Time.fixedDeltaTime;

        if (terreno != null && terreno.terrainData != null)
        {
            float suelo = terreno.SampleHeight(siguiente) + terreno.transform.position.y;
            float medioAlto = colision != null ? colision.bounds.extents.y : 1f;
            float alturaDeseada = Mathf.Max(suelo + alturaSobreTerreno + medioAlto, objetivo.position.y);
            siguiente.y = Mathf.MoveTowards(rb.position.y, alturaDeseada, velocidad * Time.fixedDeltaTime);
            Vector3 local = terreno.transform.InverseTransformPoint(siguiente);
            Vector3 tamano = terreno.terrainData.size;
            local.x = Mathf.Clamp(local.x, 0f, tamano.x);
            local.z = Mathf.Clamp(local.z, 0f, tamano.z);
            siguiente = terreno.transform.TransformPoint(local);
        }
        rb.MovePosition(siguiente);
    }

    Vector3 BuscarDireccionLibre(Vector3 direccionObjetivo)
    {
        float radio = colision != null ? Mathf.Max(0.35f, Mathf.Min(colision.bounds.extents.x, colision.bounds.extents.z) * 0.8f) : 0.8f;
        float distanciaSonda = Mathf.Max(radio * 2f, velocidad * Time.fixedDeltaTime * 4f);
        Vector3 origen = colision != null ? colision.bounds.center : rb.position;
        if (DireccionLibre(origen, radio, direccionObjetivo, distanciaSonda)) return direccionObjetivo;

        Vector3 derecha = Quaternion.AngleAxis(65f, Vector3.up) * direccionObjetivo;
        Vector3 izquierda = Quaternion.AngleAxis(-65f, Vector3.up) * direccionObjetivo;
        bool libreDerecha = DireccionLibre(origen, radio, derecha, distanciaSonda);
        bool libreIzquierda = DireccionLibre(origen, radio, izquierda, distanciaSonda);
        if (libreDerecha && libreIzquierda)
            return Vector3.Dot(derecha, direccionObjetivo) >= Vector3.Dot(izquierda, direccionObjetivo) ? derecha : izquierda;
        if (libreDerecha) return derecha;
        if (libreIzquierda) return izquierda;
        return Vector3.zero;
    }

    bool DireccionLibre(Vector3 origen, float radio, Vector3 direccion, float distancia)
    {
        if (!Physics.SphereCast(origen, radio, direccion, out RaycastHit hit, distancia,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return true;
        return hit.transform == objetivo || hit.transform.IsChildOf(objetivo);
    }
}
