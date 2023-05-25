using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BezierPath))]
[CanEditMultipleObjects]
public class EditorHandler : Editor
{
    BezierPath _path;
    bool _clicked = false;
    bool _dragging = false;
    private int _pointSelected = -1;
    private int _indexSelected = 0;
    private int _handleSelected = 0;
    private int _planeSelected = 0;
    private string[] _planeOptions = { "Top-down (xz)", "Side-on (xy)", "Side-on (yz)", "Free-space (xyz)" };
    Vector3 _point = Vector3.zero;
    private BezierPoint[][] _pathPoints;
    private KeyCode _keyPressed = KeyCode.None;
    private readonly KeyCode[] _hotkeys = {KeyCode.LeftShift, KeyCode.LeftControl, KeyCode.S, KeyCode.M};
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
        
        //Debug.Log(sceneCamera.scaledPixelHeight - sceneCamera.pixelHeight);
        
        // Reads from the bottom left, so this line is necessary
        mousePosition.y = sceneCamera.scaledPixelHeight - mousePosition.y;
        
        Ray ray = sceneCamera.ScreenPointToRay(mousePosition);
        float distance;
        
        // Null check

        if (path == null)
        {
            clicked = false;
            hover = false;
            key = KeyCode.None;
            return worldPoint;
        }
        
