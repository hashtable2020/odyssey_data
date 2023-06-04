using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
public class NewCameraController : MonoBehaviour
{
    public BezierPath path;
    public float speed;

    public float resolution = 0.01f;

    public Vector3 offset;
    public float backset = 0.1f;

    public bool recordingData;
    public int width = 360;
    public int height = 180;

    public int averageWindow = 3;

    public string dataDir = "Assets/Data/";
    
    public delegate IEnumerator UpdateCamera();

    public UpdateCamera MaskUpdate;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(FollowPath());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void TakeScreenshot(string filename)
    {
        RenderTexture rt = new RenderTexture(width, height, 24);
        path.uiParams.dataCamera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);
        path.uiParams.dataCamera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        path.uiParams.dataCamera.targetTexture = null;
        RenderTexture.active = null;
        
        Destroy(rt);
        byte[] bytes = screenShot.EncodeToPNG();
        File.WriteAllBytes(filename, bytes);
    }

    IEnumerator FollowPath()
    {
        bool instantiated = false;
        int imageIndex = 0;
        while (true)
        {
            if (PathParams.CoursePoints.Length <= 1)
            {
                continue;
            }
            if (!instantiated)
            {
                transform.position = PathParams.CoursePoints[0][0].basePoint;
                instantiated = true;
            }

            List<int> randomIndices = new List<int>();

            for (int a = 0; a < PathParams.CoursePoints.Length; a++)
            {
                int randomIndex = PathParams.CoursePoints[a].Length > 1
                    ? Mathf.FloorToInt(Random.value * PathParams.CoursePoints[a].Length)
                    : 0;
                randomIndices.Add(randomIndex);
            }
            
            path.SpawnSigns(randomIndices);
            
            BezierPoint p1;
            BezierPoint p2 = PathParams.CoursePoints[0][0];

            for (int i = 0; i < PathParams.CoursePoints.Length; i++)
            {
                p1 = p2;
                p2 = PathParams.CoursePoints[(i + 1) % PathParams.CoursePoints.Length][randomIndices[(i + 1) % PathParams.CoursePoints.Length]];
                Vector3[] bezierPoints = Handles.MakeBezierPoints(p1.basePoint, p2.basePoint, p1.HandlePoints()[1],
                    p2.HandlePoints()[0], Mathf.FloorToInt(1 / resolution));
                transform.position = bezierPoints[0];
                for (int j = averageWindow; j < bezierPoints.Length - averageWindow; j++)
                {
                    yield return new WaitForSeconds((bezierPoints[j] - bezierPoints[j - 1]).magnitude / speed);
                    transform.position = bezierPoints[j] + offset + backset * p1.HandlePoints()[0].normalized;
                    float turningAngle = Vector3.SignedAngle(Vector3.forward, bezierPoints[j] - bezierPoints[j - 1],
                        Vector3.up);
                    transform.eulerAngles = new Vector3(0,turningAngle, 0);
                    
                    // Do camera stuff here
                    if (recordingData)
                    {
                        // Clockwise is positive curvature, anticlockwise is negative curvature;
                        string file = dataDir + imageIndex + "-NormalImage.png";
                        string maskFile = dataDir + imageIndex + "-" + Curvature(bezierPoints[j-averageWindow], bezierPoints[j], bezierPoints[j+averageWindow]) + "-MaskedImage.png";

                        TakeScreenshot(file);
                        path.uiParams.maskMode = true;
                        yield return MaskUpdate();
                        TakeScreenshot(maskFile);
                        path.uiParams.maskMode = false;
                        yield return MaskUpdate();
                    }

                    imageIndex++;
                }

            }
        }
    }

    float Curvature(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        Vector2 p1 = new Vector2(v1.x, v1.z);
        Vector2 p2 = new Vector2(v2.x, v2.z);
        Vector2 p3 = new Vector2(v3.x, v3.z);
        float area = (p2.x - p1.x) * (p3.y - p2.y) - (p2.y - p1.y) * (p3.x - p2.x) / 2;
        float d1 = (p2 - p1).magnitude;
        float d2 = (p3 - p2).magnitude;
        float d3 = (p1 - p3).magnitude;
        return 4 * area / (d1 * d2 * d3);
    }
}
