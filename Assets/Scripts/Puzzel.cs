using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class Puzzel : MonoBehaviour

{
    public GameObject MyPuzzle;
    private void OnTriggerEnter( Collider other){
        if (other.tag=="Player")
        {
            MyPuzzle.SetActive(true);
            Debug.Log("player entred");
        }
    }
    private void OnTriggerExit( Collider other){
        if (other.tag=="Player")
        {
            MyPuzzle.SetActive(false);
            Debug.Log("player exist");
        }
    }
}
