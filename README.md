# BDM4065ControlAPI
A C# API for controlling Philips BDM4065 monitor via its RS232 interface

This is a fork from andy-w's initial work, remodelled as a library to be integrated in other pojects.

The compiled .dll in ./PhilipsSerial/bin/debug currently uses .NET Framework 4.6 for compatibility with Mono on macOS and Linux. The code will compile just fine if you wish to use 4.7 and later though.

# How to implement
You can either download the pre-compiled .dll under ./PhilipsSerial/bin/debug/PhilipsSerial.dll or open the PhilipsSerial project in a C# editor of your choosing (Visual Studio, Jetbrains Rider, Xamarin Studio etc) and compile it from there.

You can then include the compiled .dll in your project to add a GUI, command line interface or other layers.

# Basic usage
The API uses a single class, Monitor, with a single constructor which takes a SerialPort object as an argument. To instantiate a Monitor object you first need to define a serial port on your system, available in the System.IO.Ports namespace.

Here's an example using the COM4 connection under Windows, instantiating the Monitor object and sending a command for toggling the nonitor on or off. You need to check and make sure which serial port your system is using, or write additional code to identify it for you. Under Linux or macOS, the serial port should be somewhere under /dev/tty*.

SerialPort port = new SerialPort("COM4");
Monitor philipsMonitor = new Monitor(port);
monitor.togglePower();

# Interface
Since the original version of this software was implemented directly into a Windows Forms class, this API version has replaced the GUI controls with a public interface, hiding the internal structure. The following methods are made public:

```
setInputHDMI(): void            - Switches to HDMI input<br />
setInputMHL(): void             - Switches to HDMI-MHL input<br />
setInputDP(): void              - Switches to Displayport input<br />
setInputMiniDP(): void          - Switches to Mini Displayport input<br />
setInputVGA(): void             - Switches to VGA input<br />
setPowerOn(): void              - Turns the monitor on<br />
setPowerOff(): void             - Sets the monitor to sleep<br />
togglePower(): void             - If the monitor is on, it's turned off, and vice versa<br />
setVolume(value: int): void     - Sets the volume to an int value 0 - 100 (NOT YET WORKING)<br />
getCurrentInput(): string       - Returns the current input as a string
```
