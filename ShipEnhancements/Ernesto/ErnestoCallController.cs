using ShipEnhancements.Ernesto;
using UnityEngine;

namespace ShipEnhancements.Ernesto;

public class ErnestoCallController : MonoBehaviour
{
	[SerializeField]
	private CharacterDialogueTree _dialogueTree;
	[SerializeField]
	private OWAudioSource _dialAudio;

	private bool _remoteConversation;

	private void Awake()
	{
		_dialogueTree.OnStartConversation += OnStartConversation;
		_dialogueTree.OnEndConversation += OnEndConversation;
	}

	private void OnStartConversation()
	{
		if (!_dialogueTree.enabled)
		{
			_remoteConversation = true;
		}
	}

	private void OnEndConversation()
	{
		_remoteConversation = false;
		DialogueConditionManager.SharedInstance.SetConditionState("SE_ERNESTO_LONGPICKUP");
		DialogueConditionManager.SharedInstance.SetConditionState("SE_ERNESTO_CLOSECALL");

		if (DialogueConditionManager.SharedInstance.GetConditionState("SE_ERNESTO_EXPLODE_SHIP"))
		{
			DialogueConditionManager.SharedInstance.SetConditionState("SE_ERNESTO_EXPLODE_SHIP");
			ErnestoDetectiveController.SetCustomText("You said there was something trying to get inside. Now nobody can get inside.");
			SELocator.GetShipDamageController().Explode();
		}
	}

	public void PlayDialAudio()
	{
		_dialAudio.Play();
	}

	public void StopDialAudio()
	{
		_dialAudio.Stop();
	}

	public bool InRemoteConversation() => 
		ShipEnhancements.InMultiplayer && _remoteConversation;

	private void OnDestroy()
	{
		_dialogueTree.OnStartConversation -= OnStartConversation;
		_dialogueTree.OnEndConversation -= OnEndConversation;
	}
}