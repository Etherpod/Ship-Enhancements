using UnityEngine;
using UnityEngine.UI;

namespace ShipEnhancements.Utils;

public class TextInputReader : MonoBehaviour
{
	public delegate void ReaderSubmitEvent();

	public event ReaderSubmitEvent OnSubmit;
	
	[SerializeField]
	private Text _textField;
	[SerializeField]
	private Text _placeholderText;
	[SerializeField]
	private Transform _caretTransform;
	[SerializeField]
	private float _caretBlinkInterval = 1.2f;
	[SerializeField]
	private int _maxCharacters = 500;

	private int _caretIndex;
	private bool _updateCaretNextFrame;
	private float _lastInputTime;
	private static readonly char[] kSeparators = { ' ', '.', ',', '\t', '\r', '\n' };

	private void Awake()
	{
		enabled = false;
	}

	private void Start()
	{
		_updateCaretNextFrame = true;
	}
	
	private void Update()
	{
		if (_updateCaretNextFrame)
		{
			UpdateCaretPosition();
			_updateCaretNextFrame = false;
		}
		
		var lastEvent = new Event();
		while (Event.PopEvent(lastEvent))
		{
			if (lastEvent.rawType == EventType.KeyDown)
			{
				OnKeyPressed(lastEvent);
				_lastInputTime = Time.time;
			}
			else if (lastEvent.rawType == EventType.MouseDown)
			{
				RectTransformUtility.ScreenPointToLocalPointInRectangle(_textField.rectTransform, 
					lastEvent.mousePosition,
					Locator.GetPlayerCamera()._mainCamera, 
					out Vector2 localMousePos);
				var offset = GetCanvasOffset(_textField.transform, _textField.canvas);
				localMousePos += offset * 2;
				localMousePos.y *= -1;
				
				_caretIndex = GetCharacterIndexFromPosition(localMousePos);
				_updateCaretNextFrame = true;
				_lastInputTime = Time.time;
			}
		}

		bool caretEnabled = (Time.time - _lastInputTime) % _caretBlinkInterval < _caretBlinkInterval / 2f;
		_caretTransform.gameObject.SetActive(caretEnabled);
		
		_placeholderText.gameObject.SetActive(string.IsNullOrEmpty(_textField.text));
	}

	private void OnKeyPressed(Event keyEvent)
	{
		var modifiers = keyEvent.modifiers;
		bool control = SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX
			? (modifiers & EventModifiers.Command) > EventModifiers.None
			: (modifiers & EventModifiers.Control) > EventModifiers.None;
		bool shift = (modifiers & EventModifiers.Shift) > EventModifiers.None;
		bool alt = (modifiers & EventModifiers.Alt) > EventModifiers.None;
		
		bool onlyControl = control && !alt && !shift;
		bool onlyShift = shift && !control && !alt;

		var keyCode = keyEvent.keyCode;
		if (keyCode == KeyCode.Backspace)
		{
			Backspace(onlyControl);
			return;
		}
		
		if (keyCode == KeyCode.Delete)
		{
			ForwardSpace();
			return;
		}
		
		switch (keyCode)
		{
			case KeyCode.UpArrow:
				MoveUp();
				return;
			case KeyCode.DownArrow:
				MoveDown();
				return;
			case KeyCode.RightArrow:
				MoveRight(onlyControl);
				return;
			case KeyCode.LeftArrow:
				MoveLeft(onlyControl);
				return;
			case KeyCode.Home:
				MoveTextStart();
				return;
			case KeyCode.End:
				MoveTextEnd();
				return;
		}
		
		if (onlyControl && keyCode == KeyCode.V)
		{
			Insert(GUIUtility.systemCopyBuffer);
			return;
		}

		var c = keyEvent.character;
		if (c is '\r' || c is '\u0003')
		{
			c = '\n';
		}
		
		if (c == '\n' && !onlyShift)
		{
			OnSubmit?.Invoke();
			return;
		}

		if (IsValidChar(c))
		{
			Insert(c.ToString());
		}
	}

	private void Insert(string text)
	{
		if (_maxCharacters > 0 && _textField.text.Length >= _maxCharacters)
		{
			return;
		}
		
		_textField.text = _textField.text.Insert(_caretIndex, text);
		_caretIndex += text.Length;
		
		if (_textField.text.Length >= _maxCharacters)
		{
			_textField.text = _textField.text.Substring(0, _maxCharacters);
			_caretIndex = _maxCharacters;
		}

		_updateCaretNextFrame = true;
	}

