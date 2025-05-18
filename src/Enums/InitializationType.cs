using Intellenum;

namespace Soenneker.Utils.SingletonDictionary.Enums;

[Intellenum<string>]
public partial class InitializationType
{
    public static readonly InitializationType AsyncKey = new(nameof(AsyncKey));
    public static readonly InitializationType AsyncKeyToken = new(nameof(AsyncKeyToken));
    public static readonly InitializationType Async = new(nameof(Async));
    public static readonly InitializationType Sync = new(nameof(Sync));
    public static readonly InitializationType SyncKey = new(nameof(SyncKey));
    public static readonly InitializationType SyncKeyToken = new(nameof(SyncKeyToken));
}