using UnityEngine;

public class ProjectileBehaviour : MonoBehaviour
{
    private float speed;
    private float lifeTime;
    private int damage;
    private float maxDistance;
    private float lingerTime;

    private Vector3 startPos;
    private bool reachedMax = false;
    private float lingerTimer = 0f;

    public float stopDuration = 2f;

    private ComboCounterUI comboCounter;   // ← no longer a public Inspector field

    void Awake()
    {
        // Finds the ComboCounterUI anywhere in the scene automatically
        comboCounter = FindObjectOfType<ComboCounterUI>();

        if (comboCounter == null)
            Debug.LogWarning("[ProjectileBehaviour] No ComboCounterUI found in scene.");
    }

    public void Init(float spd, float life, int dmg, float maxDist, float linger)
    {
        speed = spd;
        lifeTime = life;
        damage = dmg;
        maxDistance = maxDist;
        lingerTime = linger;

        startPos = transform.position;
    }

    void Update()
    {
        if (!reachedMax)
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);

            float traveled = Vector3.Distance(startPos, transform.position);
            if (traveled >= maxDistance)
            {
                reachedMax = true;
                lingerTimer = 0f;
            }
        }
        else
        {
            lingerTimer += Time.deltaTime;
            if (lingerTimer >= lingerTime)
                Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        DetectionFSM enemy = other.GetComponent<DetectionFSM>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            enemy.ApplyStop(stopDuration);
            enemy.MarkAsHit();

            comboCounter?.RegisterExternalHit();

            Destroy(gameObject);
        }
    }
}