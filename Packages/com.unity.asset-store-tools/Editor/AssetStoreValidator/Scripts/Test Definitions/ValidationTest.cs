using System;
using System.Threading.Tasks;

namespace AssetStoreTools.Validator
{
    public abstract class ValidationTest
    {
        public int Id;
        public string Title;
        public string Description;
        public string TestMethodName;
        public TestResult Result;

        public event Action<int, TestResult> OnTestComplete;

        protected ValidationTest(ValidationTestScriptableObject source)
        {
            Id = source.Id;
            Title = source.Title;
            Description = source.Description;
            TestMethodName = source.TestMethodName;

            Result = new TestResult();
        }

        public abstract void Run();

        protected void OnTestCompleted()
        {
            OnTestComplete?.Invoke(Id, Result);
        }
    }
}
