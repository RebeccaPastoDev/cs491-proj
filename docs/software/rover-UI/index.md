# 🖥️ Rover UI

The Rover UI provides an interface for monitoring rover information and managing mission tasks within the `LunarSouthPole_Cesium` Unity scene. It includes rover status displays, mission planning controls, and desktop and VR interfaces.

## 📊 Rover Status Display

The `RoverStatusUI.cs` script displays important rover information, including:

- **Status:** Shows whether the rover is Online or in Standby.
- **Position:** Displays the rover's X, Y, and Z coordinates.
- **Speed:** Displays the rover's speed in meters per second.
- **Heading:** Shows the rover's direction in degrees.
- **Mission Progress:** Displays the current mission status.

The status display starts in Standby, and the mission progress starts as Not Started.

## 🎮 Mission Controls

The interface includes controls for managing the rover's mission:

- **Start Mission:** Changes the rover status to Online and displays In Progress.
- **Pause Mission:** Changes the rover status to Standby and displays Paused.
- **Reset Mission:** Stops the rover's movement, returns it to its starting position and rotation, and resets the mission status to Not Started.

## 📋 Mission Planning Interface

The `MissionPlanningUI.cs` script provides an interface for selecting and assigning rover tasks.

Users can select from four available tasks:

- **Find Water**
- **Collect Water**
- **Deposit Water at Tank**
- **Return to Base**

The selected tasks appear in the Assigned Tasks section. If no tasks are selected, the interface displays "No tasks selected."

## 🗺️ Route Overview and Mission Progress

The interface includes a Route Overview that displays the planned destinations based on the selected tasks.

The route can include:

- Base Camp
- Water Search Area
- Water Collection Site
- Water Tank

The Mission Progress section provides a place to display mission status and updates. The interface also uses a page switcher to display the mission progress page when a mission is submitted.

## 🚀 Mission Assignment

The Assign Mission button prepares a mission request using the selected tasks and route information.

Before submission, the system checks the communication connection and requires recorded-data test mode. It also uses mission acknowledgments to receive information about whether a mission request was accepted.

The assignment button is disabled when no tasks are selected or while waiting for an acknowledgment.

## 🔄 Desktop and VR Interface

The Rover UI supports both desktop and VR displays.

The `RoverStatusUI.cs` script updates the rover information on both interfaces. The `MissionPlanningUI.cs` script also synchronizes task selections between desktop and VR controls.

Both interfaces include mission-planning controls and an Assign Mission button.

## 🔮 Future Improvements

- Continue improving the user interface.
- Enhance the display of rover information.
- Improve the overall user experience.
- Expand interface functionality as the project develops.