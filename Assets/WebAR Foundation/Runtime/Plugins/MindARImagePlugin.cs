using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace WebARFoundation 
{
    public static class MindARImagePlugin
    {
        public delegate void OnARUpdateEvent(int targetIndex, int isFound);
        public static event Action onARReadyAction;
        public static event Action onCameraConfigChangeAction;
        public static event OnARUpdateEvent onARUpdateAction;
        // public static WebCamTexture videoTexture;
        private static class Native
        {            
            [DllImport("__Internal")]
            public static extern void MindARImage_SetCallbacks(Action onARReady, Action onCameraConfigChange, OnARUpdateEvent onARUpdate);
            [DllImport("__Internal")]
            public static extern void MindARImage_StartAR();
            [DllImport("__Internal")]
            public static extern void MindARImage_StopAR();
            [DllImport("__Internal")]
            public static extern bool MindARImage_IsRunning();
            [DllImport("__Internal")]
            public static extern int MindARImage_GetVideoWidth();
            [DllImport("__Internal")]
            public static extern int MindARImage_GetVideoHeight();
            [DllImport("__Internal")]
            public static extern int MindARImage_GetNumTargets();
            [DllImport("__Internal")]
            public static extern int MindARImage_GetTargetWidth(int targetIndex);
            [DllImport("__Internal")]
            public static extern int MindARImage_GetTargetHeight(int targetIndex);
            [DllImport("__Internal")]
            public static extern IntPtr MindARImage_GetCameraParamsPtr();        
            [DllImport("__Internal")]
            public static extern IntPtr MindARImage_GetTargetWorldMatrixPtr(int targetIndex);
            [DllImport("__Internal")]
            public static extern void MindARImage_TextureUpdate(int texture);
            [DllImport("__Internal")]
            public static extern void MindARImage_SetIsFacingUser(bool value);
            [DllImport("__Internal")]
            public static extern void MindARImage_SetMindFilePath(string path);
            [DllImport("__Internal")]
            public static extern void MindARImage_SetMaxTrack(float value);
            [DllImport("__Internal")]
            public static extern void MindARImage_SetFilterMinCF(float value);
            [DllImport("__Internal")]
            public static extern void MindARImage_SetFilterBeta(float value);
        }
        static MindARImagePlugin() {
            #if UNITY_EDITOR
            #else
                Native.MindARImage_SetCallbacks(OnARReady, OnCameraConfigChange, OnARUpdate);
            #endif
        }
        [MonoPInvokeCallback(typeof(Action))]
        public static void OnARReady()
        {
            onARReadyAction.Invoke();
        }
        [MonoPInvokeCallback(typeof(Action))]
        public static void OnCameraConfigChange()
        {
            onCameraConfigChangeAction.Invoke();            
        }
        [MonoPInvokeCallback(typeof(OnARUpdateEvent))]
        public static void OnARUpdate(int targetIndex, int isFound)
        {
            onARUpdateAction.Invoke(targetIndex, isFound);
        }
        public static void BindVideoTexture(Texture texture) {
        // public static void BindVideoTexture(Texture2D texture) {
            #if UNITY_EDITOR         
                Color[] colorList = new Color[10];
                for (int i = 0; i < colorList.Length; i++) {
                    colorList[i] = new Color((UnityEngine.Random.Range(0f, 1f)), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), 1f);
                }
                for (int x = 0; x < texture.width; x++) {
                    for (int y = 0; y < texture.height; y++) {
                        int index = (int) UnityEngine.Random.Range(0, colorList.Length-1);
                        ((Texture2D) texture).SetPixel(x, y, colorList[index]);
                    }
                }
                ((Texture2D) texture).Apply();
            #else
                Native.MindARImage_TextureUpdate((int) texture.GetNativeTexturePtr());
            #endif
        }
        public static void SetMindFilePath(string path) {
            #if UNITY_EDITOR
            #else
                Native.MindARImage_SetMindFilePath(path);
            #endif
        }
        public static void SetMaxTrack(int value) {
            #if UNITY_EDITOR
            #else
                Native.MindARImage_SetMaxTrack(value);
            #endif
        }
        public static void SetFilterMinCF(float value) {
            #if UNITY_EDITOR
            #else
                Native.MindARImage_SetFilterMinCF(value);
            #endif
        }
        public static void SetFilterBeta(float value) {
            #if UNITY_EDITOR
            #else
                Native.MindARImage_SetFilterBeta(value);
            #endif
        }
        public static void SetIsFacingUser(bool value) {            
            #if UNITY_EDITOR
            #else
                Native.MindARImage_SetIsFacingUser(value);
            #endif
        }
        public static void StartAR()
        {
            #if UNITY_EDITOR
                MindARImagePlugin.OnARReady();
                MindARImagePlugin.OnCameraConfigChange();
                // MindARImagePlugin.OnARUpdate(0);
            #else
                Native.MindARImage_StartAR();
            #endif
        }
        public static void StopAR()
        {
            #if UNITY_EDITOR                
            #else
                Native.MindARImage_StopAR();
            #endif
        }
        public static bool IsRunning()
        {
            #if UNITY_EDITOR
                return true;
            #else
                return Native.MindARImage_IsRunning();
            #endif
        }
        public static int GetVideoWidth() {
            #if UNITY_EDITOR
                return 640;
            #else 
                return Native.MindARImage_GetVideoWidth();
            #endif
        }
        public static int GetVideoHeight() {
            #if UNITY_EDITOR
                return 480;
            #else 
                return Native.MindARImage_GetVideoHeight();
            #endif
        }
        public static int GetNumTargets() {
            #if UNITY_EDITOR
                List<ImageTracker> imageTrackers = SceneManager.GetActiveScene()
                 .GetRootGameObjects()
                 .SelectMany(gameObject => gameObject
                     .GetComponentsInChildren<ImageTracker>())
                 .ToList();
                int maxTargetIndex = 0;
                foreach(ImageTracker imageTracker in imageTrackers) {
                    maxTargetIndex = Math.Max(maxTargetIndex, imageTracker.targetIndex);
                }
                return maxTargetIndex + 1;
            #else 
                return Native.MindARImage_GetNumTargets();
            #endif
        }
        public static int GetTargetWidth(int targetIndex) {
            #if UNITY_EDITOR
                return 500;
            #else 
                return Native.MindARImage_GetTargetWidth(targetIndex);
            #endif
        }
        public static int GetTargetHeight(int targetIndex) {
            #if UNITY_EDITOR
                return 500;
            #else 
                return Native.MindARImage_GetTargetHeight(targetIndex);
            #endif
        }
        public static float[] GetCameraParams() {
            #if UNITY_EDITOR
                return new float[4] {41.11209f, 1.333333f, 1, 10000};                
            #else
                IntPtr ptr = Native.MindARImage_GetCameraParamsPtr();
                float[] arr = new float[4];
                Marshal.Copy(ptr, arr, 0, 4);
                return arr;
            #endif
        }
        public static float[] GetTargetWorldMatrix(int targetIndex) {
            #if UNITY_EDITOR
                return new float[16] {
                    1, 0, 0, 0f, 
                    0, 1, 0, 0f, 
                    0, 0, 1, 0f, 
                    -250, -250, -2000, 1f
                };
            #else
                IntPtr ptr = Native.MindARImage_GetTargetWorldMatrixPtr(targetIndex);
                float[] arr = new float[16];
                Marshal.Copy(ptr, arr, 0, 16);
                return arr;
            #endif
        }
    }
}

