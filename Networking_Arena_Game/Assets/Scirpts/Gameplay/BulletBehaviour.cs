using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletBehaviour : MonoBehaviour
{
    public GameObject hitParticle;

    private void OnCollisionEnter(Collision collision)
    {
        GameObject hitFx = Instantiate(hitParticle, transform.position, Quaternion.identity);
        Destroy(hitFx, 3f);
        Destroy(gameObject);
    }
}
