using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Cwl.Helper.FileUtil;
using Cwl.Helper.Runtime;
using Cwl.Helper.String;
using Cwl.LangMod;

namespace Cwl.API.Processors;

/// <summary>
///     event raised when game serializes and deserializes
/// </summary>
public class GameIOProcessor
{
    public delegate void GameIOProcess(GameIOContext context);

    public static GameIOContext? LastUsedContext { get; private set; }

    private static event GameIOProcess OnGamePreSaveProcess = delegate { };
    private static event GameIOProcess OnGamePostSaveProcess = delegate { };
    private static event GameIOProcess OnGamePreLoadProcess = delegate { };
    private static event GameIOProcess OnGamePostLoadProcess = delegate { };

    public static void AddSave(GameIOProcess saveProcess, bool post)
    {
        Add(saveProcess, true, post);
    }

    public static void AddLoad(GameIOProcess loadProcess, bool post)
    {
        Add(loadProcess, false, post);
    }

    private static void Add(GameIOProcess ioProcess, bool save, bool post)
    {
        switch (save, post) {
            case (true, true):
                OnGamePostSaveProcess += Process;
                return;
            case (true, false):
                OnGamePreSaveProcess += Process;
                return;
            case (false, true):
                OnGamePostLoadProcess += Process;
                return;
            case (false, false):
                OnGamePreLoadProcess += Process;
                return;
        }

        void Process(GameIOContext context)
        {
            try {
                ioProcess(context);
            } catch (Exception ex) {
                var type = save ? "save" : "load";
                var state = post ? "post" : "pre";
                CwlMod.Warn<GameIOProcessor>("cwl_warn_processor".Loc(type, state, ex.Message));
                // noexcept
            }
        }
    }

    internal static void Save(string path, bool post)
    {
        LastUsedContext = new(path);
        if (post) {
            OnGamePostSaveProcess(LastUsedContext);
        } else {
            OnGamePreSaveProcess(LastUsedContext);
        }
    }

    internal static void Load(string path, bool post)
    {
        LastUsedContext = new(path);
        if (post) {
            OnGamePostLoadProcess(LastUsedContext);
        } else {
            OnGamePreLoadProcess(LastUsedContext);
        }
    }

    internal static void RegisterEvents()
    {
        var methods = (TypeQualifier.Plugins ?? [])
            .SelectMany(p => p.GetType().Assembly.DefinedTypes)
            .SelectMany(CachedMethods.GetCachedMethods)
            .Where(mi => mi.IsStatic && !mi.IsGenericMethod);

        foreach (var method in methods) {
            foreach (var attr in method.GetCustomAttributes<CwlGameIOEvent>(true)) {
                var (save, post) = attr switch {
                    CwlPreLoad => (false, false),
                    CwlPostLoad => (false, true),
                    CwlPreSave => (true, false),
                    CwlPostSave => (true, true),
                    _ => throw new NotImplementedException(attr.GetType().Name),
                };

                Add(ctx => method.FastInvokeStatic(ctx), save, post);
            }
        }
    }

    /// <summary>
    ///     helper class given as event arg to save/load custom data to game save
    /// </summary>
    public class GameIOContext(string path)
    {
        private const string Storage = "chunks";
        private const string Extension = "chunk";

        /// <summary>
        ///     serialize data to current save folder
        /// </summary>
        /// <param name="data">arbitrary data</param>
        /// <param name="chunkName">unique identifier, omit/null will use full qualified type name</param>
        public void Save<T>(T data, string? chunkName = null)
        {
            if (data is null) {
                return;
            }

            var type = typeof(T);
            if (data is IChunkable chunk) {
                chunkName = chunk.ChunkName;
            }

            chunkName ??= $"{type.Assembly.GetName().Name}.{type.FullName ?? type.Name}";

            var file = Path.Combine(path, Storage, $"{chunkName}.{Extension}");
            ConfigCereal.WriteConfig(data, file);

            CwlMod.Log<GameIOContext>($"save {file.ShortPath()}");
        }

        /// <summary>
        ///     deserialize data from current save folder
        /// </summary>
        /// <param name="inferred">arbitrary data, default(null) for class type and default({}) for value type</param>
        /// <param name="chunkName">unique identifier, omit/null will use full qualified type name</param>
        /// <returns>bool indicating success</returns>
        public bool Load<T>(out T? inferred, string? chunkName = null)
        {
            var type = typeof(T);
            chunkName ??= $"{type.Assembly.GetName().Name}.{type.FullName ?? type.Name}";

            var file = Path.Combine(path, Storage, $"{chunkName}.{Extension}");
            ConfigCereal.ReadConfig(file, out inferred);

            CwlMod.Log<GameIOContext>($"load {file.ShortPath()}");
            return inferred is not null;
        }
    }
}