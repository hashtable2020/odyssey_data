using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.Intrinsics;
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
        gameObject.SetActive(false);

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


                    Vector3[] bezierPoints = Handles.MakeBezierPoints(p1.basePoint + trackParams.yOffset,
                        p2.basePoint + trackParams.yOffset,
                        p1.HandlePoints[1] + trackParams.yOffset, p2.HandlePoints[0] + trackParams.yOffset, division);

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
                        Vector3 normal = Vector3.Normalize(Vector3.Cross(Vector3.up, b - a));

                        Vector3[] leftPoints =
                        {
                            a + (trackParams.roadWidth + trackParams.roadLineWidth) / 2 * normal,
                            a + (trackParams.roadWidth - trackParams.roadLineWidth) / 2 * normal
                        };
                        Vector3[] rightPoints =
                        {
                            a - (trackParams.roadWidth - trackParams.roadLineWidth) / 2 * normal,
                            a - (trackParams.roadWidth + trackParams.roadLineWidth) / 2 * normal
                        };
                        //Debug.DrawLine(rightPoints[0], rightPoints[1], Color.red);

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
                    /*string message = "";

                    foreach (int index in secondLeftIndices)
                    {
                        message += index;
                        message += ", ";
                    }
                    
                    Debug.Log(message);*/
                    for (int i = 0; i < firstRightIndices.Count; i++)
                    {
                        //Debug.Log(leftVertices[(a+1) % leftVertices.Length].Count);
                        //Debug.Log(secondLeftIndices[(i-1) % secondLeftIndices.Count] + 1);
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
                    // Get correct vertices from correct path
                    //leftVertices[a].AddRange(new [] {leftVertices[(a+1) % leftVertices.Length][0],
                        //leftVertices[(a+1) % leftVertices.Length][1], Vector3.up});
                        
                    // Get correct vertices from correct path
                    //rightVertices[a].AddRange(new [] {rightVertices[(a+1) % rightVertices.Length][0],
                        //rightVertices[(a+1) % rightVertices.Length][1], Vector3.up});
                }
                //Debug.Log(leftVertices[a].Count);
            }
        }

        if (trackParams.cleanSplits)
        {
            for (int x = 0; x < points.Length + (closedLoop ? 0 : -1); x++)
            {
                if (points[(x + 1) % points.Length].Length > 1 || points[x].Length > 1)
                {
                    bool broken = false;
                    Vector3[] tmpLeftTriangles = TriangleVertices(leftVertices[x]).ToArray();
                    Vector3[] tmpRightTriangles = TriangleVertices(rightVertices[x]).ToArray();
                    for (int leftT = 0; leftT < tmpLeftTriangles.Length; leftT += 3)
                    {
                        for (int rightT = 0; rightT < tmpRightTriangles.Length; rightT += 3)
                        {
                            // If triangles intersect
                            if (TrianglesIntersect(
                                    tmpLeftTriangles[leftT], tmpLeftTriangles[leftT + 1], tmpLeftTriangles[leftT + 2],
                                    tmpRightTriangles[rightT], tmpRightTriangles[rightT + 1], tmpRightTriangles[rightT + 2]))
                            {
                                Debug.Log("Intersected!");
                                //DrawTriangles(new [] {tmpLeftTriangles[leftT], tmpLeftTriangles[leftT + 1], tmpLeftTriangles[leftT + 2],
                                    //tmpRightTriangles[rightT], tmpRightTriangles[rightT + 1], tmpRightTriangles[rightT + 2]}.ToList(), Color.red);
                                if (points[x].Length > 1)
                                {
                                    int leftIndex = SearchFromIndex(leftVertices[x], leftT / 3, 1, Vector3.up);
                                    int rightIndex = SearchFromIndex(rightVertices[x], rightT / 3, 1, Vector3.up);
                                    //Debug.Log(leftIndex);
                                    
                                    leftVertices[x].RemoveRange(Mathf.FloorToInt(leftT / 6) * 2, (leftIndex == -1) ? 0 : leftIndex - Mathf.FloorToInt(leftT / 6) * 2);
                                    rightVertices[x].RemoveRange(Mathf.FloorToInt(rightT / 6) * 2, (rightIndex == -1) ? 0 : rightIndex - Mathf.FloorToInt(rightT / 6) * 2);
                                    
                                    broken = true;
                                    break;
                                } 
                                if (points[(x + 1) % points.Length].Length > 1)
                                {
                                    int leftIndex = SearchFromIndex(leftVertices[x], leftT / 3, -1, Vector3.up);
                                    int rightIndex = SearchFromIndex(rightVertices[x], rightT / 3, -1, Vector3.up);
                                    //Debug.Log(leftIndex);
                                    
                                    
                                    leftVertices[x].RemoveRange((leftIndex == -1) ? 0 : leftIndex, Mathf.FloorToInt(leftT / 6) * 2 - ((leftIndex == -1) ? 0 : leftIndex));
                                    rightVertices[x].RemoveRange((rightIndex == -1) ? 0 : rightIndex, Mathf.FloorToInt(rightT / 6) * 2 - ((rightIndex == -1) ? 0 : rightIndex));
                                    
                                    broken = true;
                                    break;
                                }
                            }
                        }


                        if (broken)
                        {
                            break;
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

        List<int> leftIndices = ReturnIndices(triangleLeft.Count, 0, closedLoop);
        List<int> rightIndices = ReturnIndices(triangleRight.Count, 0, closedLoop);
        
        Debug.Log(triangleLeft.Count);
        Debug.Log(Mathf.Max(leftIndices.ToArray()));
        
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

        meshFilters[0].mesh = leftMesh;
        meshFilters[1].mesh = rightMesh;

        gameObject.SetActive(true);
    }

    /*List<int> ReturnIndices(int length, int offset)
    {
        List<int> newIndices = new List<int>();
        for (int i = 0; i < length; i++)
        {
            newIndices.AddRange(new [] {2 * i + offset, 2 * i + 1 + offset, 2 * i + 2 + offset
                , 2 * i + 3 + offset, 2 * i + 2 + offset, 2 * i + 1 + offset
            });
        }

        return newIndices;
    }*/

    /*Vector3[] FlipNormals(Vector3[] normals)
    {
        Vector3[] newNormals = new Vector3[normals.Length];
        for (int i = 0; i < normals.Length; i++)
        {
            newNormals[i] = -normals[i];
        }

        return newNormals;
    }*/

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

    List<int> ReturnIndices(int length, int offset = 0, bool closedLoop = false)
    {
        List<int> newIndices = new List<int>();
        for (int i = offset; i < offset + length; i += 6)
        {
            newIndices.AddRange(new [] {i, i + 1, i + 2, i + 5, i + 4, i + 3});
        }

        if (closedLoop)
        {
            newIndices.AddRange(new []
            {
                offset + length - 2,
                offset + length - 1,
                offset,
                offset + 1,
                offset,
                offset + length - 1
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

        return (p2 - p1).magnitude < 0.05f;
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

    /*bool TriangleIntersection(Vector3[] t1, Vector3[] t2)
    {
        for (int i = 0; i < t1.Length; i++)
        {
            for (int j = 0; j < t2.Length; j++)
            {
                Vector3 p1 = t1[i];
                Vector3 p2 = t1[(i + 1) % t1.Length];
                Vector3 p3 = t2[j];
                Vector3 p4 = t2[(j + 1) % t2.Length];

                bool intersecting;
                Vector3 intersection = IntersectionPoint(LineFromPoints(p1, p2), LineFromPoints(p3, p4), out intersecting);

                if (intersecting)
                {
                    if (Mathf.Sign(intersection.x - p1.x) != Mathf.Sign(intersection.x - p2.x) && 
                        Mathf.Sign(intersection.x - p1.z) != Mathf.Sign(intersection.x - p2.z) && 
                        Mathf.Sign(intersection.x - p3.x) != Mathf.Sign(intersection.x - p4.x) && 
                        Mathf.Sign(intersection.x - p3.z) != Mathf.Sign(intersection.x - p4.z))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    float[] LineFromPoints(Vector3 p1, Vector3 p2)
    {
        float m = (p2.z - p1.z) / (p2.x - p1.x);
        float c = p1.z - m * p1.z;
        return new[] { m, c };
    }

  Vector3 IntersectionPoint(float[] l1, float[] l2, out bool intersecting)
    {
        if (l2[0] - l1[0] < 0.001f)
        {
            intersecting = false;
            return Vector3.zero;
        }

        intersecting = true;
        float x = (l1[1] - l2[1]) / (l2[0] - l1[0]);
        float z = l1[0] * x + l1[1];
        return new Vector3(x, 0, z);
    }*/

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

    /*MeshVertex[] AddSplitAttribute(Vector3[] arr, int split)
    {
        MeshVertex[] newVertices = new MeshVertex[arr.Length];
        for (int i = 0; i < arr.Length; i++)
        {
            newVertices[i] = MeshVertex(arr[i], split);
        }

        return newVertices;
    }*/

    /*Vector3[] ReturnVertices(MeshVertex[] arr)
    {
        Vector3[] newVertices = new Vector3[arr.Length];
        for (int i = 0; i < arr.Length; i++)
        {
            newVertices[i] = arr[i].vertex;
        }

        return newVertices;
    }*/

}

[System.Serializable]
public class RoadParams
{
    public float roadWidth = 10f;
    public float roadLineWidth = 1f;
    public Vector3 yOffset = 0.005f * Vector3.up;
    public bool cleanSplits = true;
}

/*public class MeshVertex
{
    public Vector3 vertex;
    // -1 if part of the path before the split, 1 if part of the path after the split
    public int split = 0;

    public MeshVertex(Vector3 point, int split)
    {
        vertex = point;
        this.split = split;
    }
}*/