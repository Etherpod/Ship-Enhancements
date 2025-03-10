using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class SEItemSocket : OWItemSocket
{
    [SerializeField]
    private OWItem _prefabItem;

    private FirstPersonManipulator _manipulator;
    private ScreenPrompt _createItemPrompt;
    private ScreenPrompt _noCreateItemPrompt;

    public override void Awake()
    {
        Reset();
        _sector = SELocator.GetShipSector();
        base.Awake();
        _acceptableType = GetAcceptableType();
        _manipulator = FindObjectOfType<FirstPersonManipulator>();
        _createItemPrompt = new ScreenPrompt(InputLibrary.interactSecondary, "Create New " + _prefabItem.GetDisplayName());
        _noCreateItemPrompt = new ScreenPrompt("Cannot create items in multiplayer");

        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
        if ((bool)preventSystemFailure.GetProperty())
        {
            GlobalMessenger.AddListener("ShipHullDetached", OnShipSystemFailure);
        }
    }

    public override void Start()
    {
        base.Start();
        AssetBundleUtilities.ReplaceShaders(_prefabItem.gameObject);
        if ((bool)unlimitedItems.GetProperty())
        {
            if (ShipEnhancements.InMultiplayer)
            {
                Locator.GetPromptManager().AddScreenPrompt(_noCreateItemPrompt, PromptPosition.Center, false);
            }
            else
            {
                Locator.GetPromptManager().AddScreenPrompt(_createItemPrompt, PromptPosition.Center, false);
            }
        }
        CreateItem();
    }

    public override void Update()
    {
        if (_removedItem != null && !_removedItem.IsAnimationPlaying())
        {
            if (OnSocketableDoneRemoving != null)
            {
                OnSocketableDoneRemoving(this._removedItem);
            }
            _removedItem = null;
            // change
            enabled = (bool)unlimitedItems.GetProperty();
            return;
        }
        if (_socketedItem != null && !_socketedItem.IsAnimationPlaying())
        {
            if (OnSocketableDonePlacing != null)
            {
                OnSocketableDonePlacing(_socketedItem);
            }
            // change
            enabled = (bool)unlimitedItems.GetProperty();
        }

        if ((bool)unlimitedItems.GetProperty())
        {
            bool focused = _manipulator.GetFocusedItemSocket() == this;

            if (ShipEnhancements.InMultiplayer)
            {
                _noCreateItemPrompt.SetVisibility(focused && _socketedItem == null);
            }
            else
            {
                _createItemPrompt.SetVisibility(focused && _socketedItem == null);
            }

            if (focused && _socketedItem == null
                && OWInput.IsNewlyPressed(InputLibrary.interactSecondary, InputMode.Character))
            {
                CreateItem();
            }
        }
    }

    protected virtual ItemType GetAcceptableType()
    {
        return ItemType.Invalid;
    }

    protected virtual void CreateItem()
    {
        if (_socketedItem != null) return;

        OWItem newItem = Instantiate(_prefabItem);
        PlaceIntoSocket(newItem);
    }

    private void OnShipSystemFailure()
    {
        _sector = null;
        _socketedItem?.SetSector(null);
    }

    private void OnDestroy()
    {
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
        if ((bool)preventSystemFailure.GetProperty())
        {
            GlobalMessenger.RemoveListener("ShipHullDetached", OnShipSystemFailure);
        }
    }
}
