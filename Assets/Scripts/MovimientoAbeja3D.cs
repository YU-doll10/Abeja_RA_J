using UnityEngine;

public class MovimientoAbeja3D : MonoBehaviour
{
    public float velocidad = 5f;
    public float velocidadGiro = 100f;
    public float alturaSobreTerreno = 1.5f;
    public float radioEsquinasTerreno = 30f;

    private Rigidbody rb;
    private Terrain terreno;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        terreno = Terrain.activeTerrain != null ? Terrain.activeTerrain : FindFirstObjectByType<Terrain>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    void Update()
    {
        float giro = Input.GetAxis("Horizontal") * velocidadGiro * Time.deltaTime;
        transform.Rotate(0f, giro, 0f);
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        float movimientoZ = Input.GetAxis("Vertical");
        Vector3 movimiento = transform.forward * movimientoZ * velocidad * Time.fixedDeltaTime;
        if (Input.GetKey(KeyCode.Space)) movimiento.y = velocidad * Time.fixedDeltaTime;
        else if (Input.GetKey(KeyCode.LeftShift)) movimiento.y = -velocidad * Time.fixedDeltaTime;
        Vector3 siguientePosicion = rb.position + movimiento;
        if (terreno != null && terreno.terrainData != null) siguientePosicion = MantenerSobreTerreno(siguientePosicion);
        rb.MovePosition(siguientePosicion);
    }

    private Vector3 MantenerSobreTerreno(Vector3 posicion)
    {
        Vector3 local = terreno.transform.InverseTransformPoint(posicion);
        Vector3 tamano = terreno.terrainData.size;
        local.x = Mathf.Clamp(local.x, 0f, tamano.x);
        local.z = Mathf.Clamp(local.z, 0f, tamano.z);
        float radio = Mathf.Clamp(radioEsquinasTerreno, 0f, Mathf.Min(tamano.x, tamano.z) * 0.25f);
        Vector2 punto = new Vector2(local.x, local.z);
        punto = ProyectarDentroDeEsquinasRedondeadas(punto, tamano.x, tamano.z, radio);
        local.x = punto.x;
        local.z = punto.y;
        Vector3 limitada = terreno.transform.TransformPoint(local);
        float alturaSuelo = terreno.SampleHeight(limitada) + terreno.transform.position.y;
        limitada.y = Mathf.Max(posicion.y, alturaSuelo + alturaSobreTerreno);
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
