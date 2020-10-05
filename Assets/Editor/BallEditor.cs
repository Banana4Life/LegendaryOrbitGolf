using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Ball))]
class BallEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Place in Orbit"))
        {
            ((Ball) target).PlaceInOrbit();
        }
    }
}
