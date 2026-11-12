# Brickbreaker Base (scripts-only package source)

Copy the `BrickbreakerBase` folder into a Unity project's `Assets/` folder (tested target: Unity 6,
URP 2D template). It contains scripts only: no prefabs, scenes, sprites, audio, animation or fonts.

## Use it

1. Open the project. A dialog offers to import the TextMesh Pro essentials (Import now / Later /
   Don't ask again). The editor setup script also adds the `Bouncy` tag automatically.
   Re-run the checks any time with **Tools > Brickbreaker Base > Run Setup**.
2. In any scene: **GameObject > Brickbreaker Base > Game Bootstrap** (or add the
   `BrickbreakerBootstrap` component to an empty GameObject).
3. Press Play. The bootstrap builds the camera, walls, grid, launcher, balls and all UI at runtime.

## Controls

Drag inside the field to aim (dotted preview), release to fire. START on the menu begins a run.

## Layout

```
BrickbreakerBase/
  Runtime/    game logic, BrickbreakerBootstrap, WorldFactory (sprites/objects), UIFactory + HudBuilder (UI)
  Runtime/HUD HUD components
  Editor/     BrickbreakerBaseSetup (adds the Bouncy tag, offers the TMP essentials)
```

Everything lives in the `BrickbreakerBase` namespace and the default assembly (no asmdef, so it
works whichever optional packages the project has).

## Extension points

- New ball types: subclass `BaseBallHitEffect` and use it in `WorldFactory.CreateBall` / `LaunchManager.AddBall`.
  `BrickManager.GetNeighbors/GetBricksInRow/GetBricksInColumn` are there for spread effects.
- Sound: fill `BrickbreakerBootstrap.Sounds` (one entry per `SoundType`) or call `SoundManager.SetClip`.
- Rules/tuning: public fields on `BrickbreakerBootstrap`, `GameManager`, `BrickManager`, `CoinManager`,
  `LaunchManager`, `LauncherController` and `BallController`; wave/HP tables are in `BrickConfig`.

## Exporting

In Unity: select `Assets/BrickbreakerBase`, then Assets > Export Package.
