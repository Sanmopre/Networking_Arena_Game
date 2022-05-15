using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed;

    // Start is called before the first frame update
    void Start()
    {
        gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 lookingAt = (gameObject.transform.position * -1).normalized; // Boss is always at 0,0,0
        gameObject.GetComponent<Rigidbody>().velocity = lookingAt * speed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        gameObject.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;

        gameObject.SetActive(false);
    }
}
