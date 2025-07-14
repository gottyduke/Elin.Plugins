using UnityEngine;
using UnityEngine.UI;

namespace CustomizerMinus.Components;

internal class SpriteStateAnimator : EMono
{
    private readonly Sprite[] _sprites = new Sprite[16];
    internal Image? _image;
    private bool _init;

    internal void SetSprite(int dir, int frame)
    {
        if (!_init) {
            return;
        }

        if (_image != null) {
            _image.sprite = _sprites[dir * 4 + frame];
        }
    }

    internal void SliceSheet(Texture2D sheet)
    {
        var width = sheet.width / 4;
        var height = sheet.height / 4;

        for (var h = 0; h < 4; ++h) {
            for (var w = 0; w < 4; ++w) {
                var xPos = w * width;
                var yPos = (3 - h) * height;
                var rect = new Rect(xPos, yPos, width, height);
                _sprites[h * 4 + w] = Sprite.Create(sheet, rect, new(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
            }
        }

        _init = true;
    }
}