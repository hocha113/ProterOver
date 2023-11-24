using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


class RecursiveCallAnalyzer : CSharpSyntaxWalker
{
    private List<AnalysisIssue> issues = new List<AnalysisIssue>();
    private string currentFile;

    public List<AnalysisIssue> GetIssues()
    {
        return issues;
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        currentFile = node.SyntaxTree.FilePath;

        // 检查方法体中是否存在递归调用
        var methodBody = node.DescendantNodesAndSelf().OfType<BlockSyntax>().FirstOrDefault();
        if (methodBody != null)
        {
            foreach (var invocation in methodBody.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var invokedMethodName = invocation.Expression.ToString();
                if (invokedMethodName == node.Identifier.Text)
                {
                    issues.Add(new AnalysisIssue
                    {
                        FilePath = currentFile,
                        LineNumber = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }
            }
        }

        base.VisitMethodDeclaration(node);
    }
}

class AnalysisIssue
{
    public string FilePath { get; set; }
    public int LineNumber { get; set; }
}

