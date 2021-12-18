using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AbilitiesCDManager : MonoBehaviour
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
        cd.fillAmount = 1 - (player.grenadeTimer / player.grenadeCooldown);

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
