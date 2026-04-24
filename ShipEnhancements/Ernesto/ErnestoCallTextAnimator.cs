using UnityEngine;
using UnityEngine.UI;

namespace ShipEnhancements.Ernesto;

public class ErnestoCallTextAnimator : MonoBehaviour
{
	private ErnestoCallController _callController;
	private CharacterDialogueTree _dialogueTree;
	private Text _text;
	private float _startTime;
	private float _pickUpTime;

	private void Awake()
	{
		_text = gameObject.GetRequiredComponent<Text>();
		_callController = Locator.GetPlayerCamera().transform.Find("ErnestoCallDialogue")
			.GetComponent<ErnestoCallController>();
		_dialogueTree = _callController.GetComponentInChildren<CharacterDialogueTree>();
		_dialogueTree.OnEndConversation += OnEndConversation;
		_startTime = Time.time;

		if (DialogueConditionManager.SharedInstance.GetConditionState("SE_ERNESTO_LONGPICKUP"))
		{
			_pickUpTime = Random.Range(20f, 40f);
		}
		else
		{
			_pickUpTime = Random.Range(8f, 15f);
		}
	}

	private void Start()
	{
		_callController.PlayDialAudio();
	}

	private void Update()
	{
		var timePassed = Time.time - _startTime;
		int num = (int)(timePassed % 4);
		string text = "";
		for (int i = 0; i < num; i++)
		{
			text += ". ";
		}

		_text.text = text;

		if (PlayerData.GetPersistentCondition("SE_KNOWS_ERNESTO") && timePassed > _pickUpTime)
		{
			PatchClass.BypassDialogueOptionLock(_dialogueTree, 0);
			_dialogueTree.OnEndConversation -= OnEndConversation;
			_callController.StopDialAudio();
			Destroy(this);
		}
	}

	private void OnEndConversation()
	{
		_dialogueTree.OnEndConversation -= OnEndConversation;
		_callController.StopDialAudio();
		Destroy(this);
	}
}