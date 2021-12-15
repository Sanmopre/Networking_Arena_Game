using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShotgunBehaviour : MonoBehaviour
{
    Client client = null;
    int damage = 0;

    public float lifeTime = 0.25f;

    private void Start()
    {
        if (!Globals.singlePlayer)
            client = GameObject.Find("Client").GetComponent<Client>();
        damage = GameObject.Find("GameManager").GetComponent<Game_Manager>().shotgunDamage;

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!gameObject.CompareTag(other.gameObject.tag))
        {
            if (!Globals.singlePlayer)
                client.RequestHit(other.gameObject.name, damage);
            else
                Debug.Log(other.name + " hit for " + damage + " damage");
        }
    }
}
