using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(World))]
class WorldEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("New Universe"))
        {
            ((World) target).NewUniverse();
        }

        if (GUILayout.Button("Load Planet Prefabs"))
        {
            ((World) target).LoadPlanetPrefabs();
        }
    }
}