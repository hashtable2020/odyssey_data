using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BezierPath))]
[CanEditMultipleObjects]
public class EditorHandler : Editor
{
    BezierPath _path;
    bool _clicked = false;
    bool _hovering = false;
    bool _stillSelected = true;
    Vector3 _point = Vector3.zero;
    KeyCode _keyPressed = KeyCode.None;
    KeyCode[] _hotkeys = new KeyCode[] {KeyCode.LeftShift, KeyCode.LeftAlt, KeyCode.S, KeyCode.M};

    private SerializedProperty[] _parameters = new SerializedProperty[2];
    //public Button resetPath;
    
    // Function to check whether the road was clicked and if so, the world space coordinates of the click
    public Vector3 ClickHandler(BezierPath path, out bool clicked, out bool hover, out KeyCode key, out bool changeKey)
    {
        // Default behaviour: Leave the keyPressed variable unchanged
        changeKey = false;
        key = KeyCode.None;
        // All extra information
        hover = false;
        clicked = false;
        
        Vector3 mousePosition = Event.current.mousePosition;
        Vector3 worldPoint = Vector3.down;

        Camera sceneCamera = SceneView.currentDrawingSceneView.camera;
        
        // Reads from the bottom left, so this line is necessary
        mousePosition.y = sceneCamera.pixelHeight - mousePosition.y;
        
        Ray ray = sceneCamera.ScreenPointToRay(mousePosition);
        RaycastHit hitInfo;
        
        // Null check

        if (path == null || path.uiParams.roadCollider == null)
        {
            clicked = false;
            hover = false;
            key = KeyCode.None;
            return worldPoint;
        }
        
        // Checks separately if the mouse is over the road plane and if the mouse has been clicked

        if (path.uiParams.roadCollider.Raycast(ray, out hitInfo, path.uiParams.maxMouseDistance))
        {
            //Debug.DrawRay(sceneCamera.transform.position, hitInfo.distance * ray.direction, Color.red, 1);
            worldPoint = hitInfo.point;
            //Debug.Log("Hovering!");
            // If the mouse is hovering over the road collider
            hover = true;
        }
        
        // If the user has clicked, leave key unchanged
        if (Event.current.type == EventType.MouseDown)
        {
            clicked = true;

            /*if (hover)
            {
                Debug.Log("Clicked at " + worldPoint);
            }*/
        } 
        // If the user is holding down a key, change the global hotkey variable to the key pressed
        else if (Event.current.type == EventType.KeyDown)
        {
            if (Array.IndexOf(_hotkeys, Event.current.keyCode) > -1)
            {
                //Debug.Log("Hotkey pressed!");
                key = Event.current.keyCode;
                changeKey = true;
            }
        } 
        // If the user has let go of the key, return the key to null.
        else if (Event.current.type == EventType.KeyUp)
        {
            key = KeyCode.None;
            changeKey = true;
        }

        //Debug.Log(key);
        return worldPoint;
    }
    void OnSceneGUI()
    {
        
        /*if (Event.current.type == EventType.KeyDown)
        {
            if (Event.current.keyCode == KeyCode.LeftShift)
            {
                Debug.Log("Shift is being pressed!");   
            }
        }*/
        //Debug.Log("Updating!");
        if (_stillSelected)
        {
            Selection.activeObject = target;
        }
        if (_path == null)
        {
            return;
        }

        KeyCode tmpKey = _keyPressed;
        bool changeKey;
        
        // Gets the current mouse position and whether the user has clicked it in this frame
        _point = ClickHandler(_path, out _clicked, out _hovering, out tmpKey, out changeKey);

        if (changeKey)
        {
            _keyPressed = tmpKey;
        }

        // Keyboard shortcuts, alt+click to remove, shift+click to add, s to split the path, m to merge
        if (_clicked)
        {
            _stillSelected = _hovering;
            /*if (_point != Vector3.down)
            {
                Debug.Log("Clicked at " + _point);
            }
            else
            {
                Debug.Log("Deselected path object.");
            }*/
        }

        if (_clicked || _keyPressed != KeyCode.None)
        {
            //Debug.Log("Clicked?: " + _clicked + ", Key pressed: " + _keyPressed);
            if (_keyPressed == KeyCode.LeftShift && _clicked)
            {
                Debug.Log("Shift-clicked!");
            }
            //Debug.Log("Key pressed: " + _keyPressed);
            
            //_path.PointHandler(_keyPressed, -1, _point);
        }

    }

    void OnEnable()
    {
        _path = target as BezierPath;
        _parameters[0] = serializedObject.FindProperty("pathParams");
        _parameters[1] = serializedObject.FindProperty("uiParams");
    }

