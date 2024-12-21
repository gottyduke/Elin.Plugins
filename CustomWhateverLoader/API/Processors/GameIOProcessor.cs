using System;
using Cwl.LangMod;
using Cwl.Loader;

namespace Cwl.API.Processors;

public class GameIOProcessor
{
    public delegate void GameIOProcess(Game game);

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

    internal static void Add(GameIOProcess ioProcess, bool save, bool post)
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
        
        void Process(Game game)
        {
            try {
                ioProcess(game);
            } catch (Exception ex) {
                var type = save ? "save" : "load";
                var state = post ? "post" : "pre";
                CwlMod.Warn("cwl_warn_processor".Loc("game", type, state, ex.Message));
                // noexcept
            }
        }
    }

    internal static void Save(Game game, bool post)
    {
        if (post) {
            OnGamePostSaveProcess(game);
        } else {
            OnGamePreSaveProcess(game);
        }
    }
    
    internal static void Load(Game game, bool post)
    {
        if (post) {
            OnGamePostLoadProcess(game);
        } else {
            OnGamePreLoadProcess(game);
        }
    }
}