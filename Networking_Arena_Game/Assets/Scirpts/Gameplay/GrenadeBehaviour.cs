using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrenadeBehaviour : MonoBehaviour
{

    public GameObject explosionParticle;
    
    public float height = 3f;
    public float explosionRadius;
    public float gravity = 9.8f;

    private Vector3 target;
    private float time = 0;
    private Vector3 velocity = new Vector3();
    private void Start()
    {
        velocity = GetInitVelParabolic(transform.position, target, 0.1f, height);

        //Vector3 tt = GetInitVelParabolic(gameObject.transform.position, target,0,0);
        //Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
        //rigidbody.AddForce(tt * rigidbody.mass, ForceMode.Impulse);

    }
    private void Update()
    {
        transform.position = GetParabolaNextPosition(transform.position, velocity, gravity, Time.deltaTime);
        time += Time.deltaTime;
        velocity.y += gravity * Time.deltaTime;
    }
    Vector3 GetInitVelParabolic(Vector3 initialPos, Vector3 destinationPos, float rangeOffset, float heightOffset)
    {
        float gravity = -9.8f;
        Vector3 velocityVec = new Vector3();
        Vector3 shootDir = new Vector3(destinationPos.x, 0f, destinationPos.z) - new Vector3(initialPos.x, 0f, initialPos.z);

        shootDir = shootDir.normalized;
        float range = shootDir.magnitude;
        range += rangeOffset;
        float maxYPos = destinationPos.y + heightOffset;

        if (maxYPos < initialPos.y)
            maxYPos = initialPos.y;


        float Yvelocity = -2.0f * gravity * (maxYPos - initialPos.y);
        if (Yvelocity < 0) Yvelocity = 0f;
        velocityVec.y = Mathf.Sqrt(Yvelocity);

        //Time to max Y
        Yvelocity = -2.0f * (maxYPos - initialPos.y) / gravity;
        if (Yvelocity < 0)
            Yvelocity = 0f;
        float timeToMax = Mathf.Sqrt(Yvelocity);

        //Time to destination
        Yvelocity = -2.0f * (maxYPos - destinationPos.y) / gravity;
        if (Yvelocity < 0)
            Yvelocity = 0f;
        float timeToDest = Mathf.Sqrt(Yvelocity);

        float totalTime = timeToMax + timeToDest;
        float horizontalVelocityMagnitude = range / totalTime;

        velocityVec.x = horizontalVelocityMagnitude * shootDir.x;
        velocityVec.z = horizontalVelocityMagnitude * shootDir.z;


        return velocityVec;

    }

    public static Vector3 GetParabolaNextPosition(Vector3 position, Vector3 velocity, float gravity, float time)
    {
        velocity.y += gravity * time;
        return position + velocity * time;
    }

    public void SetTarget(Vector3 newTarget)
    {
        target = newTarget;
    }

    private void OnCollisionEnter(Collision collision)
    {
        ApplyDamage();
        GameObject hitFx = Instantiate(explosionParticle, transform.position, Quaternion.identity);
        Destroy(hitFx, 3f);
        Destroy(gameObject);
    }

    void ApplyDamage()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var hitCollider in hitColliders)
        {
            Debug.Log(hitCollider.gameObject.name);
        }
    }
}
