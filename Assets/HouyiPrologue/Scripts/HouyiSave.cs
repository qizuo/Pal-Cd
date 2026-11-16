using System;
using System.IO;
using UnityEngine;

namespace Houyi.NineSuns
{
    [Serializable]
    public sealed class HouyiProgress
    {
        public int version = 1, chapter, stage, cluesFound;
        public Vector3 checkpoint;
        public float yaw, pitch;
        public float sunburstCooldown;
        public bool[] targets, beasts, actors;
        public bool ropeCut, supplyTaken, loreRead;
        public bool precisionRecoveryUnlocked;
    }

    public static class HouyiSave
    {
        // Isolation tests use a separate directory and disable startup continuation.
        public static string DirectoryOverride;
        public static bool AutoLoadEnabled = true;
        public static string SavePath => Path.Combine(DirectoryOverride ?? Application.persistentDataPath, "houyi-progress.json");
        public static string ChapterPath(int chapter) => Path.Combine(DirectoryOverride ?? Application.persistentDataPath, "houyi-chapter-" + chapter + ".json");
        public static bool Write(HouyiProgress progress)
        {
            if (progress == null || progress.chapter < 0 || progress.chapter > 3) return false;
            if (progress.chapter == 3)
                progress.precisionRecoveryUnlocked |= progress.stage == 6 || TryRead(3, out var previous) && previous.precisionRecoveryUnlocked;
            return WriteFile(ChapterPath(progress.chapter), progress) && WriteFile(SavePath, progress);
        }
        static bool WriteFile(string path, HouyiProgress progress)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temporary = path + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(progress, true));
                // Same-directory renames work on Windows Mono and Android. If interrupted
                // between the two renames, the last completed event remains in the backup.
                if (File.Exists(path))
                {
                    if (ReadFile(path, out _))
                    {
                        if (File.Exists(path + ".bak")) File.Delete(path + ".bak");
                        File.Move(path, path + ".bak");
                    }
                    else File.Delete(path);
                }
                File.Move(temporary, path);
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { Debug.LogWarning("后羿自动存档失败：" + e.Message); return false; }
        }
        public static bool TryRead(out HouyiProgress progress)
        {
            return ReadFile(SavePath, out progress) || ReadFile(SavePath + ".bak", out progress);
        }
        public static bool TryRead(int chapter, out HouyiProgress progress)
        {
            progress = null;
            if (chapter < 0 || chapter > 3) return false;
            foreach (string path in new[] { ChapterPath(chapter), ChapterPath(chapter) + ".bak", SavePath, SavePath + ".bak" })
            {
                if (!ReadFile(path, out var data) || data.chapter != chapter) continue;
                progress = data;
                if (path == SavePath || path == SavePath + ".bak") WriteFile(ChapterPath(chapter), data);
                return true;
            }
            return false;
        }
        static bool ReadFile(string path, out HouyiProgress progress)
        {
            progress = null;
            try
            {
                if (!File.Exists(path)) return false;
                var data = JsonUtility.FromJson<HouyiProgress>(File.ReadAllText(path));
                if (data == null || data.version != 1 || data.chapter < 0 || data.chapter > 3 || data.stage < (data.chapter == 0 ? 1 : 0)
                    || data.stage > (data.chapter == 1 ? 8 : 6) || !Finite(data.checkpoint.x) || !Finite(data.checkpoint.y)
                    || !Finite(data.checkpoint.z) || !Finite(data.yaw) || !Finite(data.pitch) || !Finite(data.sunburstCooldown)) return false;
                if (data.chapter == 0 && (data.targets == null || data.beasts == null)) return false;
                if (data.chapter > 0 && (data.actors == null || data.cluesFound < 0 || data.cluesFound > 3)) return false;
                progress = data; return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { Debug.LogWarning("后羿存档无法读取：" + e.Message); return false; }
        }
        static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
        public static void Delete()
        {
            DeleteFiles(SavePath);
            for (int chapter = 0; chapter <= 3; chapter++) DeleteFiles(ChapterPath(chapter));
        }
        public static void DeleteChapter(int chapter)
        {
            DeleteFiles(ChapterPath(chapter));
            // Prevent a legacy backup from resurrecting a reset chapter.
            foreach (string path in new[] { SavePath, SavePath + ".bak" })
                if (ReadFile(path, out var data) && data.chapter == chapter) DeleteFiles(path);
        }
        static void DeleteFiles(string basePath)
        {
            foreach (string suffix in new[] { "", ".bak", ".tmp" })
            {
                string path = basePath + suffix;
                try { if (File.Exists(path)) File.Delete(path); }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                { Debug.LogWarning("后羿旧存档无法清除：" + e.Message); }
            }
        }
    }
}
