using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AbilitiesCDManagerDash : MonoBehaviour
{
    [Header("General")]
    public Image cd;
    public Player_Controller player;

    [Header("Audio")]
    public AudioSource  audioSource;
    public AudioClip    cooldownCompleteSFX;

    private bool onCooldown = false;

    void Start()
    {
        cd.fillAmount = 0f;
    }

    void Update()
    {
        //quite dirty fix, whateva
        if (!player.firstDash) 
        {
            cd.fillAmount = 0f;
        }
        else
        {
            cd.fillAmount = 1 - (player.dashCounter / player.dashCooldown);
        }
        
        ManageCooldownSFX();
    }

    void ManageCooldownSFX()
    {
        if (cd.fillAmount > 0.0f && !onCooldown)
        {
            onCooldown = true;
        }

        if ((cd.fillAmount <= 0.0f) && onCooldown)
        {
            audioSource.clip = cooldownCompleteSFX;
            audioSource.Play();

            onCooldown = false;
        }
    }
}
