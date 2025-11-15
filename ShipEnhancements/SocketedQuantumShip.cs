using System.Collections.Generic;
using UnityEngine;

namespace ShipEnhancements;

public class SocketedQuantumShip : SocketedQuantumObject
{
    public override void Awake()
    {
        _sockets = [];
        base.Awake();
        GlobalMessenger.AddListener("ShipEnterQuantumMoon", OnShipEnterQuantumMoon);
        GlobalMessenger.AddListener("ShipExitQuantumMoon", OnShipExitQuantumMoon);
        enabled = false;
    }

    private void OnShipEnterQuantumMoon()
    {
        GameObject qm = Locator.GetQuantumMoon().gameObject;
        if (qm == null) return;

        Transform sockets = qm.GetComponentInChildren<QuantumShrine>()._socketRoot.transform;
        List<QuantumSocket> socketList = [];
        QuantumSocket[] componentsInChildren = sockets.GetComponentsInChildren<QuantumSocket>();
        for (int k = 0; k < componentsInChildren.Length; k++)
        {
            if (componentsInChildren[k].transform.parent.parent.name == "Pivot_North") continue;

            for (int l = 0; l < componentsInChildren[k].GetProbabilityMultiplier(); l++)
            {
                if (!_childSockets.Contains(componentsInChildren[k]))
                {
                    socketList.Add(componentsInChildren[k]);
                }
            }
        }
        SetQuantumSockets(socketList.ToArray());

        Minimap playerMinimap = GameObject.Find("SecondaryGroup/HUD_Minimap/Minimap_Root").GetComponent<Minimap>();
        playerMinimap._minimapRenderersToSwitchOnOff[4].enabled = false;

        enabled = true;
    }

    private void OnShipExitQuantumMoon()
    {
        SetQuantumSockets([]);
        enabled = false;
    }

    public override bool IsPlayerEntangled()
    {
        return base.IsPlayerEntangled() || PlayerState.IsInsideShip();
    }

    private void OnDestroy()
    {
        GlobalMessenger.RemoveListener("ShipEnterQuantumMoon", OnShipEnterQuantumMoon);
        GlobalMessenger.RemoveListener("ShipExitQuantumMoon", OnShipExitQuantumMoon);
    }
}
