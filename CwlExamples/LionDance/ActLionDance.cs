using System.Collections.Generic;

namespace LionDance;

// the class name must match the type cell of the ability
internal class ActLionDance : AI_Dance
{
    // depending on the derived class, you may override any method
    public override IEnumerable<Status> Run()
    {
        // say a string defined in LangGame, which plays doodle sound
        owner.Say("act_liondance_trigger");
        // then do the base AI_Dance.Run()
        return base.Run();
    }
}