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
    [Min(1)] public int vidasEnemigo = 5;
    [Min(0)] public int cantidadAlMorir = 0;
    [Min(0.5f)] public float separacionAlNacer = 3f;

    private Rigidbody rb;
    private MovimientoAbeja3D controlAbeja;
    private CapsuleCollider colision;
    private Terrain terreno;
    private bool persecucionIniciada;
    private bool yaGeneroReemplazos;
    private static Texture2D texturaEnemigaRoja;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        controlAbeja = GetComponent<MovimientoAbeja3D>();
        controlAbeja.controlJugador = false;
        AplicarColorEnemigo();
        VidasAbeja vidas = GetComponent<VidasAbeja>();
        if (vidas == null) vidas = gameObject.AddComponent<VidasAbeja>();
        vidas.Inicializar(false, vidasEnemigo);
        colision = GetComponent<CapsuleCollider>();
        if (colision == null) colision = CrearColisionadorAproximado();
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

    public void GenerarReemplazosAlMorir()
    {
        if (yaGeneroReemplazos || cantidadAlMorir <= 0) return;
        yaGeneroReemplazos = true;

        Vector3 haciaJugador = objetivo != null ? objetivo.position - transform.position : transform.forward;
        haciaJugador.y = 0f;
        if (haciaJugador.sqrMagnitude < 0.001f) haciaJugador = transform.forward;
        haciaJugador.Normalize();
        Vector3 lateral = Vector3.Cross(Vector3.up, haciaJugador).normalized;
        // Usa una separación limitada: algunos modelos importados tienen escalas
        // locales enormes aunque su tamaño visible sea normal.
        float radio = colision != null ? Mathf.Max(colision.bounds.extents.x, colision.bounds.extents.z) : 0f;
        float separacion = Mathf.Max(separacionAlNacer, Mathf.Clamp(radio * 1.5f, 0f, 6f));

        for (int i = 0; i < cantidadAlMorir; i++)
        {
            float lado = i % 2 == 0 ? -1f : 1f;
            Vector3 posicion = transform.position + lateral * lado * separacion + haciaJugador * (separacion * 0.35f);
            AjustarNacimientoAlTerreno(ref posicion);

            GameObject nueva = Instantiate(gameObject, posicion, transform.rotation);
            nueva.name = "Abeja_Enemiga";

            MovimientoAbejaEnemiga nuevaIA = nueva.GetComponent<MovimientoAbejaEnemiga>();
            if (nuevaIA != null)
            {
                nuevaIA.objetivo = objetivo;
                nuevaIA.yaGeneroReemplazos = false;
            }
            VidasAbeja nuevasVidas = nueva.GetComponent<VidasAbeja>();
            if (nuevasVidas != null) nuevasVidas.Inicializar(false, nuevaIA != null ? nuevaIA.vidasEnemigo : vidasEnemigo);
        }
    }

    CapsuleCollider CrearColisionadorAproximado()
    {
        Renderer renderizador = GetComponentInChildren<Renderer>();
        CapsuleCollider nuevo = gameObject.AddComponent<CapsuleCollider>();
        nuevo.direction = 1;

        if (renderizador == null) return nuevo;

        Bounds limites = renderizador.bounds;
        Vector3 escala = transform.lossyScale;
        nuevo.center = transform.InverseTransformPoint(limites.center);
        nuevo.height = Mathf.Max(0.5f, limites.size.y / Mathf.Max(Mathf.Abs(escala.y), 0.001f));
        nuevo.radius = Mathf.Max(0.25f,
            Mathf.Max(limites.size.x / Mathf.Max(Mathf.Abs(escala.x), 0.001f),
                      limites.size.z / Mathf.Max(Mathf.Abs(escala.z), 0.001f)) * 0.5f);
        return nuevo;
    }

    void AjustarNacimientoAlTerreno(ref Vector3 posicion)
    {
        if (terreno == null || terreno.terrainData == null) return;
        float medioAlto = colision != null ? colision.bounds.extents.y : 1f;
        float suelo = terreno.SampleHeight(posicion) + terreno.transform.position.y;
        posicion.y = Mathf.Max(posicion.y, suelo + alturaSobreTerreno + medioAlto);

        Vector3 local = terreno.transform.InverseTransformPoint(posicion);
        Vector3 tamano = terreno.terrainData.size;
        local.x = Mathf.Clamp(local.x, 0f, tamano.x);
        local.z = Mathf.Clamp(local.z, 0f, tamano.z);
        posicion = terreno.transform.TransformPoint(local);
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
