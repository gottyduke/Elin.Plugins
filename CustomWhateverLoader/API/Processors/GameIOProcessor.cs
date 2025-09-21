﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Cwl.API.Attributes;
using Cwl.Helper;
using Cwl.Helper.FileUtil;
using Cwl.Helper.String;
using Cwl.LangMod;
using HarmonyLib;
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

    public static GameIOContext? GetPersistentModContext(string dirName)
    {
        if (dirName.IsInvalidPath()) {
            return null;
        }

        return new(Path.Combine(Application.persistentDataPath, dirName));
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
    public class GameIOContext
    {
        private const string Storage = "chunks";
        private const string ChunkExt = ".chunk";
        private const string BinaryChunkExt = ".chunkb";
        private const string CompressedChunkExt = ".chunkc";
        private readonly DirectoryInfo _chunkDir;

        private readonly string _path;

        public GameIOContext(string path)
        {
            if (path.IsInvalidPath()) {
                throw new ArgumentException($"{path} is invalid");
            }

            _path = path;
            _chunkDir = new(Path.Combine(path, Storage));

            _chunkDir.Create();
        }

        public FileInfo? GetChunkFile(string chunkName)
        {
            return _chunkDir.GetFiles($"{chunkName}.*")
                .OrderByDescending(f => f.LastWriteTime)
                .LastOrDefault();
        }

        /// <summary>
        ///     serialize data into current context, compressed
        /// </summary>
        /// <param name="data">arbitrary data</param>
        /// <param name="chunkName">unique identifier, omit/null will use full qualified type name</param>
        public void Save<T>(T data, string? chunkName = null)
        {
            SaveImpl(data, chunkName, CompressedChunkExt, ConfigCereal.WriteDataCompressed);
        }

        /// <summary>
        ///     serialize data to current context, binary format
        /// </summary>
        /// <param name="data">arbitrary data</param>
        /// <param name="chunkName">unique identifier, omit/null will use full qualified type name</param>
        [Obsolete("use Save instead")]
        public void SaveBinary<T>(T data, string? chunkName = null)
        {
            SaveImpl(data, chunkName, BinaryChunkExt, ConfigCereal.WriteDataBinary);
        }

        /// <summary>
        ///     serialize data to current context, uncompressed
        /// </summary>
        /// <param name="data">arbitrary data</param>
        /// <param name="chunkName">unique identifier, omit/null will use full qualified type name</param>
        public void SaveUncompressed<T>(T data, string? chunkName = null)
        {
            SaveImpl(data, chunkName, ChunkExt, ConfigCereal.WriteData);
        }

        /// <summary>
        ///     deserialize data from current context, use the latest chunk automatically
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

            var file = GetChunkFile(chunkName);
            if (file is null) {
                CwlMod.Warn<GameIOContext>($"chunk {chunkName} does not exist");
                return false;
            }

            switch (file.Extension) {
                case ChunkExt:
                    ConfigCereal.ReadData(file.FullName, out inferred);
                    break;
                case BinaryChunkExt:
                    ConfigCereal.ReadDataBinary(file.FullName, out inferred);
                    break;
                case CompressedChunkExt:
                    ConfigCereal.ReadDataCompressed(file.FullName, out inferred);
                    break;
            }

            if (inferred is not null) {
                CwlMod.Log<GameIOContext>($"load {file.ShortPath()}");
            }

            return inferred is not null;
        }

        /// <summary>
        ///     remove a chunk
        /// </summary>
        /// <param name="chunkName">unique identifier</param>
        /// <returns>bool indicating removal success</returns>
        public bool Remove(string chunkName)
        {
            try {
                _chunkDir.GetFiles($"{chunkName}.*")
                    .Do(f => f.Delete());

                return true;
            } catch (Exception ex) {
                CwlMod.Warn<GameIOContext>($"failed to remove chunk {chunkName} // {_path}\n{ex.Message}");
                return false;
                // noexcept
            }
        }

        /// <summary>
        ///     clear all chunks
        /// </summary>
        public void Clear()
        {
            try {
                _chunkDir.Delete(true);
            } catch {
                // noexcept
            }
        }

        private void SaveImpl<T>(T data, string? chunkName, string ext, Action<T, string> writer)
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

            var file = Path.Combine(_chunkDir.FullName, $"{chunkName}{ext}");
            writer(data, file);

            CwlMod.Log<GameIOContext>($"save {file.ShortPath()}");
        }
    }
}