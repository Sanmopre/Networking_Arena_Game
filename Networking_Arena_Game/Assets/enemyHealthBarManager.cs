using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyHealthBarManager : MonoBehaviour
{

    private void LateUpdate()
    {
        transform.LookAt(Camera.main.transform);
        transform.eulerAngles = new Vector3(transform.eulerAngles.x,180,transform.eulerAngles.z);
    }
}
