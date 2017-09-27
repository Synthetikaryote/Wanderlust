using UnityEngine;
using System.Runtime.InteropServices;

// reference:
// http://answers.unity3d.com/questions/564664/how-i-can-move-mouse-cursor-without-mouse-but-with.html
public static class MouseUtils {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
	[StructLayout(LayoutKind.Sequential)]
	public struct POINT
	{
		public int X;
		public int Y;

		public POINT(int x, int y)
		{
			this.X = x;
			this.Y = y;
		}
	}

	[DllImport("user32.dll")]
	private static extern bool SetCursorPos(int X, int Y);
	[DllImport("user32.dll")]
	private static extern bool GetCursorPos(out POINT pos);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
	public enum _CGEventType {
		kCGEventLeftMouseDown       = 1,
		kCGEventLeftMouseUp         = 2,
		kCGEventRightMouseDown      = 3,
		kCGEventRightMouseUp        = 4,
		kCGEventMouseMoved          = 5,
		kCGEventLeftMouseDragged    = 6,
		kCGEventRightMouseDragged   = 7,
		kCGEventKeyDown             = 10,
		kCGEventKeyUp               = 11,
		kCGEventFlagsChanged        = 12,
		kCGEventScrollWheel         = 22,
		kCGEventTabletPointer       = 23,
		kCGEventTabletProximity     = 24,
		kCGEventOtherMouseDown      = 25,
		kCGEventOtherMouseUp        = 26,
		kCGEventOtherMouseDragged   = 27
//		kCGEventTapDisabledByTimeout = 0xFFFFFFFE,
//		kCGEventTapDisabledByUserInput = 0xFFFFFFFF
	};

	public enum _CGMouseButton {
		kCGMouseButtonLeft = 0,
		kCGMouseButtonRight = 1,
		kCGMouseButtonCenter = 2
	};
     
	public struct CGPoint {
		public float x;
		public float y;
 
		public CGPoint(float x, float y) {
			this.x = x;
			this.y = y;
		}
 
		public override string ToString () {
			return string.Format ("["+x+","+y+"]");
		}
	}

	[DllImport("/System/Library/Frameworks/Quartz.framework/Versions/Current/Quartz")]
	private static extern uint CGEventCreateMouseEvent(int? source, _CGEventType mouseType, CGPoint mouseCursorPosition, _CGMouseButton mouseButton);
	[DllImport("/System/Library/Frameworks/Quartz.framework/Versions/Current/Quartz")]
	private static extern uint CGEventCreate(int? source);
	[DllImport("/System/Library/Frameworks/CoreFoundation.framework/Versions/Current/CoreFoundation")]
	private static extern void CFRelease(uint eventRef);
	[DllImport("/System/Library/Frameworks/Quartz.framework/Versions/Current/Quartz")]
	private static extern CGPoint CGEventGetLocation(uint eventRef);
 
	public static void MouseEvent(_CGEventType eventType, CGPoint position)
	{
		CGEventCreateMouseEvent((int?)null, _CGEventType.kCGEventLeftMouseDown, position, _CGMouseButton.kCGMouseButtonLeft);
	}
#endif

	public static Vector3 GetCursorPosition() {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
		POINT p;
		GetCursorPos(out p);
		return new Vector3(p.X, p.Y, 0f);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
//		var e = CGEventCreate(null);
//		var point = CGEventGetLocation(e);
//		CFRelease(e);
//		return new Vector3(point.x, point.y, 0f);
		return Input.mousePosition;
#endif
	}
	public static void SetCursorPosition(int x, int y) {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
		SetCursorPos(x, y);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
//		MouseEvent(_CGEventType.kCGEventMouseMoved, new CGPoint(x, y));
#endif
	}
}
