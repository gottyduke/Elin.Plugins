using UnityEngine;

namespace EC.Components;

internal class AuxNote : MonoBehaviour
{
    private void Update()
    {
        var aux = GetComponentInParent<AuxTooltip>();
        var baseNote = aux.BaseNote!;
        var tooltip = GetComponent<UITooltip>();

        if (baseNote.gameObject.activeSelf) {
            return;
        }

        tooltip.data = null;
        tooltip.SetActive(false);
    }

    private void OnEnable()
    {
        var aux = GetComponentInParent<AuxTooltip>();
        var baseNote = aux.BaseNote!;

        var scale = ELayer.ui.canvasScaler.scaleFactor;

        var baseRect = baseNote.GetComponent<RectTransform>();
        var baseSize = baseRect.sizeDelta;
        var rect = GetComponent<RectTransform>();

        var basePos = baseRect.position;
        var auxPos = basePos with { x = basePos.x - baseSize.x * scale };

        if (name == "aux_note_1") {
            var notes = aux.GetComponentsInChildren<AuxNote>();
            var last = notes[0].GetComponent<RectTransform>();
            auxPos = auxPos with {
                x = auxPos.x - last.sizeDelta.x * scale,
            };
        }

        rect.position = auxPos;
    }
}