using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Game_Manager : MonoBehaviour
{
    public Text endtext;
    public GameObject endTextObject;

    //player structure
    public struct player
    {
        public int lives;
        public float hp;
        //more stuff im guessing
    }

    //In seconds
    public float initial_time = 90;
    public int numberOfLifes = 4;
    public float player_HP = 100;
    private bool gameStarted = false;

    //players
    public player Player;
    public player Enemy;

    //Damage stats
    public int bulletDamage = 3;
    public int missileDamage = 35;
    public int shotgunDamage = 20;

    private bool playerWon = false;
    private bool enemyWon = false;

    //Respawn points
    public GameObject respawnPositionEnemy;
    public GameObject respawnPositionPlayer;
    private GameObject playerObject;
    private GameObject enemyObject;

    public Game_Menu_Manager menu_manager;
    
    void Start()
    {
        Player.hp = player_HP;
        Enemy.hp = player_HP;

        Player.lives = numberOfLifes;
        Enemy.lives = numberOfLifes;

        playerObject = GameObject.Find("Player");
        enemyObject = GameObject.Find("Enemy");
        endTextObject.SetActive(false);
        gameStarted = true;
    }

    void Update()
    {
        if (gameStarted)
        TimeManager();

        if(initial_time < 0)
            CheckIfWin();
    }

    void TimeManager()
    {
        initial_time -= Time.deltaTime;
    }

    //Public function to be used in player scripts when taking damage
    public void TakeDamage (int damage, int player) 
    {
        switch (player)
        {
            case 1:
                Player.hp -= damage;
                if (Player.hp <= 0)
                {
                    Respawn_Player();
                    Player.lives -= 1;
                    menu_manager.UpdatePlayerLifesUI();
                    CheckIfWin();
                }
                break;
            case 2:
                Enemy.hp -= damage;
                if (Enemy.hp <= 0)
                {
                    Respawn_Enemy();
                    Enemy.lives -= 1;
                    menu_manager.UpdatePlayerLifesUI();
                    CheckIfWin();
                }
                break;
            case 0:
                break;
        }
    }
    
    private void CheckIfWin()
    {
        if (Enemy.lives == 0) 
        {
            endtext.text = "VICTORY";
            endTextObject.SetActive(true);
        }
            

        if (Player.lives == 0)
        {
            endtext.text = "DEFEAT";
            endTextObject.SetActive(true);
        }


        if(Player.lives > Enemy.lives) 
        {
            endtext.text = "VICTORY";
            endTextObject.SetActive(true);
        }

        if (Player.lives < Enemy.lives)
        {
            endtext.text = "DEFEAT";
            endTextObject.SetActive(true);
        }
        

        if (Player.lives == Enemy.lives)
        {
            endtext.text = "TIE";
            endTextObject.SetActive(true);
        }

    }
    
    private void Respawn_Enemy() 
    {
        Enemy.hp = player_HP;
        enemyObject.transform.position = respawnPositionEnemy.transform.position;
    }

    private void Respawn_Player()
    {
        Player.hp = player_HP;
        playerObject.transform.position = respawnPositionPlayer.transform.position;
    }
}
