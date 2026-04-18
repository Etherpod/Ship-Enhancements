using UnityEngine;
using UnityEngine.UI;

namespace ShipEnhancements;

public class ErnestoCallTextAnimator : MonoBehaviour
{
	private CharacterDialogueTree _dialogueTree;
	private OWAudioSource _callAudio;
	private Text _text;
	private float _startTime;
	private float _animationTime;

	private void Awake()
	{
		_text = gameObject.GetRequiredComponent<Text>();
		_dialogueTree = Locator.GetPlayerCamera().transform.Find("ErnestoCallDialogue/ConversationZone")
			.GetComponent<CharacterDialogueTree>();
		_callAudio = _dialogueTree.transform.parent.Find("CallAudio_Loop").GetComponent<OWAudioSource>();
		_dialogueTree.OnEndConversation += OnEndConversation;
		_startTime = Time.time;
		_animationTime = Random.Range(10f, 20f);
	}

	private void Start()
	{
		_callAudio.Play();
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

		if (timePassed > _animationTime)
		{
			PatchClass.BypassDialogueOptionLock(_dialogueTree, 0);
			_callAudio.Stop();
			Destroy(this);
		}
	}

	private void OnEndConversation()
	{
		_dialogueTree.OnEndConversation -= OnEndConversation;
		_callAudio.Stop();
		Destroy(this);
	}
}