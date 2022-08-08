using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using AssetStoreTools.Utility.Json;
    
namespace AssetStoreTools.Validator
{
    public class PathBoxElement : VisualElement
    {
        private const string PackagesLockPath = "Packages/packages-lock.json";

        private TextField _folderPathField;
        private Button _browseButton;
        
        public PathBoxElement()
        {
            ConstructPathBox();
        }

        public string GetPathBoxValue()
        {
            return _folderPathField.value;
        }

        public void SetPathBoxValue(string path)
        {
            _folderPathField.value = path;
            TestActions.Instance.SetMainPath(path);
        }

        private void ConstructPathBox()
        {
            AddToClassList("path-box");
            
            _folderPathField = new TextField
            {
                label = "Assets path",
                isReadOnly = true
            };
            
            _browseButton = new Button (Browse) {text = "Browse"};
            
            Add(_folderPathField);
            Add(_browseButton);
        }

        private void Browse()
        {
            string result = EditorUtility.OpenFolderPanel("Select a directory", "Assets", "");

            if (result == string.Empty)
                return;

            if (ValidateFolderPath(ref result))
                _folderPathField.value = result;
            else
                return;

            SetPathBoxValue(result);
        }
        
        private bool ValidateFolderPath(ref string resultPath)
        {
            var folderPath = resultPath;
            var pathWithinProject = Application.dataPath.Replace("Assets", "");

            // Selected path is within the project
            if (folderPath.Contains(pathWithinProject))
            {
                var localPath = folderPath.Replace(pathWithinProject, "");

                if (localPath.StartsWith("Packages"))
                {
                    if (IsValidLocalPackage(localPath, out var adbPath))
                    {
                        resultPath = adbPath;
                        return true;
                    }
                }

                if (localPath.StartsWith("Assets"))
                {
                    resultPath = localPath;
                    return true;
                }

                DisplayMessage("Folder not found", "Selection must be within Assets folder or a local package.");
                return false;
            }

            // Selected path is not within the project, but could be a local package
            if (!IsValidLocalPackage(folderPath, out var adbPath2))
            {
                DisplayMessage("Folder not found", "Selection must be within Assets folder or a local package.");
                return false;
            }
            
            resultPath = adbPath2;
            return true;
        }
        
        private bool IsValidLocalPackage(string packageFolderPath, out string assetDatabasePackagePath)
        {
            assetDatabasePackagePath = string.Empty;
            
            string packageManifestPath = $"{packageFolderPath}/package.json";

            if (!File.Exists(packageManifestPath))
                return false;
            try
            {
                var localPackages = GetAllLocalPackages();

                if (localPackages == null || localPackages.Count == 0)
                    return false;

                foreach (var package in localPackages)
                {
                    var localPackagePath = package.Get("path_absolute").AsString();
                    
                    if (localPackagePath != packageFolderPath) 
                        continue;
                    
                    assetDatabasePackagePath = package.Get("path_assetdb").AsString();
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private List<JsonValue> GetAllLocalPackages()
        {
            try
            {
                string packageLockJsonString = File.ReadAllText(PackagesLockPath);
                JSONParser parser = new JSONParser(packageLockJsonString);
                var packageLockJson = parser.Parse();

                var packages = packageLockJson.Get("dependencies").AsDict();
                var localPackages = new List<JsonValue>();

                foreach (var kvp in packages)
                {
                    var packageSource = kvp.Value.Get("source").AsString();
                    
                    if (!packageSource.Equals("embedded") && !packageSource.Equals("local")) 
                        continue;
                    
                    var packagePath = kvp.Value.Get("version").AsString().Substring("file:".Length);
                        
                    if (packageSource.Equals("embedded"))
                        packagePath = $"Packages/{packagePath}";
                    else if (packageSource.Equals("local") && packagePath.StartsWith("../"))
                        packagePath = packagePath.Substring("../".Length);
                        
                    JsonValue localPackage = new JsonValue
                    {
                        ["name"] = JsonValue.NewString(kvp.Key),
                        ["source"] = JsonValue.NewString(kvp.Value.Get("source")),
                        ["path_absolute"] = JsonValue.NewString(packagePath),
                        ["path_assetdb"] = JsonValue.NewString($"Packages/{kvp.Key}")
                    };
                        
                    localPackages.Add(localPackage);
                }

                return localPackages;
            }
            catch
            {
                return null;
            }
        }

        private void DisplayMessage(string title, string message)
        {
            if (EditorUtility.DisplayDialog(title, message, "Okay", "Cancel"))
                Browse();
        }
    }
}