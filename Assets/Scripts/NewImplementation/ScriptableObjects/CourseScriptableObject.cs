using UnityEngine;

namespace NewImplementation
{
    [CreateAssetMenu(fileName = "CourseData", menuName = "Tools/CourseSO")]
    public class CourseScriptableObject : ScriptableObject
    {
        public BezierPoint[][] courseSave;
    }
}