using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.Intrinsics;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
[ExecuteAlways]
public class BezierMesh : MonoBehaviour
{

    // Create mesh
    public BezierPath path;
    public MeshFilter[] meshFilters;
    public MeshCollider[] meshColliders;
    public RoadParams trackParams;

    public void UpdateMesh()
    {
        if (path == null || path.pathParams.CoursePoints.Length <= 1)
        {
            if (meshFilters != null)
            {
                foreach (MeshFilter filter in meshFilters)
                {
                    if (filter.sharedMesh != null)
                    {
                        filter.sharedMesh.Clear();
                    }
                }
            }

            return;
        }

        //Debug.Log("Starting mesh initialisation!");

        if (path == null)
            return;
        BezierPoint[][] points = path.pathParams.CoursePoints;
        int division = (int)Mathf.Round(1 / path.pathParams.resolution);
        bool closedLoop = path.pathParams.closedLoop;

        List<Vector3>[] leftVertices = new List<Vector3>[points.Length + (closedLoop ? 0 : -1)];
        List<Vector3>[] rightVertices = new List<Vector3>[points.Length + (closedLoop ? 0 : -1)];

        for (int i = 0; i < points.Length + (closedLoop ? 0 : -1); i++)
        {
            List<Vector3> tmpLeftVertices = new List<Vector3>();
            List<Vector3> tmpRightVertices = new List<Vector3>();
            /*bool startSplit = points[(i + 1) % points.Length].Length > 1;
            bool endSplit = points[i].Length > 1;*/
            
            for (int j = 0; j < points[i].Length; j++)
            {
                BezierPoint p1 = points[i][j];
                for (int k = 0; k < points[(i + 1) % points.Length].Length; k++)
                {
                    BezierPoint p2 = points[(i + 1) % points.Length][k];


                    Vector3[] bezierPoints = Handles.MakeBezierPoints(p1.basePoint,
                        p2.basePoint,
                        p1.HandlePoints[1], p2.HandlePoints[0], division);

                    for (int l = 0; l < bezierPoints.Length; l++)
                    {
                        Vector3 a = bezierPoints[l];
                        Vector3 b;
                        if (l == bezierPoints.Length - 1)
                        {
                            b = 2 * bezierPoints[l] - bezierPoints[l - 1];
                        }
                        else
                        {
                            b = bezierPoints[l + 1];
                        }

                        // Going left from the path difference
                        Vector3 normal = Vector3.Normalize(Vector3.Cross(b-a, Vector3.up));

                        Vector3[] leftPoints =
                        {
                            a + (trackParams.roadWidth + trackParams.roadLineWidth) / 2 * normal + trackParams.leftOffset,
                            a + (trackParams.roadWidth - trackParams.roadLineWidth) / 2 * normal + trackParams.leftOffset
                        };
                        Vector3[] rightPoints =
                        {
                            a - (trackParams.roadWidth - trackParams.roadLineWidth) / 2 * normal + trackParams.rightOffset,
                            a - (trackParams.roadWidth + trackParams.roadLineWidth) / 2 * normal + trackParams.rightOffset
                        };
                        /*int pathTester = 1;
                        if ((startSplit && k == pathTester) || (endSplit && j == pathTester))
                        {
                            Debug.DrawLine(leftPoints[0], leftPoints[1], Color.red, 1);
                            Debug.DrawLine(rightPoints[0], rightPoints[1], Color.red, 1);
                        }*/

                        tmpLeftVertices.AddRange(leftPoints);
                        tmpRightVertices.AddRange(rightPoints);
                    }
                    
                    
                    
                    //Debug.DrawLine(lastLeftPoints[0], lastLeftPoints[1], Color.red);
                    //Debug.DrawLine(lastRightPoints[0], lastRightPoints[1], Color.red);
                    tmpLeftVertices.Add(Vector3.up);
                    tmpRightVertices.Add(Vector3.up);
                    //tmpLeftVertices.AddRange(lastLeftPoints);
                    //tmpRightVertices.AddRange(lastRightPoints);
                    // Prevents weird path behaviour

                    /*int offset = (i == points.Length - 2 && !closedLoop) ? -1 : 0;
                    //int offset = 0;
                    // Vertex index logic
                    indices.AddRange(ReturnIndices(division + offset,
                        2 * pathNum * division));

                    pathNum++;*/

                }
            }

            leftVertices[i] = tmpLeftVertices;
            rightVertices[i] = tmpRightVertices;
        }
        

        if (points.Length > 2)
        {
            //Debug.Log(points.Length);
            for (int a = 0; a < points.Length + (closedLoop ? 0 : -1); a++)
            {
                if (!(a == points.Length + (closedLoop ? 0 : -1) - 1 && !closedLoop))
                {
                    List<int> firstLeftIndices = FindAllIndices(leftVertices[a], Vector3.up);
                    List<int> secondLeftIndices = FindAllIndices(leftVertices[(a+1) % leftVertices.Length], Vector3.up);
                    /*string message = "";

                    foreach (int index in secondLeftIndices)
                    {
                        message += index;
                        message += ", ";
                    }
                    
                    Debug.Log(message);*/
                    for (int i = 0; i < firstLeftIndices.Count; i++)
                    {
                        //Debug.Log(leftVertices[(a+1) % leftVertices.Length].Count);
                        //Debug.Log(secondLeftIndices[(i-1) % secondLeftIndices.Count] + 1);
                        if (i == 0)
                        {
                            leftVertices[a].InsertRange(firstLeftIndices[i], 
                                new [] 
                                {
                                    leftVertices[(a+1) % leftVertices.Length][0],
                                    leftVertices[(a+1) % leftVertices.Length][1]
                                });
                        }
                        else
                        {
                            if (secondLeftIndices.Count == 1)
                            {
                                leftVertices[a].InsertRange(firstLeftIndices[i], 
                                    new [] 
                                    {
                                        leftVertices[(a+1) % leftVertices.Length][0],
                                        leftVertices[(a+1) % leftVertices.Length][1]
                                    });
                            }
                            else
                            {
                                leftVertices[a].InsertRange(firstLeftIndices[i], 
                                    new [] 
                                    {
                                        leftVertices[(a+1) % leftVertices.Length][secondLeftIndices[(i-1) % secondLeftIndices.Count] + 1],
                                        leftVertices[(a+1) % leftVertices.Length][secondLeftIndices[(i-1) % secondLeftIndices.Count] + 2]
                                    });
                            }
                            
                        }
                        
                    }
                    
                    List<int> firstRightIndices = FindAllIndices(rightVertices[a], Vector3.up);
                    List<int> secondRightIndices = FindAllIndices(rightVertices[(a+1) % rightVertices.Length], Vector3.up);
                    
                    for (int i = 0; i < firstRightIndices.Count; i++)
                    {
                        
                        if (i == 0)
                        {
                            rightVertices[a].InsertRange(firstRightIndices[i], 
                                new [] 
                                {
                                    rightVertices[(a+1) % rightVertices.Length][0],
                                    rightVertices[(a+1) % rightVertices.Length][1]
                                });
                        }
                        else
                        {
                            if (secondRightIndices.Count == 1)
                            {
                                rightVertices[a].InsertRange(firstRightIndices[i], 
                                    new [] 
                                    {
                                        rightVertices[(a+1) % rightVertices.Length][0],
                                        rightVertices[(a+1) % rightVertices.Length][1]
                                    });
                            }
                            else
                            {
                                rightVertices[a].InsertRange(firstRightIndices[i], 
                                    new [] 
                                    {
                                        rightVertices[(a+1) % rightVertices.Length][secondRightIndices[(i-1) % secondRightIndices.Count] + 1],
                                        rightVertices[(a+1) % rightVertices.Length][secondRightIndices[(i-1) % secondRightIndices.Count] + 2]
                                    });
                            }
                            
                        }
                        
                    }
                }
            }
        }


        //Debug.Log("Left vertices after culling: " + Unpack(leftVertices).Count);
        

        //Debug.Log(tmpLeftVertices.Count);
        List<Vector3> triangleLeft = TriangleCourseVertices(leftVertices);
        List<Vector3> triangleRight = TriangleCourseVertices(rightVertices);

        //Debug.Log(triangleLeft.Count);

        List<int> leftIndices = NormalIndices(triangleLeft, closedLoop);
        List<int> rightIndices = NormalIndices(triangleRight, closedLoop);
        
        //Debug.Log(triangleLeft.Count);
        //Debug.Log(Mathf.Max(leftIndices.ToArray()));
        
        //DrawTriangles(triangleLeft, leftIndices, Color.red, 0.1f);

        Mesh leftMesh = new Mesh();
        Mesh rightMesh = new Mesh();

        leftMesh.vertices = triangleLeft.ToArray();
        rightMesh.vertices = triangleRight.ToArray();
        
        leftMesh.SetTriangles(leftIndices, 0);
        rightMesh.SetTriangles(rightIndices, 0);
        
        //rightMesh.normals = FlipNormals(rightMesh.normals);
        
        leftMesh.RecalculateNormals();
        rightMesh.RecalculateNormals();
        leftMesh.Optimize();
        rightMesh.Optimize();

        /*for (int i = 0; i < leftMesh.normals.Length; i++)
        {
            leftMesh.normals[i] = Vector3.up;
            rightMesh.normals[i] = Vector3.up;
        }*/

        meshColliders[0].sharedMesh.Clear();
        meshColliders[1].sharedMesh.Clear();
        
        meshFilters[0].mesh = leftMesh;
        meshFilters[1].mesh = rightMesh;
        
        meshColliders[0].sharedMesh = leftMesh;
        meshColliders[1].sharedMesh = rightMesh;

        // Edit them after colliders have been set

        if (trackParams.cleanSplits)
        {
            for (int x = 0; x < points.Length + (closedLoop ? 0 : -1); x++)
            {
                bool startSplit = points[(x + 1) % points.Length].Length > 1;
                bool endSplit = points[x].Length > 1;
                Vector3 direction = (trackParams.rightOffset.y > trackParams.leftOffset.y) ? Vector3.up : Vector3.down;
                
                if (startSplit)
                {
                    int firstLeftIndex = SearchFromIndex(leftVertices[x], 0, 1, Vector3.up);
                    int secondLeftIndex = SearchFromIndex(leftVertices[x], 0, 2, Vector3.up);
                    int rightIndex = SearchFromIndex(rightVertices[x], 0, 0, Vector3.up);
                    for (int a = firstLeftIndex + 1; a < secondLeftIndex; a += 2)
                    {
                        //Debug.Log(leftVertices[x].Count - a);
                        Vector3 avgPoint = (leftVertices[x].Count - a == 1) ? 
                            (leftVertices[x][a - 2] + leftVertices[x][a - 1]) / 2 : 
                            (leftVertices[x][a] + leftVertices[x][a + 1]) / 2;
                        Ray checkingRay = new Ray(avgPoint, direction);
                        
                        //Debug.DrawRay(avgPoint, direction, Color.red, 1);
                        RaycastHit hitInfo;
                        float maxDistance = (leftVertices[x].Count - a <= 2)
                            ? (leftVertices[x][a] - leftVertices[x][a - 2]).magnitude
                            : (leftVertices[x][a + 2] - leftVertices[x][a]).magnitude;
                        // Checks to see if the right collider is hit
                        if (meshColliders[1].Raycast(checkingRay, out hitInfo, maxDistance))
                        {
                            Debug.Log("Intersected!");
                            leftVertices[x].RemoveRange(0, a - 1);
                            break;
                        }
                        
                    }
                } else if (endSplit)
                {
                    int firstLeftIndex = SearchFromIndex(leftVertices[x], leftVertices[x].Count - 1, -1, Vector3.up);
                    int secondLeftIndex = SearchFromIndex(leftVertices[x], leftVertices[x].Count - 1, -2, Vector3.up);
                    int rightIndex = SearchFromIndex(rightVertices[x], 0, 0, Vector3.up);
                    for (int a = firstLeftIndex - 1; a > secondLeftIndex; a -= 2)
                    {
                        Vector3 avgPoint = (leftVertices[x].Count - a == 1) ? 
                            (leftVertices[x][a - 2] + leftVertices[x][a - 1]) / 2 : 
                            (leftVertices[x][a] + leftVertices[x][a + 1]) / 2;
                        Ray checkingRay = new Ray(avgPoint, direction);
                        //Debug.DrawRay(avgPoint, direction, Color.red, 1);
                        RaycastHit hitInfo;
                        float maxDistance = (leftVertices[x].Count - a <= 2)
                            ? (leftVertices[x][a] - leftVertices[x][a - 2]).magnitude
                            : (leftVertices[x][a + 2] - leftVertices[x][a]).magnitude;
                        // Checks to see if the right collider is hit
                        if (meshColliders[1].Raycast(checkingRay, out hitInfo, maxDistance))
                        {
                            leftVertices[x].RemoveRange(a + 1, firstLeftIndex - a - 2);
                            break;
                        }
                    }
                }
            }
            triangleLeft = TriangleCourseVertices(leftVertices);
            triangleRight = TriangleCourseVertices(rightVertices);

            leftIndices = NormalIndices(triangleLeft, closedLoop);
            rightIndices = NormalIndices(triangleRight, closedLoop);

            leftMesh.Clear();
            rightMesh.Clear();

            leftMesh.vertices = triangleLeft.ToArray();
            rightMesh.vertices = triangleRight.ToArray();
            
            leftMesh.SetTriangles(leftIndices, 0);
            rightMesh.SetTriangles(rightIndices, 0);

            leftMesh.RecalculateNormals();
            rightMesh.RecalculateNormals();
            leftMesh.Optimize();
            rightMesh.Optimize();
        }
    }

    