    public override void OnInspectorGUI()
    {
        if (_parameters == null)
            return;
        EditorGUILayout.PropertyField(_parameters[0], new GUIContent("Path Parameters"));
        EditorGUILayout.PropertyField(_parameters[1], new GUIContent("UI Parameters"));
        serializedObject.ApplyModifiedProperties();
    }

    
    // Draws a multi-curve spline from several bezier points and a specific offset, colour, texture and width
    // Also draws normals if required with a length of normalLength, thickness of normalThickness and division normals
    // along each curve
    void DrawBezierSpline(BezierPoint[] curvePoints, 
        float offset, Color color, Texture2D texture, float width, 
        bool showNormals=false, float normalLength=1, float normalThickness=1, int division=20)
    {
        for (int i = 0; i < curvePoints.Length + (_path.pathParams.closedLoop ? 0 : -1); i++)
        {
            Vector3[] normals = 
            {
                Vector3.Normalize(Vector3.Cross(Vector3.up, curvePoints[i].HandlePoints[1])),
                Vector3.Normalize(Vector3.Cross(Vector3.up, curvePoints[(i + 1) % curvePoints.Length].HandlePoints[0]))
            };
            Handles.DrawBezier(curvePoints[i].BasePoint + offset * normals[0],
                curvePoints[(i + 1) % curvePoints.Length].BasePoint + offset * normals[1],
                curvePoints[i].HandlePoints[1],
                curvePoints[(i + 1) % curvePoints.Length].HandlePoints[0],
                color,
                texture,
                width
            );
            
            if (showNormals)
            {
                Vector3[] normalPoints = Handles.MakeBezierPoints(
                    curvePoints[i].BasePoint + offset * normals[0],
                    curvePoints[(i + 1) % curvePoints.Length].BasePoint + offset * normals[1],
                    curvePoints[i].HandlePoints[1],
                    curvePoints[(i + 1) % curvePoints.Length].HandlePoints[0],
                    division);
                for (int j = 0; j < normalPoints.Length - 1; j++)
                {
                    Vector3 diff = normalPoints[j + 1] - normalPoints[j];
                    Vector3 localNormal = Vector3.Normalize(Vector3.Cross(diff, Vector3.up));
                    Handles.DrawLine(normalPoints[j] + normalLength * localNormal / 2, 
                        normalPoints[j] - normalLength * localNormal / 2,
                        normalThickness);
                }
                
            }
        }
    }

    void DrawBezierDisplay(BezierPoint[] curvePoints)
    {
        DrawBezierSpline(curvePoints, 0, 
            _path.uiParams.curveColor, 
            _path.uiParams.curveTexture, 
            _path.uiParams.curveWidth,
            _path.uiParams.showNormals,
            _path.uiParams.normalDistance,
            _path.uiParams.normalWidth,
            Mathf.RoundToInt(1 / _path.pathParams.resolution));
        
        if (_path.uiParams.showNormals)
        {
            DrawBezierSpline(curvePoints, -_path.uiParams.normalDistance/2, 
                _path.uiParams.curveColor, 
                _path.uiParams.curveTexture, 
                _path.uiParams.offsetWidth);
            DrawBezierSpline(curvePoints, _path.uiParams.normalDistance/2, 
                _path.uiParams.curveColor, 
                _path.uiParams.curveTexture, 
                _path.uiParams.offsetWidth);
        }
        
    }
    
    // Return all paths that need to be drawn
    BezierPoint[][] PathsFromCourse(BezierPoint[][] coursePoints, bool closedLoop)
    {
        int size = (closedLoop ? 0 : -1);
        List<int> sizes = new List<int>();
        int sizeIndex = 0;
        for (int i = 0; i < coursePoints.Length; i++)
        {
            if (coursePoints[i].Length >= 2)
            {
                size += coursePoints[i].Length + 1;
                List<int> newSizes = new List<int>();
                for (int j = 0; j < coursePoints.Length; j++)
                {
                    newSizes.Add(3);
                }
                newSizes.Add(0);
                sizes.AddRange(newSizes);
                sizeIndex += coursePoints.Length + 1;
            }
            else
            {
                sizes[sizeIndex]++;
            }
        }
        
        BezierPoint[][] newPaths = new BezierPoint[size][];
        int currentIndex = 0;
        int currentPathIndex = 0;
        for (int i = 0; i < coursePoints.Length + (closedLoop ? 0 : -1); i++)
        {
            if (coursePoints[i].Length > 1)
            {
                for (int j = 1; j < coursePoints[i].Length + 1; j++)
                {
                    BezierPoint[] newPath =
                    {
                        coursePoints[i - 1][0],
                        coursePoints[i][j - 1],
                        coursePoints[(i + 1) % coursePoints.Length][0]
                    };
                    newPaths[currentIndex + j - 1] = newPath;
                }
                currentIndex += coursePoints[i].Length + 1;
                currentPathIndex = 0;
                newPaths[currentIndex] = new BezierPoint[sizes[currentIndex]];
            }
            else
            {
                newPaths[currentIndex][currentPathIndex] = coursePoints[i][0];
                currentPathIndex++;
            }
        }

        return newPaths;
    }
    
    void DrawMultipleSplines(BezierPoint[][] coursePoints)
    {
        BezierPoint[][] newPaths = PathsFromCourse(coursePoints, _path.pathParams.closedLoop);
        foreach (BezierPoint[] path in newPaths)
        {
            DrawBezierDisplay(path);
        }
    }
}
