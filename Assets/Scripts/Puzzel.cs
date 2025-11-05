using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Puzzel : MonoBehaviour

{
    public GameObject Mypuzzle;
    private void OnTeiggerEnter( Collider other){
        if (other.tag=="Player")
        {
            Mypuzzle.SetActive(true);
            Debug.Log("player entred");
        }
    }
    private void OnTeiggerExit( Collider other){
        if (other.tag=="Player")
        {
            Mypuzzle.SetActive(false);
            Debug.Log("player exist");
        }
    }
}
