using UnityEditor;
using UnityEngine;
using System;
using AssetStoreTools.Uploader;
using AssetStoreTools.Validator;

namespace AssetStoreTools
{
    internal class AssetStoreTools : EditorWindow
    {
        [MenuItem("Asset Store Tools v2/Asset Store Uploader", false, 0)]
        public static void ShowAssetStoreToolsUploader()
        {
            Type inspectorType = Type.GetType("UnityEditor.InspectorWindow,UnityEditor.dll");
            GetWindow<AssetStoreUploader>(inspectorType);
        }
        
                
        [MenuItem("Asset Store Tools v2/Asset Store Validator", false, 1)]
        public static void ShowAssetStoreToolsValidator()
        {
            Type inspectorType = Type.GetType("UnityEditor.InspectorWindow,UnityEditor.dll");
            GetWindow<AssetStoreValidation>(typeof(AssetStoreUploader), inspectorType);
        }

        [MenuItem("Asset Store Tools v2/Publisher Portal", false, 20)]
        public static void OpenPublisherPortal()
        {
            Application.OpenURL("https://publisher.unity.com/");
        }

        [MenuItem("Asset Store Tools v2/Submission Guidelines", false, 21)]
        public static void OpenSubmissionGuidelines()
        {
            Application.OpenURL("https://assetstore.unity.com/publishing/submission-guidelines/");
        }

        [MenuItem("Asset Store Tools v2/Provide Feedback", false, 50)]
        public static void OpenFeedback()
        {
            Application.OpenURL("https://forum.unity.com/threads/new-asset-store-tools-version-coming-july-20th-2022.1310939/");
        }
    }
}