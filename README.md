# GlobalInput - Unity3D tool
This is a script that will hook into windows event system and add a callback function to send keyboad events to unity even when unity doesnt have focus.

Place the script anywhere in your assets folder and it will take care of the rest.

### Functions available:
```C#
// Keycodes are stored in GlobalKeyCode
// e.g. GlobalKeyCode.LEFT is the left arrow key

// GetKey
GlobalInput.GetKey( GlobalKeyCode );
	// Works the same as Input.GetKey( KeyCode )
	// Will output TRUE if the key in the parentheses is being held
    
// GetKeyDown
GlobalInput.GetKeyDown( GlobalKeyCode );
	// Works the same as Input.GetKeyDown( KeyCode )
	// Will output TRUE only ONCE if the key in the parentheses is being held
    
// GetKeyUp
GlobalInput.GetKeyUp( GlobalKeyCode );
	// Works the same as Input.GetKeyUp( KeyCode )
	// Will output TRUE only ONCE when the key in the parentheses is lifted after being pressed
```
