using System;
using UnityEngine;

namespace ShipEnhancements;

public class ErnestonianText : NomaiWallText
{
    [SerializeField]
    private bool _alwaysAllowFocus = false;

    public override void LateInitialize()
    {
        base.LateInitialize();
        foreach (var line in _textLines)
        {
            line.SetUnreadState(true);
        }

        string text = OWUtilities.RemoveByteOrderMark(_nomaiTextAsset);
        TranslatorTextBuilder.AddTranslation(text);
    }

    public override bool CheckAllowFocus(float focusDistance, Vector3 focusDirection)
    {
        if (_alwaysAllowFocus)
        {
            return focusDistance <= _interactRange;
        }

        return base.CheckAllowFocus(focusDistance, focusDirection);
    }
}
