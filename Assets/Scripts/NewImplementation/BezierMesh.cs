using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
[ExecuteAlways]
public class BezierMesh : MonoBehaviour
{

    // Create mesh
    public BezierPath path;
    public MeshFilter roadMeshFilter;
    public RoadParams trackParams;

    public void UpdateMesh()
    {
        if (path == null || path.pathParams.CoursePoints.Length <= 1)
        {
            roadMeshFilter.sharedMesh.Clear();
            return;
        }
        //Debug.Log("Starting mesh initialisation!");
        gameObject.SetActive(false);

        List<Vector3> tmpLeftVertices = new List<Vector3>();
        List<Vector3> tmpRightVertices = new List<Vector3>();

        if (path == null)
            return;
        BezierPoint[][] points = path.pathParams.CoursePoints;
        int division = (int) Mathf.Round(1/path.pathParams.resolution);
        bool closedLoop = path.pathParams.closedLoop;

        for (int i = 0; i < points.Length + (closedLoop ? 0 : -1); i++)
        {
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
                            a - (trackParams.roadWidth + trackParams.roadLineWidth) / 2 * normal,
                            a - (trackParams.roadWidth - trackParams.roadLineWidth) / 2 * normal
                        };
                        
                        //Debug.DrawLine(rightPoints[0], rightPoints[1], Color.red);
                        
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
        }
        
        //Debug.Log(tmpLeftVertices.Count);
        List<Vector3> triangleLeft = TriangleVertices(tmpLeftVertices);
        List<Vector3> triangleRight = TriangleVertices(tmpRightVertices);

        //DrawTriangles(triangleLeft, Color.red);
        
        int leftTriangleCount = triangleLeft.Count;
        //Debug.Log(leftTriangleCount);
        List<Vector3> combinedVertices = new List<Vector3>();
        
        combinedVertices.AddRange(triangleLeft);
        combinedVertices.AddRange(triangleRight);


        Mesh combinedMesh = new Mesh();
        combinedMesh.vertices = combinedVertices.ToArray();

        combinedMesh.subMeshCount = 2;
        
        System.Collections.Generic.List<int> leftIndices = ReturnIndices(leftTriangleCount);
        System.Collections.Generic.List<int> rightIndices = ReturnIndices(leftTriangleCount, leftTriangleCount);
        
        
        combinedMesh.SetTriangles(leftIndices, 0, true, 0);
        combinedMesh.SetTriangles(rightIndices, 1, true, 0);
        
        combinedMesh.RecalculateNormals();
        combinedMesh.Optimize();

        roadMeshFilter.mesh = combinedMesh;

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

    List<int> ReturnIndices(int length, int offset = 0)
    {
        List<int> newIndices = new List<int>();
        for (int i = offset; i < offset + length; i += 6)
        {
            newIndices.AddRange(new [] {i, i + 1, i + 2, i + 5, i + 4, i + 3});
        }
        newIndices.AddRange(new []
        {
            offset + length - 2,
            offset + length - 1,
            offset,
            offset + 1,
            offset,
            offset + length - 1
        });
        return newIndices;
    } 

    List<Vector3> TriangleVertices(List<Vector3> vertices)
    {
        List<Vector3> newVertices = new System.Collections.Generic.List<Vector3>();
        for (int i = 0; i < vertices.Count - 2; i++)
        {
            newVertices.AddRange(new [] {vertices[i], vertices[i+1], vertices[i+2]});
        }

        return newVertices;
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

}

[System.Serializable]
public class RoadParams
{
    public float roadWidth = 10f;
    public float roadLineWidth = 1f;
    public Vector3 yOffset = 0.005f * Vector3.up;
}