using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShotgunCollision : MonoBehaviour
{
    private Game_Manager game;
    bool doDamage = false;
    private void Start()
    {
        game = GameObject.Find("GameManager").GetComponent<Game_Manager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (doDamage)
        {
            doDamage = false;
            game.TakeDamage(game.shotgunDamage, 2);
        }
        //Debug.Log("sdfsd");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("sdfsd");
        if (other.gameObject.name == "Enemy")
        {
            Debug.Log("sdfsd");
            doDamage = true;
        }

    }
}
