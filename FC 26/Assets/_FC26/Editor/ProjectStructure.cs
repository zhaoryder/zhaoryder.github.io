//=============================================================================
// 文件名：ProjectStructure.cs
// 所属模块：Editor 工具
// 命名空间：FC26.Editor
// 作用：Unity Editor 菜单工具，一键生成 FC 26 工程标准目录树。
//       通过菜单项 "FC26/生成工程目录结构" 调用，自动在 Assets/_FC26 下创建：
//         Scripts/  (Core|Data|Input|Match|Player|Ball|AI|Stadium|Referee|UI|Camera|Utils)
//         Prefabs/
//         Materials/
//         Scenes/
//         ScriptableObjects/
//         Settings/
//       并为每个目录生成 .gitkeep 占位文件，确保空目录被版本管理保留。
// 备注：本脚本仅 Editor 环境可用（位于 Editor 文件夹，使用 #if UNITY_EDITOR 包裹）。
//=============================================================================
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FC26.Editor
{
    /// <summary>
    /// 工程目录结构生成工具。
    /// </summary>
    public static class ProjectStructure
    {
        // 工程根目录（相对 Assets 的路径）。
        private const string RootPath = "Assets/_FC26";

        // Scripts 下的子模块目录。
        private static readonly string[] ScriptSubDirs =
        {
            "Core", "Data", "Input", "Match", "Player", "Ball",
            "AI", "Stadium", "Referee", "UI", "Camera", "Utils"
        };

        // 资源目录。
        private static readonly string[] AssetDirs =
        {
            "Prefabs", "Materials", "Scenes", "ScriptableObjects", "Settings"
        };

        /// <summary>
        /// 菜单项：一键生成工程目录结构。
        /// 路径：FC26/生成工程目录结构
        /// </summary>
        [MenuItem("FC26/生成工程目录结构", priority = 0)]
        public static void GenerateStructure()
        {
            Debug.Log("[ProjectStructure] 开始生成工程目录结构...");

            int createdCount = 0;

            // 1. 创建 Scripts 各子模块目录。
            foreach (string sub in ScriptSubDirs)
            {
                createdCount += CreateDirectoryWithKeep($"{RootPath}/Scripts/{sub}");
            }

            // 2. 创建资源目录。
            foreach (string dir in AssetDirs)
            {
                createdCount += CreateDirectoryWithKeep($"{RootPath}/{dir}");
            }

            // 3. 刷新 AssetDatabase，使新目录在 Project 窗口可见。
            AssetDatabase.Refresh();

            Debug.Log($"[ProjectStructure] 工程目录结构生成完成，共创建 {createdCount} 个目录。");
            EditorUtility.DisplayDialog(
                "FC 26 目录结构",
                $"工程目录结构生成完成！\n共创建 {createdCount} 个目录。\n根目录: {RootPath}",
                "确定");
        }

        /// <summary>
        /// 创建指定路径的目录（若不存在），并生成 .gitkeep 占位文件。
        /// </summary>
        /// <param name="path">相对 Assets 的目录路径</param>
        /// <returns>本次是否新建了目录（已存在返回 0，新建返回 1）</returns>
        private static int CreateDirectoryWithKeep(string path)
        {
            // 将相对路径转为绝对路径。
            string fullPath = Path.Combine(Application.dataPath, path.Substring("Assets/".Length));

            if (Directory.Exists(fullPath))
            {
                // 目录已存在，跳过。
                return 0;
            }

            Directory.CreateDirectory(fullPath);

            // 生成 .gitkeep 占位文件，确保空目录被 Git 保留。
            string keepPath = Path.Combine(fullPath, ".gitkeep");
            if (!File.Exists(keepPath))
            {
                File.WriteAllText(keepPath, "# 占位文件，保留空目录到版本管理\n");
            }

            return 1;
        }

        /// <summary>
        /// 菜单项：清理工程中的空目录（可选维护工具）。
        /// 路径：FC26/清理空目录
        /// 注意：仅清理 _FC26 下的空目录，且保留 .gitkeep。
        /// </summary>
        [MenuItem("FC26/清理空目录", priority = 1)]
        public static void CleanEmptyDirectories()
        {
            string rootFullPath = Path.Combine(Application.dataPath, "_FC26");
            if (!Directory.Exists(rootFullPath))
            {
                EditorUtility.DisplayDialog("清理空目录", "未找到 _FC26 目录。", "确定");
                return;
            }

            int removed = CleanEmptyDirectoriesRecursive(rootFullPath);
            AssetDatabase.Refresh();

            Debug.Log($"[ProjectStructure] 清理完成，移除 {removed} 个空目录。");
            EditorUtility.DisplayDialog("清理空目录", $"移除 {removed} 个空目录。", "确定");
        }

        /// <summary>
        /// 递归清理空目录。
        /// </summary>
        private static int CleanEmptyDirectoriesRecursive(string dirPath)
        {
            int removed = 0;

            // 先递归子目录。
            foreach (string sub in Directory.GetDirectories(dirPath))
            {
                removed += CleanEmptyDirectoriesRecursive(sub);
            }

            // 检查当前目录是否为空（仅含 .gitkeep 或完全空）。
            string[] entries = Directory.GetFileSystemEntries(dirPath);
            bool onlyKeep = true;
            foreach (string entry in entries)
            {
                string name = Path.GetFileName(entry);
                if (name != ".gitkeep" && name != ".DS_Store")
                {
                    onlyKeep = false;
                    break;
                }
            }

            if (onlyKeep && entries.Length > 0)
            {
                // 删除 .gitkeep 后删除目录。
                foreach (string entry in entries)
                {
                    File.Delete(entry);
                }

                Directory.Delete(dirPath);
                removed++;
            }
            else if (entries.Length == 0)
            {
                Directory.Delete(dirPath);
                removed++;
            }

            return removed;
        }
    }
}
#endif
