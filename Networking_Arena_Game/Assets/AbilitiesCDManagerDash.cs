using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AbilitiesCDManagerDash : MonoBehaviour
{
    public Image cd;
    public Player_Controller player;
    // Start is called before the first frame update
    void Start()
    {
        cd.fillAmount = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        //quite dirty fix, whateva
        if (!player.firstDash) 
        {
            cd.fillAmount = 0;
        }
        else
        {
            cd.fillAmount = 1 - (player.dashCounter / player.dashCooldown);
        }
       
    }
}
