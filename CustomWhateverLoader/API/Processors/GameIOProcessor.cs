using System;
using System.IO;
using Cwl.LangMod;
using Cwl.Loader;

namespace Cwl.API.Processors;

public class GameIOProcessor
{
    public delegate void GameIOProcess(GameIOContext context);

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
                CwlMod.Warn("cwl_warn_processor".Loc(type, state, ex.Message));
                // noexcept
            }
        }
    }

    internal static void Save(string path, bool post)
    {
        var context = new GameIOContext(path);
        if (post) {
            OnGamePostSaveProcess(context);
        } else {
            OnGamePreSaveProcess(context);
        }
    }

    internal static void Load(string path, bool post)
    {
        var context = new GameIOContext(path);
        if (post) {
            OnGamePostLoadProcess(context);
        } else {
            OnGamePreLoadProcess(context);
        }
    }

    public class GameIOContext(string path)
    {
        private const string Storage = "chunks";
        private const string Extension = ".chunk";

        public void Save<T>(T data, string? chunkName = null)
        {
            if (data is null) {
                return;
            }

            var type = typeof(T);
            chunkName ??= $"{type.Assembly.GetName().Name}.{type.FullName ?? type.Name}";

            var file = Path.Combine(path, Storage, $"{chunkName}.{Extension}");
            IO.SaveFile(file, data, GameIO.compressSave, GameIO.jsWriteGame);
        }

        public bool Load<T>(out T? inferred, string? chunkName = null)
        {
            var type = typeof(T);
            chunkName ??= $"{type.Assembly.GetName().Name}.{type.FullName ?? type.Name}";

            var file = Path.Combine(path, Storage, $"{chunkName}.{Extension}");
            if (!File.Exists(file)) {
                inferred = default;
                return false;
            }

            inferred = IO.LoadFile<T>(file, GameIO.compressSave, GameIO.jsReadGame);
            return inferred is not null;
        }
    }
}