/*

Title: GlobalInput
Description: Uses low level C# to hook into hook events and replicate unity's input manager but even when the application doesn't have focus.
	This script should work the same as unity's Input class 
	eg: Input.GetKeyDown( KeyCode.Left ) becomes GlobalInput.GetKeyDown( GlobalKeyCode.LEFT )
Auther: Kalvin Pearce
Github: https://github.com/kalvinpearce/

*/

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class GlobalInput : MonoBehaviour
{
	#region SINGLETON
	static GlobalInput _instance;
	public static GlobalInput Instance
	{
		get 
		{
			if( _instance == null )
			{
				_instance = GameObject.FindObjectOfType<GlobalInput>();
				
				if( _instance == null )
				{
					GameObject container = new GameObject("GlobalInputManager");
					_instance = container.AddComponent<GlobalInput>();
				}
			}
		
			return _instance;
		}
	}
	#endregion

	/* Variables */

	// Hook types
    private enum HookType : int
    {
        WH_JOURNALRECORD = 0,
        WH_JOURNALPLAYBACK = 1,
        WH_KEYBOARD = 2,
        WH_GETMESSAGE = 3,
        WH_CALLWNDPROC = 4,
        WH_CBT = 5,
        WH_SYSMSGFILTER = 6,
        WH_MOUSE = 7,
        WH_HARDWARE = 8,
        WH_DEBUG = 9,
        WH_SHELL = 10,
        WH_FOREGROUNDIDLE = 11,
        WH_CALLWNDPROCRET = 12,
        WH_KEYBOARD_LL = 13,
        WH_MOUSE_LL = 14
    }

	public static bool[] keyStates = new bool[255];
	public static bool[] keyDownStates = new bool[255];
	public static bool[] keyUpStates = new bool[255];

	private const int WM_KEYDOWN     = 0x0100;
	private const int WM_KEYUP       = 0x0101;

	private static IntPtr windowHandle;
	private static LowLevelKeyboardProc llKbProc;
	private static IntPtr hookID = IntPtr.Zero;
	
	// Set the hook
	public static bool SetHook() 
	{
		// If no process linked, link hook process
		if( llKbProc == null )
			llKbProc = HookCallback;
			
		// If already hooked, unhook
		if( hookID != IntPtr.Zero )
			UnsetHook();
		
		// Hook new process
		Process curProcess = Process.GetCurrentProcess();
		ProcessModule curModule = curProcess.MainModule;
		windowHandle = GetModuleHandle(curModule.ModuleName);
		hookID = SetWindowsHookEx( (int)HookType.WH_KEYBOARD_LL, llKbProc, windowHandle, 0);

		// If hooking failed, return false
		if( hookID == IntPtr.Zero )
		{
			UnityEngine.Debug.Log( "Failed to hook" );
			return false;
		}
		
		// Hooked successfully
		UnityEngine.Debug.Log( "Hooked successfully" );
		return true;
	}

	// Unhook process
	public static void UnsetHook() 
	{
		UnhookWindowsHookEx( hookID );
		hookID = IntPtr.Zero;
	}

	// Hook function for windows to call on keystroke events
	private static IntPtr HookCallback( int nCode, IntPtr wParam, IntPtr lParam )
	{
		if ( nCode >= 0 )
		{
			// Read vkCode from lParam
			int vkCode = Marshal.ReadInt32(lParam);

			// If vkCode key is down, set the key state to true
			if( wParam == (IntPtr)WM_KEYDOWN )
				SetKeyState( vkCode, true );
			
			// If vkCode key is up, set the key state to false
			if( wParam == (IntPtr)WM_KEYUP )
				SetKeyState( vkCode, false );
			
		}

		// Call next hook
		return CallNextHookEx(hookID, nCode, wParam, lParam);
	}

	static void SetKeyState( int key, bool state )
	{
		if( !keyStates[ key] )
			keyDownStates[ key ] = state;
		
		keyStates[ key ] = state;
		keyUpStates[ key ] = !state;
	}

	public static bool GetKey( GlobalKeyCode key )
	{
		return keyStates[ (int)key ];
	}

	public static bool GetKeyDown( GlobalKeyCode key )
	{
		if( keyDownStates[ (int)key ] )
		{
			keyDownStates[ (int)key ] = false;
			return true;
		}
		
		return false;
	}

	public static bool GetKeyUp( GlobalKeyCode key )
	{
		if( keyUpStates[ (int)key ] )
		{
			keyUpStates[ (int)key ] = false;
			return true;
		}
		
		return false;
	}

	static GlobalInput()
    {
		// Hook process
		SetHook();

		GameObject container = new GameObject("GlobalInputManager");
		_instance = container.AddComponent<GlobalInput>();
		DontDestroyOnLoad( _instance );
    }

	void OnDisable()
    {
        UnityEngine.Debug.Log("Application ending after " + Time.time + " seconds");
		UnityEngine.Debug.Log("Uninstall hook");
        UnsetHook();
    }

	/* Extern Functions */
	[DllImport( "user32.dll", CharSet = CharSet.Auto, SetLastError = true )]
	private static extern IntPtr SetWindowsHookEx( int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId );

	[DllImport( "user32.dll", CharSet = CharSet.Auto, SetLastError = true )]
	[return: MarshalAs( UnmanagedType.Bool )]
	private static extern bool UnhookWindowsHookEx( IntPtr hhk );

	[DllImport( "user32.dll", CharSet = CharSet.Auto, SetLastError = true )]
	private static extern IntPtr CallNextHookEx( IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam );

	[DllImport( "kernel32.dll", CharSet = CharSet.Auto, SetLastError = true )]
	private static extern IntPtr GetModuleHandle( string lpModuleName );
	
	[DllImport( "user32.dll" )]
	private static extern IntPtr GetForegroundWindow();

	// LowLevelKeyboardProc template
	private delegate IntPtr LowLevelKeyboardProc( int nCode, IntPtr wParam, IntPtr lParam );
}

