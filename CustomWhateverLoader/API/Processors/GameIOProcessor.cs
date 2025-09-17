using System;
using System.IO;
using System.Reflection;
using Cwl.API.Attributes;
using Cwl.Helper;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.LangMod;
using MethodTimer;
using UnityEngine;

namespace Cwl.API.Processors;

/// <summary>
///     event raised when game serializes and deserializes
/// </summary>
public class GameIOProcessor
{
    public static GameIOContext? LastUsedContext { get; private set; }
    public static GameIOContext? PersistentContext => field ??= new(Application.persistentDataPath);

    private static event Action<GameIOContext?>? OnGamePreSaveProcess;
    private static event Action<GameIOContext?>? OnGamePostSaveProcess;
    private static event Action<GameIOContext?>? OnGamePreLoadProcess;
    private static event Action<GameIOContext?>? OnGamePostLoadProcess;

    public static void AddSave(Action<GameIOContext?> saveProcess, bool post)
    {
        Add(saveProcess, true, post);
    }

    public static void AddLoad(Action<GameIOContext?> loadProcess, bool post)
    {
        Add(loadProcess, false, post);
    }

    public static GameIOContext? GetPersistentModContext(string modId)
    {
        if (modId.IsInvalidPath()) {
            return null;
        }

        return new(Path.Combine(Application.persistentDataPath, modId));
    }

    private static void Add(Action<GameIOContext?> ioProcess, bool save, bool post)
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

        void Process(GameIOContext? context)
        {
            try {
                ioProcess(context);
            } catch (Exception ex) {
                var state = post ? "post" : "pre";
                var type = save ? "save" : "load";
                CwlMod.WarnWithPopup<GameIOProcessor>("cwl_warn_processor".Loc(state, type, ex.Message), ex);
                // noexcept
            }
        }
    }

    internal static void Save(string path, bool post)
    {
        LastUsedContext = new(path);
        if (post) {
            OnGamePostSaveProcess?.Invoke(LastUsedContext);
        } else {
            OnGamePreSaveProcess?.Invoke(LastUsedContext);
        }
    }

    internal static void Load(string path, bool post)
    {
        LastUsedContext = new(path);
        if (post) {
            OnGamePostLoadProcess?.Invoke(LastUsedContext);
        } else {
            OnGamePreLoadProcess?.Invoke(LastUsedContext);
        }
    }

    [Time]
    internal static void RegisterEvents(MethodInfo method, CwlGameIOEvent io)
    {
        var (save, post) = io switch {
            CwlPreLoad => (false, false),
            CwlPostLoad => (false, true),
            CwlPreSave => (true, false),
            CwlPostSave => (true, true),
            _ => throw new NotImplementedException(io.GetType().Name),
        };

        Add(ctx => method.FastInvokeStatic(ctx!), save, post);

        var state = post ? "post" : "pre";
        var type = save ? "save" : "load";
        CwlMod.Log<GameIOContext>("cwl_log_processor_add".Loc(state, type, method.GetAssemblyDetail(false)));
    }

    /// <summary>
    ///     helper class given as event arg to save/load custom data to game save
    /// </summary>
    public class GameIOContext(string path)
    {
        private const string Storage = "chunks";
        private const string ChunkExt = "chunk";
        private const string CompressedChunkExt = "chunkc";

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

            if (chunkName.IsInvalidPath()) {
                CwlMod.Warn<GameIOContext>($"invalid chunk {chunkName}");
                return;
            }

            var file = Path.Combine(path, Storage, $"{chunkName}.{CompressedChunkExt}");
            ConfigCereal.WriteDataCompressed(data, file);

            CwlMod.Log<GameIOContext>($"save {file.ShortPath()}");
        }

        /// <summary>
        ///     serialize data to current save folder, uncompressed
        /// </summary>
        /// <param name="data">arbitrary data</param>
        /// <param name="chunkName">unique identifier, omit/null will use full qualified type name</param>
        public void SaveUncompressed<T>(T data, string? chunkName = null)
        {
            if (data is null) {
                return;
            }

            var type = typeof(T);
            if (data is IChunkable chunk) {
                chunkName = chunk.ChunkName;
            }

            chunkName ??= $"{type.Assembly.GetName().Name}.{type.FullName ?? type.Name}";

            if (chunkName.IsInvalidPath()) {
                CwlMod.Warn<GameIOContext>($"invalid chunk {chunkName}");
                return;
            }

            var file = Path.Combine(path, Storage, $"{chunkName}.{ChunkExt}");
            ConfigCereal.WriteData(data, file);

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
            inferred = default;

            var type = typeof(T);
            chunkName ??= $"{type.Assembly.GetName().Name}.{type.FullName ?? type.Name}";

            if (chunkName.IsInvalidPath()) {
                CwlMod.Warn<GameIOContext>($"invalid chunk {chunkName}");
                return false;
            }

            var file = Path.Combine(path, Storage, chunkName);
            var chunkc = new FileInfo($"{file}.{CompressedChunkExt}");
            var legacy = new FileInfo($"{file}.{ChunkExt}");

            if (!chunkc.Exists || chunkc.LastWriteTime < legacy.LastWriteTime ||
                !ConfigCereal.ReadData(chunkc.FullName, out inferred)) {
                ConfigCereal.ReadConfig(legacy.FullName, out inferred);
            }

            if (inferred is not null) {
                CwlMod.Log<GameIOContext>($"load {file.ShortPath()}");
            }

            return inferred is not null;
        }

        /// <summary>
        ///     Remove a chunk
        /// </summary>
        /// <param name="chunkName">unique identifier</param>
        /// <returns>bool indicating removal success</returns>
        public bool Remove(string chunkName)
        {
            var file = Path.Combine(path, Storage, chunkName);
            if (file.IsInvalidPath()) {
                return false;
            }

            try {
                var chunkc = new FileInfo($"{file}.{CompressedChunkExt}");
                chunkc.Delete();

                var legacy = new FileInfo($"{file}.{ChunkExt}");
                legacy.Delete();

                CwlMod.Log<GameIOContext>($"remove {file.ShortPath()}");
                return true;
            } catch (Exception ex) {
                CwlMod.Warn<GameIOContext>($"failed to remove chunk {chunkName} // {file.ShortPath()}\n{ex.Message}");
                return false;
                // noexcept
            }
        }
    }
}