using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explotion_Script : MonoBehaviour
{
    private Game_Manager game;
    private GameObject gameObj;
    private void Start()
    {
        //game = GameObject.Find("GameManager").GetComponent<Game_Manager>();
        gameObj = GameObject.Find("GameManager");
        game = gameObj.GetComponent<Game_Manager>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "Enemy")
        {
            //WHY THE FUCK IS THIS NULL HERE???????
            if(game == null)
            {
                Debug.Log("WTFFFFF");
            }

            if (gameObj == null) 
            {
                Debug.Log("WTFFFFsdfawefawefewfF");
            }


            game.TakeDamage(game.missileDamage, 2);

        }

    }
}
