using System.Collections.Generic;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class SEItemSocket : OWItemSocket
{
    [SerializeField]
    protected OWItem _prefabItem;

    protected FirstPersonManipulator _manipulator;
    protected OWCamera _playerCam;
    protected ScreenPrompt _createItemPrompt;
    protected ScreenPrompt _noCreateItemPrompt;

    protected List<OWItem> _itemPool = [];
    protected List<OWItem> _spawnedItems = [];
    protected readonly int _numItemsToSpawn = 3;

    public override void Awake()
    {
        Reset();
        _sector = SELocator.GetShipSector();
        base.Awake();
        _acceptableType = GetAcceptableType();
        _manipulator = FindObjectOfType<FirstPersonManipulator>();
        _createItemPrompt = new ScreenPrompt(InputLibrary.interactSecondary, "Create New " + _prefabItem.GetDisplayName());
        _noCreateItemPrompt = new ScreenPrompt("Item Limit Reached");

        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
        if ((bool)preventSystemFailure.GetProperty())
        {
            GlobalMessenger.AddListener("ShipHullDetached", OnShipSystemFailure);
        }
    }

    public override void Start()
    {
        if (_socketedItem != null)
        {
            _socketedItem.MoveAndChildToTransform(_socketTransform);
        }

        enabled = (bool)unlimitedItems.GetProperty();

        _playerCam = Locator.GetPlayerCamera();
        AssetBundleUtilities.ReplaceShaders(_prefabItem.gameObject);
        if ((bool)unlimitedItems.GetProperty())
        {
            Locator.GetPromptManager().AddScreenPrompt(_createItemPrompt, PromptPosition.Center, false);
            if (ShipEnhancements.InMultiplayer)
            {
                Locator.GetPromptManager().AddScreenPrompt(_noCreateItemPrompt, PromptPosition.Center, false);

                for (int i = 0; i < _numItemsToSpawn; i++)
                {
                    OWItem newItem = Instantiate(_prefabItem, transform);
                    newItem.gameObject.SetActive(false);
                    _itemPool.Add(newItem);
                }
            }
        }

        if (!ShipEnhancements.InMultiplayer || ShipEnhancements.QSBAPI.GetIsHost() 
            || !(bool)unlimitedItems.GetProperty())
        {
            CreateItem();
        }
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
            UpdatePromptVisibility(focused);

            if (focused && _socketedItem == null && (!ShipEnhancements.InMultiplayer || _itemPool.Count > 0)
                && OWInput.IsNewlyPressed(InputLibrary.interactSecondary, InputMode.Character))
            {
                CreateItem();
            }
        }
    }

    protected virtual void UpdatePromptVisibility(bool focused)
    {
        bool flag = focused && _socketedItem == null && _playerCam.enabled && OWInput.IsInputMode(InputMode.Character | InputMode.ShipCockpit);
        if (flag != _createItemPrompt.IsVisible())
        {
            _createItemPrompt.SetVisibility(flag && (!ShipEnhancements.InMultiplayer 
                || _itemPool.Count > 0));
        }
        else if (flag != _noCreateItemPrompt.IsVisible())
        {
            _noCreateItemPrompt.SetVisibility(flag && ShipEnhancements.InMultiplayer
                && _itemPool.Count == 0);
        }
    }

    protected virtual ItemType GetAcceptableType()
    {
        return ItemType.Invalid;
    }

    public virtual void CreateItem()
    {
        if (_socketedItem != null) return;

        if (!ShipEnhancements.InMultiplayer || !(bool)unlimitedItems.GetProperty())
        {
            OWItem newItem = Instantiate(_prefabItem);
            PlaceIntoSocket(newItem);
        }
        else if (_itemPool.Count > 0)
        {
            OWItem newItem = _itemPool[0];
            _itemPool.Remove(newItem);
            _spawnedItems.Add(newItem);
            newItem.gameObject.SetActive(true);
            PlaceIntoSocket(newItem);

            ShipEnhancements.Instance.ModHelper.Events.Unity.FireInNUpdates(() =>
            {
                ShipEnhancements.WriteDebugMessage("Socket item: " + newItem.GetDisplayName()
                    + ShipEnhancements.QSBInteraction.GetIDFromItem(newItem));
            }, 5);

            foreach (uint id in ShipEnhancements.PlayerIDs)
            {
                ShipEnhancements.QSBCompat.SendCreateItem(id, newItem, this);
            }
        }
    }

    public virtual void CreateItemRemote(OWItem item, bool socketItem)
    {
        if (_itemPool.Contains(item))
        {
            _itemPool.Remove(item);
            _spawnedItems.Add(item);
            item.gameObject.SetActive(true);
            if (socketItem)
            {
                PlaceIntoSocket(item);
                ShipEnhancements.Instance.ModHelper.Events.Unity.FireInNUpdates(() =>
                {
                    ShipEnhancements.WriteDebugMessage("Socket item: " + item.GetDisplayName()
                        + ShipEnhancements.QSBInteraction.GetIDFromItem(item));
                }, 5);
            }
        }
    }

    public OWItem[] GetSpawnedItems()
    {
        return _spawnedItems.ToArray();
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
