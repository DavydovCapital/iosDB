# Cobra Strike

Native Unity 6 3D FPS for iPhone. Not a web app.

Five missions, first-person gunplay, touch sticks + keyboard/mouse, arcade-smooth movement.

## Play (Windows)

`Builds/CobraStrike3D.exe` after a Windows build, or open the project in Unity 6000.5.9f1 and press Play.

- WASD move, mouse look, left click fire, right click / Shift ADS, R reload
- On iPhone: left stick move, right side drag look, FIRE / ADS / R

## Missions

1. BLACKSITE DAWN
2. IRON HARBOR
3. ASH RIDGE
4. DEAD GRID
5. COMMAND CORE

## Build

Unity menu:

- `Cobra/Build Game Scene`
- `Cobra/Build Windows`
- `Cobra/Build iOS` (writes `Builds/iOS` Xcode project)
- `Cobra/Build Scene And iOS`

Command line:

```
Unity.exe -batchmode -nographics -quit -projectPath <this folder> -executeMethod SceneBuilder.BuildSceneAndiOS -logFile Logs/ios-export.log
```
