using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlanetMeshGenerator))]
class PlanetMeshGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Save as Prefab"))
        {
            ((PlanetMeshGenerator) target).SaveAsPrefab();
        }
        if (GUILayout.Button("Generate Planet"))
        {
            ((PlanetMeshGenerator) target).GeneratePlanet();
        }
        if (GUILayout.Button("Cleanup"))
        {
            ((PlanetMeshGenerator) target).CleanUp();
        }
    }
}