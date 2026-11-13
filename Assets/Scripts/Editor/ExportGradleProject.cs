using UnityEditor;

public class ExportGradleProject
{
    // 此方法将在Jenkins构建时被调用
    public static void PerformExport()
    {
        // 获取构建设置中所有激活的场景
        string[] scenes = GetEnabledScenes();
        string outputPath = System.Environment.GetEnvironmentVariable("WORKSPACE") + "/GradleProject";

        // 配置BuildOptions
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            // 关键选项：指示Unity导出Gradle工程，而非直接生成APK
            options = BuildOptions.AcceptExternalModificationsToPlayer
        };

        // 执行构建导出
        BuildPipeline.BuildPlayer(options);
    }

    private static string[] GetEnabledScenes()
    {
        // 辅助方法：从 EditorBuildSettings 中获取所有启用的场景路径
        // ... 具体实现代码
        return new string[]
        {
            "Assets/Scenes/Init.unity",
            "Assets/Scenes/Home.unity",
            "Assets/Scenes/Game.unity",

        }; // 示例
    }
}