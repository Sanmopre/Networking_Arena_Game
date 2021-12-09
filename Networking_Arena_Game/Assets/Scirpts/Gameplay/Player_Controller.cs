using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Controller : MonoBehaviour
{
    private Rigidbody myRigidbody;
    public Camera mainCamera;
    public Vector3 cameraOffset;
    public float cameraMovementFollowUpSpeed;

    [Header("Movement Vars")]
    public float movementSpeed;
    private Vector3 moveInput = new Vector3();
    Vector3 pointToLook = new Vector3();
    public float speedReductionWhileShooting = 2.0f;

    [Header("Dash")]
    public float dashCooldown = 4.0f;
    public float dashDuration = 1.0f;
    public float dashForce = 50.0f;
    private bool inDash = false;
    private bool dashInCooldown = false;
    private float dashCounter = 0.0f;

    [Header("Shooting variables")]
    public Transform canonPosition;
    public GameObject bulletPrefab;
    public float bulletForce = 15f;
    public float firerate = 0.5f;
    float firerateCount = 0.0f;
    public float deviationRange = 0.05f;
    bool shooting = false;

    [Header("Shotgun")]
    public GameObject shotgunFire;
    public float fireDuration = 0.25f;
    public float shotgunfirerate = 1.0f;
    float shotCounter = 0;
    bool shootingShotgun = false;

    [Header("Missile Attack")]
    public GameObject crosshairPrefab;
    public GameObject explotionCollider;
    public GameObject missilePrefab;
    public float missileSpawnHeight = 40f;
    public float missileVelocity = -35f;
    public float expltionTimer = 2.0f;
    float explotionCounter = 0f;
    public float explotionDuration = 1.0f;
    bool missileComming = false;
    Vector3 explotionPosition;    
    public float grenadeCooldown;
    private float grenadeTimer;

    [Header("Animator")]
    private Animator animator;
    public float rotateThreshold;
    //float lookAndMoveAngle;

    public Vector3 cameraPositionForCanvas;
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
        //Firerate
        firerateCount += Time.deltaTime;
        shotCounter += Time.deltaTime;
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

        ManageExplotionFromMissile();
    }

    private void ShootingInput() 
    {
        //Shooting Behaviour
        if (!shootingShotgun && Input.GetMouseButton(0))
        {
            shooting = true;
            Shoot();
        }
        else 
        {
            shooting = false;
        }

        if (!shooting && Input.GetMouseButton(1))
        {
            shootingShotgun = true;
            ShoutgunAttack();

        }
        else 
        {
            shootingShotgun = false;
        }
       
        if (Input.GetKeyDown(KeyCode.F) && grenadeTimer >= grenadeCooldown)
        {
            ShootMissile();
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
            if (shooting) 
            {
                if (moveInput.x != 0 && moveInput.z != 0)
                {
                    myRigidbody.velocity = moveInput / 1.4f * movementSpeed / speedReductionWhileShooting;
                }
                else
                {
                    myRigidbody.velocity = moveInput * movementSpeed / speedReductionWhileShooting;
                }
            }
            else 
            {
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

    void ManageExplotionFromMissile() 
    {
        if(missileComming && explotionCounter > expltionTimer)
        {
            GameObject explotion = Instantiate(explotionCollider, explotionPosition, Quaternion.identity);
            Destroy(explotion, explotionDuration);
            missileComming = false;
        }
        else 
        {
            explotionCounter += Time.deltaTime;
        }
    }
    void ShootMissile()
    {
        Instantiate(crosshairPrefab, new Vector3(GetPlayerPointToLook().x, 1 , GetPlayerPointToLook().z), Quaternion.identity);
        GameObject grenade = Instantiate(missilePrefab, new Vector3(GetPlayerPointToLook().x, missileSpawnHeight, GetPlayerPointToLook().z), Quaternion.identity);
        grenade.GetComponent<Rigidbody>().velocity = new Vector3(0, missileVelocity, 0);
        missileComming = true;
        explotionCounter = 0;
        explotionPosition = GetPlayerPointToLook();

    }
    void CameraFollow() 
    {
        Vector3 desiredPosition = new Vector3(transform.position.x - cameraOffset.x, cameraOffset.y, transform.position.z - cameraOffset.z);
        cameraPositionForCanvas = desiredPosition;
        Vector3 smoothedPosition = Vector3.Lerp(mainCamera.transform.position, desiredPosition, cameraMovementFollowUpSpeed);
        mainCamera.transform.position = smoothedPosition;
    }

    void Shoot()
    {
        if (firerateCount > firerate)
        {
            Vector3 offset = new Vector3(90, transform.rotation.eulerAngles.y, 0);
            GameObject bullet = Instantiate(bulletPrefab, canonPosition.position, Quaternion.Euler(canonPosition.forward + offset));

            Rigidbody bulletRB = bullet.GetComponent<Rigidbody>();
            Vector3 randomDeviation = new Vector3(Random.Range(deviationRange, -deviationRange), 0, Random.Range(deviationRange, -deviationRange));
            bulletRB.AddForce((canonPosition.forward + randomDeviation) * bulletForce, ForceMode.Impulse);
            firerateCount = 0;
        }
    }

    void ShoutgunAttack() 
    {
        if (shotCounter > shotgunfirerate)
        {
            Vector3 offset = new Vector3(0, transform.rotation.eulerAngles.y, 0);
            GameObject shotgunFireObj = Instantiate(shotgunFire, canonPosition.position, Quaternion.Euler(canonPosition.forward + offset));
            Destroy(shotgunFireObj, fireDuration);
            shotCounter = 0;
            shotgunFireObj.transform.parent = transform;
        }
    }
}
