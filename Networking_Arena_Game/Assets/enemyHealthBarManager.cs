using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class enemyHealthBarManager : MonoBehaviour
{
    public Image healthBar;
    private Game_Manager game;
    public Text nameText;
    private void Start()
    {
        game = GameObject.Find("GameManager").GetComponent<Game_Manager>();

        if (!Globals.singlePlayer)
            nameText.text = GameObject.Find("Enemy").GetComponent<Enemy_Controller>().username;
    }
    private void LateUpdate()
    {
        transform.LookAt(Camera.main.transform);
        transform.eulerAngles = new Vector3(transform.eulerAngles.x,180,transform.eulerAngles.z);
        healthBar.fillAmount = game.Enemy.hp / game.player_HP;
    }
}
