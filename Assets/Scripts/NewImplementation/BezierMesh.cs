using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.Burst.Intrinsics;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
[ExecuteAlways]
public class BezierMesh : MonoBehaviour
{

    // Create mesh
    public BezierPath path;
    public MeshFilter[] meshFilters;
    public MeshRenderer[] meshRenderers;
    public MeshCollider[] meshColliders;
    public GameObject[] roadObjects;
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

            for (int j = 0; j < points[i].Length; j++)
            {
                BezierPoint p1 = points[i][j];
                for (int k = 0; k < points[(i + 1) % points.Length].Length; k++)
                {
                    BezierPoint p2 = points[(i + 1) % points.Length][k];


                    Vector3[] bezierPoints = Handles.MakeBezierPoints(p1.basePoint,
                        p2.basePoint,
                        p1.HandlePoints()[1], p2.HandlePoints()[0], division);

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

                        tmpLeftVertices.AddRange(leftPoints);
                        tmpRightVertices.AddRange(rightPoints);
                    }
                    
                    
                    tmpLeftVertices.Add(Vector3.up);
                    tmpRightVertices.Add(Vector3.up);

                }
            }

            leftVertices[i] = tmpLeftVertices;
            rightVertices[i] = tmpRightVertices;
        }
        

        if (points.Length > 2)
        {
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

        
        List<Vector3> triangleLeft = TriangleCourseVertices(leftVertices);
        List<Vector3> triangleRight = TriangleCourseVertices(rightVertices);

        List<int> leftIndices = NormalIndices(triangleLeft, closedLoop);
        List<int> rightIndices = NormalIndices(triangleRight, closedLoop);


        meshFilters[0].mesh = ReturnMesh(triangleLeft, leftIndices);
        meshFilters[1].mesh = ReturnMesh(triangleRight, rightIndices);
        
        DestroyImmediate(meshColliders[0]);
        DestroyImmediate(meshColliders[1]);

        meshColliders[0] = roadObjects[0].AddComponent<MeshCollider>();
        meshColliders[1] = roadObjects[1].AddComponent<MeshCollider>();
        
        meshColliders[0].sharedMesh = null;
        meshColliders[1].sharedMesh = null;
        
        meshColliders[0].sharedMesh = ReturnMesh(triangleLeft, leftIndices);
        meshColliders[1].sharedMesh = ReturnMesh(triangleRight, rightIndices);

        // Edit them after colliders have been set

        if (trackParams.cleanSplits)
        {
            List<List<Vector3>[]> combinedVertices = DetectCollisions(points, leftVertices, rightVertices, closedLoop);
            leftVertices = combinedVertices[0];
            rightVertices = combinedVertices[1];
            
            triangleLeft = TriangleCourseVertices(leftVertices);
            triangleRight = TriangleCourseVertices(rightVertices);

            //Debug.Log(triangleLeft.Count);

            leftIndices = NormalIndices(triangleLeft, closedLoop);
            rightIndices = NormalIndices(triangleRight, closedLoop);


            meshFilters[0].mesh = ReturnMesh(triangleLeft, leftIndices);
            meshFilters[1].mesh = ReturnMesh(triangleRight, rightIndices);
        
            DestroyImmediate(meshColliders[0]);
            DestroyImmediate(meshColliders[1]);

            meshColliders[0] = roadObjects[0].AddComponent<MeshCollider>();
            meshColliders[1] = roadObjects[1].AddComponent<MeshCollider>();
        
            meshColliders[0].sharedMesh = null;
            meshColliders[1].sharedMesh = null;
        
            meshColliders[0].sharedMesh = ReturnMesh(triangleLeft, leftIndices);
            meshColliders[1].sharedMesh = ReturnMesh(triangleRight, rightIndices);
        }
    }
    
    // Doesn't work when vertices are flipped
    List<List<Vector3>[]> DetectCollisions(BezierPoint[][] points, List<Vector3>[] leftVertices, List<Vector3>[] rightVertices, bool closedLoop)
    {
        List<List<Vector3>[]> totalVertices = new List<List<Vector3>[]>();
        List<Vector3>[] newLeftVertices = leftVertices;
        List<Vector3>[] newRightVertices = rightVertices;
        for (int x = 0; x < points.Length + (closedLoop ? 0 : -1); x++)
        {
             bool startSplit = points[(x + 1) % points.Length].Length > 1;
             bool endSplit = points[x].Length > 1;
             Vector3 direction = trackParams.rightOffset.y > trackParams.leftOffset.y ? Vector3.up : Vector3.down;
             float maxDistance = Mathf.Abs(20 * (trackParams.rightOffset.y - trackParams.leftOffset.y));

             if (startSplit)
             {
                 int firstLeftIndex = SearchFromIndex(leftVertices[x], 0, 1, Vector3.up);
                 int secondLeftIndex = SearchFromIndex(leftVertices[x], 0, 2, Vector3.up);
                 for (int a = firstLeftIndex + 1; a < secondLeftIndex; a += 2)
                 {
                     //Debug.Log(leftVertices[x].Count - a);
                     Vector3 avgPoint = leftVertices[x].Count - a == 1 ? 
                         (leftVertices[x][a - 2] + leftVertices[x][a - 1]) / 2 : 
                         (leftVertices[x][a] + leftVertices[x][a + 1]) / 2 + maxDistance * direction;
                     Ray checkingRay = new Ray(avgPoint, -direction);
                     
                     RaycastHit hitInfo;

                     //Debug.DrawRay(avgPoint, -direction * maxDistance, Color.red, 1);
                     // Checks to see if the right collider is hit
                     if (Physics.Raycast(checkingRay, out hitInfo, maxDistance, trackParams.rightMask))
                     {
                         //Debug.Log("Intersected!");
                         //Debug.DrawLine(avgPoint, avgPoint - maxDistance * direction, Color.red, 1);
                         int rightIndexA = SearchClosest(rightVertices[x], 1, hitInfo.point);
                         int rightIndexB = SearchFromIndex(rightVertices[x], rightIndexA, -1, Vector3.up);
                         rightIndexB = rightIndexB == -1 ? 0 : rightIndexB;
                         //Debug.DrawRay(rightVertices[x][rightIndexA], Vector3.up, Color.red, 1);
                         newLeftVertices[x].RemoveRange(firstLeftIndex + 1, a - firstLeftIndex - 1);
                         newRightVertices[x].RemoveRange(rightIndexB, (int) Mathf.Floor((rightIndexA - rightIndexB) / 2) * 2);
                         break;
                     }
                     
                 }
             } else if (endSplit)
             {
                 int firstLeftIndex = SearchFromIndex(leftVertices[x], 0, 2, Vector3.up);
                 int secondLeftIndex = SearchFromIndex(leftVertices[x], 0, 1, Vector3.up);
                 for (int a = firstLeftIndex - 1; a > secondLeftIndex; a -= 2)
                 {
                     Vector3 avgPoint = (leftVertices[x].Count - a == 1) ? 
                         (leftVertices[x][a - 2] + leftVertices[x][a - 1]) / 2 : 
                         (leftVertices[x][a] + leftVertices[x][a + 1]) / 2 + maxDistance * direction;
                     Ray checkingRay = new Ray(avgPoint, -direction);
                     
                     RaycastHit hitInfo;
                     //Debug.DrawRay(avgPoint, -direction * maxDistance, Color.red, 1);
                     
                     // Checks to see if the right collider is hit
                     if (Physics.Raycast(checkingRay, out hitInfo, maxDistance, trackParams.rightMask))
                     {
                         //Debug.Log("Intersected!");
                         int rightIndexA = SearchClosest(rightVertices[x], -1, hitInfo.point);
                         int rightIndexB = SearchFromIndex(rightVertices[x], rightIndexA, 1, Vector3.up);
                         rightIndexB = rightIndexB == -1 ? 0 : rightIndexB;
                         //Debug.Log(rightIndexA);
                         //Debug.Log(rightIndexB);
                         
                         rightIndexA = (rightIndexB - rightIndexA) % 2 == 1 ? rightIndexA : rightIndexA + 1;


                         //Debug.DrawRay(rightVertices[x][rightIndexA], Vector3.up, Color.red, 1);
                         //Debug.DrawRay(hitInfo.point, Vector3.up, Color.red, 1);
                         newLeftVertices[x].RemoveRange(a, firstLeftIndex - a);
                         newRightVertices[x].RemoveRange(rightIndexA, rightIndexB - rightIndexA);
                         break;
                     }
                 }
             }
        }
        totalVertices.Add(newLeftVertices);
        totalVertices.Add(newRightVertices);

        return totalVertices;
    }

    Mesh ReturnMesh(List<Vector3> vertices, List<int> indices)
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.vertices = vertices.ToArray();
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.Optimize();

        return mesh;
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

    int SearchFromIndex(List<Vector3> arr, int currentIndex, int occurrence, Vector3 search)
    {
        int newOccurrence = occurrence;
        int index = currentIndex;
        while (true)
        {
            if (newOccurrence != 0)
            {
                index += occurrence > 0 ? 1 : -1;
                
                if (index < 0)
                {
                    return 0;
                }
                if (index >= arr.Count)
                {
                    return arr.Count - 1;
                }
                //Debug.Log(arr[index]);
                if (arr[index] == search)
                {
                    newOccurrence += occurrence > 0 ? -1 : 1;
                }
            }
            else
            {
                return index;
            }
        }
    }

    int SearchClosest(List<Vector3> arr, int direction, Vector3 pos, float threshold = 0.005f)
    {
        if (arr.Count == 0)
        {
            return 0;
        } 
        if (arr.Count == 1)
        {
            return 0;
        }
        int index = direction == 1 ? 0 : arr.Count - 1;
        float smallestDistance = (arr[index] - pos).magnitude;
        int smallestIndex = index;
        while (true)
        {
            if ((arr[index] - pos).magnitude <= threshold)
            {
                return index;
            }

            if ((arr[index] - pos).magnitude < smallestDistance)
            {
                smallestDistance = (arr[index] - pos).magnitude;
                smallestIndex = index;
            }
            index += direction;

            if (index < 0 || index >= arr.Count)
            {
                return smallestIndex;
            }
            
        }
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
    public LayerMask leftMask = 1 << 9;
    public LayerMask rightMask = 1 << 10 ;
    public Material leftRoadMat;
    public Material rightRoadMat;
    public Material leftMaskMat;
    public Material rightMaskMat;
}