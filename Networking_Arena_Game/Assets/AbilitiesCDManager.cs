using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AbilitiesCDManager : MonoBehaviour
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
        cd.fillAmount = 1 - (player.grenadeTimer / player.grenadeCooldown);
    }
}
