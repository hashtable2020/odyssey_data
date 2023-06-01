using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public class NewCameraController : MonoBehaviour
{
    public BezierPath path;
    public float speed;

    public float resolution = 0.01f;

    public Vector3 offset;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(FollowPath());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator FollowPath()
    {
        while (true)
        {
            BezierPoint p1;
            BezierPoint p2 = PathParams.CoursePoints[0][0];
            for (int i = 0; i < PathParams.CoursePoints.Length; i++)
            {
                int randomIndex = PathParams.CoursePoints[(i + 1) % PathParams.CoursePoints.Length].Length > 1
                    ? Mathf.FloorToInt(Random.value *
                                       PathParams.CoursePoints[(i + 1) % PathParams.CoursePoints.Length]
                                           .Length)
                    : 0;
                    
                p1 = p2;
                p2 = PathParams.CoursePoints[(i + 1) % PathParams.CoursePoints.Length][randomIndex];
                Vector3[] bezierPoints = Handles.MakeBezierPoints(p1.basePoint + offset, p2.basePoint + offset, p1.HandlePoints()[1] + offset,
                    p2.HandlePoints()[0] + offset, Mathf.FloorToInt(1 / resolution));
                transform.position = bezierPoints[0];
                for (int j = 1; j < bezierPoints.Length; j++)
                {
                    yield return new WaitForSeconds((bezierPoints[j] - bezierPoints[j - 1]).magnitude / speed);
                    transform.position = bezierPoints[j];
                    transform.eulerAngles = new Vector3(0,Vector3.SignedAngle(Vector3.forward, bezierPoints[j] - bezierPoints[j - 1], Vector3.up), 0);
                }

            }
        }

    }
}
