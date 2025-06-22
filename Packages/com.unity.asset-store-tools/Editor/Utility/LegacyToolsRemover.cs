using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AssetStoreTools
{
    [InitializeOnLoad]
    public class LegacyToolsRemover
    {
        private const string MessagePart1 = "A legacy version of the Asset Store Tools " +
            "was detected at the following path:\n";
        private const string MessagePart2 = "\n\nWould you like it to be removed?";
        private const string SessionKey = "ASTLegacyToolsRemoverActive";

        static LegacyToolsRemover()
        {
            try
            {
                if (Application.isBatchMode)
                    return;

                CheckAndRemoveLegacyTools();
            }
            catch { }
        }

        private static void CheckAndRemoveLegacyTools()
        {
            if (PlayerPrefs.GetInt(SessionKey, 1) == 0 || !ProjectContainsLegacyTools(out string path))
                return;

            var relativePath = path.Substring(Application.dataPath.Length - "Assets".Length).Replace("\\", "/");
            var result = EditorUtility.DisplayDialogComplex("Asset Store Tools", MessagePart1 + relativePath + MessagePart2, "Yes", "No", "No and do not display this again");
            
            // If "No" - do nothing
            if (result == 1)
                return;

            // If "Yes" - remove legacy tools
            if (result == 0)
            {
                File.Delete(path);
                File.Delete(path + ".meta");
                RemoveEmptyFolders(Path.GetDirectoryName(path).Replace("\\", "/"));
                AssetDatabase.Refresh();
            }

            // If "Yes" or "No and do not show again" - prevent future execution
            PlayerPrefs.SetInt(SessionKey, 0);
        }

        private static bool ProjectContainsLegacyTools(out string path)
        {
            path = null;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.ManifestModule.Name == "AssetStoreTools.dll")
                {
                    path = assembly.Location;
                    break;
                }
            }

            if (string.IsNullOrEmpty(path))
                return false;
            return true;
        }

        private static void RemoveEmptyFolders(string directory)
        {
            if (directory.EndsWith(Application.dataPath))
                return;

            if (Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0)
            {
                var parentPath = Path.GetDirectoryName(directory).Replace("\\", "/");

                Directory.Delete(directory);
                File.Delete(directory + ".meta");

                RemoveEmptyFolders(parentPath);
            }
        }
    }
}