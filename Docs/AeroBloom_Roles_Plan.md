 т  # AeroBloom: Frutiger Aero Parkour

## Elevator Pitch

**AeroBloom** is a first-person Frutiger Aero parkour game about restoring a glossy sky-garden network. The player sprints, slides, dashes, wall-runs, uses bubble springs, and collects **Aero Seeds** while syncing floating relays across a clean glass-and-water world.

Reference direction: the game can be close in mood to **Frutiger Aero Simulator**, but the hook should be ours: a timed relay-restoration course with collectibles, movement combos, and a small eco-tech story instead of only freeform parkour.

## Team Split For 3 People

### Person 1: Gameplay / Level Design

Owner of the playable feel.

- Tune first-person movement: sprint, jump, slide, dash, double jump, wall-run, bounce pads, speed gates.
- Build and test the main route from start to finish.
- Place checkpoints so the game is challenging but not frustrating.
- Make sure the whole run is completable in 2-5 minutes.
- Final deliverables: playable course, tuned movement, checkpoints, timer, finish portal.

### Person 2: Art / Theme / World Dressing

Owner of Frutiger Aero scoring.

- Import the FlooferLand Frutiger Aero asset pack if the team bought it.
- Replace prototype primitives with pack props where useful: MSN Buddy, buildings, globe, music props.
- Improve materials: glossy glass, blue water, lime greens, white panels, bubbles, clean UI-like signs.
- Add skybox, reflective water, floating objects, visible route language.
- Final deliverables: beautiful themed scene, props, lighting, post-processing, screenshots.

### Person 3: Audio / Story / Presentation / Build

Owner of polish and defense.

- Add music loop and SFX from the asset pack or replace the generated prototype tones.
- Write the short story: restoring AeroBloom relays and caching Aero Seeds.
- Record gameplay trailer or gameplay video.
- Prepare presentation speech: hook, mechanics, theme, what is innovative, what each teammate did.
- Build Windows version and upload to itch.io after presentation day before 11:59 pm.

## Rubric Strategy

- **Complexity (20):** full loop: movement, level, checkpoints, collectibles, timer, finish, UI, audio, build menu.
- **Game Design (10):** route teaches movement in stages: basic jumps, bounce, moving platform, wall-run, spiral finale.
- **Fun (10):** speed, FOV feedback, short respawns, collectibles, timer replayability.
- **Innovation (10):** Frutiger Aero as mechanics: Aero Seeds, bubble springs, glass wall-runs, relay syncing.
- **Theme (20):** water, glass, sky blue, lime eco-tech, glossy skeuomorphic HUD, early-2000s network story.
- **Graphics (10):** URP bloom, fog, transparent materials, skybox, floating bubbles, props.
- **Audio (10):** ambient pad, pickup tones, checkpoint tones now; replace/extend with asset pack audio later.
- **Story (10):** simple coherent objective: restore a sky-garden network before entering the Bloom Portal.
- **Presentation (15):** show trailer first, then live run, then explain how the theme is inside mechanics.

## Current Prototype Controls

- `WASD` or arrows: move
- Mouse: look
- `Shift`: sprint
- `Space`: jump / double jump / wall-jump
- `Ctrl` or `C`: slide while sprinting
- `E`: dash
- `R`: respawn at last relay
- `Esc`: unlock cursor

## Unity Workflow

1. Open the project in Unity 6000.3.5f2.
2. Press Play in `Assets/Scenes/Field.unity`; the prototype generates automatically if the scene has no `AeroLevelDirector`.
3. For a saved scene, use `AeroBloom > Build Playable Prototype Scene`.
4. For a Windows build, use `AeroBloom > Build Windows Prototype`.
5. Import the asset pack with `Assets > Import Package > Custom Package...`, then replace prototype props gradually.

## Minimum Final Build Checklist

- Main route can be completed from start to finish.
- All checkpoints work.
- Player cannot softlock after falling.
- HUD shows timer, seeds, relays, speed.
- Finish portal ends the run.
- Audio is audible but not annoying.
- Windows build runs on a different machine.
- itch.io page has screenshots, controls, and a downloadable Windows build.

