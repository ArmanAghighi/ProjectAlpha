// Ref: https://forum.unity.com/threads/how-to-assign-matrix4x4-to-transform.121966/
using UnityEngine;

namespace WebARFoundation
    {
    public class Utils {
        public static void AssignMatrix4x4FromArray(ref Matrix4x4 p, float[] values) {
            for (int i = 0; i < 16; i++) {
                p[i] = values[i];
            }
        }

        public static Vector3 GetTranslationFromMatrix(ref Matrix4x4 matrix) {
            return new Vector3(matrix.m03, matrix.m13, matrix.m23);
        }

        public static Quaternion GetRotationFromMatrix(ref Matrix4x4 matrix) {
            Vector3 forward;
            forward.x = matrix.m02;
            forward.y = matrix.m12;
            forward.z = matrix.m22;
        
            Vector3 upwards;
            upwards.x = matrix.m01;
            upwards.y = matrix.m11;
            upwards.z = matrix.m21;
        
            return Quaternion.LookRotation(forward, upwards);
        }

        public static Vector3 GetScaleFromMatrix(ref Matrix4x4 matrix) {
            Vector3 scale;
            scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
            scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
            scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
            return scale;
        }
    }
}