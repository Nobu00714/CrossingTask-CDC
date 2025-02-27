using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSizeAdjust : MonoBehaviour
{
    [SerializeField] private Camera camera;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(Screen.currentResolution.height);
        camera.orthographicSize = Screen.currentResolution.height/2;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
