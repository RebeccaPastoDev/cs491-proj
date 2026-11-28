# 🥽 Unity VR Development

## 🥽 VR Headset Integration

The project includes VR support using the Meta Quest 3S headset. Unity uses OpenXR and the XR Interaction Toolkit to support the VR environment and interactions.

## 🎥 VR Camera Setup

The VR camera is configured through the XR Origin in Unity. This allows the user to view the lunar environment through the VR headset.

## 📊 VR Heads-Up Display (HUD)

The VR HUD displays important rover information, including:

- **Position:** Rover X, Y, and Z coordinates.
- **Speed:** Rover speed in meters per second.
- **Heading:** Rover direction in degrees.
- **Battery:** Rover battery voltage.

The `ROSHUD.cs` script updates the information displayed on the VR HUD. If information is unavailable, the display shows `N/A`.

## 🎮 VR Controller and UI Interaction

The VR interface includes mission-planning controls for selecting tasks:

- Find Water
- Collect Water
- Deposit Water
- Return to Base

The `MissionPlanningUI.cs` script connects the desktop and VR task toggles. When a task is selected on either interface, the selection is synchronized with the other interface.

The VR interface also includes an assigned-tasks display and an **Assign Mission** button connected to the mission assignment function.

## 🔮 Future Improvements

- Continue improving the VR experience.
- Enhance the user interface and overall usability.
- Improve integration between VR components and the lunar environment.
- Expand VR functionality as the project develops.
- Continue testing and refining the VR system.