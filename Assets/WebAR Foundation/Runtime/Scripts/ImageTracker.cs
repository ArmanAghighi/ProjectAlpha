using UnityEngine;

namespace WebARFoundation
{
    public class ImageTracker : MonoBehaviour
    {
        [SerializeField] public int targetIndex;

        public void UpdatePose(Vector3 translation, Quaternion rotation, Vector3 scale) {
            transform.localPosition = translation;
            transform.localRotation = rotation;
            transform.localScale = scale;
        }
    }
}
