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
        var auxSize = GetComponent<RectTransform>().sizeDelta;

        var basePos = baseRect.position;
        var auxPos = basePos with { x = basePos.x - baseSize.x * scale };

        if (name == "aux_note_1") {
            var notes = aux.GetComponentsInChildren<AuxNote>();
            var last = notes[0].GetComponent<RectTransform>();
            last.position = last.position with {
                y = last.position.y - last.sizeDelta.y / 2f * scale,
            };
            auxPos = auxPos with {
                y = auxPos.y + auxSize.y / 2f * scale,
            };
        }

        var rect = GetComponent<RectTransform>();
        rect.position = auxPos;
    }
}