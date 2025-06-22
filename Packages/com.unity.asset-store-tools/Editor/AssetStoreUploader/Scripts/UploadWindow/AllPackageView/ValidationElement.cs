using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AssetStoreTools.Validator;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AssetStoreTools.Uploader
{
    public class ValidationElement : VisualElement
    {
        private Button _validateButton;
        private Button _viewReportButton;
        
        private VisualElement _infoBox;
        private Label _infoBoxLabel;
        private Image _infoBoxImage;

        private string _localPath;
        
        public ValidationElement()
        {
            ConstructValidationElement();
            EnableValidation(false);
        }

        public void SetLocalPath(string path)
        {
            _localPath = path;
            
            EnableValidation(true);
        }

        private void ConstructValidationElement()
        {
            VisualElement validatorButtonRow = new VisualElement();
            validatorButtonRow.AddToClassList("selection-box-row");

            VisualElement validatorLabelHelpRow = new VisualElement();
            validatorLabelHelpRow.AddToClassList("label-help-row");

            Label validatorLabel = new Label { text = "Validation" };
            Image validatorLabelTooltip = new Image
            {
                tooltip = "You can use the Asset Store Validator to check your package for common publishing issues"
            };
            
            _validateButton = new Button(GetOutcomeResults) { name = "ValidateButton", text = "Validate" };
            _validateButton.AddToClassList("validation-button");
            
            validatorLabelHelpRow.Add(validatorLabel);
            validatorLabelHelpRow.Add(validatorLabelTooltip);

            validatorButtonRow.Add(validatorLabelHelpRow);
            validatorButtonRow.Add(_validateButton);

            Add(validatorButtonRow);

            SetupInfoBox("");
        }
        
        private async void GetOutcomeResults()
        {
            TestActions testActions = TestActions.Instance;
            testActions.SetMainPath(_localPath);
            
            ValidationState.Instance.SetMainPath(_localPath);
            _validateButton.SetEnabled(false);

            var testsPath = "Packages/com.unity.asset-store-tools/Editor/AssetStoreValidator/Tests";
            var testObjects = ValidatorUtility.GetAutomatedTestCases(testsPath, true);
            var automatedTests = testObjects.Select(t => new AutomatedTest(t)).ToList();

            // Make sure everything is collected and validation button is disabled
            await Task.Delay(100);
            
            var outcomeList = new List<TestResult>();
            foreach (var test in automatedTests)
            {
                try
                {
                    test.Run();
                    ValidationState.Instance.ChangeResult(test.Id, test.Result);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return;
                }

                outcomeList.Add(test.Result);
            }
            
            EnableInfoBox(true, outcomeList);
            _validateButton.SetEnabled(true);
            
            ValidationState.Instance.SaveJson();
        }
        
        private void EnableValidation(bool enable)
        {
            style.display = enable ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void EnableInfoBox(bool enable, List<TestResult> outcomeList)
        {
            if (!enable)
            {
                _infoBox.style.display = DisplayStyle.None;
                return;
            }
            
            var errorCount = outcomeList.Count(x => x.Result == TestResult.ResultStatus.Fail);
            var warningCount = outcomeList.Count(x => x.Result == TestResult.ResultStatus.Warning);
            var passCount = outcomeList.Count(x => x.Result == TestResult.ResultStatus.Pass);
            
            _infoBox.Q<Label>().text = $"{errorCount} errors, {warningCount} warnings, {passCount} passed";

            if (errorCount > 0)
                _infoBoxImage.image = EditorGUIUtility.IconContent("console.erroricon@2x").image;
            else if (warningCount > 0)
                _infoBoxImage.image = EditorGUIUtility.IconContent("console.warnicon@2x").image;

            _validateButton.text = "Re-validate";
            _infoBox.style.display = DisplayStyle.Flex;
        }
        
        private void SetupInfoBox(string infoText)
        {
            _infoBox = new Box { name = "InfoBox" };
            _infoBox.style.display = DisplayStyle.None;
            _infoBox.AddToClassList("info-box");

            _infoBoxImage = new Image();
            _infoBoxLabel = new Label { text = infoText };
            _viewReportButton = new Button (ViewReport) {text = "View report"};
            _viewReportButton.AddToClassList("hyperlink-button");
            
            _infoBox.Add(_infoBoxImage);
            _infoBox.Add(_infoBoxLabel);
            _infoBox.Add(_viewReportButton);

            Add(_infoBox);
        }

        private void ViewReport()
        {
            // Re-run validation if it is out of sync
            if (ValidationState.Instance.ValidationStateData.SerializedMainPath != _localPath)
                GetOutcomeResults();
            
            // Show the Validator
            AssetStoreTools.ShowAssetStoreToolsValidator();
        }
    }
}