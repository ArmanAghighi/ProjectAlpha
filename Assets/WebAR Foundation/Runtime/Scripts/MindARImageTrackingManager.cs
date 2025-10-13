using TMPro;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace WebARFoundation
{
    public class MindARImageTrackingManager : MonoBehaviour
    {
        public delegate void OnTargetEvent(int targetIndex);
        public Action<int> OnTargetFound;
        public Action OnTargetLost;
        public string mindFileURL;
        public bool autoStart = true;
        public bool facingUser = false;
        public int maxTrack = 1;
        public TextMeshProUGUI debug;
        public Action OnARActivated;
        public TMP_Dropdown reset;
        
        [Range(1, 6)] public int stability = 4;

        [SerializeField] private TextMeshProUGUI pos;
        [SerializeField] private TextMeshProUGUI rot;
        [SerializeField] private TextMeshProUGUI sca;
        [SerializeField] private float positionSmoothSpeed = 12f;
        [SerializeField] private float rotationSmoothSpeed = 12f;
        [SerializeField] private float scaleSmoothSpeed = 8f;
        [SerializeField] private Camera contentCamera;
        [SerializeField] private bool billboardToCamera = false;
        [SerializeField] private Quaternion rotationOffset;
        [SerializeField] private float stableDelay = 0.02f;

        public void SetLogs(TextMeshProUGUI placeholder, string text) => placeholder.text = text;

        private ARCamera arCamera;
        private Vector3 smoothedPos = Vector3.zero;
        private Quaternion smoothedRot = Quaternion.identity;
        private Vector3 smoothedScale = Vector3.one;
        private Vector3 lastRawPos = Vector3.zero;
        private float[,] markerDimensions;
        private float stableTimer = 0f;
        private List<ImageTracker> imageTrackers;
        private bool alreadyFound = false;
        private Dictionary<ImageTracker, Transform> trackerTransforms = new();

        void Awake()
        {
            arCamera = GetComponentInChildren<ARCamera>();
            if (arCamera == null) return;
            
            imageTrackers = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<ImageTracker>()).ToList();

            int maxTargetIndex = 0;
            foreach (var tracker in imageTrackers)
            {
                maxTargetIndex = Math.Max(maxTargetIndex, tracker.targetIndex);
                //if(tracker.gameObject.activeInHierarchy) tracker.gameObject.SetActive(false);
            }

            MindARImagePlugin.onARReadyAction += OnARReady;
            MindARImagePlugin.onARUpdateAction += OnARUpdate;
            MindARImagePlugin.onCameraConfigChangeAction += OnCameraConfigChange;
        }

        void OnDestroy() => StopAR();

        void Start()
        {
            if (autoStart) StartAR();
        }

        public void StopAR()
        {
            if (MindARImagePlugin.IsRunning())
                MindARImagePlugin.StopAR();
        }

        public void StartAR()
        {
            MindARImagePlugin.SetIsFacingUser(facingUser);
            MindARImagePlugin.SetMindFilePath(mindFileURL);
            MindARImagePlugin.SetMaxTrack(maxTrack);
            MindARImagePlugin.SetFilterMinCF(0.001f);
            float filterBeta = 1000 / Mathf.Pow(10, stability);
            MindARImagePlugin.SetFilterBeta(filterBeta);
            MindARImagePlugin.StartAR();
        }

        private void OnARReady()
        {
            int numTargets = MindARImagePlugin.GetNumTargets();
            markerDimensions = new float[numTargets, 2];
            for (int i = 0; i < numTargets; i++)
            {
                markerDimensions[i, 0] = MindARImagePlugin.GetTargetWidth(i);
                markerDimensions[i, 1] = MindARImagePlugin.GetTargetHeight(i);
            }
            OnARActivated?.Invoke();
        }

        private void OnCameraConfigChange()
        {
            int videoWidth = MindARImagePlugin.GetVideoWidth();
            int videoHeight = MindARImagePlugin.GetVideoHeight();
            float[] camParams = MindARImagePlugin.GetCameraParams();
            arCamera.UpdateCameraConfig(videoWidth, videoHeight, camParams[0], camParams[2], camParams[3], false);
            MindARImagePlugin.BindVideoTexture(arCamera.GetWebCamTexture());//.GetNativeTexturePtr()
        }

        private void OnARUpdate(int targetIndex, int isFound)
        {
            float[] worldMatrix = MindARImagePlugin.GetTargetWorldMatrix(targetIndex);
            var tracker = imageTrackers.Find(t => t.targetIndex == targetIndex);
            if (tracker != null)
            {
                if (isFound == 1)
                {
                    OnTargetFound?.Invoke(targetIndex);

                    if (!alreadyFound) alreadyFound = true;
                    if (reset.interactable) reset.interactable = false;
                    if (!tracker.gameObject.activeInHierarchy) tracker.gameObject.SetActive(true);

                    UpdateTargetPose(tracker, targetIndex, worldMatrix, applyOffset: true, applyBillboard: billboardToCamera);
                }
                else
                {
                    if (alreadyFound)
                    {
                        if (!reset.interactable) reset.interactable = true;
                        OnTargetLost?.Invoke();
                    }
                    reset.onValueChanged.AddListener(OnDropdownChanged);
                    void OnDropdownChanged(int index)
                    {
                        if (index == 2)
                        {
                            if (alreadyFound) alreadyFound = false;
                            if (tracker.gameObject.activeInHierarchy) tracker.gameObject.SetActive(false);
                        }
                    }
                }
                SetLogs(debug, $"isFound : {isFound} , alreadyFound : {alreadyFound}");
            }
        }

        private void UpdateTargetPose(ImageTracker tracker, int targetIndex, float[] matrixArray, bool applyOffset = true, bool applyBillboard = true)
        {
            if (!trackerTransforms.TryGetValue(tracker, out Transform t))
            {
                t = tracker.transform;
                trackerTransforms[tracker] = t;
            }

            float markerWidth = markerDimensions[targetIndex, 0];
            float markerHeight = markerDimensions[targetIndex, 1];

            Matrix4x4 m = new Matrix4x4();
            Utils.AssignMatrix4x4FromArray(ref m, matrixArray);

            m.m03 += (m.m00 * markerWidth / 2 + m.m01 * markerHeight / 2);
            m.m13 += (m.m10 * markerWidth / 2 + m.m11 * markerHeight / 2);
            m.m23 = -(m.m20 * markerWidth / 2 + m.m21 * markerHeight / 2 + m.m23);
            m.m20 = -m.m20; m.m21 = -m.m21; m.m22 = -m.m22;

            Vector3 rawPos = Utils.GetTranslationFromMatrix(ref m);
            Quaternion rawRot = Utils.GetRotationFromMatrix(ref m);
            Vector3 rawScale = Utils.GetScaleFromMatrix(ref m) * (markerWidth / 1.35f);

            if (applyOffset)
                rawRot *= rotationOffset;

            float distance = (contentCamera != null) ? Vector3.Distance(contentCamera.transform.position, rawPos) : 1f;
            float dynamicDeadzone = Mathf.Max(0.01f, distance * 0.03f);

            bool significantChange = (lastRawPos - rawPos).sqrMagnitude > dynamicDeadzone * dynamicDeadzone;

            if (significantChange)
                stableTimer = 0f;
            else
                stableTimer += Time.deltaTime;

            lastRawPos = rawPos;

            if (stableTimer >= stableDelay)
            {
                smoothedPos = Vector3.Lerp(smoothedPos, rawPos, 1f - Mathf.Exp(-positionSmoothSpeed * Time.deltaTime));
                smoothedRot = Quaternion.Slerp(smoothedRot, rawRot, 1f - Mathf.Exp(-rotationSmoothSpeed * Time.deltaTime));
                smoothedScale = Vector3.Lerp(smoothedScale, rawScale, 1f - Mathf.Exp(-scaleSmoothSpeed * Time.deltaTime));

                if (applyBillboard && contentCamera != null)
                {
                    Vector3 dir = contentCamera.transform.position - smoothedPos;
                    if (dir.sqrMagnitude < 1e-6f) dir = contentCamera.transform.forward;
                    smoothedRot = Quaternion.LookRotation(dir, Vector3.up) * rotationOffset;
                }

                bool hasChanged = (t.localPosition - smoothedPos).sqrMagnitude > 1e-6f ||
                                  Quaternion.Angle(t.localRotation, smoothedRot) > 0.01f ||
                                  (t.localScale - smoothedScale).sqrMagnitude > 1e-6f;

                if (hasChanged)
                    tracker.UpdatePose(smoothedPos, smoothedRot, smoothedScale);

                if (pos != null) pos.SetText("Position: {0:F2}, {1:F2}, {2:F2}", t.localPosition.x, t.localPosition.y, t.localPosition.z);
                if (rot != null) rot.SetText("Rotation: {0:F2}, {1:F2}, {2:F2}", t.localEulerAngles.x, t.localEulerAngles.y, t.localEulerAngles.z);
                if (sca != null) sca.SetText("Scale: {0:F2}, {1:F2}, {2:F2}", t.localScale.x, t.localScale.y, t.localScale.z);
            }
        }

        public ImageTracker GetImageTrackerByIndex(int index)
        {
            if (imageTrackers == null) return null;
            return imageTrackers.Find(t => t.targetIndex == index);
        }
    }
}
