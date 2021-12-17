using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShotgunBehaviour : MonoBehaviour
{
    Client client = null;
    int damage = 0;

    public float lifeTime = 0.25f;

    List<GameObject> hitObjs = new List<GameObject>();

    private void Start()
    {
        if (!Globals.singlePlayer)
            client = GameObject.Find("Client").GetComponent<Client>();
        damage = GameObject.Find("GameManager").GetComponent<Game_Manager>().shotgunDamage;

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        foreach (GameObject go in hitObjs)
            if (go == other.gameObject)
                return;
        hitObjs.Add(other.gameObject);

        if (!gameObject.CompareTag(other.gameObject.tag))
        {
            if (!Globals.singlePlayer)
                client.RequestHit(other.gameObject.name, damage);
            else
                Debug.Log(other.name + " hit for " + damage + " damage");
        }
    }
}
