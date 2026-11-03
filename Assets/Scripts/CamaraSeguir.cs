using UnityEngine;

public class CamaraSeguir : MonoBehaviour
{
    public Transform objetivo; // La abeja
    public float distancia = 10f; // Distancia detrás de la abeja
    public float altura = 5f; // Altura de la cámara
    public float suavidad = 5f; // Suavidad del seguimiento

    void LateUpdate()
    {
        if (objetivo != null)
        {
            // Calcular la posición deseada de la cámara 
            Vector3 posicionDeseada = objetivo.position - objetivo.forward * distancia + Vector3.up * altura;
            
            // Mover la cámara suavemente hacia esa posición
            transform.position = Vector3.Lerp(transform.position, posicionDeseada, suavidad * Time.deltaTime);
            
            // La cámara siempre mira hacia la abeja
            transform.LookAt(objetivo);
        }
    }
}