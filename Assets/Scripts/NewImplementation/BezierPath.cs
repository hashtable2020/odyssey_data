using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class BezierPath : MonoBehaviour
{
    public PathParams pathParams;

    public UIParams uiParams;
    
    // Sends the key pressed, the point selected (if any) (use -1 as a null value) and the position of the click (use Vector3.down)
    public delegate void EventHandler(KeyCode key, int index, Vector3 pos);
    // Depending on the event, either add, remove or split points
    public EventHandler PointHandler;
    
    // When refreshed, look at the points and create a mesh from those points
    public EventHandler Refresh;

    void Awake()
    {
        Debug.Log("Working!");
    }
    
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Working!");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

public class BezierPoint {
    public Vector3 BasePoint;
    public Vector3[] HandlePoints;

    public BezierPoint(Vector3 basePoint, Vector3[] handles)
    {
        BasePoint = basePoint;
        HandlePoints = handles;
    }
}

[System.Serializable]
public class PathParams
{
    public BezierPoint[][] CoursePoints;
    public float resolution = 0.1f;
    public bool closedLoop;
}

[System.Serializable]
public class UIParams
{
    public Collider roadCollider;
    public float maxMouseDistance = 1000f;
    public float pointSize = 0.1f;
    public Color curveColor = Color.green;
    public Texture2D curveTexture = null;
    public float curveWidth = 0.05f;
    public bool showNormals = true;
    // Width of the offset lines
    public float offsetWidth = 0.5f;
    public float normalDistance = 0.1f;
    public Color normalColor = Color.cyan;
    public float normalWidth = 0.01f;
}