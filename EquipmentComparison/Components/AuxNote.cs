using System.Collections;
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
            var alpha = baseNote.cg.alpha;
            if (name != "aux_note_0") {
                var mod = EcConfig.Modifier!.Value;
                if (mod != KeyCode.None && Input.GetKey(mod)) {
                    alpha = 0f;
                }
            }

            tooltip.cg.alpha = alpha;
            return;
        }

        tooltip.data = null;
        tooltip.SetActive(false);
    }

    private void OnEnable()
    {
        StartCoroutine(AdjustPosition());
    }

    private IEnumerator AdjustPosition()
    {
        yield return null;

        var aux = GetComponentInParent<AuxTooltip>();
        var rect = aux.BaseNote!.Rect();

        if (name != "aux_note_0") {
            var notes = aux.GetComponentsInChildren<AuxNote>()
                .OrderBy(n => n.name)
                .ToList();
            var index = notes.IndexOf(this);
            rect = notes[index - 1].Rect();
        }

        AttachToRect(rect);
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