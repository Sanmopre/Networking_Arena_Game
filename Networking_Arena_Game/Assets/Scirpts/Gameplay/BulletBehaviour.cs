using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletBehaviour : MonoBehaviour
{
    public GameObject   hitParticle;
    private Game_Manager game;
    public float        lifetime = 1.0f;
    Client client = null;

    private void Start()
    {
        Destroy(gameObject, lifetime);
        game = GameObject.Find("GameManager").GetComponent<Game_Manager>();

        client = GameObject.Find("Client").GetComponent<Client>();

    }

    private void Update()
    {
    }

    private void OnCollisionEnter(Collision other)
    {
        GameObject hitFx = Instantiate(hitParticle, transform.position, Quaternion.identity);
        Destroy(hitFx, 3f);
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((other.gameObject.name == "Enemy" || other.gameObject.name == "Player") && !gameObject.CompareTag(other.gameObject.tag))
        {
            client.RequestHit(other.gameObject.name, game.bulletDamage);
            GameObject hitFx = Instantiate(hitParticle, transform.position, Quaternion.identity);
            Destroy(hitFx, 3f);
            Destroy(gameObject);
        }
    }
}
