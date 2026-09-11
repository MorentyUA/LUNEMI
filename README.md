# LUNEMI

A 2D game about a sad astronaut on an abandoned planet. A dark action-platformer with metroidvania and survival-horror elements: a lone miner with a pickaxe, a battery-powered flashlight and a cracked helmet descends into caves where light is the main resource and darkness is the main enemy. Built with Unity 6 (URP 2D).

An early 2D prototype of the concept that later grew into **Dust Vein** (the project settings still carry `productName: Dust Vein`).

## About the game

- **Light as a resource.** The flashlight (URP `Light2D`) drains constantly while it is on. Once it is empty, you sit in the dark.
- **Portable charger.** Hold **G** and the charge flows from the battery pack into both the flashlight and the player's health at the same time. The charge is finite.
- **Charging stations.** Trigger zones with an unlimited source: hold **E** to refill the battery pack, the flashlight and HP. Effectively safe rooms / checkpoints.
- **Pickaxe.** Tap LMB for a single swing, hold for a swing loop. The attack direction (forward / up / down) is locked at the start of the swing from vertical input. Breaks blocks and hits enemies with knockback.
- **Breakable blocks.** Cracked sprite at 50% HP, shake on hit, particles, a loot table with drop chances and amounts. An `invulnerable` mode for decorative bedrock.
- **Pushing crates.** RMB next to a block enters the push stance, adding movement toward the block turns it into push walk. The player's collider widens forward and the block smoothly lowers its mass, but **only** when the player pushes it from the side rather than standing on top.
- **Ledge grab.** Two probes (lower occupied, upper free) while falling toward a wall. The player grabs the ledge, plays the `hug` animation and smoothly climbs up to a position computed from the collider bounds.
- **Head bounce.** One free bounce off an enemy per landing. A second attempt is punished with damage and a slide-off.
- **The Biomorf enemy.** Patrol → aggro → attack → death. Sees the player via line of sight, remembers them after losing contact, flanks when the player is directly below, waits for the player to land instead of running underneath, jumps over obstacles with a landing check and only drops off ledges when the fall is safe.
- **Gibs and loot.** An enemy first finishes its death animation and only then bursts into parts, blood particles and loot. Loot can shrink and vanish over time.
- **Horror feedback.** Blood overlays pulse at low HP and flash on hit. The helmet HUD switches between 4 sprites by health state, turns red and shakes below 30% HP.
- **Inventory.** A slot grid on **Tab** with drag-and-drop, stacks, merging, swapping and a ghost icon while dragging. Opening the inventory locks player control. Items are picked up with **E** and fly to the player while spinning and shrinking.
- **Camera.** Cinemachine 3. Hold **W / S** and the camera pans up / down after a delay so you can scout the darkness ahead. Parallax backgrounds.

## Controls

| Action | Key |
|---|---|
| Move | **A / D** |
| Jump | **Space** |
| Pickaxe swing (tap / hold) | **LMB** |
| Aim swing up / down | **W / S** during the swing |
| Push a block | **RMB** next to a block (+ move toward it) |
| Look up / down | hold **W / S** |
| Flashlight | **F** |
| Portable charger | hold **G** |
| Charging station / pick up item | **E** |
| Inventory | **Tab** |

All keys are exposed in the inspector as `KeyCode` fields and can be changed without touching the code.

## Script structure

Everything lives in `Assets/Scripts/`:

