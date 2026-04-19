using System;

namespace Cwl.API.Drama;

public class DramaEventDynamicTalk(DramaEventTalk baseTalk) : DramaEventTalk
{
    public required Func<bool> enableIf;

    public override bool Play()
    {
        return !enableIf() || baseTalk.Play();
    }
}