public enum GlobalKeyCode : int 
{
	LBUTTON  = 0x01,
	RBUTTON  = 0x02,
	CANCEL   = 0x03,
	MBUTTON  = 0x04,
	BACK     = 0x08,
	TAB      = 0x09,
	CLEAR    = 0x0C,
	RETURN   = 0x0D,
	SHIFT    = 0x10,
	CONTROL  = 0x11,
	MENU     = 0x12,
	PAUSE    = 0x13,
	CAPITAL  = 0x14,
	ESCAPE   = 0x1B,
	SPACE    = 0x20,
	PRIOR    = 0x21,
	NEXT     = 0x22,
	END      = 0x23,
	HOME     = 0x24,
	LEFT     = 0x25,
	UP       = 0x26,
	RIGHT    = 0x27,
	DOWN     = 0x28,
	SELECT   = 0x29,
	PRINT    = 0x2A,
	EXECUTE  = 0x2B,
	SNAPSHOT = 0x2C,
	INSERT   = 0x2D,
	DELETE   = 0x2E,
	HELP     = 0x2F,

	// Number keys
	N0		= 0x30,
	N1		= 0x31,
	N2		= 0x32,
	N3		= 0x33,
	N4		= 0x34,
	N5		= 0x35,
	N6		= 0x36,
	N7		= 0x37,
	N8		= 0x38,
	N9		= 0x39,
	
	// Letter keys
	A	= 0x41,
	B	= 0x42,
	C	= 0x43,
	D	= 0x44,
	E	= 0x45,
	F	= 0x46,
	G	= 0x47,
	H	= 0x48,
	I	= 0x49,
	J	= 0x4A,
	K	= 0x4B,
	L	= 0x4C,
	M	= 0x4D,
	N	= 0x4E,
	O	= 0x4F,
	P	= 0x50,
	Q	= 0x51,
	R	= 0x52,
	S	= 0x53,
	T	= 0x54,
	U	= 0x55,
	V	= 0x56,
	W	= 0x57,
	X	= 0x58,
	Y	= 0x59,
	Z	= 0x5A,

