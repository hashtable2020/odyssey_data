using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        int division = (int) Mathf.Round(1/path.pathParams.resolution);
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
                    
                        
                    Vector3[] bezierPoints = Handles.MakeBezierPoints(p1.basePoint + trackParams.yOffset, p2.basePoint + trackParams.yOffset,
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
                        Vector3[] rightPoints = {
                            a - (trackParams.roadWidth - trackParams.roadLineWidth) / 2 * normal,
                            a - (trackParams.roadWidth + trackParams.roadLineWidth) / 2 * normal
                        };
                        
                        //Debug.DrawLine(rightPoints[0], rightPoints[1], Color.red);

                        int splitAttribute = (points[i].Length > 1)
                            ? 1
                            : (points[(i + 1) % points.Length].Length > 1 ? -1 : 0);
                        
                        tmpLeftVertices.AddRange(leftPoints);
                        tmpRightVertices.AddRange(rightPoints);
                    }
                    
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
        
        Debug.Log(Unpack(leftVertices).Count);

        for (int x = 0; x < points.Length + (closedLoop ? 0 : -1); x++)
        {
            bool broken = false;
            Vector3[] tmpLeftTriangles = TriangleVertices(leftVertices[x]).ToArray();
            Vector3[] tmpRightTriangles = TriangleVertices(rightVertices[x]).ToArray();
            for (int leftT = 0; leftT < tmpLeftTriangles.Length; leftT += 3)
            {
                for (int rightT = 0; rightT < tmpRightTriangles.Length; rightT += 3)
                {
                    // If triangles intersect
                    if (TriangleIntersection(
                            new[] { tmpLeftTriangles[leftT], tmpLeftTriangles[leftT + 1], tmpLeftTriangles[leftT + 2] },
                            new[]
                            {
                                tmpRightTriangles[rightT], tmpRightTriangles[rightT + 1], tmpRightTriangles[rightT + 2]
                            }))
                    {
                        if (points[x].Length > 1)
                        {
                            leftVertices[x] = SliceArray(leftVertices[x].ToArray(), 0, leftT / 3).ToList();
                            rightVertices[x] = SliceArray(rightVertices[x].ToArray(), 0, leftT / 3).ToList();
                            broken = true;
                            break;
                        } 
                        if (points[(x + 1) % points.Length].Length > 1)
                        {
                            leftVertices[x] = SliceArray(leftVertices[x].ToArray(), leftT / 3, -1).ToList();
                            rightVertices[x] = SliceArray(rightVertices[x].ToArray(), leftT / 3, -1).ToList();
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
        
        Debug.Log(Unpack(leftVertices).Count);
        
             //Debug.Log(tmpLeftVertices.Count);
        List<Vector3> triangleLeft = TriangleVertices(Unpack(leftVertices));
        List<Vector3> triangleRight = TriangleVertices(Unpack(rightVertices));

        //Debug.Log(triangleRight.Count - triangleLeft.Count);

        //DrawTriangles(triangleRight, Color.red);
        
        //int leftTriangleCount = triangleLeft.Count;
        //Debug.Log(leftTriangleCount);

        //Debug.Log("Left num: " + triangleLeft.Count + ", Combined num: " + combinedVertices.Count);
        
        

        /*combinedMesh.subMeshCount = 2;
        
        System.Collections.Generic.List<int> leftIndices = ReturnIndices(leftTriangleCount);
        System.Collections.Generic.List<int> rightIndices = ReturnIndices(leftTriangleCount, leftTriangleCount);
        
        
        combinedMesh.SetTriangles(leftIndices, 0, true, 0);
        combinedMesh.SetTriangles(rightIndices, 1, true, 0);*/

        List<int> leftIndices = ReturnIndices(triangleLeft.Count, 0, closedLoop);
        List<int> rightIndices = ReturnIndices(triangleRight.Count, 0, closedLoop);

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

    Vector3[] SliceArray(Vector3[] arr, int start, int end)
    {
        int newEnd = (end < 0) ? (arr.Length + end + 1) : end;
        Vector3[] newArr = new Vector3[newEnd - start];
        for (int i = start; i < newEnd; i++)
        {
            newArr[i - start] = arr[i];
        }

        return newArr;
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

    // Quick 2D algorithm, no need for fancy 3D stuff
    List<Vector3> TriangleVertices(List<Vector3> vertices)
    {
        List<Vector3> newVertices = new System.Collections.Generic.List<Vector3>();
        for (int i = 0; i < vertices.Count - 2; i++)
        {
            newVertices.AddRange(new [] {vertices[i], vertices[i+1], vertices[i+2]});
        }

        return newVertices;
    }

    bool TriangleIntersection(Vector3[] t1, Vector3[] t2)
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
    }

    void DrawTriangles(List<Vector3> vertices, Color color)
    {
        for (int i = 0; i < (vertices.Count) / 3; i++)
        {
            Debug.DrawLine(vertices[3 * i], vertices[3 * i + 1], color);
            Debug.DrawLine(vertices[3 * i + 1], vertices[3 * i + 2], color);
            Debug.DrawLine(vertices[3 * i + 2], vertices[3 * i], color);
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

    List<Vector3> Unpack(List<Vector3>[] arr)
    {
        List<Vector3> newArr = new List<Vector3>();
        foreach (List<Vector3> list in arr)
        {
            newArr.AddRange(list);
        }

        return newArr;
    }

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