//============================above code is from sdk and the code below is better at performance
//using System;
//using System.Runtime.InteropServices;
//using AOT;
//
//namespace WebARFoundation 
//{
//    public static class MindARImagePlugin
//    {
//        public delegate void OnARUpdateEvent(int targetIndex, int isFound);
//
//        public static event Action onARReadyAction;
//        public static event Action onCameraConfigChangeAction;
//        public static event OnARUpdateEvent onARUpdateAction;
//
//        private static class Native
//        {            
//            [DllImport("__Internal")]
//            public static extern void MindARImage_SetCallbacks(Action onARReady, Action onCameraConfigChange, OnARUpdateEvent onARUpdate);
//
//            [DllImport("__Internal")]
//            public static extern void MindARImage_StartAR();
//
//            [DllImport("__Internal")]
//            public static extern void MindARImage_StopAR();
//
//            [DllImport("__Internal")]
//            public static extern bool MindARImage_IsRunning();
//
//            [DllImport("__Internal")]
//            public static extern int MindARImage_GetVideoWidth();
//
//            [DllImport("__Internal")]
//            public static extern int MindARImage_GetVideoHeight();
//
//            [DllImport("__Internal")]
//            public static extern int MindARImage_GetNumTargets();
//
//            [DllImport("__Internal")]
//            public static extern int MindARImage_GetTargetWidth(int targetIndex);
//
//            [DllImport("__Internal")]
//            public static extern int MindARImage_GetTargetHeight(int targetIndex);
//
//            [DllImport("__Internal")]
//            public static extern IntPtr MindARImage_GetCameraParamsPtr();        
//
//            [DllImport("__Internal")]
//            public static extern IntPtr MindARImage_GetTargetWorldMatrixPtr(int targetIndex);
//
//            [DllImport("__Internal")]
//            public static extern void MindARImage_TextureUpdate(IntPtr texture);
//
//            [DllImport("__Internal")]
//            public static extern void MindARImage_SetIsFacingUser(bool value);
//
//            [DllImport("__Internal")]
//            public static extern void MindARImage_SetMindFilePath(string path);
//
//            [DllImport("__Internal")]
//            public static extern void MindARImage_SetMaxTrack(int value);
//
//            [DllImport("__Internal")]
//            public static extern void MindARImage_SetFilterMinCF(float value);
//
//            [DllImport("__Internal")]
//            public static extern void MindARImage_SetFilterBeta(float value);
//        }
//
//        static MindARImagePlugin() {
//            Native.MindARImage_SetCallbacks(OnARReady, OnCameraConfigChange, OnARUpdate);
//        }
//
//        [MonoPInvokeCallback(typeof(Action))]
//        public static void OnARReady()
//        {
//            onARReadyAction?.Invoke();
//        }
//
//        [MonoPInvokeCallback(typeof(Action))]
//        public static void OnCameraConfigChange()
//        {
//            onCameraConfigChangeAction?.Invoke();            
//        }
//
//        [MonoPInvokeCallback(typeof(OnARUpdateEvent))]
//        public static void OnARUpdate(int targetIndex, int isFound)
//        {
//            onARUpdateAction?.Invoke(targetIndex, isFound);
//        }
//
//        public static void BindVideoTexture(IntPtr texturePtr)
//        {
//            Native.MindARImage_TextureUpdate(texturePtr);
//        }
//
//        public static void SetMindFilePath(string path) => Native.MindARImage_SetMindFilePath(path);
//        public static void SetMaxTrack(int value) => Native.MindARImage_SetMaxTrack(value);
//        public static void SetFilterMinCF(float value) => Native.MindARImage_SetFilterMinCF(value);
//        public static void SetFilterBeta(float value) => Native.MindARImage_SetFilterBeta(value);
//        public static void SetIsFacingUser(bool value) => Native.MindARImage_SetIsFacingUser(value);
//
//        public static void StartAR() => Native.MindARImage_StartAR();
//        public static void StopAR() => Native.MindARImage_StopAR();
//        public static bool IsRunning() => Native.MindARImage_IsRunning();
//
//        public static int GetVideoWidth() => Native.MindARImage_GetVideoWidth();
//        public static int GetVideoHeight() => Native.MindARImage_GetVideoHeight();
//        public static int GetNumTargets() => Native.MindARImage_GetNumTargets();
//        public static int GetTargetWidth(int targetIndex) => Native.MindARImage_GetTargetWidth(targetIndex);
//        public static int GetTargetHeight(int targetIndex) => Native.MindARImage_GetTargetHeight(targetIndex);
//
//        public static float[] GetCameraParams() {
//            IntPtr ptr = Native.MindARImage_GetCameraParamsPtr();
//            float[] arr = new float[4];
//            Marshal.Copy(ptr, arr, 0, 4);
//            return arr;
//        }
//
//        public static float[] GetTargetWorldMatrix(int targetIndex) {
//            IntPtr ptr = Native.MindARImage_GetTargetWorldMatrixPtr(targetIndex);
//            float[] arr = new float[16];
//            Marshal.Copy(ptr, arr, 0, 16);
//            return arr;
//        }
//    }
//}
//