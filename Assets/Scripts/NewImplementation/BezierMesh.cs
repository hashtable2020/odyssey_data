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
        //Debug.Log("Starting mesh initialisation!");
        gameObject.SetActive(false);
        Mesh combinedMesh = roadMeshFilter.sharedMesh;
        combinedMesh.Clear();
        
        combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        List<Vector3> tmpLeftVertices = new List<Vector3>();
        List<Vector3> tmpRightVertices = new List<Vector3>();

        List<int> leftIndices = new List<int>();
        List<int> rightIndices = new List<int>();
        
        if (path == null)
            return;
        BezierPoint[][] points = path.pathParams.CoursePoints;
        int division = (int) Mathf.Round(1/path.pathParams.resolution);
        bool closedLoop = path.pathParams.closedLoop;
        int pathNum = 0;
        
        for (int i = 0; i < points.Length + (closedLoop ? 0 : -1); i++)
        {
            for (int j = 0; j < points[i].Length; j++)
            {
                BezierPoint p1 = points[i][j];
                for (int k = 0; k < points[(i + 1) % points.Length].Length; k++)
                {
                    BezierPoint p2 = points[(i + 1) % points.Length][k];
                    Vector3 yOffset = 0.005f * Vector3.up;
                        
                    Vector3[] bezierPoints = Handles.MakeBezierPoints(p1.basePoint + yOffset, p2.basePoint + yOffset,
                        p1.HandlePoints[0] + yOffset, p2.HandlePoints[1] + yOffset, division);

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
                        
                        tmpLeftVertices.AddRange(leftPoints);
                        tmpRightVertices.AddRange(rightPoints);
                    }
                    // Vertex index logic
                    leftIndices.AddRange(ReturnIndices(division,
                        pathNum * division));
                    rightIndices.AddRange(ReturnIndices(division,
                        pathNum * division));

                    pathNum++;

                }
            }
        }

        Vector3[] combinedVertices = new Vector3[tmpLeftVertices.Count + tmpRightVertices.Count];

        combinedMesh.vertices = combinedVertices;
        Debug.Log(combinedVertices.Length);

        for (int x = 0; x < rightIndices.Count; x++)
        {
            rightIndices[x] += tmpLeftVertices.Count;
        }

        //Debug.Log(tmpLeftVertices[0]);
        combinedMesh.subMeshCount = 2;
        
        combinedMesh.SetTriangles(leftIndices, 0);
        combinedMesh.SetTriangles(rightIndices, 1);

        Color32[] colors = new Color32[tmpLeftVertices.Count + tmpRightVertices.Count];

        Fill(colors, trackParams.leftLaneColor, 0, tmpLeftVertices.Count);
        Fill(colors, trackParams.rightLaneColor, tmpLeftVertices.Count, tmpLeftVertices.Count + tmpRightVertices.Count);

        combinedMesh.SetColors(colors);
        
        
        Vector2[] uvs = new Vector2[combinedMesh.vertexCount];

        for (int i = 0; i < uvs.Length; i++)
        {
            uvs[i] = new Vector2(combinedMesh.vertices[i].x, combinedMesh.vertices[i].z);
        }

        combinedMesh.uv = uvs;

        gameObject.SetActive(true);
    }

    List<int> ReturnIndices(int length, int offset)
    {
        List<int> newIndices = new List<int>();
        for (int i = 0; i < length - 1; i++)
        {
            newIndices.AddRange(new [] {2 * i + offset, 2 * i + 1 + offset, 2 * i + 2 + offset
                , 2 * i + 1 + offset, 2 * i + 2 + offset, 2 * i + 3 + offset});
        }

        return newIndices;
    }

    void Fill(Color32[] arr, Color32 color, int start, int end)
    {
        for (int i = start; i < end; i++)
        {
            arr[i] = color;
        }
    }
    
    
}

[System.Serializable]
public class RoadParams
{
    public float roadWidth = 10f;
    public float roadLineWidth = 1f;
    public Color leftLaneColor = new (0.26f, 0.53f, 0.96f);
    public Color rightLaneColor = new (0.96f, 0.89f, 0.25f);
}