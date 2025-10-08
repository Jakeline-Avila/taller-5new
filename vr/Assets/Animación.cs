using UnityEngine;
using System.Collections;
using System.Collections.Generic;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
  

public class Animacion : MonoBehaviour
{
    void Start()
    {
        var anim = GetComponent<Animation>();
        anim["Armature|*Nadar*"].wrapMode = WrapMode.Loop; // Repite el clip
        anim.Play("Armature|*Nadar*"); // Reproduce la animación
    }
}






