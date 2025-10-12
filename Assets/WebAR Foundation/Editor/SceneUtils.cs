using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace WebARFoundation {

    internal static class SceneUtils {
    
        [MenuItem("GameObject/WebAR/Image Tracker", false, 10)]
        static void CreateImageTracker(MenuCommand menuCommand) {
            ObjectFactory.CreateGameObject("ImageTracker", typeof(ImageTracker));            
        }

        [MenuItem("GameObject/WebAR/AR Session (Image Tracking)", false, 10)]
        static void CreateARImageSession(MenuCommand menuCommand)
        {
            var session = ObjectFactory.CreateGameObject("Image AR Session", typeof(MindARImageTrackingManager));

            var arCamera = CreateARMainCamera();
            Place(arCamera.gameObject, session.transform);

            var webcamBackground = CreateWebcamBackground();            
            Place(webcamBackground.gameObject, arCamera.transform);
            webcamBackground.transform.localPosition = new Vector3(0, 0, 1000);

            arCamera.gameObject.GetComponent<ARCamera>().webcamQuad = webcamBackground;

            // Selection.activeGameObject = session.gameObject;
        }

        static Camera CreateARMainCamera()
        {
            var cameraGo = ObjectFactory.CreateGameObject("AR Camera", typeof(Camera), typeof(ARCamera));             

            var mainCam = Camera.main;
            if (Camera.main != null)
            {
                Debug.LogWarningFormat(
                    mainCam.gameObject,
                    "AR Camera requires the \"MainCamera\" Tag, but the current scene contains another Camera tagged \"MainCamera\". For AR to function properly, remove the \"MainCamera\" Tag from \'{0}\'.",
                    mainCam.name);
            }
            
            cameraGo.tag = "MainCamera";

            var camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Color;
            camera.backgroundColor = Color.black;
            
            return camera;
        }

        static GameObject CreateWebcamBackground() 
        {
            var gameObject = ObjectFactory.CreateGameObject("Webcam Background");

            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = new Material(Shader.Find("Unlit/Texture"));
            meshRenderer.sharedMaterial.renderQueue = 1998;

            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();

            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(-0.5f, -0.5f, 0),
                new Vector3( 0.5f, -0.5f, 0),
                new Vector3(-0.5f,  0.5f, 0),
                new Vector3( 0.5f,  0.5f, 0)
            };
            mesh.vertices = vertices;

            int[] tris = new int[6]
            {
                // lower left triangle
                0, 2, 1,
                // upper right triangle
                2, 3, 1
            };
            mesh.triangles = tris;

            Vector3[] normals = new Vector3[4]
            {
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward
            };
            mesh.normals = normals;

            Vector2[] uv = new Vector2[4]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };
            mesh.uv = uv;

            meshFilter.mesh = mesh;

            return gameObject;
        }

        static void Place(GameObject go, Transform parent)
        {
            var transform = go.transform;

            if (parent != null)
            {
                go.transform.parent = parent;
                ResetTransform(transform);
                go.layer = parent.gameObject.layer;
            }
            else
            {
                // Puts it at the scene pivot, and otherwise world origin if there is no Scene view
                var view = SceneView.lastActiveSceneView;
                if (view != null)
                    view.MoveToView(transform);
                else
                    transform.position = Vector3.zero;

                StageUtility.PlaceGameObjectInCurrentStage(go);
            }

            GameObjectUtility.EnsureUniqueNameForSibling(go);
        }

        static void ResetTransform(Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            if (transform.parent is RectTransform)
            {
                var rectTransform = transform as RectTransform;
                if (rectTransform != null)
                {
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.anchoredPosition = Vector2.zero;
                    rectTransform.sizeDelta = Vector2.zero;
                }
            }
        }
    }
}