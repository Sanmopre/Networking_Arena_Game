using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class healthBarManager : MonoBehaviour
{
    public Player_Controller player;

    private void LateUpdate()
    {
        transform.LookAt(player.cameraPositionForCanvas);
        transform.Rotate(0,180,0);
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0);
    }
}
