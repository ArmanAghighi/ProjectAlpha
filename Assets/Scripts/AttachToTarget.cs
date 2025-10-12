using UnityEngine;
using System.Collections;

namespace WebARFoundation
{
    [DisallowMultipleComponent]
    public class AttachToTarget : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MindARImageTrackingManager mindARImageTrackingManager;
        [SerializeField] private Camera contentCamera;

        [Header("Smoothing")]
        [SerializeField] private float positionSmoothSpeed = 8f;
        [SerializeField] private Quaternion rotationOffset;

        private Vector3 smoothedPos;
        private Quaternion smoothedRot;

        private bool targetVisible = false;
        private Coroutine lostRoutine;


        private void Awake()
        {
            smoothedPos = transform.localPosition;
            smoothedRot = transform.localRotation;
        }

        private void Start()
        {
            if (mindARImageTrackingManager != null)
            {
                mindARImageTrackingManager.OnTargetLost += HandleTargetLost;
                mindARImageTrackingManager.OnTargetFound += HandleTargetFound;
            }
        }

        private void OnDestroy()
        {
            if (mindARImageTrackingManager != null)
            {
                mindARImageTrackingManager.OnTargetLost -= HandleTargetLost;
                mindARImageTrackingManager.OnTargetFound -= HandleTargetFound;
            }
        }

        private void HandleTargetFound(int index)
        {
            targetVisible = true;

            if (lostRoutine != null)
            {
                StopCoroutine(lostRoutine);
                lostRoutine = null;
            }
            if (mindARImageTrackingManager != null)
            {
                var tracker = mindARImageTrackingManager.GetImageTrackerByIndex(index);
                if (tracker != null)
                {
                    smoothedPos = tracker.transform.position;
                    transform.localPosition = Vector3.zero;
                }
            }
        }

        private void HandleTargetLost()
        {
            smoothedPos = transform.localPosition;
            smoothedRot = transform.localRotation;
            targetVisible = false;

            if (lostRoutine != null)
                StopCoroutine(lostRoutine);

            lostRoutine = StartCoroutine(MoveToCenterRoutine());
        }

        private IEnumerator MoveToCenterRoutine()
        {
            while (!targetVisible)
            {
                if (contentCamera != null)
                {
                    Vector3 centerPos = contentCamera.transform.position + contentCamera.transform.forward * 2200;

                    smoothedPos = Vector3.Lerp(smoothedPos, centerPos, 1f - Mathf.Exp(-positionSmoothSpeed * Time.deltaTime));
                    
                    ApplyBillboard();
                    UpdatePose(smoothedPos, smoothedRot);
                }

                yield return null;
            }
        }

        public void ApplyBillboard()
        {
            if (contentCamera == null) return;

            Vector3 dir = contentCamera.transform.position - smoothedPos;
            if (dir.sqrMagnitude < 1e-6f)
                dir = contentCamera.transform.forward;

            smoothedRot = Quaternion.LookRotation(dir, Vector3.up) * rotationOffset;
        }

        public void UpdatePose(Vector3 translation, Quaternion rotation)
        {
            transform.position = translation;
            transform.rotation = rotation;
        }
    }
}
