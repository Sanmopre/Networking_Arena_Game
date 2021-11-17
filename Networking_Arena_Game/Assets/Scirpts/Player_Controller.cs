using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Controller : MonoBehaviour
{
    public float movementSpeed;
    private Rigidbody myRigidbody;
    public Camera mainCamera;
    public Vector3 cameraOffset;

    private Vector3 moveInput;


    void Start()
    {
        myRigidbody = GetComponent<Rigidbody>();
    }


    void Update()
    {
        //Get input//
        moveInput = new Vector3(Input.GetAxisRaw("Horizontal"),0f,Input.GetAxisRaw("Vertical"));

    }

    private void FixedUpdate()
    {
        //Player movment
        MovePlayer();

        //Player rotation
        transform.LookAt(GetPlayerPointToLook());
    }

    void MovePlayer() 
    {
        myRigidbody.velocity = moveInput * movementSpeed;
        mainCamera.transform.position = new Vector3(transform.position.x - cameraOffset.x, cameraOffset.y, transform.position.z - cameraOffset.z);
    }

    Vector3 GetPlayerPointToLook() 
    {
        //Ray to mouse position for rotation
        Ray cameraRay = mainCamera.ScreenPointToRay(Input.mousePosition);
        //Intersection with plane
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float rayLenght;
        Vector3 pointToLook = new Vector3();
       
        if (groundPlane.Raycast(cameraRay, out rayLenght))
        {
            pointToLook = cameraRay.GetPoint(rayLenght);
            Debug.DrawLine(cameraRay.origin, pointToLook, Color.blue);
        }

        //Adjustment
        return new Vector3(pointToLook.x, transform.position.y, pointToLook.z);
    }
}
