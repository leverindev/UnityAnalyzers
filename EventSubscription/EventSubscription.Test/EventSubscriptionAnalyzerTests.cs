using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace EventSubscription.Test
{
    [TestClass]
    public class EventSubscriptionAnalyzerTests : DiagnosticVerifier
    {
        [TestMethod]
        public void TestAnalyzer_HasUnsubscription_EmptyResult()
        {
            var test = @"
namespace Test
{
    public class TestClass
    {
        public event Action TestEvent;

        public TestClass()
        {
            TestEvent += OnTestEvent;
        }

        private void OnTestEvent()
        {
            TestEvent -= OnTestEvent;
        }
    }
}";

            VerifyCSharpDiagnostic(test, new DiagnosticResult[0]);
        }

        [TestMethod]
        public void TestAnalyzer_NoUnsubscriptions_HasResult()
        {
            var test = @"
namespace Test
{
    public class TestClass
    {
        public event Action TestEvent;

        public TestClass()
        {
            TestEvent += OnTestEvent;
        }

        private void OnTestEvent()
        {
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "EventSubscription",
                Message = $"Subscribe without unsubscribe",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 10, 13)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new EventSubscriptionAnalyzer();
        }
    }
}
