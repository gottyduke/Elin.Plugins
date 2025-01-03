using UnityEngine;

namespace Dona.Stats;

// は隣接した敵の複製体を中立仲間として作り出す能力。
// 複製体はオリジナルよりもLvが低く、ドナココ本人よりも敵に優先的に狙われる。
// 200回
// デジカメ, あちこち擦り切れているが、レンズは丁寧に手入れがされている。
// ステータスを上昇、首は他の装備をつけれない
internal class ConDonaAfterImage : Condition
{
    public override bool CanManualRemove => false;

    public override void OnStart()
    {
        Msg.SetColor(Msg.colors.Ono);
        Msg.Say(source.GetText("textPhase"), owner);

        value = DonaConfig.ImageDuration?.Value ?? 100;
    }

    public override void OnValueChanged()
    {
        if (value > 0) {
            owner.SetSummon(value + 1);
            return;
        }

        Msg.SetColor(Msg.colors.Ono);
        Kill();
        Msg.SetColor();
    }

    public override void OnRemoved()
    {
        owner.PlayEffect("vanish");
        owner.PlaySound("vanish");
        owner.Destroy();
    }
}