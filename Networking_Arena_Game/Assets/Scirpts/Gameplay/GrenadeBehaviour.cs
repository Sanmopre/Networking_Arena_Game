using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrenadeBehaviour : MonoBehaviour
{

    public GameObject explosionParticle;
    public float explosionRadius;

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
