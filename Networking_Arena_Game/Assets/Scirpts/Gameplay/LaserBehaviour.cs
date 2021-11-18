using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserBehaviour : MonoBehaviour
{
    public GameObject hitEffect;
    public float hitOffset = 0;

    public float width;

    public float maxLength;
    private LineRenderer laser;

    public float mainTextureLength = 1f;
    public float noiseTextureLength = 1f;
    private Vector4 length = new Vector4(1, 1, 1, 1);

    private bool laserSaver = false;
    private bool updateSaver = false;

    private ParticleSystem[] effects;
    private ParticleSystem[] hit;

    void Start()
    {
        //Get LineRender and ParticleSystem components from current prefab;  
        laser = GetComponent<LineRenderer>();
        laser.startWidth = width;
        laser.endWidth = width;
        effects = GetComponentsInChildren<ParticleSystem>();
        hit = hitEffect.GetComponentsInChildren<ParticleSystem>();
    }

    void Update()
    {

        laser.material.SetTextureScale("_MainTex", new Vector2(length[0], length[1]));
        laser.material.SetTextureScale("_Noise", new Vector2(length[2], length[3]));

        //To set LineRender position
        if (laser != null && updateSaver == false)
        {
            laser.SetPosition(0, transform.position);
            RaycastHit hit;        
            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, maxLength))
            {
                //End laser position if collides with object
                laser.SetPosition(1, hit.point);
                hitEffect.transform.position = hit.point + hit.normal * hitOffset;

                //Hit effect zero rotation
                hitEffect.transform.rotation = Quaternion.identity;
                foreach (var AllPs in effects)
                {
                    if (!AllPs.isPlaying) AllPs.Play();
                }

                //Texture tiling
                length[0] = mainTextureLength * (Vector3.Distance(transform.position, hit.point));
                length[2] = noiseTextureLength * (Vector3.Distance(transform.position, hit.point));
            }
            else
            {
                //End laser position if doesn't collide with object
                var EndPos = transform.position + transform.forward * maxLength;
                laser.SetPosition(1, EndPos);
                hitEffect.transform.position = EndPos;

                foreach (var AllPs in this.hit)
                {
                    if (AllPs.isPlaying) AllPs.Stop();
                }
                //Texture tiling
                length[0] = mainTextureLength * (Vector3.Distance(transform.position, EndPos));
                length[2] = noiseTextureLength * (Vector3.Distance(transform.position, EndPos));
            }
            //Insurance against the appearance of a laser in the center of coordinates!
            if (laser.enabled == false && laserSaver == false)
            {
                laserSaver = true;
                laser.enabled = true;
            }
        }
    }

    public void DisablePrepare()
    {
        if (laser != null)
        {
            laser.enabled = false;
        }
        updateSaver = true;
        //Effects can be null in multiple shooting
        if (effects != null)
        {
            foreach (var AllPs in effects)
            {
                if (AllPs.isPlaying) AllPs.Stop();
            }
        }
    }
}
