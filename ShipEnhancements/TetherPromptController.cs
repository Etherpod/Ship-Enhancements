using UnityEngine;

namespace ShipEnhancements;

public class TetherPromptController : MonoBehaviour
{
    private ScreenPrompt _reelInPrompt;
    private ScreenPrompt _reelOutPrompt;
    private ScreenPrompt _disconnectPrompt;
    private bool _attachedToTether;

    private void Awake()
    {
        _reelInPrompt = new ScreenPrompt(InputLibrary.toolOptionUp, "<CMD>" + "   " + "Reel tether in", 0, ScreenPrompt.DisplayState.Normal, false);
        _reelOutPrompt = new ScreenPrompt(InputLibrary.toolOptionDown, "<CMD>" + "   " + "Reel tether out", 0, ScreenPrompt.DisplayState.Normal, false);
        _disconnectPrompt = new ScreenPrompt(InputLibrary.freeLook, InputLibrary.interact, "<CMD>   Disconnect Tether", ScreenPrompt.MultiCommandType.HOLD_ONE_AND_PRESS_2ND);

        GlobalMessenger.AddListener("AttachPlayerTether", OnAttachPlayerTether);
        GlobalMessenger.AddListener("DetachPlayerTether", OnDetachPlayerTether);

        ShipEnhancements.Instance.ModHelper.Events.Unity.RunWhen(() => Locator.GetPromptManager() != null, () =>
        {
            Locator.GetPromptManager().AddScreenPrompt(_reelInPrompt, PromptPosition.LowerLeft, false);
            Locator.GetPromptManager().AddScreenPrompt(_reelOutPrompt, PromptPosition.LowerLeft, false);
            Locator.GetPromptManager().AddScreenPrompt(_disconnectPrompt, PromptPosition.LowerLeft, false);
        });
    }

    private void OnAttachPlayerTether()
    {
        _attachedToTether = true;
    }

    private void OnDetachPlayerTether()
    {
        _attachedToTether = false;
    }

    private void Update()
    {
        _reelInPrompt.SetVisibility(false);
        _reelOutPrompt.SetVisibility(false);
        _disconnectPrompt.SetVisibility(false);

        if (!OWInput.IsInputMode(InputMode.Character))
        {
            return;
        }

        _reelInPrompt.SetVisibility(_attachedToTether);
        _reelOutPrompt.SetVisibility(_attachedToTether);
        _disconnectPrompt.SetVisibility(_attachedToTether);
    }

    private void OnDestroy()
    {
        GlobalMessenger.RemoveListener("AttachPlayerTether", OnAttachPlayerTether);
        GlobalMessenger.RemoveListener("DetachPlayerTether", OnDetachPlayerTether);
    }
}
