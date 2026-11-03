using UnityEngine;

public class MovimientoAbeja3D : MonoBehaviour
{
<<<<<<< Updated upstream
    public float velocidad = 10f;
    public float velocidadGiro = 100f; // Nueva variable para controlar qué tan rápido gira
    private Rigidbody rb;
=======
    public float velocidad = 5f;
    private Rigidbody rb;
    private Vector3 movimiento;
>>>>>>> Stashed changes

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
<<<<<<< Updated upstream
        // 1. GIRAR con A y D (o Flechas Izquierda/Derecha)
        float giro = Input.GetAxis("Horizontal") * velocidadGiro * Time.deltaTime;
        transform.Rotate(0, giro, 0); // Gira sobre su propio eje Y
=======
        movimiento.x = Input.GetAxisRaw("Horizontal");
        movimiento.z = Input.GetAxisRaw("Vertical");
>>>>>>> Stashed changes
    }

    void FixedUpdate()
    {
<<<<<<< Updated upstream
        // 2. MOVERSE adelante/atrás con W y S (o Flechas Arriba/Abajo)
        float movimientoZ = Input.GetAxis("Vertical");
        
        // transform.forward hace que se mueva hacia donde está mirando la abeja
        Vector3 movimiento = transform.forward * movimientoZ * velocidad * Time.fixedDeltaTime;

        // 3. SUBIR y BAJAR con Espacio y Shift
        if (Input.GetKey(KeyCode.Space))
        {
            movimiento.y = velocidad * Time.fixedDeltaTime;
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            movimiento.y = -velocidad * Time.fixedDeltaTime;
        }

        // 4. Aplicar el movimiento
        rb.MovePosition(rb.position + movimiento);
    }
}
=======
        Vector3 nuevaPosicion = rb.position + movimiento * velocidad * Time.fixedDeltaTime;
        rb.MovePosition(nuevaPosicion);
    }
}
>>>>>>> Stashed changes
