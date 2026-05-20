// Partial class file #2: defines the handler in a DIFFERENT syntax tree.
// This is the key to the repro: RoutePatternHighlighter calls
//   semanticModel.GetSymbolInfo(identifier)
// passing identifier nodes from this file, while `semanticModel` was created
// for the syntax tree of XEndpoints.Map.cs above. Roslyn throws:
//   System.ArgumentException: Syntax node is not within syntax tree
//     at Microsoft.CodeAnalysis.CSharp.CSharpSemanticModel.CheckSyntaxNode
//
// See: src/Framework/AspNetCoreAnalyzers/src/Analyzers/RouteEmbeddedLanguage/
//      RoutePatternHighlighter.cs -> HighlightSymbol(...)
//
//   foreach (var item in methodSymbol.DeclaringSyntaxReferences)
//   {
//       var methodSyntax = item.GetSyntax(cancellationToken); // <- other tree
//       var parameterReferences = methodSyntax
//           .DescendantNodes()
//           .OfType<IdentifierNameSyntax>()
//           .Where(i => i.Identifier.Text == matchingParameter.Name)
//           .Where(i => semanticModel.GetSymbolInfo(i) ...); // <- BOOM
//   }
//
// The fix is to obtain the correct SemanticModel for `methodSyntax.SyntaxTree`
// from `semanticModel.Compilation.GetSemanticModel(methodSyntax.SyntaxTree)`
// before calling GetSymbolInfo.

public static partial class XEndpoints
{
    public static Task<IResult> LoadAsync(
        string organizationId,
        string yId,
        string zId,
        string wId)
    {
        // Reference the parameters so they're not unused; the highlighter
        // walks these IdentifierNameSyntax nodes via GetSymbolInfo.
        var combined = $"{organizationId}/{yId}/{zId}/{wId}";
        return Task.FromResult(Results.Ok(combined));
    }
}
