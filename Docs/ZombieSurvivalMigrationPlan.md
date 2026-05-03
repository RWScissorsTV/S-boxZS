# Zombie Survival on Sandbox Migration Plan

## Direction

Rebuild Zombie Survival on top of the Facepunch Sandbox base instead of porting the old project wholesale. The old project is the gameplay reference. The new project should keep Sandbox's player, inventory, weapon, physics, UI, spawn, cleanup, and network patterns wherever they already solve the problem.

## Principles

- Prefer adding small ZS-specific components around Sandbox systems over replacing Sandbox systems.
- Keep host-authoritative game rules: round phase, roles, infection, wins, loadouts, purchases, barricade damage.
- Keep ZS code isolated under `Code/ZombieSurvival` unless a tiny hook in an existing Sandbox file is clearly cleaner.
- Build a playable vertical slice early, then deepen features.
- Treat old ZS assets as optional references; use Sandbox assets/prefabs first when they fit.

## Old To New Mapping

- `ZSGame` becomes `ZombieSurvivalGame`, a `GameObjectSystem` that uses Sandbox player events.
- `PlayerState` role data moves onto `PlayerData` through partial class extensions.
- `PlayerRoleManager` becomes small role applicator logic: tags, health, inventory restrictions, movement, visuals.
- Old `Inventory`, `Weapon`, `AmmoComponent`, and reload code are replaced by Sandbox `PlayerInventory`, `BaseCarryable`, `BaseWeapon`, `BaseBulletWeapon`, and ammo support.
- Old `ObjectPickup` is not ported directly. Build-phase prop movement should use Sandbox `Physgun`, and ZS nails/barricades should hook into Sandbox tool/weapon components.
- Old `DamageableObject` becomes a smaller barricade/damage component that implements `Component.IDamageable`.
- Old `ZombieMeleeAttack` becomes a Sandbox-native zombie attack component or weapon that uses Sandbox trace and damage conventions.
- Old chat/kill feed should use Sandbox `Chat`, `Feed`, and notices.
- Old HUD gets replaced by small Razor panels that sit alongside Sandbox `Vitals`, `Inventory`, and `Scoreboard`.

## Implementation Order

1. Round and role foundation
   - Waiting for players.
   - Build phase.
   - Survival phase.
   - Round end/reset.
   - Initial zombie selection.
   - Human death converts to zombie.
   - Role tags and health.

2. Minimum zombie gameplay
   - Zombie melee attack.
   - Zombie movement tuning.
   - Zombies cannot use human inventory/tools.
   - Humans and zombies cannot damage teammates.
   - Import old zombie/headcrab assets as player-controlled forms without replacing the Sandbox player prefab.
   - Trigger form-specific melee animations and sounds from the Sandbox-native attack component.

3. Barricade core
   - Damageable barricade component.
   - Props can be marked as barricade material.
   - Zombies damage barricades.
   - Barricades break cleanly.
   - Round reset restores or cleans barricade state.

4. Nailing and repair
   - Use Sandbox Toolgun or a ZS nail tool mode rather than the old custom toolgun.
   - Nail held/aimed props to backing surfaces.
   - Nail count affects barricade health.
   - Repair tool uses Sandbox weapon/carryable patterns.

5. Human loadouts and shop
   - Define curated ZS loadouts from existing Sandbox weapon prefabs first.
   - Add a buy menu only after roles, damage, and barricades work.
   - Coins/rewards attach to `PlayerData`.
   - Preserve Sandbox pickup/drop where possible.

6. ZS HUD
   - Phase timer.
   - Human/zombie counts.
   - Role indicator.
   - Barricade health prompt/panel.
   - Buy menu entry point.

7. Visual roles and zombie forms
   - Citizen zombie first, using existing citizen body/morph/clothing behavior.
   - Add walker/headcrab forms after the core loop is stable.
   - Reuse old custom zombie/headcrab assets only where Sandbox has no better base asset.

8. Spawn and map rules
   - Human/zombie spawn groups if maps provide them.
   - Late join rules.
   - Round cleanup.
   - Disable or gate Sandbox spawn menu/tool actions by phase and role.
   - Curate the Q menu so ZS players see barricade-relevant props instead of the full Sandbox catalog.
   - Add build points/currency for prop spawning before introducing deeper economy/shop rules.

9. Balance and polish
   - Timers, health, movement, zombie damage, repair limits.
   - Sounds, notices, kill feed details.
   - Better visual feedback for infection, round start, barricade state.

10. Hardening
   - Build verification after each slice.
   - Test local host flow.
   - Test two-player flow.
   - Test late join/disconnect.
   - Test round reset with damaged/nail props.

## First Playable Target

Two players join. After the build timer, one becomes a zombie. The zombie has a melee attack and can kill humans. Humans win if the timer expires; zombies win if every human is converted. Barricades are introduced immediately after this works.

## Current Status

- Round and role foundation is implemented and build-verified.
- Minimum zombie gameplay is implemented and build-verified: zombies get melee, movement tuning, no human inventory, and same-role damage protection.
- Barricade core is implemented and build-verified: build-phase prop spawning creates ZS barricades, zombies can damage/break them, and round transitions clean them up.
- First repair/fortify pass is implemented: humans can press barricades during build/survival to repair damage or raise max health up to a configurable cap.
- First ZS HUD pass is implemented in the shared system UI: phase, timer, role, objective, and human/zombie counts.
- Q menu first pass is implemented: ZS mode shows the prop-focused spawn flow, uses build points for prop spawning, hides the utility/tool side, and blocks Toolgun actions while leaving the Physics Gun path intact.
- Minimum zombie gameplay has been deepened with imported old zombie/headcrab model, animgraph, and sound assets. ZS now mounts those forms as a visual layer on the Sandbox player, keeping the base player/controller systems intact.
- Next slice: in-game test and tune zombie form scale/collision/camera, then decide between Physics Gun/build feel or weapon purchase/loadout economy.
