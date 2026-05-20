# Repro for [dotnet/aspnetcore#64398](https://github.com/dotnet/aspnetcore/issues/64398)

> **Document highlights: Syntax node is not within syntax tree**

The ASP.NET Core `RoutePatternHighlighter` throws
`System.ArgumentException: Syntax node is not within syntax tree` when the
caret is moved onto a route parameter name in a `Map*` call whose handler
method group is defined in a **different file** (for example, another file of
the same partial class).

## Repro steps

1. Install Visual Studio 2026 Insiders **18.6.0 [11617.104.main]** (or
   **11206.111**, per the original report) or newer, and the .NET 10 SDK.
2. Clone this repo and open `Repro64398.csproj` in VS.
3. Open `XEndpoints.Map.cs`.
4. Place the caret inside any of the route parameter names in the route
   template (e.g. `organizationId`, `yId`, `zId`, `wId`) on this line:

   ```csharp
   app.MapPost("/organizations/{organizationId}/y/{yId}/z/{zId}/w/{wId}/load", LoadAsync);
   ```

5. A message bar appears:

   > Feature 'Document highlights' is currently unavailable due to an internal
   > error.

   And the activity log / output window contains:

   ```
   StreamJsonRpc.RemoteInvocationException: Syntax node is not within syntax tree
   ...
   System.ArgumentException: Syntax node is not within syntax tree
      at Microsoft.CodeAnalysis.CSharp.CSharpSemanticModel.CheckSyntaxNode
      at Microsoft.CodeAnalysis.CSharp.CSharpSemanticModel.GetSymbolInfo
      at Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage
          .RoutePatternHighlighter.<>c__DisplayClass2_0.<HighlightSymbol>b__1
      at Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage
          .RoutePatternHighlighter.HighlightSymbol(...)
      at Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage
          .RoutePatternHighlighter.GetHighlights(...)
   ```

## Why two files matter

`XEndpoints.Map.cs` and `XEndpoints.Load.cs` declare two parts of the same
`partial class XEndpoints`:

| File                   | Contents                                  |
| ---------------------- | ----------------------------------------- |
| `XEndpoints.Map.cs`    | `app.MapPost("/.../{organizationId}/...", LoadAsync);` |
| `XEndpoints.Load.cs`   | `public static Task<IResult> LoadAsync(string organizationId, ...)` |

Inlining `LoadAsync` into `XEndpoints.Map.cs` (or making `LoadAsync` a
lambda) makes the error go away. The bug only surfaces when the handler's
syntax lives in a tree other than the route string's tree.

## Root cause

In [`RoutePatternHighlighter.HighlightSymbol`](https://github.com/dotnet/aspnetcore/blob/main/src/Framework/AspNetCoreAnalyzers/src/Analyzers/RouteEmbeddedLanguage/RoutePatternHighlighter.cs):

```csharp
foreach (var item in methodSymbol.DeclaringSyntaxReferences)
{
    var methodSyntax = item.GetSyntax(cancellationToken);   // ← lives in handler file's tree
    var parameterReferences = methodSyntax
        .DescendantNodes()
        .OfType<IdentifierNameSyntax>()
        .Where(i => i.Identifier.Text == matchingParameter.Name)
        .Where(i => semanticModel.GetSymbolInfo(i) ...);    // ← semanticModel is for route file's tree → throws
}
```

`semanticModel` is bound to the route literal's syntax tree, but the
identifier nodes come from a different tree. Roslyn's
`CSharpSemanticModel.CheckSyntaxNode` rejects this with
`ArgumentException: Syntax node is not within syntax tree`.

A fix is to obtain the right `SemanticModel` per tree:

```csharp
var methodSemanticModel = methodSyntax.SyntaxTree == semanticModel.SyntaxTree
    ? semanticModel
    : semanticModel.Compilation.GetSemanticModel(methodSyntax.SyntaxTree);
```

…and use `methodSemanticModel.GetSymbolInfo(i)` in the `.Where` clause.
