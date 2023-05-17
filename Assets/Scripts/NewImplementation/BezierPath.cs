using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteAlways]
public class BezierPath : MonoBehaviour, ISerializationCallbackReceiver
{
    public PathParams pathParams;
    public UIParams uiParams;

    public BezierMesh roadMesh;

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
                // Keyboard shortcuts, ctrl+click to remove, shift+click to add, s+click to split the path, m+click to merge, d to deselect
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
                        centre += point.basePoint;
                    }
                    centre /= arrLength;
                    
                    //Debug.Log(centre);
                    //Debug.Log(arrLength);
                    
                    Vector3 tangent = (arrLength % 2 == 0) ? 
                        (pathParams.CoursePoints[index][arrLength / 2 - 1].localHandle +
                         pathParams.CoursePoints[index][arrLength / 2].localHandle) / 2
                    : pathParams.CoursePoints[index][(arrLength - 1) / 2].localHandle;
                    //Debug.Log(tangent);
                    Vector3 normal = Vector3.Normalize(Vector3.Cross(tangent, Vector3.up));
                    
                    //Debug.Log("Normal: " + normal);

                    BezierPoint[] splitPoints = new BezierPoint[arrLength + 1];
                    for (int i = 0; i < arrLength + 1; i++)
                    {
                        float x = (2 * (float) i - arrLength);
                        splitPoints[i] = new BezierPoint(Vector3.LerpUnclamped(centre, 
                            centre + pathParams.splitWidth * normal, x), tangent);
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
                            centre += point.basePoint;
                        }
                        centre /= arrLength;
                    
                        Vector3 tangent = (arrLength % 2 == 0) ? 
                            (pathParams.CoursePoints[index][arrLength / 2 - 1].localHandle +
                             pathParams.CoursePoints[index][arrLength / 2].localHandle) / 2
                            : pathParams.CoursePoints[index][(arrLength - 1) / 2].localHandle;
                        //Debug.Log(tangent);
                        Vector3 normal = Vector3.Normalize(Vector3.Cross(tangent, Vector3.up));
                    
                        //Debug.Log("Normal: " + normal);

                        BezierPoint[] splitPoints = new BezierPoint[arrLength - 1];
                        for (int i = 0; i < arrLength - 1; i++)
                        {
                            float x = (2 * (float) i - arrLength);
                            splitPoints[i] = new BezierPoint(Vector3.LerpUnclamped(centre, 
                                centre + pathParams.splitWidth * normal, x), tangent);
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
            //Refresh += roadMesh.UpdateMesh;

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

    Vector3[][] LocalHandleLocations(BezierPoint[][] coursePoints)
    {
        Vector3[][] newHandleLocations = new Vector3[coursePoints.Length][];
        for (int i = 0; i < coursePoints.Length + (pathParams.closedLoop ? 0 : -1); i++)
        {
            for (int j = 0; j < coursePoints[i].Length; j++)
            {
                Vector3 dirVector = Vector3.zero;
                for (int k = 0; k < coursePoints[LoopIndex(i - 1, coursePoints)].Length; k++)
                {
                    dirVector += (coursePoints[i - 1][k].basePoint - coursePoints[i][j].basePoint);
                }
                for (int l = 0; l < coursePoints[LoopIndex(i + 1, coursePoints)].Length; l++)
                {
                    dirVector += (coursePoints[i - 1][l].basePoint - coursePoints[i][j].basePoint);
                }

                dirVector = dirVector.normalized;
            }
        }
    }
    
    int LoopIndex(int i, BezierPoint[][] coursePoints)
    {
        return i % coursePoints.Length;
    }
}

[System.Serializable]
public class BezierPoint {
    public Vector3 basePoint;
    public Vector3 localHandle;
    public Vector3[] HandlePoints
    {
        get { return new[] { localHandle + basePoint, -localHandle + basePoint }; }
    }
    

    public BezierPoint(Vector3 basePoint, Vector3 handle)
    {
        this.basePoint = basePoint;
        // Makes handles relative
        localHandle = handle;
    }

    public static BezierPoint Zero = new BezierPoint(Vector3.zero, Vector3.zero);
}

[System.Serializable]
public class PathParams
{
    public BezierPoint[][] CoursePoints;
    
    
    public float resolution = 0.05f;
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