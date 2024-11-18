using System.Linq;
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

        if (name == "aux_note_0") {
            AttachToRect(baseNote.GetComponent<RectTransform>());
        } else {
            var notes = aux.GetComponentsInChildren<AuxNote>().ToList();
            var index = notes.IndexOf(this);
            var last = notes[index - 1].GetComponent<RectTransform>();
            AttachToRect(last);
        }
    }

    private void AttachToRect(RectTransform baseRect)
    {
        var scale = ELayer.ui.canvasScaler.scaleFactor;

        var baseSize = baseRect.sizeDelta;
        var rect = GetComponent<RectTransform>();

        var basePos = baseRect.position;
        var auxPos = basePos;

        if (baseRect.pivot == Vector2.one) {
            auxPos = basePos with { x = basePos.x - baseSize.x * scale };
        }

        if (baseRect.pivot == Vector2.up) {
            auxPos = basePos with { x = basePos.x + baseSize.x * scale };
        }

        rect.localPosition = baseRect.localPosition;
        rect.position = auxPos;
        rect.pivot = baseRect.pivot;
    }
}