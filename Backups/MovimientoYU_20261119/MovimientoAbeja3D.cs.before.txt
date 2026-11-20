using UnityEngine;

public class MovimientoAbeja3D : MonoBehaviour
{
    public float velocidad = 5f;
    public float velocidadGiro = 100f; 
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        float giro = Input.GetAxis("Horizontal") * velocidadGiro * Time.deltaTime;
        transform.Rotate(0, giro, 0); 
    }

    void FixedUpdate()
    {
        float movimientoZ = Input.GetAxis("Vertical");
        Vector3 movimiento = transform.forward * movimientoZ * velocidad * Time.fixedDeltaTime;

        if (Input.GetKey(KeyCode.Space))
        {
            movimiento.y = velocidad * Time.fixedDeltaTime;
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            movimiento.y = -velocidad * Time.fixedDeltaTime;
        }

        rb.MovePosition(rb.position + movimiento);
    }
}
