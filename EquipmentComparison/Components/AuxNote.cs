using System.Linq;
using EC.Helper;
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
        this.StartDeferredCoroutine(AdjustPosition);
    }

    private void AdjustPosition()
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

        Util.ClampToScreen(this.Rect(), 10);
    }

    private void AttachToRect(RectTransform baseRect)
    {
        var scale = ELayer.ui.canvasScaler.scaleFactor;

        var baseSize = baseRect.sizeDelta;
        var rect = this.Rect();

        var basePos = baseRect.position;
        var auxPos = basePos;

        var pivot = baseRect.pivot == Vector2.one ? -1f : 1f;
        auxPos = auxPos with { x = basePos.x + baseSize.x * scale * pivot };

        rect.localPosition = baseRect.localPosition;
        rect.position = auxPos;
        rect.pivot = baseRect.pivot;
    }
}