| Script | What it does |
|---|---|
| `Player/PlayerController.cs` | Rigidbody2D movement, jump, twin ground raycast that ignores triggers and steep surfaces, hybrid pickaxe attack via Animation Event, push / push walk with a dynamic collider, ledge grab with smooth climb, dust on footsteps and landing, stun, knockback, blinking, `TryBounce()` for head bounces, `DisableControl()/EnableControl()` |
| `Player/PlayerHealth.cs` | HP, damage with attacker position, death (disables Player↔Enemy collisions), respawn, health bar, low-HP blood overlays and damage flashes, blood particles |
| `Player/Flashlight2DController.cs` | `Light2D` flashlight on **F**, battery with per-second drain, auto shut-off at zero, 3-state icon, charging API |
| `Player/PortableCharger2D.cs` | Portable charger on **G**: charges the flashlight and HP with separate multipliers, shakes its icon, pulses targets |
| `Player/StaticChargingStation2D.cs` | Charging station on **E**: unlimited source for the charger, flashlight and HP, sprite color change and pulsing |
| `Player/CinemachineLookVertical2D.cs` | Look up / down through `CinemachinePositionComposer.TargetOffset` with a hold delay and separate move / return speeds |
| `Enemys/EnemyController.cs` | Biomorf AI: patrol with turns at walls and pits, aggro by line of sight and facing, chase memory, flanking position, waiting for the player to land, obstacle jumping, safe step-down, damage via Animation Event, head bounce logic |
| `Enemys/EnemyHealth.cs` | Enemy HP, hurt flash, knockback, hit particles, drop table, deferred gibs after the death animation. Also holds the shared `DropAutoDestroy` helper |
| `Enemys/EnemyDamageZone.cs` (`EnemyDamage`) | Contact damage aura on an interval within a radius |
| `Enemys/DeathTrigger2D.cs` | Instant-kill zone (pits, spikes) with a tag check and a trigger-once option |
| `BreakableBlock.cs` | Block with HP, cracked sprite, shake, particles, loot table and mass reduction on side push walk |
| `ParallaxEffect.cs` | Parallax of a background layer relative to the camera |
| `UI/HelmetUI.cs` | Helmet HUD: 4 sprites by HP, red overlay, shake below 30% and on hit |
| `UI/InventoryManager.cs` | Inventory singleton on **Tab**, `AddItem` with stacks and overflow, `MoveOrStackItem` |
| `UI/InventorySlot.cs` | Drag-and-drop slot, auto-wires `Background` / `ItemIcon` / `AmountText`, ghost icon |
| `UI/PickupItem.cs` | Item pickup on **E** with a fly-to-player animation |
| `UI/ChargingPulseTarget.cs` | Color and scale pulsing for `Image` / `SpriteRenderer` with multiple simultaneous sources |

## Prefabs

- `Assets/Prefabs/Player.prefab` — the player with pickaxe, flashlight, helmet and charger.
- `Assets/Prefabs/Biomorf/` — the `Biomorf` enemy, `Blood` and three `DeadPart` gibs.
- `Assets/Prefabs/Stone block/`, `Assets/Prefabs/Countainer block/` — breakable blocks and their debris.
- `Assets/Prefabs/BigCharge.prefab` — a charge pickup.

## Layers and tags

- Layers: `Player`, `Ground`, `Enemy`, `Wall`, `Block`, `ChargeStation`, `Item`.
- Tags: `Player`, `Enemy`.

## Tech stack

- Unity **6000.3.8f1**
- Universal Render Pipeline 17.3 (2D Renderer, `Light2D`)
- Cinemachine 3.1.6
- 2D Animation, Aseprite Importer, PSD Importer, Tilemap
- Input System 1.18 is installed, but the scripts run on the legacy `UnityEngine.Input`
- TextMeshPro for UI
- Scenes: `Assets/Scenes/Lvl1.unity` (in build), `Assets/Scenes/Lvl2.unity`, 1920×1080 fullscreen

## How to run

1. Open the project folder in Unity Hub (version 6000.3.8f1).
2. Open the scene `Assets/Scenes/Lvl1.unity`.
3. Press Play.

## Asset licenses

The project uses third-party packs: **Cartoon FX Remaster FREE** (JMO Assets), **Retro Bit FX**, **Dust Particles** and **VFX Pack Impact Wallcoeur Free Version**. All rights belong to their respective authors.

---

**MORENTY**
