using System.Collections.Generic;

namespace KonoExt.Elements;

internal class SpKonoExplosion : ActBall
{
    public override bool CanRapidFire => false;

    public override bool ShowMapHighlight => true;

    public override void OnMarkMapHighlights()
    {
        if (!scene.mouseTarget.pos.IsValid) {
            return;
        }

        List<Point> list = _map.ListPointsInCircle(scene.mouseTarget.pos, source.radius);
        if (list.Count == 0) {
            list.Add(CC.pos.Copy());
        }

        foreach (var item in list) {
            item.SetHighlight(8);
        }
    }

    public override bool Perform()
    {
        List<Point> abCastle = _map.ListPointsInCircle(TP, source.radius, false, false);
        if (abCastle.Count == 0) {
            return false;
        }

        var actRef = new ActRef {
            n1 = null,
            aliasEle = "eleFire",
            act = this,
        };

        var sp = CC.elements.GetOrCreateElement(source.id);
        var power = sp.GetPower(CC);
        var ele = Create("eleFire", power / 10);

        CC.Say("spell_ball", CC, sp.Name.ToLower());

        Wait(0.8f, CC);
        ActEffect.TryDelay(() => 
            CC.PlaySound("spell_ball")
        );
        
        if (CC.IsInMutterDistance() && !core.config.graphic.disableShake) {
            Shaker.ShakeCam("ball");
        }

        ActEffect.DamageEle(CC, EffectId.Ball, power, ele, abCastle, actRef, "spell_ball");
        return true;
    }
}