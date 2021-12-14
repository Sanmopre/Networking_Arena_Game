using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explotion_Script : MonoBehaviour
{
    private Game_Manager game;
    private GameObject gameObj;
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
            game.TakeDamage(game.missileDamage, 2);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "Enemy")
        {
            //dumb work arround cuase i dont know why the F "game" is null inside here but not in update
            doDamage = true;
        }

    }
}
