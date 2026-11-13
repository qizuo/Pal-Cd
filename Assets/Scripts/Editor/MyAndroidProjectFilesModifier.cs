using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

public class MyAndroidProjectFilesModifier : IPostGenerateGradleAndroidProject
{
    public int callbackOrder
    {
        get
        {
            return 0;
        }
    }
    public void OnPostGenerateGradleAndroidProject(string gradleProjectPath)
    {
        string rootProjectPath = Directory.GetParent(gradleProjectPath)?.FullName;
        if (string.IsNullOrEmpty(rootProjectPath))
        {
            Debug.LogError("Failed to get Gradle project root directory.");
            return;
        }

        CopyNotifyRes(gradleProjectPath);

        string sourceJson = Application.dataPath + "/Config/google-services.json";
        string destJson = System.IO.Path.Combine(rootProjectPath, "launcher/google-services.json");
        CopyFileWithDirCheck(sourceJson, destJson);

        string sourceXml = Application.dataPath + "/Config/remote_config_defaults.xml";
        string destXml = System.IO.Path.Combine(gradleProjectPath, "src/main/res/xml/remote_config_defaults.xml");
        Debug.Log("0609.1." + sourceXml + " " + destXml);
        CopyFileWithDirCheck(sourceXml, destXml);

        string sourceWrapper = Application.dataPath + "/Config/gradle-wrapper.properties";
        string destWrapper = System.IO.Path.Combine(rootProjectPath, "gradle/wrapper/gradle-wrapper.properties");
        CopyFileWithDirCheck(sourceWrapper, destWrapper);

    }

    void CopyNotifyRes(string gradleProjectPath)
    {
        string sourceXml = Application.dataPath + "/Plugins/Android/res_notify";
        string destXml = System.IO.Path.Combine(gradleProjectPath, "src/main/res");

        if (!Directory.Exists(destXml))
        {
            Directory.CreateDirectory(destXml);
        }

        CopyDirectory(sourceXml, destXml);
        Debug.Log($"已拷贝资源到: {destXml}");

    }

    // 递归拷贝文件夹
    private void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (string file in Directory.GetFiles(sourceDir))
        {
            if (file.EndsWith(".meta"))
                continue;
            string destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (string subDir in Directory.GetDirectories(sourceDir))
        {
            string dirName = Path.GetFileName(subDir);
            string destSubDir = Path.Combine(destDir, dirName);
            CopyDirectory(subDir, destSubDir);
        }
    }

    private void CopyFileWithDirCheck(string source, string dest)
    {
        if (!File.Exists(source))
        {
            Debug.LogWarning($"Source file not found: {source}. Skip copying.");
            return;
        }

        string destDir = Path.GetDirectoryName(dest);
        if (!Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

        File.Copy(source, dest, true);
        Debug.Log($"Copied {source} -> {dest}");
    }
}