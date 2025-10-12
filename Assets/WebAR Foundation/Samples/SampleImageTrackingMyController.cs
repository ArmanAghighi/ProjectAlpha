using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WebARFoundation {
    public class SampleImageTrackingMyController : MonoBehaviour
    {
        [SerializeField] MindARImageTrackingManager manager;

        // Start is called before the first frame update
        void Start()
        {
            manager.mindFileURL = "https://armanitproject.ir/Test/targets.mind";
            manager.maxTrack = 1;
            manager.stability = 2;
        }

        public void StartAR()
        {
            manager.StartAR();
        }
        public void StopAR()
        {
            manager.StopAR();
        }
    }
}