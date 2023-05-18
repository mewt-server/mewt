/******************************************************************************
    Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>            
    Mewt is licensed under the terms of the AGPL-3.0-only license.             
    See <https://github.com/mewt-server/mewt> or README.md file for details.   
******************************************************************************/

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Mewt.Server.Configuration;

namespace Mewt.Server.Paths;

public class MewtPaths : IReadOnlyDictionary<PathUsage, IMewtPath>
{
    public required IMewtPath Apis { get; init; }
    public required IMewtPath Assets { get; init; }
    public required IMewtPath Contents { get; init; }
    public required IMewtPath Metadata { get; init; }
    public required IMewtPath Pages { get; init; }
    public required IMewtPath Private { get; init; }
    public required IMewtPath Public { get; init; }
    public required IMewtPath Templates { get; init; }

    private IDictionary<PathUsage, IMewtPath>? _paths;

    private IDictionary<PathUsage, IMewtPath> Init()
        => _paths = new Dictionary<PathUsage, IMewtPath>()
                    {
                        { PathUsage.Apis, this.Apis },
                        { PathUsage.Assets, this.Assets },
                        { PathUsage.Contents, this.Contents },
                        { PathUsage.Metadata, this.Metadata },
                        { PathUsage.Pages, this.Pages },
                        { PathUsage.Private, this.Private },
                        { PathUsage.Public, this.Public },
                        { PathUsage.Templates, this.Templates },
                    };

    private IDictionary<PathUsage, IMewtPath> Paths => _paths ?? Init();

    public IEnumerable<PathUsage> Keys => Paths.Keys;

    public IEnumerable<IMewtPath> Values => Paths.Values;

    public int Count => Paths.Count;

    public IMewtPath this[PathUsage key] => Paths[key];

    public bool ContainsKey(PathUsage key)
        => Paths.ContainsKey(key);

    public bool TryGetValue(PathUsage key, [MaybeNullWhen(false)] out IMewtPath value)
        => Paths.TryGetValue(key, out value);


    public IEnumerator<KeyValuePair<PathUsage, IMewtPath>> GetEnumerator()
        => Paths.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => Paths.GetEnumerator();
}