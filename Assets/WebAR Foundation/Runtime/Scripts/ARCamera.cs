using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Linq;
namespace WebARFoundation {
    public class ARCamera : MonoBehaviour
    {
        [SerializeField]
        public GameObject webcamQuad;
        // private WebCamTexture videoTexture;
        private Texture2D videoTexture;
        private Vector2 screenSize;
        private float arFov = -1;
        private float arNear = -1;
        private float arFar = -1;
        public Texture GetWebCamTexture() {
            return videoTexture;
        }
        public void UpdateCameraConfig(int videoWidth, int videoHeight, float fov, float near, float far, bool facingUser) {
            // Debug.Log("UpdateCameraConfig: " + videoWidth + ", " + videoHeight + ", " + fov + ", " + near + ", " + far);
            videoTexture = new Texture2D(videoWidth, videoHeight, TextureFormat.ARGB32, false);
            // videoTexture.wrapMode = TextureWrapMode.Clamp;
            Material material = webcamQuad.GetComponent<MeshRenderer>().material;
            material.mainTexture = videoTexture;
            if (facingUser) { // flip horizontally
                material.mainTextureScale = new Vector2(-1, 1);
                material.mainTextureOffset = new Vector2(1, 0);
            }
            arFar = far;
            arFov = fov;
            arNear = near;
        }
        void Update() {
            if (arFov != -1 && (screenSize == null || screenSize[0] != Screen.width || screenSize[1] != Screen.height)) {
                screenSize = new Vector2(Screen.width, Screen.height);
                UpdateProjection();
            }            
        }
        private void UpdateProjection() {            
            Camera camera = GetComponent<Camera>();
            camera.farClipPlane = arFar;
            camera.nearClipPlane = arNear;
            float screenWidth = screenSize[0];
            float screenHeight = screenSize[1];
            float videoWidth = videoTexture.width;
            float videoHeight = videoTexture.height;
            float videoAspect = videoWidth  / videoHeight;
            float far = camera.farClipPlane;
            float webcamZ = far * 0.99f;
            float webcamHeight = 2 * webcamZ * Mathf.Tan(arFov * Mathf.PI/180 / 2);
            float webcamWidth = webcamHeight * videoAspect;
            if (screenWidth / screenHeight > videoAspect) {
                float screenHeightAtFar = webcamHeight;
                float screenWidthAtFar = screenHeightAtFar * screenWidth / screenHeight;
                float targetScreenWidthAtFar = webcamWidth;
                float targetScreenHeightAtFar = targetScreenWidthAtFar * screenHeight / screenWidth;
                float targetFov = Mathf.Atan(targetScreenHeightAtFar/ webcamZ) * 180 / Mathf.PI;
                camera.fieldOfView = targetFov;
            } else {
                camera.fieldOfView = arFov;
            }
            webcamQuad.transform.localPosition = new Vector3(0, 0, webcamZ);
            webcamQuad.transform.localScale = new Vector3(webcamWidth, webcamHeight, 1);
        }
    }
}
