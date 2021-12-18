using System.Collections;
using UnityEngine;

public class Enemy_Controller : MonoBehaviour
{
    public string username = "null";
    int animationState = -1;



    //TODO Aitor
    public void SetAnimationState(int state) { animationState = state; }
    private Animator animator;
    // Use this for initialization
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        animator.SetInteger("State", animationState);
    }
}