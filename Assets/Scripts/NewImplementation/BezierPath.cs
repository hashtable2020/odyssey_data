using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

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
            if (PathParams.CoursePoints == null)
            {
                PathParams.CoursePoints = new[] { new[] { BezierPoint.Zero } };
                //pathParams.courseSaveData.courseSave = new BezierPoint[1][];
                pathParams = new PathParams();
                uiParams = new UIParams();
            }
            else if (PathParams.CoursePoints == null)
            {
                //PathParams.CoursePoints = pathParams.courseSaveData.courseSave;
            }

            PointHandler += (key, index, pos) =>
            {
                // Keyboard shortcuts, ctrl+click to remove, shift+click to add, s+click to split the path, m+click to merge, d to deselect
                if (key == KeyCode.LeftShift)
                {
                    BezierPoint[][] tmpPoints = new BezierPoint[PathParams.CoursePoints.Length + 1][];
                    PathParams.CoursePoints.CopyTo(tmpPoints, 0);
                    PathParams.CoursePoints = tmpPoints;
                    PathParams.CoursePoints[^1] = new[] { new BezierPoint(pos, new[] { Vector3.zero, Vector3.zero }) };
                }
                else if (key == KeyCode.LeftControl)
                {
                    if (index != -1)
                    {
                        BezierPoint[][] tmpPoints = new BezierPoint[PathParams.CoursePoints.Length - 1][];
                        int currentIndex = 0;
                        for (int i = 0; i < PathParams.CoursePoints.Length; i++)
                        {
                            if (i != index)
                            {
                                tmpPoints[currentIndex] = PathParams.CoursePoints[i];
                                currentIndex++;
                            }
                        }

                        PathParams.CoursePoints = tmpPoints;
                    }

                }
                else if (key == KeyCode.S)
                {
                    BezierPoint[][] tmpPoints = new BezierPoint[PathParams.CoursePoints.Length][];
                    int arrLength = PathParams.CoursePoints[index].Length;
                    Vector3 centre = Vector3.zero;

                    foreach (BezierPoint point in PathParams.CoursePoints[index])
                    {
                        centre += point.basePoint;
                    }

                    centre /= arrLength;

                    Vector3[] tangent =
                    {
                        (arrLength % 2 == 0)
                            ? (PathParams.CoursePoints[index][arrLength / 2 - 1].localHandles[0] +
                               PathParams.CoursePoints[index][arrLength / 2].localHandles[0]) / 2
                            : PathParams.CoursePoints[index][(arrLength - 1) / 2].localHandles[0],
                        (arrLength % 2 == 0)
                            ? (PathParams.CoursePoints[index][arrLength / 2 - 1].localHandles[1] +
                               PathParams.CoursePoints[index][arrLength / 2].localHandles[1]) / 2
                            : PathParams.CoursePoints[index][(arrLength - 1) / 2].localHandles[1]
                    };
                    Vector3 normal = Vector3.Normalize(Vector3.Cross(tangent[0], Vector3.up));

                    //Debug.Log("Normal: " + normal);

                    BezierPoint[] splitPoints = new BezierPoint[arrLength + 1];
                    for (int i = 0; i < arrLength + 1; i++)
                    {
                        float x = (2 * (float)i - arrLength);
                        splitPoints[i] = new BezierPoint(Vector3.LerpUnclamped(centre,
                            centre + pathParams.splitWidth * normal, x), tangent);
                    }

                    for (int j = 0; j < PathParams.CoursePoints.Length; j++)
                    {
                        if (j == index)
                        {
                            tmpPoints[j] = splitPoints;
                        }
                        else
                        {
                            tmpPoints[j] = PathParams.CoursePoints[j];
                        }
                    }

                    PathParams.CoursePoints = tmpPoints;
                    //Debug.Log("First split point: " + PathParams.CoursePoints[index][0].BasePoint + ", Second split point: " + PathParams.CoursePoints[index][1].BasePoint);
                }
                else if (key == KeyCode.M)
                {
                    if (PathParams.CoursePoints[index].Length != 1)
                    {
                        BezierPoint[][] tmpPoints = new BezierPoint[PathParams.CoursePoints.Length][];
                        int arrLength = PathParams.CoursePoints[index].Length;
                        Vector3 centre = Vector3.zero;

                        foreach (BezierPoint point in PathParams.CoursePoints[index])
                        {
                            centre += point.basePoint;
                        }

                        centre /= arrLength;

                        Vector3[] tangent =
                        {
                            (arrLength % 2 == 0)
                                ? (PathParams.CoursePoints[index][arrLength / 2 - 1].localHandles[0] +
                                   PathParams.CoursePoints[index][arrLength / 2].localHandles[0]) / 2
                                : PathParams.CoursePoints[index][(arrLength - 1) / 2].localHandles[0],
                            (arrLength % 2 == 0)
                                ? (PathParams.CoursePoints[index][arrLength / 2 - 1].localHandles[1] +
                                   PathParams.CoursePoints[index][arrLength / 2].localHandles[1]) / 2
                                : PathParams.CoursePoints[index][(arrLength - 1) / 2].localHandles[1]
                        };
                        
                        Vector3 normal = Vector3.Normalize(Vector3.Cross(tangent[0], Vector3.up));

                        //Debug.Log("Normal: " + normal);

                        BezierPoint[] splitPoints = new BezierPoint[arrLength - 1];
                        for (int i = 0; i < arrLength - 1; i++)
                        {
                            float x = (2 * (float)i - arrLength);
                            splitPoints[i] = new BezierPoint(Vector3.LerpUnclamped(centre,
                                centre + pathParams.splitWidth * normal, x), tangent);
                        }

                        for (int j = 0; j < PathParams.CoursePoints.Length; j++)
                        {
                            if (j == index)
                            {
                                tmpPoints[j] = splitPoints;
                            }
                            else
                            {
                                tmpPoints[j] = PathParams.CoursePoints[j];
                            }
                        }

                        PathParams.CoursePoints = tmpPoints;
                    }
                }
            };
            Refresh += () =>
            {
                uiParams.roadPlane.position = uiParams.yPlane * Vector3.up;
                uiParams.roadPlane.rotation = Quaternion.identity;
                Vector3[][] handleLocations = LocalHandleLocations(PathParams.CoursePoints);
                for (int i = 0; i < PathParams.CoursePoints.Length; i++)
                {
                    for (int j = 0; j < PathParams.CoursePoints[i].Length; j++)
                    {
                        if (!PathParams.CoursePoints[i][j].handlesChanged)
                        {

                            PathParams.CoursePoints[i][j].localHandles = new[]
                            {
                                handleLocations[i][2 * j],
                                handleLocations[i][2 * j + 1],
                            };
                        }
                        
                    }
                }
                UpdateGameObjects(PathParams.CoursePoints);
                
                roadMesh.UpdateMesh();
            };

            Reset += () =>
            {
                PathParams.CoursePoints = new[] { new[] { BezierPoint.Zero } };
                roadMesh.UpdateMesh();
                ClearGameObjects();
            };
        }
    }

    public void OnAfterDeserialize()
    {
        
    }
    void Start()
    {
        if (!Application.IsPlaying(gameObject))
        {
            Debug.Log("Working!");
        }
    }

    void UpdateGameObjects(BezierPoint[][] curvePoints)
    {
        while (curvePoints.Length - transform.childCount != 0)
        {
            if (curvePoints.Length - transform.childCount < 0)
            {
                DestroyImmediate(transform.GetChild(transform.childCount - 1).gameObject);
            }
            else
            {
                GameObject tmpObj = new GameObject();
                tmpObj.transform.parent = transform;
            }
        }
        for (int i = 0; i < curvePoints.Length; i++)
        {
            Transform currentChild = transform.GetChild(i);
            while (curvePoints[i].Length - currentChild.childCount != 0)
            {
                if (curvePoints.Length - currentChild.childCount < 0)
                {
                    DestroyImmediate(currentChild.GetChild(transform.childCount - 1));
                }
                else
                {
                    GameObject tmpObj = new GameObject();
                    tmpObj.transform.parent = currentChild;
                }
            }
            for (int j = 0; j < curvePoints[i].Length; j++)
            {
                Transform currentElement = currentChild.GetChild(j);
                while (currentElement.childCount != 3)
                {
                    if (currentElement.childCount < 3)
                    {
                        GameObject tmpObj = new GameObject();
                        tmpObj.transform.parent = currentElement;
                    }
                    else
                    {
                        DestroyImmediate(currentElement.GetChild(currentElement.childCount - 1));
                    }
                }
                currentElement.GetChild(0).position = curvePoints[i][j].basePoint;
                currentElement.GetChild(1).position = curvePoints[i][j].HandlePoints()[0];
                currentElement.GetChild(2).position = curvePoints[i][j].HandlePoints()[1];
            }
        }
    }

    public void SaveGameObjects()
    {
        PathParams.CoursePoints = new BezierPoint[transform.childCount][];
        for (int i = 0; i < transform.childCount; i++)
        {
            PathParams.CoursePoints[i] = new BezierPoint[transform.GetChild(i).childCount];
            for (int j = 0; j < transform.GetChild(i).childCount; j++)
            {
                Transform pointObj = transform.GetChild(i).GetChild(j);
                PathParams.CoursePoints[i][j] = new BezierPoint(pointObj.GetChild(0).position, 
                    new [] {pointObj.GetChild(1).position - pointObj.GetChild(0).position, 
                        pointObj.GetChild(2).position - pointObj.GetChild(0).position});
            }
        }
    }

    public void ClearGameObjects()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }
    
    Vector3[][] LocalHandleLocations(BezierPoint[][] coursePoints)
    {
        if (coursePoints == null || coursePoints.Length == 0)
        {
            return new[] { new[] { Vector3.zero, Vector3.zero } };
        }
        Vector3[][] newHandleLocations = new Vector3[coursePoints.Length][];
        if (coursePoints.Length == 1)
        {
            newHandleLocations[0] = new Vector3[2 * coursePoints[0].Length];
            for (int x = 0; x < coursePoints[0].Length; x++)
            {
                newHandleLocations[0][2 * x] = Vector3.zero;
                newHandleLocations[0][2 * x + 1] = Vector3.zero;
            }
            return newHandleLocations;
        }

        if (coursePoints.Length == 2 && coursePoints[0].Length == 1 && coursePoints[1].Length == 1)
        {
            newHandleLocations[0] = new Vector3[2];
            newHandleLocations[1] = new Vector3[2];

            for (int x = 0; x < 2; x++)
            {
                newHandleLocations[x][0] = (coursePoints[LoopIndex(x + 1, coursePoints)][0].basePoint -
                                            coursePoints[x][0].basePoint) * pathParams.controlLength;
                newHandleLocations[x][1] = (coursePoints[LoopIndex(x+1, coursePoints)][0].basePoint -
                                            coursePoints[x][0].basePoint) * pathParams.controlLength;
            }

            return newHandleLocations;
        } 

        for (int i = 0; i < coursePoints.Length; i++)
        {
            newHandleLocations[i] = new Vector3[2 * coursePoints[i].Length];
            for (int j = 0; j < coursePoints[i].Length; j++)
            {
                Vector3 dirVector1 = Vector3.zero;
                Vector3 dirVector2 = Vector3.zero;
                float[][] neighbourDistances = new float[2][];
                
                int firstNeighbour = 0;
                int secondNeighbour = 0;

                BezierPoint[] previousPoint = (i == 0 && !pathParams.closedLoop) ? 
                    coursePoints[2] :
                    coursePoints[LoopIndex(i - 1, coursePoints)];
                
                neighbourDistances[0] = new float[previousPoint.Length];
                for (int k = 0; k < previousPoint.Length; k++)
                {
                    Vector3 offset = previousPoint[k].basePoint -
                                      coursePoints[i][j].basePoint;
                    dirVector1 += offset.normalized;
                    neighbourDistances[0][firstNeighbour] = offset.magnitude;
                    firstNeighbour++;
                }

                BezierPoint[] nextPoint = (i == coursePoints.Length - 1 && !pathParams.closedLoop) ? 
                        coursePoints[^3] :
                        coursePoints[LoopIndex(i+1, coursePoints)];
                
                neighbourDistances[1] = new float[nextPoint.Length];
                
                for (int l = 0; l < nextPoint.Length; l++)
                {
                    Vector3 offset = nextPoint[l].basePoint -
                                     coursePoints[i][j].basePoint;
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

                Vector3 normalVector;

                normalVector = Vector3.Normalize(Vector3.Cross((dirVector1 + dirVector2), Vector3.up));

                // Makes sure that the normal vector points towards the start of the path
                normalVector *= Mathf.Sign(Vector3.Dot(normalVector, dirVector1));

                if (uiParams.handlesSeparate)
                {
                    newHandleLocations[i][2 * j] = collapsedDistances[0] * pathParams.controlLength * normalVector;
                    newHandleLocations[i][2 * j + 1] =
                        -collapsedDistances[1] * pathParams.controlLength * normalVector;
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

    public void SpawnSigns(List<int> randomIndices)
    {
        while (pathParams.signObj.childCount > 0)
        {
            Destroy(pathParams.signObj.GetChild(0).gameObject);
        }

        for (int i = 0; i < PathParams.CoursePoints.Length; i++)
        {
            if (PathParams.CoursePoints[i].Length > 1)
            {
                Quaternion rotation = new Quaternion();

                //Vector3 pos = (PathParams.CoursePoints[i][0].basePoint + PathParams.CoursePoints[i][1].basePoint) / 2;
                
                Vector3[] bezierPointsA = Handles.MakeBezierPoints(PathParams.CoursePoints[LoopIndex(i - 1, PathParams.CoursePoints)][0].basePoint,
                    PathParams.CoursePoints[i][0].basePoint, PathParams.CoursePoints[LoopIndex(i - 1, PathParams.CoursePoints)][0].HandlePoints()[1],
                    PathParams.CoursePoints[i][0].HandlePoints()[0], Mathf.FloorToInt(1 / pathParams.resolution));
                Vector3[] bezierPointsB = Handles.MakeBezierPoints(PathParams.CoursePoints[LoopIndex(i - 1, PathParams.CoursePoints)][0].basePoint,
                    PathParams.CoursePoints[i][1].basePoint, PathParams.CoursePoints[LoopIndex(i - 1, PathParams.CoursePoints)][0].HandlePoints()[1],
                    PathParams.CoursePoints[i][1].HandlePoints()[0], Mathf.FloorToInt(1 / pathParams.resolution));
                //Vector3 pos = PathParams.CoursePoints[LoopIndex(i - 1, PathParams.CoursePoints)][0].basePoint;
                int bezierIndex = Mathf.RoundToInt(pathParams.signPathOffset * Mathf.Floor(1 / pathParams.resolution));
                Vector3 pos = (bezierPointsA[bezierIndex] + bezierPointsB[bezierIndex]) / 2;
                Vector3 diffVector = ((bezierPointsA[bezierIndex] - bezierPointsA[bezierIndex - 1]) +
                                     (bezierPointsB[bezierIndex] - bezierPointsB[bezierIndex - 1])) / 2;
                
                //rotation.eulerAngles = Vector3.SignedAngle(Vector3.forward, PathParams.CoursePoints[LoopIndex(i - 1, PathParams.CoursePoints)][0].localHandles[1], Vector3.up) * Vector3.up;
                /*rotation.eulerAngles = Vector3.SignedAngle(Vector3.forward,
                    (PathParams.CoursePoints[i][0].localHandles[1] + PathParams.CoursePoints[i][1].localHandles[1]) / 2,
                    Vector3.up) * Vector3.up + randomIndices[i] * new Vector3(180, 180, 0);*/
                rotation.eulerAngles = Vector3.SignedAngle(Vector3.forward, diffVector,
                    Vector3.up) * Vector3.up + randomIndices[i] * new Vector3(180, 180, 0);
                
                Instantiate(pathParams.signPrefab, pos + pathParams.objOffset, 
                    rotation, pathParams.signObj);
                
            }
        }
    }
    
}

[System.Serializable]
public class BezierPoint {
    public Vector3 basePoint;
    public Vector3[] localHandles;
    public bool handlesChanged;
    public Vector3[] HandlePoints()
    {
        return new[] { localHandles[0] + basePoint, localHandles[1] + basePoint };
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
    public static BezierPoint[][] CoursePoints = {new [] {BezierPoint.Zero}};
    public float resolution = 0.05f;
    public bool closedLoop;
    public float splitWidth = 0.1f;
    public float controlLength = 0.2f;
    public float obstacleProb = 0.02f;
    public float obstacleScale = 0.75f;
    public Object obstaclePrefab;
    public Object maskedObstaclePrefab;
    public Transform obstacleObj;
    public Transform maskedObstacleObj;
    public Object signPrefab;
    public Transform signObj;
    public Vector3 objOffset = 0.03f * Vector3.up;
    public float signPathOffset = 0.1f;
}

[System.Serializable]
public class UIParams
{
    public bool maskMode;
    public bool handlesSeparate = true;
    public Transform roadPlane;
    public MeshRenderer roadRenderer;
    public Material floorMaterial;
    public Material blackMaterial;
    public Camera dataCamera;
    public NewCameraController cameraController;
    public float yPlane = 0;
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