using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class BezierPath : MonoBehaviour, ISerializationCallbackReceiver
{
    public PathParams pathParams;

    public UIParams uiParams;

    // Sends the key pressed, the point selected (if any) (use -1 as a null value) and the position of the click (use Vector3.down)
    public delegate void EventHandler(KeyCode key, int index, Vector3 pos);

    public delegate void Handler();
    // Depending on the event, either add, remove or split points
    public EventHandler PointHandler;
    
    // When refreshed, look at the points and create a mesh from those points
    public Handler Refresh;
    public Handler Reset;

    public void OnBeforeSerialize()
    {
        if (PointHandler == null || Reset == null)
        {
            if (pathParams.CoursePoints == null)
            {
                pathParams.CoursePoints = new [] {new [] {BezierPoint.Zero}};
            }
            PointHandler += (key, index, pos) =>
            {
                if (key == KeyCode.LeftShift)
                {
                    BezierPoint[][] tmpPoints = new BezierPoint[pathParams.CoursePoints.Length + 1][];
                    pathParams.CoursePoints.CopyTo(tmpPoints, 0);
                    pathParams.CoursePoints = tmpPoints;
                    pathParams.CoursePoints[^1] = new[] { new BezierPoint(pos, 0.5f * Vector3.right) };
                } else if (key == KeyCode.LeftControl)
                {
                    if (index != -1)
                    {
                        BezierPoint[][] tmpPoints = new BezierPoint[pathParams.CoursePoints.Length - 1][];
                        int currentIndex = 0;
                        for (int i = 0; i < pathParams.CoursePoints.Length; i++)
                        {
                            if (i != index)
                            {
                                tmpPoints[currentIndex] = pathParams.CoursePoints[i];
                                currentIndex++;
                            }
                        }

                        pathParams.CoursePoints = tmpPoints;
                    }
                    
                } else if (key == KeyCode.S)
                {
                    BezierPoint[][] tmpPoints = new BezierPoint[pathParams.CoursePoints.Length][];
                    int arrLength = pathParams.CoursePoints[index].Length;
                    Vector3 centre = Vector3.zero;

                    foreach (BezierPoint point in pathParams.CoursePoints[index])
                    {
                        centre += point.BasePoint;
                    }
                    centre /= arrLength;
                    
                    Vector3 tangent = (arrLength % 2 == 0) ? 
                        (pathParams.CoursePoints[index][arrLength / 2 - 1].LocalHandle +
                         pathParams.CoursePoints[index][arrLength / 2].LocalHandle) / 2
                    : pathParams.CoursePoints[index][(arrLength - 1) / 2].LocalHandle;
                    //Debug.Log(tangent);
                    Vector3 normal = Vector3.Normalize(Vector3.Cross(tangent, Vector3.up));
                    
                    //Debug.Log("Normal: " + normal);

                    BezierPoint[] splitPoints = new BezierPoint[arrLength + 1];
                    for (int i = 0; i < arrLength + 1; i++)
                    {
                        splitPoints[i] = new BezierPoint(Vector3.LerpUnclamped(centre, 
                            centre + pathParams.splitWidth * normal, i - (arrLength - 1) / 2), tangent);
                    }

                    for (int j = 0; j < pathParams.CoursePoints.Length; j++)
                    {
                        if (j == index)
                        {
                            tmpPoints[j] = splitPoints;
                        }
                        else
                        {
                            tmpPoints[j] = pathParams.CoursePoints[j];
                        }
                    }

                    pathParams.CoursePoints = tmpPoints;
                    //Debug.Log("First split point: " + pathParams.CoursePoints[index][0].BasePoint + ", Second split point: " + pathParams.CoursePoints[index][1].BasePoint);
                } else if (key == KeyCode.M)
                {
                    if (pathParams.CoursePoints[index].Length != 1)
                    {
                        BezierPoint[][] tmpPoints = new BezierPoint[pathParams.CoursePoints.Length][];
                        int arrLength = pathParams.CoursePoints[index].Length;
                        Vector3 centre = Vector3.zero;

                        foreach (BezierPoint point in pathParams.CoursePoints[index])
                        {
                            centre += point.BasePoint;
                        }
                        centre /= arrLength;
                    
                        Vector3 tangent = (arrLength % 2 == 0) ? 
                            (pathParams.CoursePoints[index][arrLength / 2 - 1].LocalHandle +
                             pathParams.CoursePoints[index][arrLength / 2].LocalHandle) / 2
                            : pathParams.CoursePoints[index][(arrLength - 1) / 2].LocalHandle;
                        //Debug.Log(tangent);
                        Vector3 normal = Vector3.Normalize(Vector3.Cross(tangent, Vector3.up));
                    
                        //Debug.Log("Normal: " + normal);

                        BezierPoint[] splitPoints = new BezierPoint[arrLength - 1];
                        for (int i = 0; i < arrLength - 1; i++)
                        {
                            splitPoints[i] = new BezierPoint(Vector3.LerpUnclamped(centre, 
                                centre + pathParams.splitWidth * normal, i - (arrLength - 1) / 2 - 1), tangent);
                        }

                        for (int j = 0; j < pathParams.CoursePoints.Length; j++)
                        {
                            if (j == index)
                            {
                                tmpPoints[j] = splitPoints;
                            }
                            else
                            {
                                tmpPoints[j] = pathParams.CoursePoints[j];
                            }
                        }

                        pathParams.CoursePoints = tmpPoints;
                    }
                }
                
            };

            Reset += () =>
            {
                pathParams.CoursePoints = new[] { new[] { BezierPoint.Zero } };
                Refresh();
            };
        }
    }

    public void OnAfterDeserialize()
    {
        
    }
    
    // Start is called before the first frame update
    void Start()
    {
        if (!Application.IsPlaying(gameObject))
        {
            Debug.Log("Working!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

public class BezierPoint {
    public Vector3 BasePoint;
    public Vector3 LocalHandle;
    public Vector3[] HandlePoints
    {
        get { return new[] { LocalHandle + BasePoint, -LocalHandle + BasePoint }; }
    }
    

    public BezierPoint(Vector3 basePoint, Vector3 handle)
    {
        BasePoint = basePoint;
        // Makes handles relative
        LocalHandle = handle;
    }

    public static BezierPoint Zero = new BezierPoint(Vector3.zero, Vector3.zero);
}

[System.Serializable]
public class PathParams
{
    public BezierPoint[][] CoursePoints;
    
    
    public float resolution = 0.01f;
    public bool closedLoop;
    public float splitWidth = 0.1f;
}

[System.Serializable]
public class UIParams
{
    public Collider roadCollider;
    public float maxMouseDistance = 1000f;
    public float pointSize = 0.05f;
    public Color pointColor = Color.red;
    public Color handleDiskColor = Color.red;
    public Color handleColor = Color.white;
    public float handleSize = 0.02f;
    public float handleWidth = 0.01f;
    public Color curveColor = Color.green;
    public Texture2D curveTexture = null;
    public float curveWidth = 0.1f;
    public bool showNormals = true;
    // Width of the offset lines
    public float offsetWidth = 0.5f;
    public float normalDistance = 0.03f;
    public Color normalColor = Color.cyan;
    public float normalWidth = 0.01f;
}