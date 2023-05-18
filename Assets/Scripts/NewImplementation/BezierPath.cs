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
                    pathParams.CoursePoints[^1] = new[] { new BezierPoint(pos, new [] {Vector3.zero, Vector3.zero}) };
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

                    Vector3[] tangent =
                    {
                        (arrLength % 2 == 0)
                            ? (pathParams.CoursePoints[index][arrLength / 2 - 1].localHandles[0] +
                               pathParams.CoursePoints[index][arrLength / 2].localHandles[0]) / 2
                            : pathParams.CoursePoints[index][(arrLength - 1) / 2].localHandles[0],
                        (arrLength % 2 == 0)
                            ? (pathParams.CoursePoints[index][arrLength / 2 - 1].localHandles[1] +
                               pathParams.CoursePoints[index][arrLength / 2].localHandles[1]) / 2
                            : pathParams.CoursePoints[index][(arrLength - 1) / 2].localHandles[1]
                    };
                    //Debug.Log(tangent);
                    Vector3 normal = Vector3.Normalize(Vector3.Cross(tangent[0], Vector3.up));
                    
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
                    
                        Vector3[] tangent =
                        {
                            (arrLength % 2 == 0)
                                ? (pathParams.CoursePoints[index][arrLength / 2 - 1].localHandles[0] +
                                   pathParams.CoursePoints[index][arrLength / 2].localHandles[0]) / 2
                                : pathParams.CoursePoints[index][(arrLength - 1) / 2].localHandles[0],
                            (arrLength % 2 == 0)
                                ? (pathParams.CoursePoints[index][arrLength / 2 - 1].localHandles[1] +
                                   pathParams.CoursePoints[index][arrLength / 2].localHandles[1]) / 2
                                : pathParams.CoursePoints[index][(arrLength - 1) / 2].localHandles[1]
                        };
                        //Debug.Log(tangent);
                        Vector3 normal = Vector3.Normalize(Vector3.Cross(tangent[0], Vector3.up));
                    
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
            Refresh += () =>
            {
                Vector3[][] handleLocations = LocalHandleLocations(pathParams.CoursePoints);
                for (int i = 0; i < pathParams.CoursePoints.Length; i++)
                {
                    for (int j = 0; j < pathParams.CoursePoints[i].Length; j++)
                    {
                        
                        if (!pathParams.CoursePoints[i][j].handlesChanged)
                        {
                            
                            pathParams.CoursePoints[i][j].localHandles = new[]
                            {
                                handleLocations[i][2 * j],
                                handleLocations[i][2 * j + 1],
                            };
                        }
                        else
                        {
                            //Debug.Log("i: " + i + ", j: " + j);
                        }
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

    Vector3[][] LocalHandleLocations(BezierPoint[][] coursePoints)
    {
        Vector3[][] newHandleLocations = new Vector3[coursePoints.Length][];
        for (int i = 0; i < coursePoints.Length; i++)
        {
            newHandleLocations[i] = new Vector3[2 * coursePoints[i].Length];
            for (int j = 0; j < coursePoints[i].Length; j++)
            {
                Vector3 dirVector1 = Vector3.zero;
                Vector3 dirVector2 = Vector3.zero;
                float[][] neighbourDistances = new float[2][];

                neighbourDistances[0] = new float[coursePoints[LoopIndex(i - 1, coursePoints)].Length];
                neighbourDistances[1] = new float[coursePoints[LoopIndex(i + 1, coursePoints)].Length];
                int firstNeighbour = 0;
                int secondNeighbour = 0;
                for (int k = 0; k < coursePoints[LoopIndex(i - 1, coursePoints)].Length; k++)
                {
                    Vector3 offset = (coursePoints[LoopIndex(i - 1, coursePoints)][k].basePoint - coursePoints[i][j].basePoint);
                    dirVector1 += offset.normalized;
                    neighbourDistances[0][firstNeighbour] = offset.magnitude;
                    firstNeighbour++;
                }
                for (int l = 0; l < coursePoints[LoopIndex(i + 1, coursePoints)].Length; l++)
                {
                    Vector3 offset =(coursePoints[LoopIndex(i + 1, coursePoints)][l].basePoint - coursePoints[i][j].basePoint);
                    dirVector2 += offset.normalized;
                    neighbourDistances[1][secondNeighbour] = offset.magnitude;
                    secondNeighbour++;
                }

                float[] collapsedDistances = new float[2];

                for (int x = 0; x < 2; x++)
                {
                    foreach (float dist in neighbourDistances[x])
                    {
                        collapsedDistances[x] += dist;
                    }

                    collapsedDistances[x] /= neighbourDistances[x].Length;
                }

                dirVector1.Normalize();
                dirVector2.Normalize();
                Vector3 normalVector = Vector3.Normalize(Vector3.Cross((dirVector1 + dirVector2), Vector3.up));
                
                // Makes sure that the normal vector points towards the start of the path
                normalVector *= Mathf.Sign(Vector3.Dot(normalVector, dirVector1));

                if (uiParams.handlesSeparate)
                {
                    newHandleLocations[i][2 * j] = collapsedDistances[0] * pathParams.controlLength * normalVector;
                    newHandleLocations[i][2 * j + 1] = - collapsedDistances[1] * pathParams.controlLength * normalVector;
                }
                else
                {
                    float avgDistance = (collapsedDistances[0] + collapsedDistances[1]) / 2;
                    newHandleLocations[i][2 * j] = avgDistance * pathParams.controlLength * normalVector;
                    newHandleLocations[i][2 * j + 1] = -avgDistance * pathParams.controlLength * normalVector;
                }
            }
        }

        return newHandleLocations;
    }
    
    int LoopIndex(int i, BezierPoint[][] coursePoints)
    {
        return (i % coursePoints.Length + coursePoints.Length) % coursePoints.Length;
    }
}

[System.Serializable]
public class BezierPoint {
    public Vector3 basePoint;
    public Vector3[] localHandles;
    public bool handlesChanged;
    public Vector3[] HandlePoints
    {
        get { return new[] { localHandles[0] + basePoint, localHandles[1] + basePoint }; }
    }
    

    public BezierPoint(Vector3 basePoint, Vector3[] handles)
    {
        this.basePoint = basePoint;
        // Makes handles relative
        localHandles = handles;
        handlesChanged = false;
    }

    public static BezierPoint Zero = new BezierPoint(Vector3.zero, new [] {Vector3.zero, Vector3.zero});
}

[System.Serializable]
public class PathParams
{
    public BezierPoint[][] CoursePoints;
    
    
    public float resolution = 0.05f;
    public bool closedLoop;
    public float splitWidth = 0.1f;
    public float controlLength = 0.2f;
}

[System.Serializable]
public class UIParams
{
    public bool handlesSeparate = true;
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