	private void ForwardSpace()
	{
		if (_textField.text.Length == 0 || _caretIndex >= _textField.text.Length) return;

		_textField.text = _textField.text.Remove(_caretIndex, 1);
	}
	
	private void Backspace(bool ctrl)
	{
		if (_textField.text.Length == 0 || _caretIndex == 0) return;

		if (ctrl)
		{
			var startIndex = FindPrevWordBegin();
			_textField.text = _textField.text.Remove(startIndex, _caretIndex - startIndex);
			_caretIndex = startIndex;
		}
		else
		{
			_textField.text = _textField.text.Remove(_caretIndex - 1, 1);
			_caretIndex = Mathf.Max(_caretIndex - 1, 0);
		}

		_updateCaretNextFrame = true;
	}

	private void MoveRight(bool ctrl)
	{
		_caretIndex = ctrl ? FindNextWordBegin() : Mathf.Min(_caretIndex + 1, _textField.text.Length);
		_updateCaretNextFrame = true;
	}

	private void MoveLeft(bool ctrl)
	{
		_caretIndex = ctrl ? FindPrevWordBegin() : Mathf.Max(_caretIndex - 1, 0);
		_updateCaretNextFrame = true;
	}

	private void MoveUp()
	{
		_caretIndex = LineUpCharacterPosition(_caretIndex, false);
		_updateCaretNextFrame = true;
	}
	
	private void MoveDown()
	{
		_caretIndex = LineDownCharacterPosition(_caretIndex, false);
		_updateCaretNextFrame = true;
	}
	
	private void MoveTextEnd()
	{
		_caretIndex = _textField.text.Length;
		_updateCaretNextFrame = true;
	}
	
	private void MoveTextStart()
	{
		_caretIndex = 0;
		_updateCaretNextFrame = true;
	}
	
	private bool IsValidChar(char c)
	{
		return c != '\0' && c != '\u007f' && (c == '\t' || c == '\n' || _textField.font.HasCharacter(c));
	}
	
	private int FindNextWordBegin()
	{
		if (_caretIndex + 1 >= _textField.text.Length)
		{
			return _textField.text.Length;
		}
		
		int num = _textField.text.IndexOfAny(kSeparators, _caretIndex + 1);
		if (num == -1)
		{
			num = _textField.text.Length;
		}
		else
		{
			num++;
		}
		
		return num;
	}
	
	private int FindPrevWordBegin()
	{
		if (_caretIndex - 2 < 0)
		{
			return 0;
		}
		
		int num = _textField.text.LastIndexOfAny(kSeparators, _caretIndex - 2);
		if (num == -1)
		{
			num = 0;
		}
		else
		{
			num++;
		}
		
		return num;
	}
	
	private int LineUpCharacterPosition(int originalPos, bool goToFirstChar)
	{
		var textGen = _textField.cachedTextGenerator;
		if (originalPos >= textGen.characters.Count)
		{
			return 0;
		}
		UICharInfo uicharInfo = textGen.characters[originalPos];
		int num = DetermineCharacterLine(originalPos, textGen);
		if (num > 0)
		{
			int num2 = textGen.lines[num].startCharIdx - 1;
			for (int i = textGen.lines[num - 1].startCharIdx; i < num2; i++)
			{
				if (textGen.characters[i].cursorPos.x >= uicharInfo.cursorPos.x)
				{
					return i;
				}
			}
			return num2;
		}
		if (!goToFirstChar)
		{
			return originalPos;
		}
		return 0;
	}
	
	private int LineDownCharacterPosition(int originalPos, bool goToLastChar)
	{
		var textGen = _textField.cachedTextGenerator;
		if (originalPos >= textGen.characterCountVisible)
		{
			return _textField.text.Length;
		}
		UICharInfo uicharInfo = textGen.characters[originalPos];
		int num = DetermineCharacterLine(originalPos, textGen);
		if (num + 1 < textGen.lineCount)
		{
			int lineEndPosition = GetLineEndPosition(textGen, num + 1);
			for (int i = textGen.lines[num + 1].startCharIdx; i < lineEndPosition; i++)
			{
				if (textGen.characters[i].cursorPos.x >= uicharInfo.cursorPos.x)
				{
					return i;
				}
			}
			return lineEndPosition;
		}
		if (!goToLastChar)
		{
			return originalPos;
		}
		return _textField.text.Length;
	}

