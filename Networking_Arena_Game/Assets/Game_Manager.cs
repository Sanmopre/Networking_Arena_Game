using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game_Manager : MonoBehaviour
{
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
    public int missileDamage = 20;
    public int shotgunDamage = 15;

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

        gameStarted = true;
    }

    void Update()
    {
        if (gameStarted)
        TimeManager();

        //Debug.Log(Enemy.hp);
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
            Debug.Log("Player won");

        if (Player.lives == 0)
            Debug.Log("Enemy won");
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
