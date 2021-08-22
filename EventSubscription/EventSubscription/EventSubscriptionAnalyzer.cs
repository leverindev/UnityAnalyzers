using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EventSubscription
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EventSubscriptionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "EventSubscription";

        private static readonly LocalizableString Title = "Subscribe without unsubscribe";
        private static readonly LocalizableString MessageFormat = "Subscribe without unsubscribe";
        private const string Category = "Subscriptions";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxTreeAction(AnalyzeSubscriptions);
        }

        private void AnalyzeSubscriptions(SyntaxTreeAnalysisContext context)
        {
            var subscribe = new HashSet<string>();
            var unsubscribe = new HashSet<string>();
            var locations = new Dictionary<string, Location>();

            AnalyzeNode(context.Tree.GetRoot(), subscribe, unsubscribe, locations);

            foreach (var method in subscribe)
            {
                if (unsubscribe.Contains(method))
                {
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(Rule, locations[method]));
            }
        }

        private void AnalyzeNode(
            SyntaxNode node,
            ISet<string> subscribe,
            ISet<string> unsubscribe,
            IDictionary<string, Location> locations)
        {
            if (node is AssignmentExpressionSyntax typedNode)
            {
                var kind = (SyntaxKind) typedNode.RawKind;
                if (kind == SyntaxKind.AddAssignmentExpression || kind == SyntaxKind.SubtractAssignmentExpression)
                {
                    if (typedNode.Right is IdentifierNameSyntax right)
                    {
                        var value = right.Identifier.Text;
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            var op = typedNode.OperatorToken.Text;

                            if (op == "+=" && !subscribe.Contains(value))
                            {
                                subscribe.Add(value);
                                locations.Add(value, typedNode.GetLocation());
                            }
                            else if (op == "-=" && !unsubscribe.Contains(value))
                            {
                                unsubscribe.Add(value);
                            }
                        }
                    }
                }
            }

            foreach (var childNode in node.ChildNodes())
            {
                AnalyzeNode(childNode, subscribe, unsubscribe, locations);
            }
        }
    }
}
