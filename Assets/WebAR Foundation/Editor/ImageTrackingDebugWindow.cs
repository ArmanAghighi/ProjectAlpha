using UnityEditor;
using UnityEngine;

namespace WebARFoundation {
    public class ImageTrackingDebugWindow : EditorWindow {

        int targetIndex = 0;

        [MenuItem("Window/WebAR/Image Tracking Debugger")]
        static void OpenCustomEditorWindow() {
            ImageTrackingDebugWindow window = GetWindow<ImageTrackingDebugWindow>();
            window.Show();
        }

        void OnGUI() {
            targetIndex = EditorGUILayout.IntField("Target Index", targetIndex);

            if (GUILayout.Button("trigger target detected")) {
                MindARImagePlugin.OnARUpdate(targetIndex, 1);
            }

            if (GUILayout.Button("trigger target lost")) {
                MindARImagePlugin.OnARUpdate(targetIndex, 0);
            }
        }
}
}