using AssetStoreTools.Utility.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AssetStoreTools.Uploader
{
    public class Package : VisualElement
    {
        public string PackageId { get; }
        public string VersionId { get; }
        public string PackageName { get; private set; }
        public string Status { get; private set; }
        public string Category { get; private set; }
        public string LastUpdatedDate  { get; private set; }
        public string LastUpdatedSize { get; private set; }
        public string SearchableText { get; private set; }
        public bool IsCompleteProject { get; private set; }

        // Unexpanded state dynamic elements
        private Button _foldoutBox;
        private Label _expanderLabel;
        private ProgressBar _uploadProgressBarHeader;
        private Label _assetLabel;
        private Label _lastDateSizeLabel;
        private Button _openInBrowserButton;
        
        // Expanded state dynamic elements
        private VisualElement _functionsBox;
        
        private Button _uploadButton;
        private ProgressBar _uploadProgressBar;
        
        private bool _expanded;
        public Action<Package> OnPackageSelection;
        
        // Workflows
        private UploadWorkflow _activeWorkflow = UploadWorkflow.FolderUpload;
        
        private VisualElement _workflowSelectionBox;
        private VisualElement _activeWorkflowElement;
        
        private FolderUploadWorkflowView _folderUploadWorkflow;
        private UnityPackageUploadWorkflowView _unityPackageUploadWorkflow;
        private HybridPackageUploadWorkflowView _hybridPackageUploadWorkflow;

        public Package(string packageId, string versionId, string packageName, string status, string category, string lastDate, string lastSize, bool isCompleteProject)
        {
            PackageId = packageId;
            VersionId = versionId;
            
            UpdateDataValues(packageName, status, category, lastDate, lastSize, isCompleteProject);
            SetupPackageElement();
        }

        public void UpdateDataValues(string packageName, string status, string category, string lastDate, string lastSize, bool isCompleteProject)
        {
            PackageName = packageName;
            Status = status;
            Category = category;
            LastUpdatedDate = FormatDate(lastDate);
            LastUpdatedSize = FormatSize(lastSize);
            SearchableText = $"{packageName} {category}".ToLower();
            IsCompleteProject = isCompleteProject;
            
            if (_foldoutBox == null) 
                return;
            
            _assetLabel.text = packageName;
            _lastDateSizeLabel.text = $"{Category} | {LastUpdatedSize} | {LastUpdatedDate}";
            
            _folderUploadWorkflow?.SetCompleteProject(IsCompleteProject);
        }

        public void ShowFunctions(bool show)
        {
            if (_functionsBox == null)
            {
                if (show)
                    SetupFunctionsElement();
                else
                    return;
            }

            if (show == _expanded)
                return;
            
            _expanded = show;
            _expanderLabel.text = !_expanded ? "►" : "▼";
            
            if (_expanded)
                _foldoutBox.AddToClassList("foldout-box-expanded");
            else
                _foldoutBox.RemoveFromClassList("foldout-box-expanded");
            
            _functionsBox.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetupPackageElement()
        { 
            AddToClassList("full-package-box");

            _foldoutBox = new Button {name = "Package"};
            _foldoutBox.AddToClassList("foldout-box");

            // Expander, Icon and Asset Label
            VisualElement foldoutBoxInfo = new VisualElement { name = "foldoutBoxInfo" };
            foldoutBoxInfo.AddToClassList("foldout-box-info");

            VisualElement labelExpanderRow = new VisualElement { name = "labelExpanderRow" };
            labelExpanderRow.AddToClassList("expander-label-row");

            _expanderLabel = new Label { name = "ExpanderLabel", text = "►" };
            _expanderLabel.AddToClassList("expander");
            
            Image assetImage = new Image { name = "AssetImage" };
            assetImage.AddToClassList("package-image");
            
            VisualElement assetLabelInfoBox = new VisualElement { name = "assetLabelInfoBox" };
            assetLabelInfoBox.AddToClassList("asset-label-info-box");

            _assetLabel = new Label { name = "AssetLabel", text = PackageName };
            _assetLabel.AddToClassList("asset-label");
            
            _lastDateSizeLabel = new Label {name = "AssetInfoLabel", text = $"{Category} | {LastUpdatedSize} | {LastUpdatedDate}"};
            _lastDateSizeLabel.AddToClassList("asset-info");
            
            assetLabelInfoBox.Add(_assetLabel);
            assetLabelInfoBox.Add(_lastDateSizeLabel);

            labelExpanderRow.Add(_expanderLabel);
            labelExpanderRow.Add(assetImage);
            labelExpanderRow.Add(assetLabelInfoBox);

            _openInBrowserButton = new Button
            {
                name = "OpenInBrowserButton",
                tooltip = "View your package in the Publishing Portal."
            };

            _openInBrowserButton.AddToClassList("open-in-browser-button");

            // Header Progress bar
            _uploadProgressBarHeader = new ProgressBar { name = "HeaderProgressBar" };
            _uploadProgressBarHeader.AddToClassList("header-progress-bar");
            _uploadProgressBarHeader.style.display = DisplayStyle.None;

            // Connect it all
            foldoutBoxInfo.Add(labelExpanderRow);
            foldoutBoxInfo.Add(_openInBrowserButton);

            _foldoutBox.Add(foldoutBoxInfo);
            _foldoutBox.Add(_uploadProgressBarHeader);

            Add(_foldoutBox);

            if (Status != "draft")
                _expanderLabel.style.display = DisplayStyle.None;

            _foldoutBox.clicked += () =>
            {
                OnPackageSelection?.Invoke(this);
                ShowFunctions(!_expanded);
            };

            _openInBrowserButton.clicked += () =>
            {
                Application.OpenURL($"https://publisher.unity.com/packages/{VersionId}/edit/upload");
            };
        }

        private void SetupFunctionsElement()
        {
            _functionsBox = new VisualElement { name = "FunctionalityBox" };
            _functionsBox.AddToClassList("functionality-box");

            _functionsBox.style.display = DisplayStyle.None;

            // Validation and uploading boxes
            var uploadingWorkflow = ConstructUploadingWorkflow();
            _functionsBox.Add(uploadingWorkflow);

            Add(_functionsBox);
        }

        private VisualElement ConstructUploadingWorkflow()
        {
            // Upload Box
            VisualElement uploadBox = new VisualElement {name = "UploadBox"};
            uploadBox.AddToClassList("upload-box");

            // Workflow selection
            _workflowSelectionBox = new VisualElement();
            _workflowSelectionBox.AddToClassList("selection-box-row");

            VisualElement labelHelpRow = new VisualElement();
            labelHelpRow.AddToClassList("label-help-row");
            
            Label workflowLabel = new Label {text = "Upload type"};
            Image workflowLabelTooltip = new Image
            {
                tooltip = "Select what content you are uploading to the Asset Store"
                + "\n\n• From Assets Folder - content located within the project's 'Assets' folder or one of its subfolders"
                + "\n\n• Pre-exported .unitypackage - content that has already been compressed into a .unitypackage file"
#if UNITY_ASTOOLS_EXPERIMENTAL
                + "\n\n• Local UPM Package - content that is located within the project's 'Packages' folder. Only embedded and local packages are supported"
#endif
            };
            
            labelHelpRow.Add(workflowLabel);
            labelHelpRow.Add(workflowLabelTooltip);

            
            var flowDrop = new ToolbarMenu() {text = "From Assets Folder"};
            flowDrop.menu.AppendAction("From Assets Folder", _ => { flowDrop.text = "From Assets Folder"; WorkflowChange(UploadWorkflow.FolderUpload);});
            flowDrop.menu.AppendAction("Pre-exported .unitypackage", _ => { flowDrop.text = "Pre-exported .unitypackage"; WorkflowChange(UploadWorkflow.UnityPackageUpload);});
#if UNITY_ASTOOLS_EXPERIMENTAL
            flowDrop.menu.AppendAction("Local UPM Package", _ => { flowDrop.text = "Local UPM Package"; WorkflowChange(UploadWorkflow.HybridPackageUpload);});
#endif // UNITY_ASTOOLS_EXPERIMENTAL
            flowDrop.AddToClassList("workflow-dropdown");

            _workflowSelectionBox.Add(labelHelpRow);
            _workflowSelectionBox.Add(flowDrop);
            
            uploadBox.Add(_workflowSelectionBox);

            AssetStoreCache.GetCachedUploadSelections(PackageId, VersionId, out JsonValue cachedSelections);
            _folderUploadWorkflow = FolderUploadWorkflowView.Create(IsCompleteProject, SerializeWorkflowSelections, cachedSelections);
            _unityPackageUploadWorkflow = UnityPackageUploadWorkflowView.Create(SerializeWorkflowSelections, cachedSelections);
            _hybridPackageUploadWorkflow = HybridPackageUploadWorkflowView.Create(SerializeWorkflowSelections, cachedSelections);
            
            uploadBox.Add(_folderUploadWorkflow);
            uploadBox.Add(_unityPackageUploadWorkflow);
            uploadBox.Add(_hybridPackageUploadWorkflow);
            
            SetActiveWorkflowElement(_folderUploadWorkflow);

            var progressUploadBox = SetupProgressUploadBox();
            uploadBox.Add(progressUploadBox);

            return uploadBox;
        }

        private void WorkflowChange(UploadWorkflow workflow)
        {
            _activeWorkflow = workflow;
            
            switch (_activeWorkflow)
            {
                case UploadWorkflow.FolderUpload:
                    SetActiveWorkflowElement(_folderUploadWorkflow);
                    break;
                
                case UploadWorkflow.UnityPackageUpload:
                    SetActiveWorkflowElement(_unityPackageUploadWorkflow);
                    break;
                
                case UploadWorkflow.HybridPackageUpload:
                    SetActiveWorkflowElement(_hybridPackageUploadWorkflow);
                    break;
            }
        }

        private void SetActiveWorkflowElement(VisualElement newActiveElement)
        {
            if (_activeWorkflowElement != null)
                _activeWorkflowElement.style.display = DisplayStyle.None;
            
            _activeWorkflowElement = newActiveElement;
            _activeWorkflowElement.style.display = DisplayStyle.Flex;
        }

        private VisualElement SetupProgressUploadBox()
        {
            var progressUploadBox = new VisualElement();
            progressUploadBox.AddToClassList("progress-upload-box");
            
            _uploadButton = new Button (BeginPackageUploadByWorkflow) { name = "UploadButton", text = "Upload"};
            _uploadButton.AddToClassList("upload-button");

            _uploadProgressBar = new ProgressBar { name = "UploadProgressBar" };
            _uploadProgressBar.AddToClassList("upload-progress-bar");
            
            progressUploadBox.Add(_uploadProgressBar);
            progressUploadBox.Add(_uploadButton);

            return progressUploadBox;
        }

        private void SerializeWorkflowSelections()
        {
            ASDebug.Log("Serializing workflow selections");
            var json = JsonValue.NewDict();
            
            // Folder upload flow
            var folderDict = JsonValue.NewDict();
            var folderPaths = new List<JsonValue>();
            foreach (var path in _folderUploadWorkflow.GetSelectedExportPaths())
                folderPaths.Add(JsonValue.NewString(path));
            folderDict["paths"] = new JsonValue(folderPaths);
            folderDict["dependencies"] = _folderUploadWorkflow.GetIncludeDependencies();
            folderDict["localPackageGuid"] = _folderUploadWorkflow.GetLocalPackageGuid();
            folderDict["localPackagePath"] = _folderUploadWorkflow.GetLocalPackagePath();

            // Package upload flow
            var unitypackageDict = JsonValue.NewDict();
            var unityPackagePaths = new List<JsonValue>();
            if(!string.IsNullOrEmpty(_unityPackageUploadWorkflow.GetSelectedPackagePath()))
                unityPackagePaths.Add(JsonValue.NewString(_unityPackageUploadWorkflow.GetSelectedPackagePath()));
            unitypackageDict["paths"] = new JsonValue(unityPackagePaths);
            unitypackageDict["localPackageGuid"] = _unityPackageUploadWorkflow.GetLocalPackageGuid();
            unitypackageDict["localPackagePath"] = _unityPackageUploadWorkflow.GetLocalPackagePath();

            // Hybrid package upload flow
            var hybridPackageDict = JsonValue.NewDict();
            var hybridPackagePaths = new List<JsonValue>();
            foreach (var path in _hybridPackageUploadWorkflow.GetSelectedExportPaths())
                hybridPackagePaths.Add(JsonValue.NewString(path));
            hybridPackageDict["paths"] = new JsonValue(hybridPackagePaths);
            hybridPackageDict["localPackageGuid"] = _hybridPackageUploadWorkflow.GetLocalPackageGuid();
            hybridPackageDict["localPackagePath"] = _hybridPackageUploadWorkflow.GetLocalPackagePath();

            json[FolderUploadWorkflowView.SerializedName] = folderDict;
            json[UnityPackageUploadWorkflowView.SerializedName] = unitypackageDict;
            json[HybridPackageUploadWorkflowView.SerializedName] = hybridPackageDict;

            AssetStoreCache.CacheUploadSelections(PackageId, VersionId, json);
        }

        private string FormatSize(string size)
        {
            if (string.IsNullOrEmpty(size))
                return "0.00 MB";
            
            float.TryParse(size, out var sizeBytes);
            return $"{sizeBytes / (1024f * 1024f):0.00} MB";
        }

        private string FormatDate(string date)
        {
            DateTime dt = DateTime.Parse(date);
            return dt.Date.ToString("yyyy-MM-dd");
        }

#region Package Uploading

        private void BeginPackageUploadByWorkflow()
        {
            switch (_activeWorkflow)
            {
                case UploadWorkflow.FolderUpload:
                    BeginFolderUploadUploading();
                    break;

                case UploadWorkflow.UnityPackageUpload:
                    BeginUnityPackageUploading();
                    break;

                case UploadWorkflow.HybridPackageUpload:
                    BeginHybridPackageUploading();
                    break;
            }
        }

        private bool ValidateUnityVersionsForUpload()
        {
            if (AssetStoreUploader.ShowPackageVersionDialog)
            {
                EditorUtility.DisplayProgressBar("Preparing...", "Checking version compatibility", 0.4f);
                var versions = AssetStoreAPI.GetPackageUploadedVersions(PackageId, VersionId);
                EditorUtility.ClearProgressBar();

                if (!versions.Any(x => x.CompareTo(AssetStoreUploader.MinRequiredPackageVersion) == 1))
                {
                    var result = EditorUtility.DisplayDialogComplex("Asset Store Tools", $"You may upload this package, but you will need to add a package using Unity version {AssetStoreUploader.MinRequiredPackageVersion} " +
                        "or higher to be able to submit a new asset", "Upload", "Cancel", "Upload and do not display this again");

                    if (result == 1)
                        return false;

                    if (result == 2)
                        AssetStoreUploader.ShowPackageVersionDialog = false;
                }
            }

            return true;
        }

        private void BeginFolderUploadUploading()
        {
            var paths = _folderUploadWorkflow.GetSelectedExportPaths();
            var includeDependencies = _folderUploadWorkflow.GetIncludeDependencies();
            var localPackageGuid = _folderUploadWorkflow.GetLocalPackageGuid();
            var localPackagePath = _folderUploadWorkflow.GetLocalPackagePath();

            if (paths.Length == 0)
            {
                EditorUtility.DisplayDialog("Exporting failed", "No folder path was selected. Please " +
                                                                "select a main folder path and try again.", "OK");
                return;
            }

            if (!ValidateUnityVersionsForUpload())
                return;

            // To-do: handle timeout error gracefully
            var outputPath = $"{FileUtil.GetUniqueTempPathInProject()}.unitypackage";

            PackageExporter.ExportPackage(paths, outputPath, includeDependencies, IsCompleteProject,
                () =>
                {
                    BeginPackageUploadPackage(outputPath, localPackageGuid, localPackagePath);
                }, Debug.LogError, AssetStoreUploader.EnableCustomExporter);
        }

        private void BeginUnityPackageUploading()
        {
            var path = _unityPackageUploadWorkflow.GetSelectedPackagePath();
            var localPackageGuid = _unityPackageUploadWorkflow.GetLocalPackageGuid();
            var localPackagePath = _unityPackageUploadWorkflow.GetLocalPackagePath();
            
            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("Uploading failed", "No UnityPackage was selected. Please " +
                                                                "select a UnityPackage path and try again.", "OK");
                return;
            }

            if (!ValidateUnityVersionsForUpload())
                return;

            BeginPackageUploadPackage(path, localPackageGuid, localPackagePath);
        }
        
        private void BeginHybridPackageUploading()
        {
            var paths = _hybridPackageUploadWorkflow.GetSelectedExportPaths();
            var localPackageGuid = _hybridPackageUploadWorkflow.GetLocalPackageGuid();
            var localPackagePath = _hybridPackageUploadWorkflow.GetLocalPackagePath();
            
            if (paths.Length == 0)
            {
                EditorUtility.DisplayDialog("Exporting failed", "No path was selected. Please " +
                                                                "select a local package path and try again.", "OK");
                return;
            }

            if (!ValidateUnityVersionsForUpload())
                return;

            var outputPath = $"{FileUtil.GetUniqueTempPathInProject()}.unitypackage";

            PackageExporter.ExportPackage(paths, outputPath, false, false,
                () =>
                {
                    BeginPackageUploadPackage(outputPath, localPackageGuid, localPackagePath);
                }, Debug.LogError, true);
        }

        private async void BeginPackageUploadPackage(string exportedPackagePath, string packageGuid, string packagePath)
        {
            var localProjectPath = Application.dataPath; // Application Data Path can only be retrieved from the main thread

            // Configure the UI
            // Disable Active Workflow
            EnableWorkflowElements(false);

            // Progress bar
            _uploadProgressBar.style.display = DisplayStyle.Flex;
            
            // Configure the upload cancel button
            _uploadButton.clickable = null;
            _uploadButton.clicked += () => AssetStoreAPI.AbortPackageUpload(VersionId);
            _uploadButton.style.flexGrow = 0;
            _uploadButton.text = "Cancel";
            
            // Set up upload progress tracking for the unexpanded package progress bar
            EditorApplication.update += OnPackageUploadProgressHeader;

            // Set up upload progress tracking for the expanded package progress bar
            EditorApplication.update += OnPackageUploadProgressContent;

            var result = await AssetStoreAPI.UploadPackage(VersionId, PackageName, exportedPackagePath, 
                packageGuid, packagePath, localProjectPath);

            switch (result.Status)
            {
                case PackageUploadResult.UploadStatus.Success:
                    OnPackageUploadSuccess();
                    break;
                case PackageUploadResult.UploadStatus.Cancelled:
                    // No actions needed
                    break;
                case PackageUploadResult.UploadStatus.Fail:
                    OnPackageUploadFail(result.Error);
                    break;
            }

            PostUploadCleanup();
        }

        private void OnPackageUploadProgressHeader()
        {
            if (!AssetStoreAPI.ActiveUploads.ContainsKey(VersionId))
                return;

            // Header progress bar is only shown when the package is not expanded
            if (!_expanded && _uploadProgressBarHeader.style.display == DisplayStyle.None)
                _uploadProgressBarHeader.style.display = DisplayStyle.Flex;
            else if (_expanded && _uploadProgressBarHeader.style.display == DisplayStyle.Flex)
                _uploadProgressBarHeader.style.display = DisplayStyle.None;

            _uploadProgressBarHeader.value = AssetStoreAPI.ActiveUploads[VersionId].Progress;
        }

        private void OnPackageUploadProgressContent()
        {
            if (!AssetStoreAPI.ActiveUploads.ContainsKey(VersionId))
                return;

            var progressValue = AssetStoreAPI.ActiveUploads[VersionId].Progress;
            _uploadProgressBar.value = progressValue;
            _uploadProgressBar.title = $"{progressValue:0.#}%";
        }

        private void OnPackageUploadSuccess()
        {
            EditorUtility.DisplayDialog("Success!", "Package has been uploaded successfully!", "OK");
            SetEnabled(false);
            AssetStoreAPI.GetRefreshedPackageData(PackageId,
                (data) => 
                {
                    UpdateDataValues(data["name"], data["status"], data["extra_info"]["category_info"]["name"],
                        data["extra_info"]["modified"], data["extra_info"]["size"], IsCompleteProject);
                    ASDebug.Log($"Updated the date and size values for package version id {VersionId}");
                    SetEnabled(true);
                    
                },
                (e) => 
                { 
                    ASDebug.LogError(e);
                    SetEnabled(true);
                });
        }

        private void OnPackageUploadFail(ASError error)
        {
            EditorUtility.DisplayDialog("Upload failed", "Package uploading failed. See Console for details", "OK");
            Debug.LogError(error);
        }

        private void PostUploadCleanup()
        {
            EnableWorkflowElements(true);
            
            // Cleanup the progress bars
            EditorApplication.update -= OnPackageUploadProgressHeader;
            EditorApplication.update -= OnPackageUploadProgressContent;

            ResetProgressBar();
            ResetUploadButton();
        }

        private void ResetProgressBar()
        {
            _uploadProgressBarHeader.style.display = DisplayStyle.None;
            _uploadProgressBarHeader.value = 0f;

            _uploadProgressBar.style.display = DisplayStyle.None;
            _uploadProgressBar.value = 0f;
            _uploadProgressBar.title = string.Empty;
        }

        private void ResetUploadButton()
        {
            _uploadButton.clickable = null;
            _uploadButton.clicked += BeginPackageUploadByWorkflow;
            _uploadButton.style.flexGrow = 1;
            _uploadButton.text = "Upload";
        }

        private void EnableWorkflowElements(bool enable)
        {
            _workflowSelectionBox.SetEnabled(enable);
            _activeWorkflowElement.SetEnabled(enable);
        }

#endregion

        [Serializable]
        private enum UploadWorkflow
        {
            FolderUpload,
            UnityPackageUpload,
            HybridPackageUpload
        }
    }
}