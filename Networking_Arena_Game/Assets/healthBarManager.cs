using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class healthBarManager : MonoBehaviour
{
    public Player_Controller player;
    public Image healthBar;
    public Game_Manager game;

    private void LateUpdate()
    {
        transform.LookAt(player.cameraPositionForCanvas);
        transform.Rotate(0,180,0);
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0);
        healthBar.fillAmount = game.Player.hp / game.player_HP;
    }
}
