using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletBehaviour : MonoBehaviour
{
    public GameObject   hitParticle;
    public float        lifetime = 1.0f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {

    }

    private void OnCollisionEnter(Collision collision)
    {
        GameObject hitFx = Instantiate(hitParticle, transform.position, Quaternion.identity);
        Destroy(hitFx, 3f);
        Destroy(gameObject);
    }
}
