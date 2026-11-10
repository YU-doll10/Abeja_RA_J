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
    private MovimientoAbeja3D animacionAbeja;
    private CapsuleCollider colision;
    private Terrain terreno;
    private bool persecucionIniciada;
    private static Texture2D texturaEnemigaRoja;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animacionAbeja = GetComponent<MovimientoAbeja3D>();
        animacionAbeja.controlJugador = false;
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

    void AplicarColorEnemigo()
    {
        if (texturaEnemigaRoja == null)
            texturaEnemigaRoja = Resources.Load<Texture2D>("Textures/abeja_enemiga_roja");

        if (texturaEnemigaRoja == null)
        {
            Debug.LogWarning("No se encontró la textura roja selectiva de la abeja enemiga.", this);
            return;
        }

        Renderer[] renderizadores = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderizador in renderizadores)
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

    void Start()
    {
        if (objetivo != null) return;
        MovimientoAbeja3D[] abejas = FindObjectsByType<MovimientoAbeja3D>(FindObjectsSortMode.None);
        foreach (MovimientoAbeja3D abeja in abejas)
        {
            if (abeja != animacionAbeja && abeja.controlJugador)
            {
                objetivo = abeja.transform;
                break;
            }
        }
        if (objetivo == null) Debug.LogWarning("Abeja enemiga: no se encontró una abeja de jugador para perseguir.", this);
    }

    bool PuedeAvanzar(Vector3 origen, float radio, Vector3 direccion, float distancia)
    {
        if (!Physics.SphereCast(origen, radio, direccion, out RaycastHit hit, distancia, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return true;
        return hit.transform == objetivo || hit.transform.IsChildOf(objetivo);
    }
    void FixedUpdate()
    {
        if (objetivo == null || rb == null) return;
        if (!persecucionIniciada)
        {
            MovimientoAbeja3D controlJugador = objetivo.GetComponent<MovimientoAbeja3D>();
            if (controlJugador == null || !controlJugador.SeMovio) return;
            persecucionIniciada = true;
        }
        Vector3 haciaObjetivo = objetivo.position - rb.position;
        if (haciaObjetivo.magnitude <= distanciaMinima) return;

        Vector3 direccion = haciaObjetivo.normalized;
        float radio = colision != null ? Mathf.Max(colision.bounds.extents.x, colision.bounds.extents.z) * 0.72f : 1f;
        float distanciaSonda = Mathf.Max(radio * 2f, velocidad * Time.fixedDeltaTime * 3f);
        Vector3 origenSonda = colision != null ? colision.bounds.center : rb.position;
        if (Physics.SphereCast(origenSonda, radio, direccion, out RaycastHit obstaculo, distanciaSonda, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && obstaculo.transform != objetivo && !obstaculo.transform.IsChildOf(objetivo))
        {
            Vector3 derecha = Quaternion.AngleAxis(55f, Vector3.up) * direccion;
            Vector3 izquierda = Quaternion.AngleAxis(-55f, Vector3.up) * direccion;
            bool libreDerecha = PuedeAvanzar(origenSonda, radio, derecha, distanciaSonda);
            bool libreIzquierda = PuedeAvanzar(origenSonda, radio, izquierda, distanciaSonda);
            if (libreDerecha && libreIzquierda)
                direccion = (rb.position + derecha - objetivo.position).sqrMagnitude < (rb.position + izquierda - objetivo.position).sqrMagnitude ? derecha : izquierda;
            else if (libreDerecha) direccion = derecha;
            else if (libreIzquierda) direccion = izquierda;
            else direccion = Vector3.ProjectOnPlane(direccion, obstaculo.normal).normalized;
        }

        Quaternion rotacionDeseada = Quaternion.LookRotation(direccion, Vector3.up);
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, rotacionDeseada, velocidadGiro * Time.fixedDeltaTime));
        Vector3 siguiente = rb.position + direccion * velocidad * Time.fixedDeltaTime;
        if (terreno != null && terreno.terrainData != null)
        {
            float suelo = terreno.SampleHeight(siguiente) + terreno.transform.position.y;
            float medioAlto = colision != null ? colision.bounds.extents.y : 1f;
            siguiente.y = Mathf.Max(siguiente.y, suelo + alturaSobreTerreno + medioAlto);
            Vector3 local = terreno.transform.InverseTransformPoint(siguiente);
            Vector3 tamano = terreno.terrainData.size;
            local.x = Mathf.Clamp(local.x, 0f, tamano.x);
            local.z = Mathf.Clamp(local.z, 0f, tamano.z);
            siguiente = terreno.transform.TransformPoint(local);
        }
        siguiente = animacionAbeja.MantenerEnCamino(siguiente);
        rb.MovePosition(siguiente);
    }
}





