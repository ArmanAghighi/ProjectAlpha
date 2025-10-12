using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;

/*

namespace WebARFoundation
{
    [CustomEditor(typeof(MindARImageTrackingManager))]
    public class MindARImageTrackingManagerEditor : Editor
    {
        SerializedProperty mindFilePath;
        SerializedProperty mindStreamingAsset;
    
        const string kAssetPrefix = "Assets/StreamingAssets";
    
        void OnEnable()
        {
            mindFilePath = serializedObject.FindProperty("mindFilePath");
            mindStreamingAsset = serializedObject.FindProperty("mindStreamingAsset");
        }

        public override VisualElement CreateInspectorGUI()
        {
            // Create a new VisualElement to be the root of our inspector UI
            VisualElement myInspector = new VisualElement();

            // Add a simple label
            Label title = new Label("Place a .mind file under Assets/StreamingAssets and enter the name here: e.g. targets.mind");
            title.style.whiteSpace = WhiteSpace.Normal;
            title.style.marginTop = 10;
            title.style.marginBottom = 10;

            Label warningLabel = new Label("");
            warningLabel.style.color = new Color(1, 0, 0);
            warningLabel.visible = false;

            // PropertyField mindAssetField = new PropertyField(mindStreamingAsset, ".mind file");        
            // mindAssetField.RegisterCallback<ChangeEvent<Object>>((ChangeEvent<Object> e) => {
            //     string path = AssetDatabase.GetAssetPath(e.newValue);            
            //     string extension = Path.GetExtension(path);

            //     Debug.Log("field path: " + path);
            //     Debug.Log("extension: " + extension);
            //     Debug.Log("mindFilePath: " + mindFilePath + "..." + (mindFilePath ==null));
            //     if (mindFilePath == null) return; // null when build
            //     if (string.IsNullOrEmpty(path)) return;

            //     if (!path.StartsWith(kAssetPrefix)) {
            //         EditorUtility.DisplayDialog("", "Please place the .mind under StreamingAssets", "OK");
            //         mindStreamingAsset.objectReferenceValue = null;
            //         mindFilePath.stringValue = "";                
            //     } else if (!extension.Equals(".mind")) {
            //         EditorUtility.DisplayDialog("", "Invalid file type. Please choose a .mind file", "OK");
            //         mindStreamingAsset.objectReferenceValue = null;
            //         mindFilePath.stringValue = "";
            //     } else {
            //         mindFilePath.stringValue = path.Substring(kAssetPrefix.Length);
            //     }
            //     serializedObject.ApplyModifiedProperties();
            // });

            PropertyField mindPathField = new PropertyField(mindFilePath, ".mind file name");        
            mindPathField.RegisterCallback<FocusOutEvent>((FocusOutEvent e) => {                        
                Debug.Log("mindFilePath: " + mindFilePath.stringValue);
                string guid = AssetDatabase.AssetPathToGUID(kAssetPrefix + "/" + mindFilePath.stringValue);
                Debug.Log("guid: " + guid);
                string extension = Path.GetExtension(mindFilePath.stringValue);
                if (string.IsNullOrEmpty(guid)) {
                    EditorUtility.DisplayDialog("", "Couldn't locate your file.", "OK");
                } else if (!extension.Equals(".mind")) {
                    EditorUtility.DisplayDialog("", "A .mind file is required", "OK");
                }
            });

            myInspector.Add(title);
            // myInspector.Add(mindAssetField);
            myInspector.Add(mindPathField);
            return myInspector;
        }
    
        // public void _OnInspectorGUI()
        // {
        //     GUI.Box(new Rect(10,10,100,90), "Loader Menu");
        //     if (GUI.Button(new Rect(20,40,80,20), "Level 1")) {
        //         Debug.Log("load level 1...");
        //     }

        //     Debug.Log("OnInspectorGUI");
        //     serializedObject.Update();
        //     var ele = EditorGUILayout.PropertyField(streamingAsset);


        //     EditorGUILayout.PropertyField(filePath);
    
        //     if (streamingAsset.objectReferenceValue == null) {
        //         return;
        //     }
    
        //     string assetPath = AssetDatabase.GetAssetPath(streamingAsset.objectReferenceValue.GetInstanceID());
        //     if (assetPath.StartsWith(kAssetPrefix)) {
        //         assetPath = assetPath.Substring(kAssetPrefix.Length);
        //     }
        //     filePath.stringValue = assetPath;

        //     EditorGUILayout.LabelField(assetPath);

        //     serializedObject.ApplyModifiedProperties();
        // }
    }
}

*/