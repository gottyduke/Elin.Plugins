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

        if (name == "aux_note_0") {
            AttachToRect(aux.BaseNote!.Rect());
        } else {
            var notes = aux.GetComponentsInChildren<AuxNote>()
                .OrderBy(n => n.name)
                .ToList();
            var index = notes.IndexOf(this);
            AttachToRect(notes[index - 1].Rect());
        }
    }

    private void AttachToRect(RectTransform baseRect)
    {
        var scale = ELayer.ui.canvasScaler.scaleFactor;

        var baseSize = baseRect.sizeDelta;
        var rect = this.Rect();

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
        
        Util.ClampToScreen(rect, 13);
    }
}