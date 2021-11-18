using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Controller : MonoBehaviour
{
    public float movementSpeed;
    private Rigidbody myRigidbody;
    public Camera mainCamera;
    public Vector3 cameraOffset;
    public float cameraMovementFollowUpSpeed;

    private Vector3 moveInput;

    //Shooting variables
    public Transform canonPosition;
    public GameObject bulletPrefab;
    public float bulletForce = 15f;


    void Start()
    {
        myRigidbody = GetComponent<Rigidbody>();
    }


    void Update()
    {
        //Get input//
        moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

        //Shooting Behaviour
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Shoot();
        }
    }

    private void FixedUpdate()
    {
        //Player movment
        MovePlayer();

        //Player rotation
        transform.LookAt(GetPlayerPointToLook());

        //SMOOOOTH CAMERA FOLLOW
        CameraFollow();


    }


    void MovePlayer() 
    {
        myRigidbody.velocity = moveInput * movementSpeed;
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

    void CameraFollow() 
    {
        Vector3 desiredPosition = new Vector3(transform.position.x - cameraOffset.x, cameraOffset.y, transform.position.z - cameraOffset.z);
        Vector3 smoothedPosition = Vector3.Lerp(mainCamera.transform.position, desiredPosition, cameraMovementFollowUpSpeed);
        mainCamera.transform.position = smoothedPosition;
    }

    void Shoot()
    {
        GameObject bullet = Instantiate(bulletPrefab, canonPosition.position, Quaternion.identity);
        Rigidbody bulletRB = bullet.GetComponent<Rigidbody>();
        bulletRB.AddForce(canonPosition.forward * bulletForce, ForceMode.Impulse);
    }
}
