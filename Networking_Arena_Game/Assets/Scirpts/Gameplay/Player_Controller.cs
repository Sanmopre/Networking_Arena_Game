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
    private Vector3 moveInput;
    
    // --- Dash Vars
    public float dashCooldown;
    public float dashDuration = 0.5f;
    private bool dashInCooldown = false;
    private float dashCooldownCount = 0.0f;

    //Shooting variables
    public Transform canonPosition;
    public GameObject bulletPrefab;
    public float bulletForce = 15f;

    //Special Attack
    public GameObject laserPrefab;
    private GameObject Instance;
    private LaserBehaviour LaserScript;

    void Start()
    {
        myRigidbody = GetComponent<Rigidbody>();
    }


    void Update()
    {
        GetMoveInput();

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

    private void FixedUpdate()
    {
        //Player movment
        MovePlayer();

        //Player rotation
        transform.LookAt(GetPlayerPointToLook());

        //SMOOOOTH CAMERA FOLLOW
        CameraFollow();

    }

    void GetMoveInput()
    {
        //moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        
        float xInput = Input.GetAxisRaw("Horizontal");
        float zInput = Input.GetAxisRaw("Vertical");

        GetDashInput(ref xInput,ref zInput);
        
        moveInput.x = xInput + zInput;                                                              // This is a workaround to the movement vs camera problem.
        moveInput.z = zInput - xInput;
    }

    void GetDashInput(ref float xInput, ref float zInput)
    {
        if (!dashInCooldown)
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.Space))
            {   
                if (Input.GetAxisRaw("Horizontal") != 0.0f)
                {
                    xInput *= 2;
                }
                if (Input.GetAxisRaw("Vertical") != 0.0f)
                {
                    zInput *= 2;
                }

                dashInCooldown = true;
            }
        }
        else
        {
            dashCooldownCount += Time.deltaTime;
            if (dashCooldownCount >= dashCooldown)
            {
                dashCooldownCount = 0.0f;
                dashInCooldown = false;
            }
        }
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
