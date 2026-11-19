using UnityEngine;

public class ProyectilAbeja : MonoBehaviour
{
    private VidasAbeja objetivo;
    private Vector3 direccion;
    private float velocidad;
    private float alcance;
    private float radio;
    private float distanciaRecorrida;
    private float tiempoInicio;
    private bool impactoProcesado;

    public void Inicializar(VidasAbeja vidasObjetivo, Vector3 direccionDisparo, float velocidadVuelo, float distanciaMaxima, float radioProyectil)
    {
        objetivo = vidasObjetivo;
        direccion = direccionDisparo.sqrMagnitude > 0.001f ? direccionDisparo.normalized : transform.forward;
        velocidad = velocidadVuelo;
        alcance = distanciaMaxima;
        radio = Mathf.Max(0.05f, radioProyectil);
        tiempoInicio = Time.time;
        Destroy(gameObject, 4f);
    }

    void Update()
    {
        if (impactoProcesado) return;

        float pasoDistancia = velocidad * Time.deltaTime;
        Vector3 inicio = transform.position;
        Vector3 fin = inicio + direccion * pasoDistancia;

        if (objetivo != null && objetivo.vidasActuales > 0)
        {
            MovimientoAbejaEnemiga enemiga = objetivo.GetComponent<MovimientoAbejaEnemiga>();
            CapsuleCollider colisionador = objetivo.GetComponent<CapsuleCollider>();
            if (enemiga != null && enemiga.enabled && colisionador != null && colisionador.enabled && TocaColisionador(inicio, fin, colisionador))
            {
                impactoProcesado = true;
                objetivo.RecibirDanio();
                Destroy(gameObject);
                return;
            }
        }

        transform.position = fin;
        distanciaRecorrida += pasoDistancia;
        if (distanciaRecorrida >= alcance || Time.time - tiempoInicio >= 2f) Destroy(gameObject);
    }

    bool TocaColisionador(Vector3 inicio, Vector3 fin, Collider colisionador)
    {
        float tramo = Vector3.Distance(inicio, fin);
        int muestras = Mathf.Max(1, Mathf.CeilToInt(tramo / Mathf.Max(radio * 0.5f, 0.05f)));
        float radioCuadrado = radio * radio;
        for (int i = 0; i <= muestras; i++)
        {
            Vector3 punto = Vector3.Lerp(inicio, fin, i / (float)muestras);
            Vector3 puntoMasCercano = colisionador.ClosestPoint(punto);
            if ((punto - puntoMasCercano).sqrMagnitude <= radioCuadrado) return true;
        }
        return false;
    }
}
