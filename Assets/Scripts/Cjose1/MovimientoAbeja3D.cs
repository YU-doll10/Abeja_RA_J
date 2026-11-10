using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovimientoAbeja3D : MonoBehaviour
{
    public bool controlJugador = true;
    public float velocidad = 10f;
    public float velocidadGiro = 100f;
    public float alturaSobreTerreno = 1.5f;
    public float radioEsquinasTerreno = 30f;
    public float frecuenciaAlas = 22f;
    public float anguloAleteo = 32f;
    public AudioClip sonidoDisparo;

    private Rigidbody rb;
    private Terrain terreno;
    private MeshCollider caminoTransitable;
    private CapsuleCollider colisionAbeja;
    private Transform alaIzquierda;
    private Transform alaDerecha;
    private Quaternion rotacionBaseIzquierda;
    private Quaternion rotacionBaseDerecha;
    public bool SeMovio { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        terreno = Terrain.activeTerrain != null ? Terrain.activeTerrain : FindFirstObjectByType<Terrain>();
        BuscarCaminoTransitable();
        VidasAbeja vidas = GetComponent<VidasAbeja>();
        if (vidas == null) vidas = gameObject.AddComponent<VidasAbeja>();
        vidas.Inicializar(controlJugador);
        if (controlJugador && GetComponent<DisparoAbeja>() == null) gameObject.AddComponent<DisparoAbeja>();
        PrepararColision();
        BuscarAlas();
        if (alaIzquierda != null) rotacionBaseIzquierda = alaIzquierda.localRotation;
        if (alaDerecha != null) rotacionBaseDerecha = alaDerecha.localRotation;
        if (rb != null)
        {
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    void PrepararColision()
    {
        colisionAbeja = GetComponent<CapsuleCollider>();
        if (colisionAbeja == null) colisionAbeja = gameObject.AddComponent<CapsuleCollider>();
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        Bounds mundo = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) mundo.Encapsulate(renderers[i].bounds);
        Vector3 min = mundo.min, max = mundo.max;
        Vector3[] esquinas = {
            new Vector3(min.x,min.y,min.z), new Vector3(min.x,min.y,max.z), new Vector3(min.x,max.y,min.z), new Vector3(min.x,max.y,max.z),
            new Vector3(max.x,min.y,min.z), new Vector3(max.x,min.y,max.z), new Vector3(max.x,max.y,min.z), new Vector3(max.x,max.y,max.z)
        };
        Bounds local = new Bounds(transform.InverseTransformPoint(esquinas[0]), Vector3.zero);
        for (int i = 1; i < esquinas.Length; i++) local.Encapsulate(transform.InverseTransformPoint(esquinas[i]));
        float diametro = Mathf.Max(local.size.x, local.size.z) * 0.38f;
        colisionAbeja.direction = 1;
        colisionAbeja.center = local.center;
        colisionAbeja.radius = Mathf.Max(diametro * 0.5f, 0.0001f);
        colisionAbeja.height = Mathf.Max(local.size.y * 0.78f, diametro);
        colisionAbeja.isTrigger = false;
    }

    void BuscarAlas()
    {
        foreach (Transform hijo in GetComponentsInChildren<Transform>(true))
        {
            if (hijo == transform) continue;
            string nombre = hijo.name.ToLowerInvariant();
            if (alaIzquierda == null && (nombre.Contains("wing_l") || nombre.Contains("wingleft") || nombre.Contains("leftwing") || nombre.Contains("ala_izq") || nombre.Contains("ala izquierda"))) alaIzquierda = hijo;
            if (alaDerecha == null && (nombre.Contains("wing_r") || nombre.Contains("wingright") || nombre.Contains("rightwing") || nombre.Contains("ala_der") || nombre.Contains("ala derecha"))) alaDerecha = hijo;
        }
        if (alaIzquierda != null && alaDerecha != null) return;
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        Bounds total = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) total.Encapsulate(renderers[i].bounds);
        float umbralX = total.extents.x * 0.22f;
        float mejorIzq = 0f, mejorDer = 0f;
        foreach (Renderer renderer in renderers)
        {
            Transform candidato = renderer.transform;
            Vector3 centroLocal = transform.InverseTransformPoint(renderer.bounds.center);
            float x = centroLocal.x;
            if (centroLocal.y < 0f || Mathf.Abs(x) < umbralX) continue;
            string nombre = candidato.name.ToLowerInvariant();
            if (nombre.Contains("body") || nombre.Contains("head") || nombre.Contains("eye")) continue;
            if (alaIzquierda == null && x < 0f && -x > mejorIzq) { alaIzquierda = candidato; mejorIzq = -x; }
            if (alaDerecha == null && x > 0f && x > mejorDer) { alaDerecha = candidato; mejorDer = x; }
        }
        if (alaIzquierda != null) rotacionBaseIzquierda = alaIzquierda.localRotation;
        if (alaDerecha != null) rotacionBaseDerecha = alaDerecha.localRotation;
    }

    void Update()
    {
        float aleteo = Mathf.Sin(Time.time * frecuenciaAlas * Mathf.PI * 2f) * anguloAleteo;
        if (alaIzquierda != null) alaIzquierda.localRotation = rotacionBaseIzquierda * Quaternion.Euler(0f, 0f, aleteo);
        if (alaDerecha != null) alaDerecha.localRotation = rotacionBaseDerecha * Quaternion.Euler(0f, 0f, -aleteo);
    }

    void FixedUpdate()
    {
        if (!controlJugador || rb == null) return;
        float giro = Input.GetAxis("Horizontal");
        float avance = Input.GetAxis("Vertical");
        bool subir = Input.GetKey(KeyCode.Space);
        bool bajar = Input.GetKey(KeyCode.LeftShift);
        if (Mathf.Abs(avance) > 0.1f || subir || bajar) SeMovio = true;
        Quaternion rotacionSiguiente = rb.rotation * Quaternion.Euler(0f, giro * velocidadGiro * Time.fixedDeltaTime, 0f);
        rb.MoveRotation(rotacionSiguiente);
        Vector3 movimiento = rotacionSiguiente * Vector3.forward * avance * velocidad * Time.fixedDeltaTime;
        if (subir) movimiento.y = velocidad * Time.fixedDeltaTime;
        else if (bajar) movimiento.y = -velocidad * Time.fixedDeltaTime;
        Vector3 siguientePosicion = rb.position + movimiento;
        if (terreno != null && terreno.terrainData != null) siguientePosicion = MantenerSobreTerreno(siguientePosicion);
        siguientePosicion = MantenerEnCamino(siguientePosicion);
        rb.MovePosition(siguientePosicion);
    }

    void BuscarCaminoTransitable()
    {
        GameObject camino = GameObject.Find("Camino_Laberinto");
        caminoTransitable = camino != null ? camino.GetComponent<MeshCollider>() : null;
    }

    public Vector3 MantenerEnCamino(Vector3 posicion)
    {
        if (caminoTransitable == null) BuscarCaminoTransitable();
        if (caminoTransitable == null || !caminoTransitable.enabled || caminoTransitable.sharedMesh == null) return posicion;

        Vector3 puntoConsulta = new Vector3(posicion.x, caminoTransitable.bounds.center.y, posicion.z);
        Vector3 puntoMasCercano = caminoTransitable.ClosestPoint(puntoConsulta);
        if (float.IsNaN(puntoMasCercano.x) || float.IsNaN(puntoMasCercano.z)) return posicion;
        posicion.x = puntoMasCercano.x;
        posicion.z = puntoMasCercano.z;
        return posicion;
    }

    private Vector3 MantenerSobreTerreno(Vector3 posicion)
    {
        Vector3 centroOffset = colisionAbeja != null ? transform.TransformVector(colisionAbeja.center) : Vector3.zero;
        Vector3 centroMundo = posicion + centroOffset;
        Vector3 local = terreno.transform.InverseTransformPoint(centroMundo);
        Vector3 tamano = terreno.terrainData.size;
        Vector3 escalaX = terreno.transform.TransformVector(Vector3.right);
        Vector3 escalaZ = terreno.transform.TransformVector(Vector3.forward);
        float margenMundoX = colisionAbeja != null ? Mathf.Max(colisionAbeja.bounds.extents.x, colisionAbeja.bounds.extents.z) : 0f;
        float margenMundoZ = margenMundoX;
        float margenX = margenMundoX / Mathf.Max(escalaX.magnitude, 0.0001f);
        float margenZ = margenMundoZ / Mathf.Max(escalaZ.magnitude, 0.0001f);
        local.x = Mathf.Clamp(local.x, margenX, tamano.x - margenX);
        local.z = Mathf.Clamp(local.z, margenZ, tamano.z - margenZ);
        float radioEsquina = Mathf.Clamp(radioEsquinasTerreno, 0f, Mathf.Min(tamano.x, tamano.z) * 0.25f);
        float radioCentro = Mathf.Max(0f, radioEsquina - Mathf.Max(margenX, margenZ));
        Vector2 punto = ProyectarDentroDeEsquinasRedondeadas(new Vector2(local.x, local.z), tamano.x, tamano.z, radioCentro);
        local.x = punto.x;
        local.z = punto.y;
        Vector3 centroLimitado = terreno.transform.TransformPoint(local);
        float alturaSuelo = terreno.SampleHeight(centroLimitado) + terreno.transform.position.y;
        float medioAlto = colisionAbeja != null ? colisionAbeja.bounds.extents.y : 0f;
        Vector3 limitada = centroLimitado - centroOffset;
        limitada.y = Mathf.Max(posicion.y, alturaSuelo + alturaSobreTerreno + medioAlto - centroOffset.y);
        return limitada;
    }
    private Vector2 ProyectarDentroDeEsquinasRedondeadas(Vector2 punto, float ancho, float fondo, float radio)
    {
        bool izquierda = punto.x < radio, derecha = punto.x > ancho - radio;
        bool abajo = punto.y < radio, arriba = punto.y > fondo - radio;
        if (!(izquierda || derecha) || !(abajo || arriba)) return punto;
        Vector2 centro = new Vector2(izquierda ? radio : ancho - radio, abajo ? radio : fondo - radio);
        Vector2 delta = punto - centro;
        if (delta.sqrMagnitude > radio * radio) punto = centro + delta.normalized * radio;
        return punto;
    }
}






