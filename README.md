# MTOvJoyFeeder
Many To One (technically many to many) Virtual Joystick Feeder. Maps one or more physical joysticks to one virtual joystick.

This project is based on https://github.com/shauleiz/vJoy and requires vJoy to be installed and working.

A few scenarios where this might be useful:

* Combine input from two controllers into one virtual controller such as a flight stick and throttle. Allowing each controller to feed information to a different set of buttons/axis.
* Combine input from two or more controllers into one virtual controller where each controller is mean to be used exclusively of the others (one at a time). This is good for a setup where the user has the option to select from multiple joysticks but you don't want to rempa the application each time a differnt controller is used.
* An application doesn't recognize a controller but will recognize a vJoy controller. In this case the virtual controller is just being used as a pass through for the physical controller.
* Multiple players for a single player game where several people have their own controller and can either take turns controlling the game or have specific tasks (input controls) they are responsible for.
* Heads up/on on one games can be converted to multi player team games. An example would be with a fighting game such as Street Fighter II, each character could be mapped to a virtual joystick being feed by a team of one or more players. The players would then need to coorindate their movement to control the character.

### How it works

The feeder sends the last data it received from a given controller to the virtual joystick. The simplest way to use this is when only one controller will be used at a time, or when only two or more joysticks are used in tandom but not utilizing overlapping controls. For example, using the X and Y axis from one joystick and the Z axis from a second joystick.

The most confusing way to utilize controller mapping is when you want to use two or more controllers at the same time to control the same action in the aplication as in the Heads up example from above. This can be very entertaining. For example, if two controllers (Joystick1 and Joystick2) are connected to one virtual joystick (vJoy1), the following scenarios will play out when input is received...

* Joystick1 presses and holds Button1 - vJoy1 presses and holds Button1
* Joystick2 presses and holds Button1 - no change
* Joystick2 releases Button1 - vJoy1 releases Button1
* Joystick 1 releases Button1 - no change

In the above scenario Joystick2 usurped the button press of Joystick1.

* Joystick1 moves left 50% and holds - vJoy1 moves left 50% and holds
* Joystick2 moves left 100% and the recenters - vJoy1 moves left 100% (from 50%) and then ceneters
* Joystick1 moves left 100% (from 50%) and then recenters - vJoy1 moves left 100% (from center) and then recneters

In the above scenario left movement might appear erratic as the two joysticks change their positions.

### Todo (in order of importance):

* Move some properties from JoystickInfo to JoystickConfig, currently there are some repeated elements that just get copied from JoystickConfig to JoystickInfo
* Improved documentation/installation instructions
* Determine current support for Force Feedback in vJoy/vXbox/SharpDX
* Test with all available joysticks
* Add timer to detect controllers when they are added
* Create Windows Service
* Create Launchbox plugin
* Need to load state for PointOfViewControllers1 - PointOfViewControllers3
* Investigate implementing oJoystickInfo.oJoystick.SetNotification()
* Clean up some calls to WriteToEventLog that are using String.Format
* Wire up Guide button when SharpDX supports it - https://github.com/sharpdx/SharpDX/issues/1074

### Changes (oldest to newest):

* Loads all joystick data and virtual joystick data
* Loop that directs all input from acquired joysticks to first virtual stick
* Adding support for a configuration file

* Added configuration name/location settings to app.config
* Accepts configuration location from command line
* Added mappings for joysticks from config
* Added If DEBUG directive around Polling logging
* Added handling of removed controller

* Added WriteToEventLog to replace all Console.Write calls
* Added better configuration/command line handling.
* Added support for multiple virtual controls associated with physical controllers
* Added config support for SleepTime

* Realised vJoy doesn't emulate xBox controllers!
* Converted using vJoyInterface to vGenInterface to get both vJoy and xBox controller support
* Found out plugining in xBox controllers in vGenInterface is buggy and didn't work
* Added vXboxInderface.dll support using vXboxInterfaceWrap.cs, kept vGen interface for vJoy
* In process of wiring up vitual controls to xBox
* Added an onExit routine to release vXBox joysticks

* Normalized and wired up Z Axis

* Wired up Diagonals on DPad for XBox Controller
* Validated All XBox inputs - Guide Button is the only one left to wire up

* Upgraded packages 
* Added shell for Windows Service
* Moved console application to its own directory

* Cleaned up debug code
* Added output for release mode
* Improved vxBox ReleasevXboxJoysticks method
* Fixed max value for in vXboxInterfaceWrap as it was 1 higher than it should have been causing the number to go negative on conversion to a Short

* Added inversion option to physical joysticks an support in NormalizeRange(), normally the Y Axis will be inverted for XBox controllers
* Added Ranges For X, Y, Z, RX, RY for controllers and virtual controllers for use by NormalizeRange() 

* Tested NormalizeRange()
* Added formatting to JSON output to remove extra newlines and spaces before integers in arrays
* Fixed issue with not dynamically setting size of oNewJoystickInfo.Map_Buttons array by using JoystickConfig.Map_Buttons instead
* Added an Assets folder to hold references and copy out required files from