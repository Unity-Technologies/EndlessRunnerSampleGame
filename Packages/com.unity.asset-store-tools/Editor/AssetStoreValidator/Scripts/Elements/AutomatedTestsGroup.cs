using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace AssetStoreTools.Validator
{
    public class AutomatedTestsGroup : VisualElement
    {
        private const string TestsPath = "Packages/com.unity.asset-store-tools/Editor/AssetStoreValidator/Tests";
        
        private readonly Dictionary<int, AutomatedTestElement> _testElements = new Dictionary<int, AutomatedTestElement>();
        private readonly Dictionary<TestResult.ResultStatus, AutomatedTestsGroupElement> _testGroupElements = 
            new Dictionary<TestResult.ResultStatus, AutomatedTestsGroupElement>();

        private List<AutomatedTest> _automatedTests = new List<AutomatedTest>();
        
        private ScrollView _allTestsScrollView;
        private ValidationInfoElement _validationInfoBox;
        private PathBoxElement _pathBox;
        private Button _validateButton;

        private static readonly TestResult.ResultStatus[] StatusOrder = {TestResult.ResultStatus.Undefined, 
            TestResult.ResultStatus.Fail, TestResult.ResultStatus.Warning, TestResult.ResultStatus.Pass};
        
        public AutomatedTestsGroup()
        {
            ConstructInfoPart();
            ConstructAutomatedTests();

            ValidationState.Instance.OnJsonSave -= Reinitialize;
            ValidationState.Instance.OnJsonSave += Reinitialize;
        }

        private void Reinitialize()
        {
            this.Clear();
            _testElements.Clear();
            _testGroupElements.Clear();
            _automatedTests.Clear();

            ConstructInfoPart();
            ConstructAutomatedTests();
        }

        private void ConstructInfoPart()
        {
            _validationInfoBox = new ValidationInfoElement();
            _pathBox = new PathBoxElement();

            var mainPath = ValidationState.Instance.ValidationStateData.SerializedMainPath;
            _pathBox.SetPathBoxValue(string.IsNullOrEmpty(mainPath) ? "Assets" : mainPath);

            _validateButton = new Button(RunAllTests) {text = "Validate"};
            _validateButton.AddToClassList("run-all-button");

            _validationInfoBox.Add(_pathBox);
            _validationInfoBox.Add(_validateButton);

            Add(_validationInfoBox);
        }

        private void ConstructAutomatedTests()
        {
            name = "AutomatedTests";

            _allTestsScrollView = new ScrollView
            {
                viewDataKey = "scrollViewKey",
            };
            _allTestsScrollView.AddToClassList("tests-scroll-view");

            _automatedTests = CreateAutomatedTestCases();
            var groupedTests = GroupTestsByStatus(_automatedTests);

            foreach (var status in StatusOrder)
            {
                var group = new AutomatedTestsGroupElement(status.ToString(), status, true);
                _testGroupElements.Add(status, group);
                _allTestsScrollView.Add(group);
                
                if (!groupedTests.ContainsKey(status))
                    continue;
                
                foreach (var test in groupedTests[status])
                {
                    var testElement = new AutomatedTestElement(test);
                    
                    _testElements.Add(test.Id, testElement);
                    group.AddTest(testElement);
                }
                
                if (StatusOrder[StatusOrder.Length - 1] != status)
                    group.AddSeparator();
            }

            Add(_allTestsScrollView);
        }

        private List<AutomatedTest> CreateAutomatedTestCases()
        {
            var testData = ValidatorUtility.GetAutomatedTestCases(TestsPath, true);
            var automatedTests = new List<AutomatedTest>();

            foreach (var t in testData)
            {
                var test = new AutomatedTest(t);
                
                if (!ValidationState.Instance.TestResults.ContainsKey(test.Id))
                    ValidationState.Instance.CreateTestContainer(test.Id);
                else
                    test.Result = ValidationState.Instance.TestResults[test.Id].Result;

                test.OnTestComplete += OnTestComplete;
                automatedTests.Add(test);
            }

            return automatedTests;
        }

        private Dictionary<TestResult.ResultStatus, List<AutomatedTest>> GroupTestsByStatus(List<AutomatedTest> tests)
        {
            var groupedDictionary = new Dictionary<TestResult.ResultStatus, List<AutomatedTest>>();
            
            foreach (var t in tests)
            {
                if (!groupedDictionary.ContainsKey(t.Result.Result))
                    groupedDictionary.Add(t.Result.Result, new List<AutomatedTest>());
                
                groupedDictionary[t.Result.Result].Add(t);
            }

            return groupedDictionary;
        }

        private async void RunAllTests()
        {
            ValidationState.Instance.SetMainPath(_pathBox.GetPathBoxValue());
            _validateButton.SetEnabled(false);

            // Make sure everything is collected and validation button is disabled
            await Task.Delay(100);
            
            foreach (var test in _automatedTests)
                test.Run();
            
            _validateButton.SetEnabled(true);
            ValidationState.Instance.SaveJson();
        }

        private void OnTestComplete(int id, TestResult result)
        {
            ValidationState.Instance.ChangeResult(id, result);
            
            var testElement = _testElements[id];
            var currentStatus = result.Result;
            var lastStatus = testElement.GetLastStatus();

            if (_testGroupElements.ContainsKey(lastStatus) && _testGroupElements.ContainsKey(currentStatus))
            {
                if (lastStatus != currentStatus)
                {
                    _testGroupElements[lastStatus].RemoveTest(testElement);
                    _testGroupElements[currentStatus].AddTest(testElement);
                }
            }

            testElement.ResultChanged();
        }
    }
}