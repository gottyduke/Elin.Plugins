using System;

namespace Cwl.API.Drama;

public class DramaEventDynamic(DramaEvent baseEvent) : DramaEvent
{
    public required Func<bool> enableIf;

    public override bool Play()
    {
        if (enableIf()) {
            baseEvent.Play();
        }
        return true;
    }
}