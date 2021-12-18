using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlayOnHover : MonoBehaviour, IPointerEnterHandler
{
    public AudioSource  audioSource;
    public AudioClip    audioClip;
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        audioSource.clip = audioClip;
        audioSource.Play();
        
        Debug.Log("Consider Yourself: Hovered");
    }
}