	private void UpdateCaretPosition()
	{
		Vector2 caretPos = Vector2.zero;
		var textGen = _textField.cachedTextGenerator;
		if (_caretIndex < textGen.characters.Count)
		{
			UICharInfo charInfo = textGen.characters[_caretIndex];
			caretPos.x = charInfo.cursorPos.x;
		}
				
		caretPos.x /= _textField.pixelsPerUnit;
		if (caretPos.x > _textField.rectTransform.rect.xMax)
		{
			caretPos.x = _textField.rectTransform.rect.xMax;
		}
				
		int line = DetermineCharacterLine(_caretIndex, textGen);
		float heightOffset = textGen.lines[line].height / _textField.pixelsPerUnit;
		caretPos.y = textGen.lines[line].topY / _textField.pixelsPerUnit - heightOffset * 0.5f;

		_caretTransform.localPosition = caretPos;
	}
	
	private int GetCharacterIndexFromPosition(Vector2 pos)
	{
		TextGenerator textGen = _textField.cachedTextGenerator;
		if (textGen.lineCount == 0)
		{
			return 0;
		}
		int characterLine = GetUnclampedCharacterLineFromPosition(pos, textGen);
		if (characterLine < 0)
		{
			return 0;
		}
		if (characterLine >= textGen.lineCount)
		{
			return textGen.characterCountVisible;
		}
		int startCharIdx = textGen.lines[characterLine].startCharIdx;
		int lineEndPosition = GetLineEndPosition(textGen, characterLine);
		int num = startCharIdx;
		while (num < lineEndPosition && num < textGen.characterCountVisible)
		{
			UICharInfo uicharInfo = textGen.characters[num];
			Vector2 vector = uicharInfo.cursorPos / _textField.pixelsPerUnit;
			float num2 = pos.x - vector.x;
			float num3 = vector.x + uicharInfo.charWidth / _textField.pixelsPerUnit - pos.x;
			if (num2 < num3)
			{
				return num;
			}
			num++;
		}
		return lineEndPosition;
	}
	
	private int GetUnclampedCharacterLineFromPosition(Vector2 pos, TextGenerator generator)
	{
		float posY = pos.y * _textField.pixelsPerUnit;
		float lastLineBottom = 0f;
		int i = 0;
		
		while (i < generator.lineCount)
		{
			// top is down and bottom is up
			// because mouse pos goes up as it goes down
			float lineTop = generator.lines[i].topY;
			float lineBottom = lineTop - generator.lines[i].height;
			
			print($"compare line {lineTop} to mouse {posY}");
			
			if (posY > lineTop)
			{
				float lineSpacing = lineTop - lastLineBottom;
				if (posY > lineTop - 0.5f * lineSpacing)
				{
					return i - 1;
				}
				return i;
			}
			
			if (posY > lineBottom)
			{
				return i;
			}
			lastLineBottom = lineBottom;
			i++;
		}
		return generator.lineCount;
	}
	
	private int GetLineEndPosition(TextGenerator gen, int line)
	{
		line = Mathf.Max(line, 0);
		if (line + 1 < gen.lines.Count)
		{
			return gen.lines[line + 1].startCharIdx - 1;
		}
		return gen.characterCountVisible;
	}
	
	private int DetermineCharacterLine(int charPos, TextGenerator generator)
	{
		for (int i = 0; i < generator.lineCount - 1; i++)
		{
			if (generator.lines[i + 1].startCharIdx > charPos)
			{
				return i;
			}
		}
		return generator.lineCount - 1;
	}
	
	private Vector2 ScreenToLocal(Vector2 screen)
	{
		Canvas canvas = _textField.canvas;
		if (canvas == null)
		{
			return screen;
		}
		Vector3 vector = Vector3.zero;
		if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
		{
			vector = _textField.transform.InverseTransformPoint(screen);
		}
		else if (canvas.worldCamera != null)
		{
			Ray ray = canvas.worldCamera.ScreenPointToRay(screen);
			Plane plane = new Plane(_textField.transform.forward, _textField.transform.position);
			plane.Raycast(ray, out float num);
			vector = _textField.transform.InverseTransformPoint(ray.GetPoint(num));
		}
		return new Vector2(vector.x, vector.y);
	}

	private Vector2 GetCanvasOffset(Transform t, Canvas canvas)
	{
		Vector2 offset = t.localPosition;
		if (t.parent != null && t.parent != canvas.transform)
		{
			offset += GetCanvasOffset(t.parent, canvas);
		}

		return offset;
	}

	public void EnableInput()
	{
		enabled = true;
	}

	public void DisableInput()
	{
		enabled = false;
	}
}