# NetworkingReworked
**(Now redone from previous version this week, solo-client supported)**

**NetworkingReworked** is a client-side mod for REPO that makes multiplayer feel like singleplayer. No more input lag, rubberbanding, or weird delay when picking up or throwing objects.

This mod doesn't change the host or the server. It just makes your game respond the way it *should*.

---

## What It Actually Does

REPO was designed so the host controls nearly everything. That means when you try to grab, move, or throw an object as a client, there's a full round-trip delay while the host approves it. That delay is why everything feels unresponsive.

**NetworkingReworked changes that:**

- **You act like the owner of every object** — unless something else (like an enemy or another player) touches it.
- When you grab or throw something, it's fully simulated on your side immediately.
- If you stop interacting, the object gradually re-syncs with the host to avoid glitches.
- If someone else grabs it, or it behaves unexpectedly (like from a collision), your control is gracefully handed back.

The result? Everything just feels responsive. Picking up and throwing items, dragging carts, and interacting with the world feels like it does in singleplayer.

---

## Key Features

- **Now no longer requires host installation**
- Zero-grab-delay and smooth object control
- Soft sync after release to avoid sudden snapping
- Graceful handoff if another player or enemy touches the object
- Hinge doors, carts, and other complex physics behave naturally

---

## Installation

1. Install [BepInEx](https://github.com/BepInEx/BepInEx/releases) into your REPO folder.
2. Drop this mod’s `.dll` into `BepInEx/plugins`.
3. Launch REPO and join any multiplayer game.

Only **you** need the mod. It works even if the host is completely unmodded.

---

## Known Quirks

- Grabbing the same object at the same time as another player might cause ownership to bounce briefly
- Some rare scripted objects (like quest items or special valuables) might behave oddly. Let me know if you find any
- Doors still rely on host assumptions, had issues with desync but I'll post an update when I have time
- The system uses a passive resync, dropping objects/throwing objects might show a brief jitter to re-align with the host's expectations

---

## Feedback

If anything feels off or you'd like to suggest improvements, feel free to reach out on Discord: `@readthisifbad`

---

No more jank. No more lag. Just REPO, the way it should've felt all along.
