using AssetStoreTools.Utility.Json;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AssetStoreTools.Uploader
{
    public class UnityPackageUploadWorkflowView : VisualElement
    {
        public const string SerializedName = "unitypackageWorkflow";

        private TextField _pathSelectionField;
        
        // Upload data
        private string _selectedPackagePath;
        private string _localPackageGuid;
        private string _localPackagePath;

        private Action _serializeSelection;

        private UnityPackageUploadWorkflowView(Action serializeSelection)
        {
            _serializeSelection = serializeSelection;
            style.display = DisplayStyle.None;

            SetupWorkflow();
        }

        public static UnityPackageUploadWorkflowView Create(Action serializeAction)
        {
            return Create(serializeAction, default(JsonValue));
        }

        public static UnityPackageUploadWorkflowView Create(Action serializeAction, JsonValue serializedValues)
        {
            try
            {
                var newInstance = new UnityPackageUploadWorkflowView(serializeAction);
                if (!serializedValues.Equals(default(JsonValue)) && serializedValues.ContainsKey(SerializedName))
                    newInstance.LoadSerializedWorkflow(serializedValues[SerializedName]);
                return newInstance;
            }
            catch
            {
                ASDebug.LogError("Failed to load serialized values for a new .unitypackage Upload Workflow. Returning a default one");
                return new UnityPackageUploadWorkflowView(serializeAction);
            }
        }

        public string GetSelectedPackagePath()
        {
            return _selectedPackagePath;
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
            
            Label folderPathLabel = new Label {text = "Package path"};
            Image folderPathLabelTooltip = new Image
            {
                tooltip = "Select the .unitypackage file you would like to upload."
            };
            
            labelHelpRow.Add(folderPathLabel);
            labelHelpRow.Add(folderPathLabelTooltip);

            _pathSelectionField = new TextField();
            _pathSelectionField.AddToClassList("path-selection-field");
            _pathSelectionField.isReadOnly = true;
            
            Button browsePathButton = new Button(BrowsePath) {name = "BrowsePathButton", text = "Browse"};
            browsePathButton.AddToClassList("browse-button");
            
            folderPathSelectionRow.Add(labelHelpRow);
            folderPathSelectionRow.Add(_pathSelectionField);
            folderPathSelectionRow.Add(browsePathButton);
            
            Add(folderPathSelectionRow);
        }

        private void LoadSerializedWorkflow(JsonValue json)
        {
            var paths = json["paths"].AsList();

            if (paths.Count == 0)
                return;

            _pathSelectionField.value = _selectedPackagePath = paths[0].IsString() ? paths[0].AsString() : null;

            var localPackageGuid = json["localPackageGuid"];
            var localPackagePath = json["localPackagePath"];
            _localPackageGuid = localPackageGuid.IsString() ? localPackageGuid.AsString() : null;
            _localPackagePath = localPackagePath.IsString() ? localPackagePath.AsString() : null;

            ASDebug.Log($"Loaded serialized Unitypackage Flow value: {_selectedPackagePath}");
        }

        private void BrowsePath()
        {
            // Path retrieval
            var relativeExportPath = string.Empty;
            
            var rootProjectPath = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
            var absolutePackagePath = EditorUtility.OpenFilePanel("Select a .unitypackage file", rootProjectPath, "unitypackage");
            
            if (string.IsNullOrEmpty(absolutePackagePath))
                return;
            
            if (absolutePackagePath.StartsWith(rootProjectPath))
                relativeExportPath = absolutePackagePath.Substring(rootProjectPath.Length);
            
            // Main upload path
            var selectedPackagePath = !string.IsNullOrEmpty(relativeExportPath) ? relativeExportPath : absolutePackagePath;
            _pathSelectionField.value = selectedPackagePath;
            
            // Export data
            _selectedPackagePath = selectedPackagePath;
            
            // TODO: Make sure this actually works 
            _localPackageGuid = string.Empty;
            _localPackagePath = string.Empty;
            _serializeSelection?.Invoke();
            
        }
    }
}