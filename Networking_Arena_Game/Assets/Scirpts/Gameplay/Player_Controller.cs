using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Controller : MonoBehaviour
{
    private Rigidbody myRigidbody;
    public Camera mainCamera;
    public Vector3 cameraOffset;
    public float cameraMovementFollowUpSpeed;

    // Movement Vars
    public float movementSpeed;
    private Vector3 moveInput = new Vector3();
    Vector3 pointToLook = new Vector3();

    //Dash
    public float dashCooldown = 4.0f;
    public float dashDuration = 1.0f;
    public float dashForce = 50.0f;
    private bool inDash = false;
    private bool dashInCooldown = false;
    private float dashCounter = 0.0f;

    //Shooting variables
    public Transform canonPosition;
    public GameObject bulletPrefab;
    public float bulletForce = 15f;

    //Special Attack
    public GameObject laserPrefab;
    private GameObject Instance;
    private LaserBehaviour LaserScript;

    //Animator
    private Animator animator;
    Vector3 crossProduct = new Vector3();
    //float lookAndMoveAngle;

    void Start()
    {
        myRigidbody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
    }


    void Update()
    {
        GetMoveInput();
        ShootingInput();
        DashCountersLogic();

       
    }
    private void FixedUpdate()
    {
        //Player movment
        MovePlayer();

        //Animation Logic
        AnimatePlayer();

        //Player rotation
        transform.LookAt(GetPlayerPointToLook());

        //SMOOOOTH CAMERA FOLLOW
        CameraFollow();
    }

    private void ShootingInput() 
    {
        //Shooting Behaviour
        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
        if (Input.GetMouseButtonDown(1))
        {
            ShootLaser();
        }
        if (Input.GetMouseButtonUp(1))
        {
            DestroyLaser();
        }
    }

    private void Dash(Vector3 dashDirection) 
    {
        //diagonal force correction
        if(dashDirection.x != 0 && dashDirection.z != 0) 
        { 
            myRigidbody.AddForce(dashDirection / 2, ForceMode.Impulse);
        }
        else
        {
            myRigidbody.AddForce(dashDirection, ForceMode.Impulse);
        }
        dashInCooldown = true;
        inDash = true;
        dashCounter = 0.0f;
    }

    private void DashCountersLogic() 
    {
        dashCounter += Time.deltaTime;
        if(dashCounter > dashDuration)
            inDash = false;

        if (dashCounter > dashCooldown)
            dashInCooldown = false;
    }

    void GetMoveInput()
    {
        moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (Input.GetKeyDown(KeyCode.Space) && dashInCooldown == false)
        {
            Dash(moveInput * dashForce * 100);
        }
    }

    void MovePlayer() 
    {
        if(inDash == false)
        {
            //DIAGONAL MOVEMENT CORRECTION (NUT SURE)
            
            if (moveInput.x != 0 && moveInput.z != 0)
            {
                myRigidbody.velocity = moveInput/ 1.4f * movementSpeed;
            }
            else 
            {
                myRigidbody.velocity = moveInput * movementSpeed;
            
            }
        }

    }
    void AnimatePlayer()
    {
        //The function below will return the degrees between the point to look and the movement input so we can 
        // properly define the corresponding animation, the last parameter is the one the other two revolve around.

        //lookAndMoveAngle = Vector3.SignedAngle(moveInput.normalized, pointToLook.normalized, gameObject.transform.up);


        //Calculate the cross product between the point to look and the movement input so we can 
        //properly define the corresponding animation based on the direction the player is looking
        Vector3 crossProduct = Vector3.Cross(moveInput, pointToLook);

        //Animation state machine
        if (moveInput == Vector3.zero)
        {
            animator.SetInteger("Run", 0);
            return;
        }
        //Run Forward
        if (moveInput.z != 0 && crossProduct.y > 0)
        {
            animator.SetInteger("Run", 1);
            return;
        }
        //Run Backwards
        else if (moveInput.z != 0 && crossProduct.y < 0)
        {
            animator.SetInteger("Run", 2);
            return;
        }
        //Run right
        if (moveInput.x != 0 && crossProduct.y < 0)
        {
            animator.SetInteger("Run", 3);
        }
        //Run left
        else if (moveInput.x != 0 && crossProduct.y > 0)
        {
            animator.SetInteger("Run", 4);
        }
    }


        Vector3 GetPlayerPointToLook() 
    {
        //Ray to mouse position for rotation
        Ray cameraRay = mainCamera.ScreenPointToRay(Input.mousePosition);
        //Intersection with plane
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float rayLenght;
        //Vector3 pointToLook = new Vector3();
       
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
        GameObject bullet = Instantiate(bulletPrefab, canonPosition.position,Quaternion.Euler(canonPosition.forward));
        Rigidbody bulletRB = bullet.GetComponent<Rigidbody>();
        bulletRB.AddForce(canonPosition.forward * bulletForce, ForceMode.Impulse);
    }
    void ShootLaser()
    {
        Destroy(Instance);
        Instance = Instantiate(laserPrefab, canonPosition.position, canonPosition.rotation);
        Instance.transform.parent = transform;
        LaserScript = Instance.GetComponent<LaserBehaviour>();
    }
    void DestroyLaser()
    {
        LaserScript.DisablePrepare();
        Destroy(Instance, 1);
    }
}