    List<int> FindAllIndices(List<Vector3> arr, Vector3 search)
    {
        List<int> newIndices = new List<int>();
        for (int i = 0; i < arr.Count; i++)
        {
            if (arr[i] == search)
            {
                newIndices.Add(i);
            }
        }

        return newIndices;
    }

    int SearchFromIndex(List<Vector3> arr, int currentIndex, int occurrence, Vector3 search, bool log=false)
    {
        int newOccurrence = occurrence;
        if (occurrence < 0)
        {
            for (int i = currentIndex; i >= 0; i--)
            {
                if (log)
                    Debug.Log(arr[i]);
                if (arr[i] == search)
                {
                    newOccurrence++;
                }

                if (newOccurrence == 0)
                {
                    if (log)
                        Debug.Log(i);
                    return i;
                }
            }
        }
        else
        {
            for (int i = currentIndex; i < arr.Count; i++)
            {
                if (arr[i] == search)
                {
                    newOccurrence--;
                }
                
                if (newOccurrence == 0)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    int SearchClosest(List<Vector3> arr, Vector3 pos, int currentIndex, int direction)
    {
        if (direction == 1)
        {
            for (int i = currentIndex; i < arr.Count; i += 2)
            {
                float maxDistance = (arr.Count - i <= 3)
                    ? (arr[i] - arr[i - 2]).magnitude
                    : (arr[i + 2] - arr[i]).magnitude;
                if ((arr[i] - pos).magnitude <= maxDistance)
                {
                    return i;
                }
            }
        } else if (direction == -1)
        {
            for (int i = currentIndex; i > 0; i -= 2)
            {
                float maxDistance = (i <= 1)
                    ? (arr[i] - arr[i + 2]).magnitude
                    : (arr[i - 2] - arr[i]).magnitude;
                if ((arr[i] - pos).magnitude <= maxDistance)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    List<int> NormalIndices(List<Vector3> coursePoints, bool closedLoop = false)
    {
        List<int> newIndices = new List<int>();
        for (int i = 0; i < coursePoints.Count; i += 3)
        {
            if (Vector3.Cross(coursePoints[i] - coursePoints[i + 1], coursePoints[i + 2] - coursePoints[i + 1]).y > 0)
            {
                newIndices.AddRange(new [] {i + 2, i + 1, i});
            }
            else
            {
                newIndices.AddRange(new [] {i, i + 1, i + 2});
            }
        }

        if (closedLoop)
        {
            newIndices.AddRange(new []
            {
                coursePoints.Count - 2,
                coursePoints.Count - 1,
                0,
                1,
                0,
                coursePoints.Count - 1,
                
            });
        }

        return newIndices;
    }

    bool TrianglesIntersect(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 u0, Vector3 u1, Vector3 u2)
    {
        Bounds b0 = new Bounds(v0, Vector3.zero);
        b0.Encapsulate(v1);
        b0.Encapsulate(v2);
        
        Bounds b1 = new Bounds(u0, Vector3.zero);
        b1.Encapsulate(u1);
        b1.Encapsulate(u2);

        Vector3 p1 = b0.center;
        Vector3 p2 = b1.center;

        return (p2 - p1).magnitude < 0.001f;
    }

    // Quick 2D algorithm, no need for fancy 3D stuff
    List<Vector3> TriangleVertices(List<Vector3> vertices)
    {
        List<Vector3> newVertices = new List<Vector3>();
        Vector3 breakVector = Vector3.up;
        for (int i = 0; i < vertices.Count - 2; i++)
        {
            if (!(vertices[i] == breakVector || vertices[i + 1] == breakVector || vertices[i + 2] == breakVector))
            {
                newVertices.AddRange(new [] {vertices[i], vertices[i+1], vertices[i+2]});
            }
        }

        return newVertices;
    }

    List<Vector3> TriangleCourseVertices(List<Vector3>[] trackPoints)
    {
        List<Vector3> newVertices = new List<Vector3>();
        foreach (List<Vector3> pathSection in trackPoints)
        {
            //Debug.Log(pathSection.Count);
            newVertices.AddRange(TriangleVertices(pathSection));
        }

        return newVertices;
    }
    

    void DrawTriangles(List<Vector3> vertices, Color color, float duration=1)
    {
        for (int i = 0; i < vertices.Count; i+=3)
        {
            Debug.DrawLine(vertices[i], vertices[i + 1], color, duration);
            Debug.DrawLine(vertices[i + 1], vertices[i + 2], color, duration);
            Debug.DrawLine(vertices[i + 2], vertices[i], color, duration);
        }
    }

    void DrawTriangles(List<Vector3> vertices, List<int> indices, Color color, float duration = 1)
    {
        for (int i = 0; i < indices.Count; i += 3)
        {
            Debug.DrawLine(vertices[indices[i]], vertices[indices[i + 1]], color, duration);
            Debug.DrawLine(vertices[indices[i + 1]], vertices[indices[i + 2]], color, duration);
            Debug.DrawLine(vertices[indices[i + 2]], vertices[indices[i]], color, duration);
        }
    }

}

[System.Serializable]
public class RoadParams
{
    public float roadWidth = 10f;
    public float roadLineWidth = 1f;
    public Vector3 leftOffset = 0.005f * Vector3.up;
    public Vector3 rightOffset = 0.006f * Vector3.up;
    public bool cleanSplits = true;
}