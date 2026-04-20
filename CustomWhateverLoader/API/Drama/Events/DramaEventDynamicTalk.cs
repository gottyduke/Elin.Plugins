using System;

namespace Cwl.API.Drama;

public class DramaEventDynamicTalk(DramaEventTalk baseTalk) : DramaEventTalk
{
    public required Func<bool> enableIf;

    public override bool Play()
    {
        if (enableIf()) {
            baseTalk.Play();
        }
        return true;
    }
}