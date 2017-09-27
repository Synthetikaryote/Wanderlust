using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Runtime.InteropServices;

public class InputUI : MonoBehaviour {

	#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
	[DllImport("user32.dll", CharSet=CharSet.Auto, ExactSpelling=true)]
	public static extern int GetKeyboardState(byte[] keystate);
	[DllImport("user32.dll", CharSet=CharSet.Auto)]
	internal static extern int MapVirtualKey(int uCode, int uMapType);
	private byte[] keyboardState;
	#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
	
	#endif

	public Object actionFramePrefab;

	private string mainKeyboard =
		"{\x01::escape:esc:0.9}{\x3B::f1:0.9}{\x3C::f2:0.9}{\x3D::f3:0.9}{\x3E::f4:0.9}{\x3F::f5:0.9}{\x40::f6:0.9}{\x41::f7:0.9}{\x42::f8:0.9}{\x43::f9:0.9}{\x44::f10:0.9}{\x57::f11:0.9}{\x58::f12:0.9}{:\x13:pause:0.9}{:\x2A:prt sc:0.9}{:\x2E:delete:0.9}~" +
		"{\x29:::`}{\x02:::1:1}{\x03:::2:1}{\x04:::3:1}{\x05:::4:1}{\x06:::5:1}{\x07:::6:1}{\x08:::7:1}{\x09:::8:1}{\x0A:::9:1}{\x0B:::0:1}{\x0C:::-}{\x0D:::=}{\x0E::backspace:1.7}~" +
		"{\x0F::tab:1.5}{\x10:::q}{\x11:::w}{\x12:::e}{\x13:::r}{\x14:::t}{\x15:::y}{\x16:::u}{\x17:::i}{\x18:::o}{\x19:::p}{\x1A:::[}{\x1B:::]}{\x2B::\\:\\:1.2}~" +
		"{\x3A::caps lock:1.4}{\x1E:::a}{\x1F:::s}{\x20:::d}{\x21:::f}{\x22:::g}{\x23:::h}{\x24:::j}{\x25:::k}{\x26:::l}{\x27:::;}{\x28:::'}{\x1C::return:enter:2.3}~" +
		"{:\xA0:left shift:lshift:2.1}{\x2C:::z}{\x2D:::x}{\x2E:::c}{\x2F:::v}{\x30:::b}{\x31:::n}{\x32:::m}{\x33:::,}{\x34:::.}{\x35:::/}{:\xA1:right shift:rshift:2.8}~" +
		"{:\xA2:left ctrl:lctrl:1.2}{:::fn}{:\x5B::lwin}{:\xA4:left alt:lalt}{:\x20::space:5}{:\xA5:right alt:ralt}{:\x5C::rwin}{:\x5D::menu}{:\xA3:right ctrl:rctrl:2.8}";
	private GameObject keyPanel;

	private float padding = 3f;

	// Use this for initialization
	void Start () {
		keyPanel = GameObject.FindWithTag("ActionPanel");
		string[] rows = mainKeyboard.Split('~');
		float x = 0f, y = 0f;
		for (int i = 0; i < rows.Length; ++i) {
			string row = rows[i];
			float height = 0;
			while (true) {
				string token = getNextToken(row, out row);
				if (token == null) {
					break;
				}
				byte scanCode = 0;
				byte virtualKey = 0;
				float widthScale = 1f;
				string displayName = token;

				string[] parts = token.Split(':');

				if (parts.Length > 2) {
					scanCode = parts[0].Length > 0 ? (byte)parts[0][0] : (byte)0;
					virtualKey = parts[1].Length > 0 ? (byte)parts[1][0] : (byte)0;
					token = parts[2];
				}

				if (parts.Length > 4) {
					displayName = parts[3];
					widthScale = float.Parse(parts[4]);
				}
				else if (parts.Length > 3) {
					float scale = 1;
					if (float.TryParse (parts[3], out scale)) {
						displayName = parts[2];
						widthScale = scale;
					}
					else {
						displayName = parts[3];
					}
				}

				// create a button frame
				GameObject actionFrame = (GameObject)GameObject.Instantiate(actionFramePrefab);
				actionFrame.transform.SetParent(transform, false);
				RectTransform rect = ((RectTransform)actionFrame.transform);
				float width = rect.sizeDelta.x * widthScale;
				height = rect.sizeDelta.y;
				rect.sizeDelta = new Vector2(width, height);
				rect.anchoredPosition = new Vector2(x, y);
				Text text = actionFrame.GetComponentInChildren<Text>();
				Image image = actionFrame.GetComponentInChildren<Image>();
				text.text = displayName;
				ActionFrame actionFrameScript = actionFrame.GetComponent<ActionFrame>();
				if (scanCode != 0 && virtualKey == 0) {
					#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
					virtualKey = (byte)MapVirtualKey((int)scanCode, 3);
					#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
					
					#endif
				}
				actionFrameScript.keycode = token;
				actionFrameScript.virtualKey = virtualKey;
				x += width + padding;
			}
			y -= height + padding;
			x = 0;
		}

		#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
		keyboardState = new byte[256];
		#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
		
		#endif
	}

	string getNextToken(string s, out string newS) {
		if (s == null || s.Length == 0) {
			newS = s;
            return null;
		}
		else if (s[0] == '{') {
			int right = s.IndexOf('}');
			if (right != -1) {
				newS = s.Substring(right + 1);
				return s.Substring(1, right - 1);
			}
		}
		else {
			newS = s.Substring(1);
			return s[0].ToString();
		}
		newS = s;
		return null;
	}
	
	// Update is called once per frame
	void Update () {
		#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
		GetKeyboardState(keyboardState);
		#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
		
		#endif
	}

	public bool GetKeyFromVirtualKey(byte virtualKey) {
		#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
		return (keyboardState[virtualKey] & 0x80) == 0x80;
		#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
		return false;
		#endif
	}
}
