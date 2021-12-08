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

    //Granade Attack
    public GameObject grenadePrefab;
    public float grenadeForce;
    public float grenadeAngle;
    public float grenadeCooldown;
    private float grenadeTimer;

    //Animator
    private Animator animator;
    public float rotateThreshold;
    //float lookAndMoveAngle;

    void Start()
    {
        myRigidbody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        grenadeTimer = grenadeCooldown;
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
       
        if (Input.GetKeyDown(KeyCode.F) && grenadeTimer >= grenadeCooldown)
        {
            ShootGranade();
            grenadeTimer = 0;
        }
        else 
        {
            grenadeTimer += Time.deltaTime;
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
        //Vector3 crossProduct = Vector3.Cross(moveInput, transform.forward);

        float dot = Vector3.Dot(moveInput, transform.forward);

        //Animation state machine
        if (moveInput == Vector3.zero)
        {
            animator.SetInteger("Run", 0);
            return;
        }
        if (moveInput.x != 0 || moveInput.z != 0)
        {
            //Run forward
            if (dot > 1 - rotateThreshold && dot < 1 + rotateThreshold)
                animator.SetInteger("Run", 1);
            //Run backwards
            if (dot > -1 - rotateThreshold && dot < -1 + rotateThreshold)
                animator.SetInteger("Run", 2);
            //Run right
            if (dot > 0 - rotateThreshold && dot < 0 + rotateThreshold)
                animator.SetInteger("Run", 3);
            //Run left
            if (dot > 0 - rotateThreshold && dot < 0 + rotateThreshold && moveInput.x > 0)
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
       
        if (groundPlane.Raycast(cameraRay, out rayLenght))
        {
            pointToLook = cameraRay.GetPoint(rayLenght);
            Debug.DrawLine(cameraRay.origin, pointToLook, Color.blue);
        }

        //Adjustment
        return new Vector3(pointToLook.x, transform.position.y, pointToLook.z);
    }


    void ShootGranade()
    {
        //Get the X and Z target position
        Vector3 targetXZ = new Vector3(pointToLook.x, 0.0f, pointToLook.z);

        //Instantiate the grenade and set the correct orientation
        GameObject grenade = Instantiate(grenadePrefab, canonPosition.position, Quaternion.identity);
        grenade.gameObject.transform.LookAt(targetXZ);
        Vector3 projectileXZPos = new Vector3(grenade.gameObject.transform.position.x, 0.0f, grenade.gameObject.transform.position.z);

        //Variables to calculate the intial speed in an arc shot
        float R = Vector3.Distance(projectileXZPos, targetXZ);
        float G = Physics.gravity.y;
        float tanAlpha = Mathf.Tan(grenadeAngle * Mathf.Deg2Rad);
        float H = (targetXZ.y - grenade.gameObject.transform.position.y);

        //Calculate initial speed required to land the grenade on the target object 
        float Vz = Mathf.Sqrt(G * R * R / (2.0f * (H - R * tanAlpha)));
        float Vy = tanAlpha * Vz;

        //Create the velocity vector
        Vector3 localVelocity = new Vector3(0f, Vy, Vz);
        Vector3 globalVelocity = grenade.gameObject.transform.TransformDirection(localVelocity);

        //Shoot the grenade
        grenade.GetComponent<Rigidbody>().velocity = globalVelocity * grenadeForce;

    }
    void CameraFollow() 
    {
        Vector3 desiredPosition = new Vector3(transform.position.x - cameraOffset.x, cameraOffset.y, transform.position.z - cameraOffset.z);
        Vector3 smoothedPosition = Vector3.Lerp(mainCamera.transform.position, desiredPosition, cameraMovementFollowUpSpeed);
        mainCamera.transform.position = smoothedPosition;
    }

    void Shoot()
    {
        Vector3 offset = new Vector3(90, transform.rotation.eulerAngles.y, 0);
        GameObject bullet = Instantiate(bulletPrefab, canonPosition.position, Quaternion.Euler(canonPosition.forward + offset));

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
