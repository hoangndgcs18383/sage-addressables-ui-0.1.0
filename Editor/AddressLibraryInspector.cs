using System;
using UnityEditor;
using UnityEngine;

namespace SAGE.AddressablesUI.Editor
{
    [CustomEditor(typeof(AddressLibTool))]
    internal sealed class AddressLibraryInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (!GUILayout.Button("Build Address Library")) return;
            try
            {
                var files = ((AddressLibTool)target).Build();
                Debug.Log("Generated " + files.Length + " address libraries.", target);
            }
            catch (Exception error) { Debug.LogException(error); }
        }
    }
}