        // Checks separately if the mouse is over the road plane and if the mouse has been clicked
        Plane plane = new Plane(Vector3.up, new Vector3(1, _path.uiParams.yPlane, 1));
        if (plane.Raycast(ray, out distance))
        {
            //Debug.Log("Collided!");
            //Debug.DrawRay(sceneCamera.transform.position, ray.GetPoint(distance), Color.red);
            worldPoint = ray.GetPoint(distance);
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
            if (System.Array.IndexOf(_hotkeys, Event.current.keyCode) > -1)
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
        // Defines new variable for later
        _pathPoints = _path.pathParams.CoursePoints;
        Selection.activeObject = target;
        if (_pointSelected != -1)
        {
            // Draws the selected point and handles
            BezierPoint selectedPoint = _pathPoints[_pointSelected][_indexSelected];
            
            /*Debug.Log("Regular");
            Debug.Log(selectedPoint.localHandles[0]);
            Debug.Log(selectedPoint.localHandles[1]);*/
            
            Handles.color = _path.uiParams.handleColor;
            Handles.DrawLine(selectedPoint.basePoint, selectedPoint.HandlePoints[0], _path.uiParams.handleWidth);
            Handles.DrawLine(selectedPoint.basePoint, selectedPoint.HandlePoints[1], _path.uiParams.handleWidth);
            Handles.color = _path.uiParams.handleDiskColor;
            Handles.DrawSolidDisc(selectedPoint.HandlePoints[0], Vector3.up, _path.uiParams.handleSize);
            Handles.DrawSolidDisc(selectedPoint.HandlePoints[1], Vector3.up, _path.uiParams.handleSize);
            
            
            // Checks if any of them have been dragged
            EditorGUI.BeginChangeCheck();
            Vector3 newPosition = _handleSelected == 0 ? Handles.PositionHandle(selectedPoint.basePoint, Quaternion.identity) : 
                Handles.PositionHandle(selectedPoint.HandlePoints[(1 - _handleSelected) / 2], Quaternion.identity);
            
            
            if (EditorGUI.EndChangeCheck())
            {
                _dragging = true;
                Undo.RecordObject(_path, "Changed path parameters");
                
                if (_handleSelected == 0)
                {
                    selectedPoint.basePoint = newPosition;
                }
                else
                {
                    selectedPoint.handlesChanged = true;
                    if (_path.uiParams.handlesSeparate)
                    {
                        selectedPoint.localHandles[(1 - _handleSelected) / 2] = newPosition - selectedPoint.basePoint;
                        selectedPoint.localHandles[(1 + _handleSelected) / 2] =
                            -selectedPoint.localHandles[(1 + _handleSelected) / 2].magnitude *
                            (selectedPoint.basePoint - newPosition).normalized;
                    }
                    else
                    {
                        selectedPoint.localHandles[0] = (newPosition - selectedPoint.basePoint) * _handleSelected;
                        selectedPoint.localHandles[1] = (selectedPoint.basePoint - newPosition) * _handleSelected;
                    }
                }
            }
            else
            {
                _dragging = false;
            }
        }
        if (_path == null)
        {
            return;
        }

        KeyCode tmpKey = _keyPressed;
        bool changeKey;
        bool hovering;
        
        // Gets the current mouse position and whether the user has clicked it in this frame
        _point = ClickHandler(_path, out _clicked, out hovering, out tmpKey, out changeKey);
        
        
        
        if (changeKey)
        {
            _keyPressed = tmpKey;
        }
        
        if (_clicked)
        {
            //Debug.Log(_point);
            bool handleBroken = false;
            if (_pointSelected != -1)
            {
                BezierPoint point = _pathPoints[_pointSelected][_indexSelected];
                for (int i = 0; i < point.HandlePoints.Length; i++)
                {
                    //Debug.Log("Index: " + i + ", Distance: " + Vector3.Distance(_point, point.HandlePoints[i]));
                    if (Vector3.Distance(_point, point.HandlePoints[i]) <= _path.uiParams.handleSize)
                    {
                        _handleSelected = 1 - 2 * i;
                        handleBroken = true;
                        break;
                    }
                }
            }
            bool pointBroken = false;
            for (int i = 0; i < _pathPoints.Length; i++)
            {
                for (int j = 0; j < _pathPoints[i].Length; j++)
                {
                    //Debug.Log(i + " " + j);
                    BezierPoint point = _pathPoints[i][j];
                    //Debug.Log("Point index: " + j + ", Distance: " + Vector3.Distance(_point, point.basePoint));
                    if (Vector3.Distance(_point, point.basePoint) <= _path.uiParams.pointSize)
                    {
                        _pointSelected = i;
                        _indexSelected = j;
                        _handleSelected = 0;
                        pointBroken = true;
                        break;
                    }
                }

                if (pointBroken)
                {
                    break;
                }
            }

            if (!pointBroken && !handleBroken && !_dragging)
            {
                _pointSelected = -1;
                _indexSelected = 0;
                _handleSelected = 0;
            }
        }
        
        
        if (_keyPressed != KeyCode.None)
        {
            //Debug.Log(_keyPressed);
            if (_clicked && _point != Vector3.down)
            {
                if (!(_keyPressed != KeyCode.LeftShift && _pointSelected == -1))
                    _path.PointHandler(_keyPressed, _pointSelected, _point);
                if (_keyPressed == KeyCode.LeftControl || _keyPressed == KeyCode.M)
                {
                    _pointSelected = -1;
                }
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
            _path.Refresh += () =>
            {
                DrawBezierDisplay(_pathPoints);
            };
            _path.Reset += () => { _pointSelected = -1; };
            _pointSelected = -1;

            _path.Refresh();
        }

    }

    public override void OnInspectorGUI()
    {
        if (_path == null || _parameters == null)
            return;
        
        EditorGUI.BeginChangeCheck();
        _planeSelected = EditorGUILayout.Popup("Bezier Path Plane", _planeSelected, _planeOptions);
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Selected Point:");
        EditorGUILayout.IntField(_pointSelected);
        EditorGUILayout.EndHorizontal();
        // Make point selected appear in Inspector
        if (_pointSelected != -1)
        {
            EditorGUI.indentLevel += 2;
            
            BezierPoint selectedPoint = _path.pathParams.CoursePoints[_pointSelected][_indexSelected];
            EditorGUILayout.BeginHorizontal();
            selectedPoint.basePoint = EditorGUILayout.Vector3Field("Base Point: ", RoundVector(selectedPoint.basePoint, 2));

            selectedPoint.basePoint = CorrectVector(selectedPoint.basePoint, _planeSelected);
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(new GUIContent("Local Handles: "));
            EditorGUILayout.BeginHorizontal();
            selectedPoint.localHandles[0] = 
                EditorGUILayout.Vector3Field("Local Handle 1: ", RoundVector(selectedPoint.localHandles[0], 2));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            selectedPoint.localHandles[1] = 
                EditorGUILayout.Vector3Field("Local Handle 2: ", RoundVector(selectedPoint.localHandles[1], 2));
            EditorGUILayout.EndHorizontal();

            selectedPoint.localHandles[0] = CorrectVector(selectedPoint.localHandles[0], _planeSelected);
            selectedPoint.localHandles[1] = CorrectVector(selectedPoint.localHandles[1], _planeSelected);
            

            EditorGUI.indentLevel -= 2;
        }
        
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Road Mesh Object: "));
        _path.roadMesh = (BezierMesh) EditorGUILayout.ObjectField(_path.roadMesh, typeof(BezierMesh), _path.roadMesh);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(_parameters[0], new GUIContent("Path Parameters"));
        EditorGUILayout.PropertyField(_parameters[1], new GUIContent("UI Parameters"));

        if (GUILayout.Button("Reset Path"))
        {
            _path.Reset();
            _path.Refresh();
        }

        /*if (GUILayout.Button("Refresh Visuals"))
        {
            _path.Refresh();
        }*/

        if (GUILayout.Button("Update Mesh"))
        {
            _path.roadMesh.UpdateMesh();
        }
        
        serializedObject.ApplyModifiedProperties();
        Repaint();
        _path.Refresh();
    }

    float RoundFloat(float x, int n)
    {
        return (float)(Mathf.Round((float)(x * System.Math.Pow(10, n))) / System.Math.Pow(10, n));
    }

    Vector3 RoundVector(Vector3 v, int n)
    {
        return new Vector3(RoundFloat(v.x, n), RoundFloat(v.y, n), RoundFloat(v.z, n));
    }

    Vector3 CorrectVector(Vector3 v, int mode)
    {
        switch (mode)
        {
            case 0:
                return new Vector3(v.x, 0, v.z);
            case 1:
                return new Vector3(v.x, v.y, 0);
            case 2:
                return new Vector3(0, v.y, v.z);
            case 3:
                return v;
            default:
                Debug.Log("Unexpected mode.");
                break;
        }

        return Vector3.zero;
    }
    
    int LoopIndex(int i, BezierPoint[][] coursePoints)
    {
        return i % coursePoints.Length;
    }

    
    /* Draws a multi-curve spline from several bezier points and a specific offset, colour, texture and width
    Also draws normals if required with a length of normalLength, thickness of normalThickness and division normals
    along each curve */
    void DrawBezierSpline(BezierPoint[][] curvePoints, Color curveColor, Texture2D texture, float width, 
        bool showNormals=false, float normalLength=1, float normalThickness=1, float offsetThickness=1
        , int division=20)
    {
        if (_path == null || curvePoints.Length == 0)
        {
            return;
        }
        if (curvePoints.Length == 1)
        {
            Vector3 diskPos;
            if (curvePoints[0][0] == null)
            {
                diskPos = Vector3.zero;
            }
            else
            {
                diskPos = curvePoints[0][0].basePoint;
            }
            Handles.color = _path.uiParams.pointColor;
            //Debug.Log(curvePoints[0][0].basePoint);
            
            Handles.DrawSolidDisc(diskPos, Vector3.up, 0.05f);
            return;
        }
        for (int i = 0; i < curvePoints.Length + (_path.pathParams.closedLoop ? 0 : -1); i++)
        {
            for (int j = 0; j < curvePoints[i].Length; j++)
            {
                for (int k = 0; k < curvePoints[LoopIndex(i+1, curvePoints)].Length; k++)
                {
                    Handles.DrawBezier(curvePoints[i][j].basePoint,
                        curvePoints[LoopIndex(i+1, curvePoints)][k].basePoint,
                        curvePoints[i][j].HandlePoints[1],
                        curvePoints[LoopIndex(i+1, curvePoints)][k].HandlePoints[0],
                        curveColor,
                        texture,
                        width
                    );
                    
                    if (showNormals)
                    {
                        Handles.color = _path.uiParams.normalColor;
                        Vector3[] normalPoints = Handles.MakeBezierPoints(
                            curvePoints[i][j].basePoint,
                            curvePoints[LoopIndex(i+1, curvePoints)][k].basePoint,
                            curvePoints[i][j].HandlePoints[1],
                            curvePoints[LoopIndex(i+1, curvePoints)][k].HandlePoints[0],
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
                Handles.DrawSolidDisc(point.basePoint, Vector3.up, _path.uiParams.pointSize);
            }
        }
        //Debug.Log("Working!");
    }

    void DrawBezierDisplay(BezierPoint[][] curvePoints)
    {
        DrawBezierSpline(curvePoints, 
                _path.uiParams.curveColor, 
                _path.uiParams.curveTexture, 
                _path.uiParams.curveWidth,
                _path.uiParams.showNormals,
                _path.uiParams.normalDistance,
                _path.uiParams.normalWidth,
                _path.uiParams.offsetWidth,
                Mathf.RoundToInt(1 / _path.pathParams.resolution));
    }
}
