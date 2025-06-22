using AssetStoreTools.Utility.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AssetStoreTools.Uploader
{
    public class HybridPackageUploadWorkflowView : VisualElement
    {
        public const string SerializedName = "hybridPackageWorkflow";
        private const string PackagesLockPath = "Packages/packages-lock.json";

        private TextField _pathSelectionField;
        private ValidationElement _validationElement;
        private VisualElement _extraPackagesElement;

        // Upload data
        private List<string> _selectedExportPaths = new List<string>();
        private string _localPackageGuid;
        private string _localPackagePath;

        private Action _serializeSelection;

        private HybridPackageUploadWorkflowView(Action serializeSelection)
        {
            _serializeSelection = serializeSelection;
            style.display = DisplayStyle.None;

            SetupWorkflow();
        }

        public static HybridPackageUploadWorkflowView Create(Action serializeAction)
        {
            return Create(serializeAction, default(JsonValue));
        }

        public static HybridPackageUploadWorkflowView Create(Action serializeAction, JsonValue serializedValues)
        {
            try
            {
                var newInstance = new HybridPackageUploadWorkflowView(serializeAction);
                if (!serializedValues.Equals(default(JsonValue)) && serializedValues.ContainsKey(SerializedName))
                    newInstance.LoadSerializedWorkflow(serializedValues[SerializedName]);
                return newInstance;
            }
            catch
            {
                ASDebug.LogError("Failed to load serialized values for a new Hybrid Package Upload Workflow. Returning a default one");
                return new HybridPackageUploadWorkflowView(serializeAction);
            }
        }

        public string[] GetSelectedExportPaths()
        {
            return _selectedExportPaths.ToArray();
        }

        public string GetLocalPackageGuid()
        {
            return _localPackageGuid;
        }

        public string GetLocalPackagePath()
        {
            return _localPackagePath;
        }

        private void SetupWorkflow()
        {
            // Path selection
            VisualElement folderPathSelectionRow = new VisualElement();
            folderPathSelectionRow.AddToClassList("selection-box-row");

            VisualElement labelHelpRow = new VisualElement();
            labelHelpRow.AddToClassList("label-help-row");

            Label folderPathLabel = new Label { text = "Package path" };
            Image folderPathLabelTooltip = new Image
            {
                tooltip = "Select a local Package you would like to export and upload to the Store."
            };

            labelHelpRow.Add(folderPathLabel);
            labelHelpRow.Add(folderPathLabelTooltip);

            _pathSelectionField = new TextField();
            _pathSelectionField.AddToClassList("path-selection-field");
            _pathSelectionField.isReadOnly = true;

            Button browsePathButton = new Button(BrowsePath) { name = "BrowsePathButton", text = "Browse" };
            browsePathButton.AddToClassList("browse-button");

            folderPathSelectionRow.Add(labelHelpRow);
            folderPathSelectionRow.Add(_pathSelectionField);
            folderPathSelectionRow.Add(browsePathButton);

            Add(folderPathSelectionRow);

            _validationElement = new ValidationElement();
            Add(_validationElement);
        }

        private void LoadSerializedWorkflow(JsonValue json)
        {
            var paths = json["paths"].AsList();

            if (paths.Count == 0)
                return;

            // Serialized path will be in ADB form, so we need to reconstruct it first
            var rootProjectPath = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
            var realPath = Path.GetFullPath(paths[0].AsString()).Replace('\\', '/');
            if (realPath.StartsWith(rootProjectPath))
                realPath = realPath.Substring(rootProjectPath.Length);

            // Do not restore any serialized values if the main package is no longer valid/exists
            if (!IsValidLocalPackage(realPath, out string relativeAssetDatabasePath))
                return;

            // Get a list of which toggles will need to be enabled
            var serializedValues = new List<string>();
            for (int i = 1; i < paths.Count; i++)
                serializedValues.Add(paths[i].AsString());

            // Treat this as a manual selection but with serialized toggle values
            HandleHybridUploadPathSelection(realPath, relativeAssetDatabasePath, serializedValues);

            ASDebug.Log($"Loaded serialized Hybrid Package Flow values with {_selectedExportPaths.Count} paths");
        }

        private void BrowsePath()
        {
            // Path retrieval
            string relativeExportPath = string.Empty;
            string rootProjectPath = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);

            var absoluteExportPath = EditorUtility.OpenFolderPanel("Select the Package", "Packages/", "");

            if (string.IsNullOrEmpty(absoluteExportPath))
                return;

            if (absoluteExportPath.StartsWith(rootProjectPath))
                relativeExportPath = absoluteExportPath.Substring(rootProjectPath.Length);

            var workingPath = !string.IsNullOrEmpty(relativeExportPath) ? relativeExportPath : absoluteExportPath;
            if (!IsValidLocalPackage(workingPath, out string relativeAssetDatabasePath))
            {
                EditorUtility.DisplayDialog("Invalid selection", "Selected export path must be a valid local package", "OK");
                return;
            }

            HandleHybridUploadPathSelection(workingPath, relativeAssetDatabasePath, null);
        }

        private void HandleHybridUploadPathSelection(string relativeExportPath, string relativeAssetDatabasePath, List<string> serializedToggles)
        {
            _pathSelectionField.value = relativeExportPath + "/";

            // Reset and reinitialize the selected export path(s) array
            _selectedExportPaths = new List<string> { relativeAssetDatabasePath };

            // Set additional upload data for the Publisher Portal backend (GUID and Package Path).
            // The backend workflow currently accepts only 1 package guid and path, so we'll use the main folder data
            _localPackageGuid = AssetDatabase.AssetPathToGUID(relativeAssetDatabasePath);
            _localPackagePath = relativeAssetDatabasePath.Substring("Packages".Length);

            _validationElement.SetLocalPath(relativeAssetDatabasePath);

            if (_extraPackagesElement != null)
            {
                _extraPackagesElement.Clear();
                Remove(_extraPackagesElement);

                _extraPackagesElement = null;
            }

            List<string> pathsToAdd = new List<string>();
            foreach (var package in GetAllLocalPackages())
            {
                // Exclude the Asset Store Tools themselves
                if (package.Get("name") == "com.unity.asset-store-tools")
                    continue;

                var localPackagePath = package.Get("path_absolute");

                if (localPackagePath == relativeExportPath)
                    continue;

                pathsToAdd.Add(package.Get("path_assetdb"));
            }

            if (pathsToAdd.Count != 0)
                PopulateExtraPathsBox(pathsToAdd, serializedToggles);

            // Only serialize current selection when no serialized toggles were passed
            if (serializedToggles == null)
                _serializeSelection?.Invoke();
        }

        private void PopulateExtraPathsBox(List<string> otherPackagesFound, List<string> checkedToggles)
        {
            // Dependencies selection
            _extraPackagesElement = new VisualElement();
            _extraPackagesElement.AddToClassList("selection-box-row");

            VisualElement extraPackagesHelpRow = new VisualElement();
            extraPackagesHelpRow.AddToClassList("label-help-row");

            Label extraPackagesLabel = new Label { text = "Extra Packages" };
            Image extraPackagesLabelTooltip = new Image
            {
                tooltip = "If your package has dependencies on other local packages, please select which of these packages should also be included in the resulting package"
            };

            VisualElement extraPackagesTogglesBox = new ScrollView { name = "ExtraPackagesTogglesToggles" };
            extraPackagesTogglesBox.AddToClassList("extra-packages-scroll-view");

            extraPackagesHelpRow.Add(extraPackagesLabel);
            extraPackagesHelpRow.Add(extraPackagesLabelTooltip);

            _extraPackagesElement.Add(extraPackagesHelpRow);
            _extraPackagesElement.Add(extraPackagesTogglesBox);

            EventCallback<ChangeEvent<bool>, string> toggleChangeCallback = OnToggledPackage;

            foreach (var path in otherPackagesFound)
            {
                var toggle = new Toggle { value = false, text = path };
                toggle.AddToClassList("extra-packages-toggle");
                toggle.tooltip = path;
                if (checkedToggles != null && checkedToggles.Contains(toggle.text))
                {
                    toggle.SetValueWithoutNotify(true);
                    _selectedExportPaths.Add(toggle.text);
                }

                toggle.RegisterCallback(toggleChangeCallback, toggle.text);
                extraPackagesTogglesBox.Add(toggle);
            }

            Add(_extraPackagesElement);
        }

        private void OnToggledPackage(ChangeEvent<bool> evt, string folderPath)
        {
            switch (evt.newValue)
            {
                case true when !_selectedExportPaths.Contains(folderPath):
                    _selectedExportPaths.Add(folderPath);
                    break;
                case false when _selectedExportPaths.Contains(folderPath):
                    _selectedExportPaths.Remove(folderPath);
                    break;
            }

            _serializeSelection?.Invoke();
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
                if (File.Exists(PackagesLockPath))
                    return CollectPackagesFromPackagesLock();

                // Fallback for earlier versions of 2019.4 which do not have packages-lock.json
                return CollectPackagesManual();
            }
            catch
            {
                return null;
            }
        }

        private List<JsonValue> CollectPackagesFromPackagesLock()
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

        private List<JsonValue> CollectPackagesManual()
        {
            // Scrape manifest.json for local packages
            string manifestJsonString = File.ReadAllText("Packages/manifest.json");
            JSONParser parser = new JSONParser(manifestJsonString);
            var manifestJson = parser.Parse();

            var packages = manifestJson.Get("dependencies").AsDict();
            var localPackages = new List<JsonValue>();

            foreach (var kvp in packages)
            {
                if (!kvp.Value.AsString().StartsWith("file:"))
                    continue;

                var packagePath = kvp.Value.AsString().Substring("file:".Length);
                if (packagePath.StartsWith("../"))
                    packagePath = packagePath.Substring("../".Length);

                JsonValue localPackage = new JsonValue
                {
                    ["name"] = JsonValue.NewString(kvp.Key),
                    ["source"] = JsonValue.NewString("local"),
                    ["path_absolute"] = JsonValue.NewString(packagePath),
                    ["path_assetdb"] = JsonValue.NewString($"Packages/{kvp.Key}")
                };

                localPackages.Add(localPackage);
            }

            // Scrape Packages folder for embedded packages
            foreach (var directory in Directory.GetDirectories("Packages"))
            {
                var path = directory.Replace("\\", "/");
                var packageManifestPath = $"{path}/package.json";

                if (!File.Exists(packageManifestPath))
                    continue;

                string packageManifestJsonString = File.ReadAllText(packageManifestPath);
                parser = new JSONParser(packageManifestJsonString);
                var packageManifestJson = parser.Parse();

                var packageName = packageManifestJson["name"].AsString();

                JsonValue embeddedPackage = new JsonValue()
                {
                    ["name"] = JsonValue.NewString(packageName),
                    ["source"] = JsonValue.NewString("embedded"),
                    ["path_absolute"] = JsonValue.NewString(path),
                    ["path_assetdb"] = JsonValue.NewString($"Packages/{packageName}")
                };

                localPackages.Add(embeddedPackage);
            }

            return localPackages;
        }
    }
}