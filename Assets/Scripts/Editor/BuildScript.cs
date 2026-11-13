using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

public class BuildScript
{
    public static void ExportAndroidProject()
    {
        //UnityEngine.Debug.Log("[Jenkins] 开始执行 Unity 导出 Android Studio 工程...");

        // 1. 强制指定导出为 Android Studio 工程
        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

        // 2. 配置 Android 签名证书（虽然由外部 Gradle 编译，但签名配置在 Unity 中设置后会被一并导出）
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = "D:/UnityProject/JCTile/jctile.jks"; // 改为你的密钥路径
        PlayerSettings.Android.keystorePass = "jctile123456";
        PlayerSettings.Android.keyaliasName = "jctile";
        PlayerSettings.Android.keyaliasPass = "jctile123456";

        // 3. 设定导出目录
        string exportPath = Path.Combine(Environment.CurrentDirectory, "JenkinsBuilds/Android/AndroidStudioProject");
        if (Directory.Exists(exportPath))
        {
            Directory.Delete(exportPath, true); // 清理旧工程
        }

        BuildPlayerOptions opt = new BuildPlayerOptions
        {
            scenes = new string[] {
                "Assets/Scenes/Init.unity",
                "Assets/Scenes/Home.unity",
                "Assets/Scenes/Game.unity"
            },
            locationPathName = exportPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(opt);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            throw new Exception("[Jenkins] Unity 导出 Android 工程失败！");
        }
        //UnityEngine.Debug.Log("[Jenkins] Unity 导出 Android 工程成功。");
    }

    public static void PerformAndroidBuild()
    {
        // 1. 必须强制关闭“导出工程”选项！
        // 如果为 true，Unity 仅输出文件夹（Gradle项目）。设置为 false 才会触发内置编译生成文件。
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

        // 2. 确保开启 AAB (Android App Bundle) 格式输出
        EditorUserBuildSettings.buildAppBundle = true;

        // 3. 强制指定构建系统为 Gradle（AAB所必需）
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

        // 4. 读取版本号
        string gameVersion = PlayerSettings.bundleVersion; // 对应 Bundle Version (如: "1.0.4")
        int versionCode = PlayerSettings.Android.bundleVersionCode; // 对应 Version Code (如: 104)

        // 配置签名信息（强烈建议在代码中或环境变量里配好）
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = "Keystore/jctile.jks";
        PlayerSettings.Android.keystorePass = "jctile123456";
        PlayerSettings.Android.keyaliasName = "jctile";
        PlayerSettings.Android.keyaliasPass = "jctile123456";

        BuildPlayerOptions opt = new BuildPlayerOptions();
        opt.scenes = new string[] {
            "Assets/Scenes/Init.unity",
            "Assets/Scenes/Home.unity",
            "Assets/Scenes/Game.unity"
        };

        // 2. 利用读取到的版本号，动态决定 AAB 的输出文件名
        string outputFolder = "Builds/Android";
        // 拼接出来的文件名类似于: game_v1.0.4_rc104.aab
        opt.locationPathName = $"{outputFolder}/game_v{gameVersion}_rc{versionCode}.aab";
        opt.target = BuildTarget.Android;
        opt.options = BuildOptions.None;

        var report = BuildPipeline.BuildPlayer(opt);

        // 8. 结果反馈
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            UnityEngine.Debug.Log($"[Jenkins Build] 打包成功！文件大小: {report.summary.totalSize / (1024 * 1024)} MB");
        }
        else
        {
            UnityEngine.Debug.LogError($"[Jenkins Build] 打包失败！错误数量: {report.summary.totalErrors}");
            // 抛出异常让 Jenkins 能够捕获到 Stage 失败
            throw new Exception("Unity Build Failed!");
        }
    }
}