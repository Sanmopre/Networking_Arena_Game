using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrenadeBehaviour : MonoBehaviour
{
    [Header("Particles")]
    public GameObject explosionParticle;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip lockTargetSFX;

    private void Start()
    {
        audioSource.clip = lockTargetSFX;
        audioSource.Play();
    }
    
    private void Update()
    {   
        if (transform.position.y <= 0.5f)
        {
            GameObject hitFx = Instantiate(explosionParticle, new Vector3(transform.position.x, 1.0f, transform.position.z), Quaternion.identity);
            Destroy(hitFx, 1f);
            Destroy(gameObject);
        }
    }
}
