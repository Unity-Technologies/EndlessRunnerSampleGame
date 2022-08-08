using System;
using System.Collections.Generic;
using System.IO;
using AssetStoreTools.Utility.Json;
using AssetStoreTools.Validator;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AssetStoreTools.Uploader
{
    public class FolderUploadWorkflowView : VisualElement
    {
        public const string SerializedName = "folderWorkflow";

        private TextField _pathSelectionField;
        private Toggle _dependenciesToggle;

        private ValidationElement _validationElement;
        private VisualElement _specialFoldersElement;

        private bool _isCompleteProject;

        // Upload data
        private List<string> _selectedExportPaths = new List<string>();
        private string _localPackageGuid;
        private string _localPackagePath;

        private Action _serializeSelection;

        // Special folders that would not work if not placed directly in the 'Assets' folder
        private readonly string[] _extraAssetFolderNames =
        {
            "Editor Default Resources", "Gizmos", "Plugins",
            "StreamingAssets", "Standard Assets", "WebGLTemplates"
        };

        private FolderUploadWorkflowView(bool isCompleteProject, Action serializeSelection)
        {
            _isCompleteProject = isCompleteProject;
            _serializeSelection = serializeSelection;
            style.display = DisplayStyle.None;

            SetupWorkflow();
        }

        public static FolderUploadWorkflowView Create(bool isCompleteProject, Action serializeAction)
        {
            return Create(isCompleteProject, serializeAction, default(JsonValue));
        }

        public static FolderUploadWorkflowView Create(bool isCompleteProject, Action serializeAction, JsonValue serializedValues)
        {
            try
            {
                var newInstance = new FolderUploadWorkflowView(isCompleteProject, serializeAction);
                if (!serializedValues.Equals(default(JsonValue)) && serializedValues.ContainsKey(SerializedName))
                    newInstance.LoadSerializedWorkflow(serializedValues[SerializedName]);
                return newInstance;
            }
            catch
            {
                ASDebug.LogError("Failed to load serialized values for a new Folder Upload Workflow. Returning a default one");
                return new FolderUploadWorkflowView(isCompleteProject, serializeAction);
            }
        }

        public void SetCompleteProject(bool isCompleteProject)
        {
            _isCompleteProject = isCompleteProject;
        }

        public string[] GetSelectedExportPaths()
        {
            return _selectedExportPaths.ToArray();
        }

        public bool GetIncludeDependencies()
        {
            return _dependenciesToggle.value;
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

            Label folderPathLabel = new Label { text = "Folder path" };
            Image folderPathLabelTooltip = new Image
            {
                tooltip = "Select the main folder of your package" +
                "\n\nAll files and folders of your package should preferably be contained within a single root folder that is named after your package" +
                "\n\nExample: 'Assets/[MyPackageName]'" +
                "\n\nNote: If your content makes use of special folders that are required to be placed in the root Assets folder (e.g. 'StreamingAssets')," +
                " you will be able to include them after selecting the main folder"
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

            // Dependencies selection
            VisualElement dependenciesSelectionRow = new VisualElement();
            dependenciesSelectionRow.AddToClassList("selection-box-row");

            VisualElement dependenciesLabelHelpRow = new VisualElement();
            dependenciesLabelHelpRow.AddToClassList("label-help-row");

            Label dependenciesLabel = new Label { text = "Dependencies" };
            Image dependenciesLabelTooltip = new Image
            {
                tooltip = "Tick this checkbox if your package content has dependencies on Unity packages from the Package Manager"
            };

            _dependenciesToggle = new Toggle { name = "DependenciesToggle", text = "Include Package Manifest" };
            _dependenciesToggle.AddToClassList("dependencies-toggle");
            _dependenciesToggle.RegisterValueChangedCallback((_) => _serializeSelection?.Invoke());

            dependenciesLabelHelpRow.Add(dependenciesLabel);
            dependenciesLabelHelpRow.Add(dependenciesLabelTooltip);

            dependenciesSelectionRow.Add(dependenciesLabelHelpRow);
            dependenciesSelectionRow.Add(_dependenciesToggle);

            Add(dependenciesSelectionRow);

            _validationElement = new ValidationElement();
            Add(_validationElement);
        }
        
        private void LoadSerializedWorkflow(JsonValue json)
        {
            var paths = json["paths"].AsList();

            if (paths.Count == 0)
                return;

            // Do not restore any values if main export path no longer exists
            if (!Directory.Exists(paths[0].AsString()))
                return;

            // Get a list of which toggles will need to be enabled
            var serializedValues = new List<string>();
            for (int i = 1; i < paths.Count; i++)
                serializedValues.Add(paths[i].AsString());

            // Treat this as a manual selection but with serialized toggle values
            HandleFolderUploadPathSelection(paths[0].AsString(), serializedValues);

            // Restore the dependencies toggle
            var dependencies = json["dependencies"];
            _dependenciesToggle.SetValueWithoutNotify(dependencies.AsBool());

            ASDebug.Log($"Loaded serialized Folder Flow values with {_selectedExportPaths.Count} paths");
        }

        #region Folder Upload

        private void BrowsePath()
        {
            // Path retrieval
            var absoluteExportPath = string.Empty;
            var relativeExportPath = string.Empty;
            var rootProjectPath = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);

            bool includeAllAssets = false;

            if (_isCompleteProject)
            {
                includeAllAssets = EditorUtility.DisplayDialog("Notice",
                    "Your package draft is set to a category that is treated" +
                    " as a complete project. Project settings will be included automatically. Would you like everything in the " +
                    "'Assets' folder to be included?\n\nYou will still be able to change the selected assets before uploading",
                    "Yes, include all folders and assets",
                    "No, I'll select what to include manually");
                if (includeAllAssets)
                    absoluteExportPath = Application.dataPath;
            }

            if (!includeAllAssets)
            {
                absoluteExportPath =
                    EditorUtility.OpenFolderPanel("Select folder to compress into a package", "Assets/", "");
                if (string.IsNullOrEmpty(absoluteExportPath))
                    return;
            }

            if (absoluteExportPath.StartsWith(rootProjectPath))
                relativeExportPath = absoluteExportPath.Substring(rootProjectPath.Length);

            if (!relativeExportPath.StartsWith("Assets/") && (relativeExportPath != "Assets" || !_isCompleteProject))
            {
                if (relativeExportPath.StartsWith("Assets") && !_isCompleteProject)
                    EditorUtility.DisplayDialog("Invalid selection",
                        "'Assets' folder is only available for packages tagged as a 'Complete Project'.", "OK");
                else
                    EditorUtility.DisplayDialog("Invalid selection", "Selected folder path must be within the project.",
                        "OK");
                return;
            }

            HandleFolderUploadPathSelection(relativeExportPath, null);
        }

        private void HandleFolderUploadPathSelection(string relativeExportPath, List<string> serializedToggles)
        {
            string selectedExportPath = relativeExportPath;
            _pathSelectionField.value = relativeExportPath + "/";

            // Main upload path is the index 0
            _selectedExportPaths = new List<string> { selectedExportPath };
            _localPackageGuid = AssetDatabase.AssetPathToGUID(relativeExportPath);
            _localPackagePath = relativeExportPath;
            
            _validationElement.SetLocalPath(relativeExportPath);

            if (_specialFoldersElement != null)
            {
                _specialFoldersElement.Clear();
                Remove(_specialFoldersElement);

                _specialFoldersElement = null;
            }

            // Prompt additional path selection (e.g. StreamingAssets, WebGLTemplates, etc.)
            List<string> specialFoldersFound = new List<string>();

            foreach (var extraAssetFolderName in _extraAssetFolderNames)
            {
                var fullExtraPath = "Assets/" + extraAssetFolderName;

                if (!Directory.Exists(fullExtraPath))
                    continue;

                if (relativeExportPath.ToLower().StartsWith(fullExtraPath.ToLower()))
                    continue;

                // Don't include nested paths
                if (!fullExtraPath.ToLower().StartsWith(relativeExportPath.ToLower()))
                    specialFoldersFound.Add(fullExtraPath);
            }

            if (specialFoldersFound.Count != 0)
                PopulateExtraPathsBox(specialFoldersFound, serializedToggles);

            // Only serialize current selection when no serialized toggles were passed
            if(serializedToggles == null)
                _serializeSelection?.Invoke();
        }

        private void PopulateExtraPathsBox(List<string> specialFoldersFound, List<string> checkedToggles)
        {
            // Dependencies selection
            _specialFoldersElement = new VisualElement();
            _specialFoldersElement.AddToClassList("selection-box-row");

            VisualElement specialFoldersHelpRow = new VisualElement();
            specialFoldersHelpRow.AddToClassList("label-help-row");

            Label specialFoldersLabel = new Label { text = "Special folders" };
            Image specialFoldersLabelTooltip = new Image
            {
                tooltip =
                    "If your package content relies on Special Folders (e.g. StreamingAssets), please select which of these folders should be included in the package"
            };

            VisualElement specialFolderTogglesBox = new VisualElement { name = "SpecialFolderToggles" };
            specialFolderTogglesBox.AddToClassList("special-folders-toggles-box");

            specialFoldersHelpRow.Add(specialFoldersLabel);
            specialFoldersHelpRow.Add(specialFoldersLabelTooltip);

            _specialFoldersElement.Add(specialFoldersHelpRow);
            _specialFoldersElement.Add(specialFolderTogglesBox);

            EventCallback<ChangeEvent<bool>, string> toggleChangeCallback = OnSpecialFolderPathToggledAsset;

            foreach (var path in specialFoldersFound)
            {
                var toggle = new Toggle { value = false, text = path };
                toggle.AddToClassList("special-folder-toggle");
                if (checkedToggles != null && checkedToggles.Contains(toggle.text))
                {
                    toggle.SetValueWithoutNotify(true);
                    _selectedExportPaths.Add(toggle.text);
                }

                toggle.RegisterCallback(toggleChangeCallback, toggle.text);
                specialFolderTogglesBox.Add(toggle);
            }

            Add(_specialFoldersElement);
        }

        private void OnSpecialFolderPathToggledAsset(ChangeEvent<bool> evt, string folderPath)
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

        #endregion

        
    }
}