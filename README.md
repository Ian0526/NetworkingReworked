Development for this mod has discontinued, this sort of worked. Some item and entity behaviors weren't patched.

# NetworkingReworked

**NetworkingReworked** is a client-side mod for REPO that makes multiplayer feel like singleplayer. It is intended to make the client feel as if they're hosting the game by delegating Photon ownership of objects to the client.

This mod doesn't change the host or the server.

---

## What It Actually Does

REPO was designed so the host controls nearly everything. That means when you try to grab, move, or throw an object as a client, there's a full round trip delay while the host approves it. That delay is why everything feels unresponsive. I'm genuinely not sure why semiwork decided to use Photon, it's honestly an incredibly poor choice for a physics heavy game, but it's the entire reason the game feels so sluggish if you're playing as a non-host.



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

Every interactable object in REPO is a `PhysGrabObject`, which implements `IPunObservable`. These objects include a `PhotonTransformView`, which is responsible for syncing their position, rotation, and movement across the network.

However, in vanilla REPO multiplayer, only the **MasterClient** has full authority over physics objects. Clients must wait for updates from the host, resulting in **laggy**, **stuttery**, or even **desynced** physics especially for fast moving or frequently manipulated items like carts, doors, and throwable objects.

I rewrote the networking logic to give each client **predictive authority** over physics objects as if they were in singleplayer. 

It turns out Photon doesn't like you manipulating fields via Reflection as most of their exposed methods reference the same fields. For example, I rewrote the getter for `PhotonView.IsMine` which resulted in `OnPhotonSerializeView` calls disallowing client fake owning clients from calling `stream.ReceiveNext()`. I needed to stream the true host location of the object to facilitate the sync behavior, so to get around this, I wrote a patch that allowed the client to access the buffer via `OnSerializeRead`. Surprisingly, this method still intakes data regardless of `PhotonView.IsMine` status. The prefix parses this data into a caching system which is used to apply extrapolation through velocity, position, and rotation adjustments. Additionally, when objects become still, the setup forcefully sets them to the true location (as long as another player isn't holding the object, the object will resync itself in that case).

There is no way to sync interactions without some authority dictating states. Without a dictating machine, each computer attempting to calculate the physics of player-interacted objects will generate micro deviations from a true state causing massive divergences. Let's say a player has a network tick-rate of 10 per secoond. To our client's physics calculations of object, tick rate is meaningless. We only need to worry about other players, they will dictate their network state. The biggest problem is the transition state of when a player begins & ends their interaction. One example being, a player throws an object by whipping their FOV left-to-right, releasing at a very particular yaw & pitch. In this instance, your machine has already delegated ownership of the object to the player. That's fine, we want that. But when they release, the object's trajectory is highly dependent on what point of their rotation they release. If you were to immediately release ownership of this object back to your client, the trajectory would rely on the last tick the player sent. This is where your deviation creeps in. The player could have released in any of the dozens of frames between the last tick sent, and the tick at which the acknowledgement of release is received. You must ensure the exact fields that dictate the physics state of the object are the same from the prior owner. This includes, the character controller's yaw & pitch, and the object's transform, rotation, momentum, and trajectory. It needs to be **identical**.

### Key Changes:

- `FakeOwnershipController` was added to every `PhysGrabObject`, allowing the local client to *simulate* ownership and physics authority.
- Instead of calling `PhotonView.TransferOwnership()`, we intercept transform sync data and **smoothly override local physics** using extrapolated host states or locally simulated data.
- I implemented a **soft sync** system that gradually reconciles the object's state back to the host after a throw or release, preventing sudden snapping or janky corrections.
- A **passive sync system** checks cached network data (`PhotonStreamCache`) and interpolates remote object states only when the item is unheld and not being interacted with.
- For edge cases like **carts** and **doors**, I detect object containment and override physics behavior manually to ensure smooth, glitch-free motion on both the client and the host.

---

## Feedback

I'm not working on this anymore, but if you are and would like to discuss anything, reach out to my Discord: `@readthisifbad`

