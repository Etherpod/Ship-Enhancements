using System;
using UnityEngine;

namespace ShipEnhancements;

[RequireComponent(typeof(Collider))]
public class FuelTankCollider : MonoBehaviour
{
	private FuelTankItem _item;

	private void Awake()
	{
		_item = GetComponentInParent<FuelTankItem>();
	}

	public void OnCollision(ImpactData data, Transform parent = null)
	{
		_item.OnImpact(data.speed);
	}
}