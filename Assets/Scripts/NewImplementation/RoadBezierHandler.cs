using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RoadBezierHandler : MonoBehaviour
{
    [Header("Course Points")]
    public Transform[] coursePoints;
    
    private EditorApplication.CallbackFunction updateFn = EditorApplication.update;

    void Awake()
    {
        // Show curve points 
        
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
