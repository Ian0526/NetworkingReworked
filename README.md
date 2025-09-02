Development for this mod has discontinued due to other projects, maybe someone will find it useful in making their own.

# NetworkingReworked

**NetworkingReworked** is a client-side mod for REPO that makes multiplayer feel like singleplayer. No more rubberbanding, or weird delay when picking up objects.

This mod doesn't change the host or the server. It just makes your game respond the way it should.

---

## What It Actually Does

REPO was designed so the host controls nearly everything. That means when you try to grab, move, or throw an object as a client, there's a full round-trip delay while the host approves it. That delay is why everything feels unresponsive.

**NetworkingReworked changes that:**

- **You act like the owner of every object** — unless something else (like an enemy or another player) touches it.
- When you grab or throw something, it's fully simulated on your side immediately.
- If you stop interacting, the object gradually re-syncs with the host to avoid glitches.
- If someone else grabs it, or it behaves unexpectedly (like from a collision), your control is gracefully handed back.

---

## Key Features

- **Now no longer requires host installation**
- Zero-grab-delay and smooth object control
- Soft sync after release to avoid sudden snapping (although, fast objects may jitter for a moment to resync with the host)
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

## For The Devs

Every single interactable object in REPO is a `PhysGrabObject`, which implements `IPunObservable`. These objects include a `PhotonTransformView`, which is responsible for syncing their position, rotation, and movement across the network.

However, in vanilla REPO multiplayer, only the **MasterClient** has full authority over physics objects. Clients must wait for updates from the host, resulting in **laggy**, **stuttery**, or even **desynced** physics—especially for fast-moving or frequently manipulated items like carts, doors, and throwable objects.

I rewrote the networking logic to give each client **predictive authority** over physics objects as if they were in singleplayer. 

It turns out Photon doesn't like you manipulating fields via Reflection as a most of their exposed methods reference the same fields, for example I rewrote the getter for `PhotonView.IsMine` which resulted in `OnPhotonSerializeView` calls disallowing client fake owning clients from calling `stream.ReceiveNext()`. I needed to stream the true host location of the object to facilitate the sync behavior, so to get around this, I wrote a patch that allowed the client to access the buffer via `OnSerializeRead`. Surprisingly, this method still intakes data regardless of `PhotonView.IsMine` status. The prefix parses this data into a caching system which is used to apply extrapolation through velocity, position, and rotation adjustments. Additionally, when objects become still, the setup forcefully sets them to the true location (as long as another player isn't holding the object, the object will resync itself in that case).

### Key Changes:

- `FakeOwnershipController` was added to every `PhysGrabObject`, allowing the local client to *simulate* ownership and physics authority.
- Instead of calling `PhotonView.TransferOwnership()`, we intercept transform sync data and **smoothly override local physics** using extrapolated host states or locally simulated data.
- I implemented a **soft sync** system that gradually reconciles the object's state back to the host after a throw or release, preventing sudden snapping or janky corrections.
- A **passive sync system** checks cached network data (`PhotonStreamCache`) and interpolates remote object states only when the item is unheld and not being interacted with.
- For edge cases like **carts** and **doors**, I detect object containment and override physics behavior manually to ensure smooth, glitch-free motion on both the client and the host.

---

## Feedback

If anything feels off or you'd like to suggest improvements, feel free to reach out on Discord: `@readthisifbad`