	LWIN		= 0x5B,
	RWIN		= 0x5C,
	APPS		= 0x5D,
	SLEEP		= 0x5F,

	// Numpad keys
	NUMPAD0		= 0x60,
	NUMPAD1		= 0x61,
	NUMPAD2		= 0x62,
	NUMPAD3		= 0x63,
	NUMPAD4		= 0x64,
	NUMPAD5		= 0x65,
	NUMPAD6		= 0x66,
	NUMPAD7		= 0x67,
	NUMPAD8		= 0x68,
	NUMPAD9		= 0x69,
	MULTIPLY 	= 0x6A,
	ADD			= 0x6B,
	SEPARATOR 	= 0x6C,
	SUBTRACT 	= 0x6D,
	DECIMAL  	= 0x6E,
	DIVIDE		= 0x6F,

	// Function keys
	F1		= 0x70,
	F2		= 0x71,
	F3		= 0x72,
	F4		= 0x73,
	F5		= 0x74,
	F6		= 0x75,
	F7		= 0x76,
	F8		= 0x77,
	F9		= 0x78,
	F10		= 0x79,
	F11		= 0x7A,
	F12		= 0x7B,
	F13		= 0x7C,
	F14		= 0x7D,
	F15		= 0x7E,
	F16		= 0x7F,
	F17		= 0x80,
	F18		= 0x81,
	F19		= 0x82,
	F20		= 0x83,
	F21		= 0x84,
	F22		= 0x85,
	F23		= 0x86,
	F24		= 0x87,

	// Lock keys
	NUMLOCK		= 0x90,
	SCROLL		= 0x91,

	// Modifiers
	LSHIFT		= 0xA0,
	RSHIFT		= 0xA1,
	LCONTROL 	= 0xA2,
	RCONTROL 	= 0xA3,
	LMENU		= 0xA4,
	RMENU		= 0xA5,

	// Media keys
	BROWSER_BACK 		= 0xA6,
	BROWSER_FORWARD 	= 0xA7,
	BROWSER_REFRESH 	= 0xA8,
	BROWSER_STOP 		= 0xA9,
	BROWSER_SEARCH 		= 0xAA,
	BROWSER_FAVORITES 	= 0xAB,
	BROWSER_HOME		= 0xAC,
	VOLUME_MUTE 		= 0xAD,
	VOLUME_DOWN 		= 0xAE,
	VOLUME_UP 			= 0xAF,
	MEDIA_NEXT_TRACK 	= 0xB0,
	MEDIA_PREV_TRACK 	= 0xB1,
	MEDIA_STOP 			= 0xB2,
	MEDIA_PLAY_PAUSE 	= 0xB3,
	LAUNCH_MAIL 		= 0xB4,
	LAUNCH_MEDIA_SELECT = 0xB5,
	LAUNCH_APP1 		= 0xB6,
	LAUNCH_APP2 		= 0xB7,

	// Symbols
	COLON			= 0xBA,
	PLUS			= 0xBB,
	COMMA			= 0xBC,
	HYPHEN			= 0xBD,
	PERIOD			= 0xBE,
	FSLASH			= 0xBF,
	TILDE	 		= 0xC0,
	BACKQUOTE 		= 0xC0, // duplicate of tilde
	LSQUAREBRACKET 	= 0xDB,
	BSLASH			= 0xDC,
	PIPE			= 0xDC,	// duplicate of back slash
	RSQUAREBRACKET	= 0xDD,
	QUOTE			= 0xDE,

	// ???
	PLAY		= 0xFA,
	ZOOM		= 0xFB,
}