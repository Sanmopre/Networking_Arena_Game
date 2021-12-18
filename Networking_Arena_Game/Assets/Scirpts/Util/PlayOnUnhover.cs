using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlayOnUnhover : MonoBehaviour, IPointerExitHandler
{
    public AudioSource  audioSource;
    public AudioClip    audioClip;
    
    public void OnPointerExit(PointerEventData eventData)
    {
        audioSource.clip = audioClip;
        audioSource.Play();
    }
}

