using System;

namespace Cwl.API.Drama;

public class DramaEventDynamic(DramaEvent baseEvent) : DramaEvent
{
    public required Func<bool> enableIf;

    public override bool Play()
    {
        return !enableIf() || baseEvent.Play();
    }
}