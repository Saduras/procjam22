#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TrackGenerator))]
public class TrackGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if(GUILayout.Button("Generate"))
            (target as TrackGenerator).Generate();
        if(GUILayout.Button("Clear"))
            (target as TrackGenerator).Clear();
    }
}
#endif