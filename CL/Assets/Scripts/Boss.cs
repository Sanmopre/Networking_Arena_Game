using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss : MonoBehaviour
{
    public int hp;

    public GameObject weak_points_go;
    public GameObject bullet;

    WeakPoint[] weak_points = null;
    int wp_amount = 0;

    void OpenWeakPoint()
    {
        /* 
        // Test
        int[] indices_amount = new int[wp_amount];
        for (int i = 0; i < wp_amount; ++i)
            indices_amount[i] = 0;

        for (int i = 0; i < 100; ++i)
        {
            int idx = Random.Range(0, wp_amount);
            ++indices_amount[idx];
        }

        for (int i = 0; i < wp_amount; ++i)
            Debug.Log("WeakPoint " + i + " used " + indices_amount[i] + " times.");
        */
        int index = Random.Range(0, wp_amount);
        weak_points[index].gameObject.SetActive(true);
    }

    void Start()
    {
        wp_amount = weak_points_go.transform.childCount;
        weak_points = new WeakPoint[wp_amount];

        for (int i = 0; i < wp_amount; ++i)
        {
            weak_points[i] = weak_points_go.transform.GetChild(i).gameObject.GetComponent<WeakPoint>();
            weak_points[i].boss = this;
            weak_points[i].gameObject.SetActive(false);
        }

        OpenWeakPoint();
    }

    void BulletAttack()
    {
        bullet.SetActive(true);
        bullet.transform.position = gameObject.transform.position;
    }

    public void WeakPointHit()
    {
        --hp;
        if (hp <= 0)
            gameObject.SetActive(false);
        else
            OpenWeakPoint();
    }

    void Update()
    {

    }
}
