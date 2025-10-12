// ref: https://github.com/yukpiz/unity-webgl-movie/blob/master/Assets/Plugins/WebGLMovieTexture.jslib
// ref: https://www.patrykgalach.com/2020/04/27/unity-js-plugin/
let plugin = {
  MindARImage_GetVideoWidth: function () {
    return Module.MindARImage.GetVideoWidth();
  },

  MindARImage_GetVideoHeight: function () {
    return Module.MindARImage.GetVideoHeight();
  },

  MindARImage_GetNumTargets: function() {
    return Module.MindARImage.GetNumTargets();
  },

  MindARImage_GetTargetWidth: function(targetIndex) {
    return Module.MindARImage.GetNumTargetWidth(targetIndex);
  },

  MindARImage_GetTargetHeight: function(targetIndex) {
    return Module.MindARImage.GetNumTargetHeight(targetIndex);
  },

  MindARImage_TextureUpdate: function (tex) {
    Module.MindARImage.TextureUpdate(tex);
  },

  MindARImage_SetCallbacks: function (onStartARPtr, onCameraConfigChangePtr, onUpdatePtr) {
    Module.MindARImage.onStartARPtr = onStartARPtr;
    Module.MindARImage.onCameraConfigChangePtr = onCameraConfigChangePtr;
    Module.MindARImage.onUpdatePtr = onUpdatePtr;
  },

  MindARImage_StartAR: function () {    
    Module.MindARImage.StartAR();
  },

  MindARImage_StopAR: function () {    
    Module.MindARImage.StopAR();
  },

  MindARImage_SetIsFacingUser: function(value) {
    Module.MindARImage.SetIsFacingUser(value);
  },
  
  MindARImage_IsRunning: function () {    
    return Module.MindARImage.IsRunning();
  },  

  MindARImage_SetMindFilePath: function(mindFilePath) {
    Module.MindARImage.SetMindFilePath(UTF8ToString(mindFilePath));
  },

  MindARImage_SetMaxTrack: function(value) {
    Module.MindARImage.SetMaxTrack(value);
  },

  MindARImage_SetFilterMinCF: function(value) {
    Module.MindARImage.SetFilterMinCF(value);
  },

  MindARImage_SetFilterBeta: function(value) {
    Module.MindARImage.SetFilterBeta(value);
  },

  MindARImage_GetCameraParamsPtr: function() {
    return Module.MindARImage.GetCameraParamsPtr();
  },

  MindARImage_GetTargetWorldMatrixPtr: function(targetIndex) {
    return Module.MindARImage.GetTargetWorldMatrixPtr(targetIndex);
  }
}

// Add plugin functions
mergeInto(LibraryManager.library, plugin);
