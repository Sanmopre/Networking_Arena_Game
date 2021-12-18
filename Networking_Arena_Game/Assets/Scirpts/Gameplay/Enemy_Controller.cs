using System.Collections;
using UnityEngine;

public class Enemy_Controller : MonoBehaviour
{
    public string username = "null";
    int animationState = -1;

    public void SetAnimationState(int state) { animationState = state; }
    private Animator animator;

    void Start() 
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        animator.SetInteger("State", animationState);
    }
}