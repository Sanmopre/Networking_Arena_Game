using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Game_Manager : MonoBehaviour
{
    //player structure
    public struct player
    {
        public int lives;
        public float hp;
        //more stuff im guessing
    }

    [Header("General Vars")]
    public float initial_time = 90;
    public int numberOfLifes = 4;
    public float player_HP = 100;
    private bool gameStarted = false;
    public int round = 0;

    [Header("Players")]
    public player Player;
    public player Enemy;

    [Header("Audio")]
    public AudioSource  bgMusic;
    public AudioSource  hitHurtSource;
    public AudioClip    hitSFX;
    public AudioClip    hurtSFX;

    [Header("Damage Stats")]
    public int bulletDamage = 3;
    public int missileDamage = 35;
    public int shotgunDamage = 20;

    private bool playerWon = false;
    private bool enemyWon = false;

    [Header("Respawn Points")]
    public GameObject respawnPositionEnemy;
    public GameObject respawnPositionPlayer;
    private GameObject playerObject;
    private GameObject enemyObject;

    [Header("UI")]
    public Game_Menu_Manager menu_manager;
    public Text endtext;
    public GameObject endTextObject;

    // NETWORKING
    Client client = null;
    
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

        if (!Globals.singlePlayer)
            client = GameObject.Find("Client").GetComponent<Client>();
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

                hitHurtSource.clip = hurtSFX;
                hitHurtSource.Play();

                if (Player.hp <= 0)
                {
                    Respawn();
                    Player.lives -= 1;
                    menu_manager.UpdatePlayerLifesUI();
                    CheckIfWin();
                }
                break;
            case 2:
                Enemy.hp -= damage;

                hitHurtSource.clip = hitSFX;
                hitHurtSource.Play();

                if (Enemy.hp <= 0)
                {
                    Respawn();
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

            if (client != null)
                client.RequestEnd();
        }
            

        if (Player.lives == 0)
        {
            endtext.text = "DEFEAT";
            endTextObject.SetActive(true);

            if (client != null)
                client.RequestEnd();
        }

        //if(Player.lives > Enemy.lives) 
        //{
        //    endtext.text = "VICTORY";
        //    endTextObject.SetActive(true);
        //}
        //
        //if (Player.lives < Enemy.lives)
        //{
        //    endtext.text = "DEFEAT";
        //    endTextObject.SetActive(true);
        //}
        //
        //
        //if (Player.lives == Enemy.lives)
        //{
        //    endtext.text = "TIE";
        //    endTextObject.SetActive(true);
        //}

    }

    private void Respawn()
    {
        Player.hp = player_HP;
        Enemy.hp = player_HP;
        if (!Globals.singlePlayer)
            playerObject.transform.position = GameObject.Find("Client").GetComponent<Client>().playerOriginalPos;
        else
        {
            playerObject.transform.position = respawnPositionPlayer.transform.position;
            enemyObject.transform.position = respawnPositionEnemy.transform.position;
        }

        ++round;
    }
}
