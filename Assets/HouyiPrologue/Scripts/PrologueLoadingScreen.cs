using UnityEngine;

namespace Houyi.NineSuns
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class PrologueLoadingScreen : MonoBehaviour
    {
        public Font chineseFont;
        // Retained for existing callers; Play mode opens chapter selection immediately.
        public static bool IsLoading => false;

        void OnGUI()
        {
            if (Application.isPlaying) return;
            int depth = GUI.depth;
            Color color = GUI.color;
            Matrix4x4 matrix = GUI.matrix;
            GUI.depth = int.MinValue;
            GUI.matrix = Matrix4x4.identity;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.matrix = matrix;
            GUI.color = color;
            GUI.depth = depth;
        }
    }
}