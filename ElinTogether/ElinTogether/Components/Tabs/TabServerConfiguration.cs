namespace ElinTogether.Components;

internal class TabServerConfiguration : TabEmpBase
{
    public override void OnLayout()
    {
        Toggle("emp_ui_sv_cfg_shared_speed", EmpConfig.Server.SharedAverageSpeed.Value, value => {
            EmpConfig.Server.SharedAverageSpeed.Value = value;
        });

        Toggle("emp_ui_sv_cfg_turn_combat", EmpConfig.Server.TurnBasedCombat.Value, value => {
            EmpConfig.Server.TurnBasedCombat.Value = value;
        });
    }
}