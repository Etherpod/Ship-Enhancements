using UnityEngine;

namespace ShipEnhancements;

public class ErnestoCallController : MonoBehaviour
{
	[SerializeField]
	private CharacterDialogueTree _dialogueTree;
	[SerializeField]
	private OWAudioSource _dialAudio;

	private void Awake()
	{
		_dialogueTree.OnEndConversation += OnEndConversation;
	}

	private void OnEndConversation()
	{
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

	private void OnDestroy()
	{
		_dialogueTree.OnEndConversation -= OnEndConversation;
	}
}