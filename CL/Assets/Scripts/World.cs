using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public float rotation_speed;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.A))
        {
            Vector3 euler = gameObject.transform.rotation.eulerAngles;
            euler.y += rotation_speed * Time.deltaTime;
            Quaternion rotation = Quaternion.Euler(euler);
            gameObject.transform.rotation = rotation;
        }
        if (Input.GetKey(KeyCode.D))
        {
            Vector3 euler = gameObject.transform.rotation.eulerAngles;
            euler.y -= rotation_speed * Time.deltaTime;
            Quaternion rotation = Quaternion.Euler(euler);
            gameObject.transform.rotation = rotation;
        }
    }
}
