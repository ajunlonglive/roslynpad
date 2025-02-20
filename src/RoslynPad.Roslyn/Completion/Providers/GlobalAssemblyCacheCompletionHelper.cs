using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace RoslynPad.Roslyn.Completion.Providers;

internal sealed class GlobalAssemblyCacheCompletionHelper
{
    private static readonly Lazy<List<string>> s_lazyAssemblySimpleNames =
         new(() => GlobalAssemblyCache.Instance.GetAssemblySimpleNames().ToList());

    private readonly CompletionItemRules _itemRules;

    public GlobalAssemblyCacheCompletionHelper(CompletionItemRules itemRules)
    {
        _itemRules = itemRules;
    }

    public Task<ImmutableArray<CompletionItem>> GetItemsAsync(string directoryPath, CancellationToken cancellationToken)
    {
        return Task.Run(() => GetItems(directoryPath, cancellationToken));
    }

    // internal for testing
    internal ImmutableArray<CompletionItem> GetItems(string directoryPath, CancellationToken cancellationToken)
    {
        var result = ArrayBuilder<CompletionItem>.GetInstance();

        var comma = directoryPath.IndexOf(',');
        if (comma >= 0)
        {
            var partialName = directoryPath.Substring(0, comma);
            foreach (var identity in GetAssemblyIdentities(partialName))
            {
                result.Add(CommonCompletionItem.Create(
                    identity.GetDisplayName(), "", glyph: Microsoft.CodeAnalysis.Glyph.Assembly, rules: _itemRules));
            }
        }
        else
        {
            foreach (var displayName in s_lazyAssemblySimpleNames.Value)
            {
                cancellationToken.ThrowIfCancellationRequested();
                result.Add(CommonCompletionItem.Create(
                    displayName, "", glyph: Microsoft.CodeAnalysis.Glyph.Assembly, rules: _itemRules));
            }
        }

        return result.ToImmutableAndFree();
    }

    private IEnumerable<AssemblyIdentity> GetAssemblyIdentities(string partialName)
    {
        return IOUtilities.PerformIO(() => GlobalAssemblyCache.Instance.GetAssemblyIdentities(partialName),
            SpecializedCollections.EmptyEnumerable<AssemblyIdentity>());
    }
}
