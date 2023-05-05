using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BezierDisplay : MonoBehaviour
{
    public float resolution = 0.1f;
    public Transform[] coursePoints;
    
    
    private EditorApplication.CallbackFunction updateFn = EditorApplication.update;

    void KeyboardHandler()
    {
        // Keyboard shortcuts: Click to move points, Shift+click to add points, Alt+click to remove points, S to split points
        if (Input.GetKeyDown("alt"))
        {
            
        }
    }
    
    void Awake()
    {
        
    }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDrawGizmos()
    {
        
    }
    
    
    
    Vector3 DeCasteljau(float t, in Vector3[] points)
    {
        Vector3[] oldPoints = points;
        Vector3[] newPoints = new Vector3[points.Length - 1];
        for (int j = 0; j < points.Length - 1; j++)
        {
            for (int i = 0; i < oldPoints.Length - 1; i++)
            {
                newPoints[i] = Vector3.Lerp(oldPoints[i], oldPoints[i + 1], t);
            }

            // Resets the process
            oldPoints = newPoints;
            if (oldPoints.Length > 0)
            {
                newPoints = new Vector3[oldPoints.Length - 1];
            }
        }

        return oldPoints[0];
    }
}
