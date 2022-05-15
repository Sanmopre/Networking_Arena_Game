using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float speed;
    [HideInInspector]
    public Vector3 target;

    // Start is called before the first frame update
    void Start()
    {
        gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 lookingAt = (target - gameObject.transform.position).normalized;
        gameObject.GetComponent<Rigidbody>().velocity = lookingAt * speed;
    }
    private void OnCollisionEnter(Collision collision)
    {
        gameObject.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;

        gameObject.SetActive(false);
    }
}
