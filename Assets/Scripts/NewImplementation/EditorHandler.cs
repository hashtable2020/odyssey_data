using System;
using System.Collections;
using System.Collections.Generic;
using Mono.Cecil.Cil;
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
    private int _pointSelected = -1;
    private int _indexSelected = 0;
    private int _handleSelected = 0;
    Vector3 _point = Vector3.zero;
    private BezierPoint[][] _pathPoints;
    private KeyCode _keyPressed = KeyCode.None;
    private readonly KeyCode[] _hotkeys = {KeyCode.LeftShift, KeyCode.LeftControl, KeyCode.S, KeyCode.M, KeyCode.D};
    private SerializedProperty[] _parameters = new SerializedProperty[2];
    //public Button resetPath;
    
    // Function to check whether the road was clicked and if so, the world space coordinates of the click
    private Vector3 ClickHandler(BezierPath path, out bool clicked, out bool hover, out KeyCode key, out bool changeKey)
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

        _pathPoints = _path.pathParams.CoursePoints;
        //Debug.Log(pathPoints);

        KeyCode tmpKey = _keyPressed;
        bool changeKey;
        
        // Gets the current mouse position and whether the user has clicked it in this frame
        _point = ClickHandler(_path, out _clicked, out _hovering, out tmpKey, out changeKey);

        if (changeKey)
        {
            _keyPressed = tmpKey;
            if (_keyPressed == KeyCode.D)
            {
                _pointSelected = -1;
                _indexSelected = 0;
                _handleSelected = 0;
            }
        }
        
        if (_clicked)
        {
            if (_pointSelected != -1)
            {
                BezierPoint point = _pathPoints[_pointSelected][_indexSelected];
                for (int i = 0; i < point.HandlePoints.Length; i++)
                {
                    //Debug.Log("Index: " + i + ", Distance: " + Vector3.Distance(_point, point.HandlePoints[i]));
                    if (Vector3.Distance(_point, point.HandlePoints[i]) <= _path.uiParams.handleSize)
                    {
                        _handleSelected = 1 - 2 * i;
                        break;
                    }
                }
            }
            bool broken = false;
            for (int i = 0; i < _pathPoints.Length; i++)
            {
                for (int j = 0; j < _pathPoints[i].Length; j++)
                {
                    //Debug.Log(i + " " + j);
                    BezierPoint point = _pathPoints[i][j];
                    //Debug.Log("Point index: " + j + ", Distance: " + Vector3.Distance(_point, point.BasePoint));
                    if (Vector3.Distance(_point, point.BasePoint) <= _path.uiParams.pointSize)
                    {
                        _pointSelected = i;
                        _indexSelected = j;
                        _handleSelected = 0;
                        broken = true;
                        break;
                    }
                }

                if (broken)
                {
                    break;
                }
            }
            // Keyboard shortcuts, ctrl+click to remove, shift+click to add, s to split the path, m to merge
            
            
            
            Debug.Log("Handle Selected: " + _handleSelected);
            Debug.Log("Point Selected: " + _pointSelected);
        }
        
        
        if (_keyPressed != KeyCode.None)
        {
            //Debug.Log(_keyPressed);
            if (_clicked)
            {
                if (!(_keyPressed != KeyCode.LeftShift && _pointSelected == -1))
                _path.PointHandler(_keyPressed, _pointSelected, _point);
                if (_keyPressed == KeyCode.LeftControl)
                {
                    _pointSelected = -1;
                }
            }
        }

        if (_pointSelected != -1)
        {
            //Debug.Log("i: " + _pointSelected + ", j: " + _indexSelected);
            BezierPoint selectedPoint = _pathPoints[_pointSelected][_indexSelected];
            Handles.color = _path.uiParams.handleColor;
            Handles.DrawLine(selectedPoint.BasePoint, selectedPoint.HandlePoints[0], _path.uiParams.handleWidth);
            Handles.DrawLine(selectedPoint.BasePoint, selectedPoint.HandlePoints[1], _path.uiParams.handleWidth);
            Handles.color = _path.uiParams.handleDiskColor;
            Handles.DrawSolidDisc(selectedPoint.HandlePoints[0], Vector3.up, _path.uiParams.handleSize);
            Handles.DrawSolidDisc(selectedPoint.HandlePoints[1], Vector3.up, _path.uiParams.handleSize);
            //Debug.Log(_handleSelected);
            
            if (_handleSelected == 0)
            {
                EditorGUI.BeginChangeCheck();
                //Debug.Log("Changing Position");
                Vector3 newPosition = Handles.PositionHandle(selectedPoint.BasePoint, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Moved point");
                    //Undo.RecordObject(_testPath, "Changed point position");
                    //Debug.Log("Updating!");
                    selectedPoint.BasePoint = newPosition;
                    _path.Refresh();
                }
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                Vector3 newPosition = Handles.PositionHandle(selectedPoint.HandlePoints[(1 - _handleSelected) / 2], Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                    Undo.RecordObject(target, "Moved point");
                    //Undo.RecordObject(_testPath, "Changed point position");
                    //Debug.Log("Updating!");
                    selectedPoint.LocalHandle = (newPosition - selectedPoint.BasePoint) * _handleSelected;
                    _path.Refresh();
                }
        }
        _path.Refresh();

    }

    void OnEnable()
    {
        _path = target as BezierPath;
        _parameters[0] = serializedObject.FindProperty("pathParams");
        _parameters[1] = serializedObject.FindProperty("uiParams");
        Tools.hidden = true;
        if (_path != null)
        {
            _pathPoints = _path.pathParams.CoursePoints;
            _path.Refresh += () => { DrawBezierDisplay(_pathPoints); };
            _pointSelected = -1;
        }

    }

    public override void OnInspectorGUI()
    {
        if (_path == null || _parameters == null)
            return;
        
        EditorGUILayout.PropertyField(_parameters[0], new GUIContent("Path Parameters"));
        EditorGUILayout.PropertyField(_parameters[1], new GUIContent("UI Parameters"));
        
        // Make point selected appear in Inspector
        if (_pointSelected != -1)
        {
            
        }

        if (GUILayout.Button("Reset Path"))
        {
            _path.Reset();
        }

        if (GUILayout.Button("Refresh Visuals"))
        {
            _path.Refresh();
        }
        serializedObject.ApplyModifiedProperties();
        _path.Refresh();
    }

    
    // Draws a multi-curve spline from several bezier points and a specific offset, colour, texture and width
    // Also draws normals if required with a length of normalLength, thickness of normalThickness and division normals
    // along each curve
    void DrawBezierSpline(BezierPoint[][] curvePoints, 
        float offset, Color curveColor, Texture2D texture, float width, 
        bool showNormals=false, float normalLength=1, float normalThickness=1, float offsetThickness=1
        , int division=20)
    {
        if (_path == null || curvePoints.Length == 0)
        {
            return;
        }
        if (curvePoints.Length == 1)
        {
            Handles.color = _path.uiParams.pointColor;
            Handles.DrawSolidDisc(curvePoints[0][0].BasePoint, Vector3.up, _path.uiParams.pointSize);
            return;
        }
        for (int i = 0; i < curvePoints.Length - 1; i++)
        {
            for (int j = 0; j < curvePoints[i].Length; j++)
            {
                for (int k = 0; k < curvePoints[(i + 1) % curvePoints.Length].Length; k++)
                {
                    Vector3[] normals = 
                    {
                        Vector3.Normalize(Vector3.Cross(Vector3.up, curvePoints[i][j].HandlePoints[1])),
                        Vector3.Normalize(Vector3.Cross(Vector3.up, curvePoints[(i + 1) % curvePoints.Length][k].HandlePoints[0]))
                    };
                    Handles.DrawBezier(curvePoints[i][j].BasePoint + offset * normals[0],
                        curvePoints[(i + 1) % curvePoints.Length][k].BasePoint + offset * normals[1],
                        curvePoints[i][j].HandlePoints[1],
                        curvePoints[(i + 1) % curvePoints.Length][k].HandlePoints[0],
                        curveColor,
                        texture,
                        width
                    );
                    
                    if (showNormals)
                    {
                        Handles.color = _path.uiParams.normalColor;
                        Vector3[] normalPoints = Handles.MakeBezierPoints(
                            curvePoints[i][j].BasePoint + offset * normals[0],
                            curvePoints[(i + 1) % curvePoints.Length][k].BasePoint + offset * normals[1],
                            curvePoints[i][j].HandlePoints[1],
                            curvePoints[(i + 1) % curvePoints.Length][k].HandlePoints[0],
                            division);
                        
                        for (int l = 0; l < normalPoints.Length - 1; l++)
                        {
                            Vector3 diff = normalPoints[l + 1] - normalPoints[l];
                            Vector3 localNormal = Vector3.Normalize(Vector3.Cross(diff, Vector3.up));
                            Handles.color = _path.uiParams.normalColor;
                            Handles.DrawLine(normalPoints[l] + normalLength * localNormal / 2, 
                                normalPoints[l] - normalLength * localNormal / 2,
                                normalThickness);
                            Handles.color = curveColor;
                            Handles.DrawLine(normalPoints[l] + normalLength * localNormal / 2,
                                normalPoints[l + 1] + normalLength * localNormal / 2,
                               offsetThickness);
                            Handles.DrawLine(normalPoints[l] - normalLength * localNormal / 2,
                                normalPoints[l + 1] - normalLength * localNormal / 2,
                                offsetThickness);
                        }
                
                }
            } 
            }
    }
        
        Handles.color = _path.uiParams.pointColor;
        foreach (BezierPoint[] path in curvePoints)
        {
            foreach (BezierPoint point in path)
            {
                Handles.DrawSolidDisc(point.BasePoint, Vector3.up, _path.uiParams.pointSize);
            }
        }
        //Debug.Log("Working!");
    }

    void DrawBezierDisplay(BezierPoint[][] curvePoints)
    {
        DrawBezierSpline(curvePoints, 0, 
                _path.uiParams.curveColor, 
                _path.uiParams.curveTexture, 
                _path.uiParams.curveWidth,
                _path.uiParams.showNormals,
                _path.uiParams.normalDistance,
                _path.uiParams.normalWidth,
                _path.uiParams.offsetWidth,
                Mathf.RoundToInt(1 / _path.pathParams.resolution));
            /*if (_path.uiParams.showNormals)
            {
                DrawBezierSpline(curvePoints, -_path.uiParams.normalDistance/2, 
                    _path.uiParams.curveColor, 
                    _path.uiParams.curveTexture, 
                    _path.uiParams.offsetWidth);
                DrawBezierSpline(curvePoints, _path.uiParams.normalDistance/2, 
                    _path.uiParams.curveColor, 
                    _path.uiParams.curveTexture, 
                    _path.uiParams.offsetWidth);
            }*/
        
    }
    
    /*// Return all paths that need to be drawn
    BezierPoint[][] PathsFromCourse(BezierPoint[][] coursePoints, bool closedLoop)
    {
        if (coursePoints == null || coursePoints.Length == 0)
        {
            return null;
        } 
        if (coursePoints.Length == 1)
        {
            return new[] { new[] { coursePoints[0][0] } };
        }
        int size = 1;
        List<int> sizes = new List<int>();
        sizes.Add(0);
        int sizeIndex = 0;
        for (int i = 0; i < coursePoints.Length; i++)
        {
            if (coursePoints[i].Length >= 2)
            {
                size += coursePoints[i].Length + 1;
                for (int j = 0; j < coursePoints.Length; j++)
                {
                    sizes.Add(3);
                }
                sizes.Add(0);
                sizeIndex += coursePoints.Length + 1;
            }
            else
            {
                sizes[sizeIndex]++;
            }
            Debug.Log("Length: " + sizes.Count);
            foreach (int newSize in sizes)
            {
                Debug.Log(newSize);
            }
            Debug.Log("__");
        }
        
        if (closedLoop)
        {
            sizes[^1]++;
        }
        

        //Debug.Log(sizes[0] + " " + sizes[1]);

        BezierPoint[][] newPaths = new BezierPoint[size][];
        int currentIndex = 0;
        int currentPathIndex = 0;
        newPaths[currentIndex] = new BezierPoint[sizes[currentIndex]];
        for (int i = 0; i < coursePoints.Length; i++)
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
                    newPaths[currentIndex + j] = newPath;
                }
                currentIndex += coursePoints[i].Length + 1;
                currentPathIndex = 0;
                newPaths[currentIndex] = new BezierPoint[sizes[currentIndex]];
            }
            else
            {
                if (sizes[currentIndex] != 1)
                {
                    newPaths[currentIndex][currentPathIndex] = coursePoints[i][0];
                    currentPathIndex++;
                }
            }
        }

        if (closedLoop)
        {
            newPaths[^1][^1] = coursePoints[0][0];
        }

        if (newPaths.Length > 1)
        {
            Debug.Log(newPaths[2][1].BasePoint);
            Debug.Log(newPaths[1][1].BasePoint);
        }
        //Debug.Log(newPaths[1][1].BasePoint);

        return newPaths;
    }
    
    void DrawMultipleSplines(BezierPoint[][] coursePoints)
    {
        BezierPoint[][] newPaths = PathsFromCourse(coursePoints, _path.pathParams.closedLoop);
        if (newPaths == null)
            return;
        foreach (BezierPoint[] path in newPaths)
        {
            DrawBezierDisplay(path);
        }
    }*/
}
