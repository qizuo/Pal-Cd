// Copy this file into Assets/Editor of an isolated copy of the project.
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ChapterOneValidation
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/HouyiPrologue/Scenes/Houyi_Prologue.unity");
        EditorApplication.isPlaying = true;
    }
}
