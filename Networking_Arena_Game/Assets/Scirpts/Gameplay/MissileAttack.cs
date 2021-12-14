using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileAttack : MonoBehaviour
{
    // Start is called before the first frame update
    public float explosionTimer = 2.0f;
    public float explosionRadius = 10.0f;
    public float initialVelocity = 10.0f;

    Game_Manager game = null;
    Client client = null;

    public GameObject grenadePrefab = null;

    void Start()
    {
        game = GameObject.Find("GameManager").GetComponent<Game_Manager>();
        GameObject go = GameObject.Find("Client");
        if (go != null)
            client = go.GetComponent<Client>();

        Instantiate(grenadePrefab, new Vector3(transform.position.x, initialVelocity * explosionTimer + 4.905f * explosionTimer * explosionTimer, transform.position.z), Quaternion.identity).GetComponent<Rigidbody>().velocity = new Vector3(0,-initialVelocity,0);
        Destroy(gameObject, explosionTimer);
    }

    private void OnDestroy()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var hitCollider in hitColliders)
            if (hitCollider.isTrigger)
            {
                if (client != null)
                    client.RequestHit(hitCollider.gameObject.name, game.missileDamage);
                else
                    Debug.Log("Hit -> " + hitCollider.gameObject.name);
            }
    }
}
