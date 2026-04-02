using UnityEngine;

public class Projectile : MonoBehaviour
{
    public int damage = 5;
    public LayerMask enemyLayer;
    public float speed = 10f;
    public float lifetime = 3f;

    private float timer = 0f;
    private bool stopped = false;

    void Update()
    {
        if (!stopped)
        {
            // Move forward in the direction set by rotation
            transform.Translate(Vector3.forward * speed * Time.deltaTime);

            timer += Time.deltaTime;
            if (timer >= lifetime)
            {
                stopped = true; // stop moving after lifetime
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            Pathogen pathogen = other.GetComponent<Pathogen>();
            if (pathogen != null)
            {
                pathogen.TakeDamage(damage);
                Debug.Log("Projectile hit enemy: " + other.name);
            }

            // Stop immediately at collision point
            stopped = true;
        }
    }
}
