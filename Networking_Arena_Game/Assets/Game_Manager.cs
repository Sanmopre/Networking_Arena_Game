using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game_Manager : MonoBehaviour
{
    public struct player
    {
        public int lives;
        public int hp;
        //more stuff im guessing
    }

    //In seconds
    public float initial_time = 90;
    public int numberOfLifes = 3;
    public int player_HP = 100;
    private bool gameStarted = false;
    public player Player1;
    public player Player2;
    
    void Start()
    {
        gameStarted = true;
    }

    void Update()
    {
        if (gameStarted)
        TimeManager();
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
                Player1.hp -= damage;
                if (Player1.hp >= 0)
                {
                    Player1.lives -= 1;
                    CheckIfWin();
                }
                break;
            case 2:
                Player2.hp -= damage;
                if (Player2.hp >= 0)
                {
                    Player2.lives -= 1;
                    CheckIfWin();
                }
                break;
            case 0:
                break;
        }
    }
    
    private void CheckIfWin()
    {
        if (Player2.lives == 0)
            Debug.Log("Player 1 won");

        if (Player1.lives == 0)
            Debug.Log("Player 2 won");
    }
    
}
