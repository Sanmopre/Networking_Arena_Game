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
    public float dashCounter = 0.0f;
    public bool firstDash = false;
    public ParticleSystem dashFx;
    private ParticleSystem dashParticles;

    [Header("Shooting variables")]
    public Transform canonPosition;
    public GameObject bulletPrefab;
    public float bulletForce = 15f;
    public float firerate = 0.5f;
    float firerateCount = 0.0f;
    public float deviationRange = 0.05f;
    bool shooting = false;
    private Vector3 rotationOffset = new Vector3();

    [Header("Shotgun")]
    public GameObject shotgunFirePrefab;
    public float shotgunfirerate = 1.0f;
    float shotCounter = 0;
    bool shootingShotgun = false;

    [Header("Missile Attack")]
    public GameObject missilePrefab;
    public float explosionTime = 1.0f;
    public float grenadeCooldown;
    public float grenadeTimer;

    [Header("Position Helper")]
    public Vector2 distanceThreshold;
    public GameObject playerPositionHelper;
    private GameObject enemyPlayer;

    [Header("Animator")]
    private Animator animator;
    public float rotateThreshold;
    private float dotProduct;




    public int GetAnimationState() { return -1; } // TODO Aitor


    //float lookAndMoveAngle;

    [Header("Audio Vars")]
    public float volume = 100;
    public AudioSource runSFX;
    public AudioSource dashSFX;
    public AudioSource shootRifleSFX;
    public AudioSource shootShotgunSFX;
    public AudioSource hitSFX;
    public AudioSource hurtSFX;

    // --- Networking ---
    Client client = null;
    // --- !Networking ---

    public Vector3 cameraPositionForCanvas;
    void Start()
    {
        myRigidbody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        grenadeTimer = grenadeCooldown;
        enemyPlayer = GameObject.Find("Enemy");
         
        if (!Globals.singlePlayer)
            client = GameObject.Find("Client").GetComponent<Client>();
        else
            gameObject.tag = "0";
    }


    void Update()
    {
        GetMoveInput();
        ShootingInput();
        DashCountersLogic();
        //Firerate
        firerateCount += Time.deltaTime;
        shotCounter += Time.deltaTime;

        //Enemy position tracker
        ManagePlayerPositionHelper();
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
        dashSFX.Play();
        
        if (!firstDash) 
        {
            firstDash = true;
        }
        dashParticles = Instantiate(dashFx, gameObject.transform);
        dashParticles.Play();
        Destroy(dashParticles, 1.0f);

        //diagonal force correction
        if (dashDirection.x != 0 && dashDirection.z != 0) 
        { 
            myRigidbody.AddForce(dashDirection / 2, ForceMode.Impulse);
        }
        else
        {
            myRigidbody.AddForce(dashDirection, ForceMode.Impulse);
        }

        gameObject.GetComponentInChildren<SkinnedMeshRenderer>().enabled = false;
        dashInCooldown = true;
        inDash = true;
        dashCounter = 0.0f;
    }

    private void DashCountersLogic() 
    {
        dashCounter += Time.deltaTime;
        if(dashCounter > dashDuration && !gameObject.GetComponentInChildren<SkinnedMeshRenderer>().enabled)
        {
            gameObject.GetComponentInChildren<SkinnedMeshRenderer>().enabled = true;
            inDash = false;
        }

        if (dashCounter > dashCooldown)
            dashInCooldown = false;
    }

    void GetMoveInput()
    {
        moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (Input.GetKeyDown(KeyCode.Space) && dashInCooldown == false && (moveInput.x != 0 || moveInput.z != 0))
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
        dotProduct = Vector3.Dot(moveInput, transform.forward);

        //Animation state machine
        if (moveInput == Vector3.zero)
        {
            if (shooting || shootingShotgun)
                animator.SetInteger("State", 5);
            else
                animator.SetInteger("State", 0);

            if(Input.GetKeyDown(KeyCode.G))
                animator.SetInteger("State", 6);

            
            return;
        }
        if (moveInput.x != 0 || moveInput.z != 0)
        {
            //If player start running cut remaining animations
            animator.SetInteger("State", 0);

            //Run forward
            if (dotProduct > 1 - rotateThreshold && dotProduct < 1 + rotateThreshold)
                animator.SetInteger("State", 1);
            //Run backwards
            if (dotProduct > -1 - rotateThreshold && dotProduct < -1 + rotateThreshold)
                animator.SetInteger("State", 2);
            //Run right
            if (dotProduct > 0 - rotateThreshold && dotProduct < 0 + rotateThreshold)
                animator.SetInteger("State", 3);
            //Run left
            if (dotProduct > 0 - rotateThreshold && dotProduct < 0 + rotateThreshold && moveInput.x > 0)
                animator.SetInteger("State", 4);
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

    void ShootMissile()
    {
        if (!Globals.singlePlayer)
            client.RequestMissile(new Vector3(GetPlayerPointToLook().x, 1, GetPlayerPointToLook().z), explosionTime);
        else
            InstantiateMissile(new Vector3(GetPlayerPointToLook().x, 1, GetPlayerPointToLook().z), explosionTime);
    }
    public void InstantiateMissile(Vector3 position, float time)
    {
        Instantiate(missilePrefab, position, Quaternion.identity).GetComponent<MissileAttack>().explosionTimer = time;
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
            Vector3 randomDeviation = new Vector3(Random.Range(deviationRange, -deviationRange), 0, Random.Range(deviationRange, -deviationRange));
            Vector3 direction = transform.forward + randomDeviation;
            if (!Globals.singlePlayer)
                client.RequestBullet(canonPosition.position, direction);
            else
                InstantiateBullet(canonPosition.position, direction, 0);

            firerateCount = 0;
        }
    }

    public void InstantiateBullet(Vector3 position, Vector3 direction, int shooterID)
    {
        Vector3 offset = new Vector3(90, transform.rotation.eulerAngles.y, 0);
        GameObject bullet = Instantiate(bulletPrefab, position, Quaternion.Euler(direction + offset));
        bullet.tag = shooterID.ToString();

        Rigidbody bulletRB = bullet.GetComponent<Rigidbody>();
        bulletRB.AddForce(direction * bulletForce, ForceMode.Impulse);
    }

    void ShoutgunAttack() 
    {
        if (shotCounter > shotgunfirerate)
        {
            Vector3 offset = new Vector3(0, transform.rotation.eulerAngles.y, 0);

            if (!Globals.singlePlayer)
                client.RequestShotgun(canonPosition.position, canonPosition.forward + offset);
            else
                InstantiateShotgun(canonPosition.position, canonPosition.forward + offset, gameObject, 0);


            shotCounter = 0;
        }
    }

    public void InstantiateShotgun(Vector3 position, Vector3 direction, GameObject parent, int shooterID)
    {
        GameObject shotgunFire = Instantiate(shotgunFirePrefab, position, Quaternion.Euler(direction), parent.transform);
        shotgunFire.tag = shooterID.ToString();
    }

    void ManagePlayerPositionHelper() 
    {
        
       if (distanceThreshold.x < Mathf.Abs(transform.position.x - enemyPlayer.transform.position.x) || distanceThreshold.y < Mathf.Abs(transform.position.z - enemyPlayer.transform.position.z))
       {
           playerPositionHelper.SetActive(true);
       }
       else 
       {
           playerPositionHelper.SetActive(false);
       }

       playerPositionHelper.transform.position = new Vector3(transform.position.x, transform.position.y - 4f, transform.position.z) ;
       playerPositionHelper.transform.LookAt(enemyPlayer.transform.position);
       playerPositionHelper.transform.eulerAngles = new Vector3(0, playerPositionHelper.transform.eulerAngles.y + 180, 0);
    }
}
