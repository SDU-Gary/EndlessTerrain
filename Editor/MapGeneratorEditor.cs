using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof (MapGenerator))]
public class MapGeneratorEditor : Editor
{
    
    MapGenerator mapGen;
    public override void OnInspectorGUI()
    {
         mapGen = (MapGenerator)target;

        DrawDefaultInspector();

        if (GUILayout.Button("Generate"))
        {
            mapGen.DrawMapInEditor();
        }
        
        if (mapGen.autoUpdate)
        {
            mapGen.DrawMapInEditor();
        }



    }

    
}
