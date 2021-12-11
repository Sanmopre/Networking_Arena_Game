using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletBehaviour : MonoBehaviour
{
    public GameObject   hitParticle;
    private Game_Manager game;
    public float        lifetime = 1.0f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
        game = GameObject.Find("GameManager").GetComponent<Game_Manager>();

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

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "Enemy")
        {
            game.TakeDamage(game.bulletDamage, 2);
            GameObject hitFx = Instantiate(hitParticle, transform.position, Quaternion.identity);
            Destroy(hitFx, 3f);
            Destroy(gameObject);
        }

        if (other.gameObject.name == "Player")
        {
            game.TakeDamage(game.bulletDamage, 1);
            GameObject hitFx = Instantiate(hitParticle, transform.position, Quaternion.identity);
            Destroy(hitFx, 3f);
            Destroy(gameObject);

        }
    }
}
