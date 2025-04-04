# NetworkingRework

This project is a **BepInEx Harmony patch** for the Unity-based multiplayer game **REPO**, focusing on fixing network latency related delay with physics. If you've played any multiplayer instance of REPO, you've probably noticed a significant difference in fluidity between singleplayer/hosting & being a client.

---

## What This Mod Does

REPO has a number of systems designed around single-player or host-authoritative assumptions. By default, physics calculations are performed through on the host and propagated through Photon. This mod:

- Replaces host-only logic (`PhotonNetwork.IsMasterClient` and `SemiFunc.IsMasterClientOrSingleplayer`) with ownership checks (`PhotonView.IsMine`)
- Enables **client-side simulation and control** of physics objects they own
- Adds **ownership transfer logic** for grabbed and colliding objects
- Adds **state syncing and physics prediction** to minimize jitter and misbehavior when objects are handed off
- Fixes **cart-related ownership and behavior** so they behave consistently when pulled by clients

**In simpler terms, this makes clients feel like they're playing on a single player instance (or hosting).**

---

## What is Photon?

[Photon Unity Networking (PUN)](https://www.photonengine.com/pun) is a real-time networking framework for Unity, allowing players to interact with shared game objects. It uses the concept of `PhotonView` components, where each object has an "owner" (one player who can write to the object's state).

The original REPO code assumes the host is the only source of truth. This mod breaks that assumption and gives clients more authority where appropriate, essentially allowing the client to take more control.

---

## How does REPO Networking work?
![Normal Photon Networking Sequence](https://i.gyazo.com/871706b74ad2a346a64d8e35480630a0.png)

In REPO, all multiplayer traffic is routed through Photon, with the MasterClient (typically the host) acting as the authoritative source of truth. When a client attempts to move or interact with an object, the update is first sent to Photon, which then forwards it to the host. The host processes the update, then sends the new state back through Photon, which replicates it to all other clients, including the original sender.

This results in a round-trip delay for every interaction, effectively doubling the input latency. While this model ensures server side authority and cheat prevention, it also introduces significant responsiveness issues, especially in physics-heavy games like REPO. For physics synchronization, this delay can cause jitter, delayed object reactions, or even desync between players.

The goal of this mod is to offload some of that authority to clients in a controlled and safe manner — reducing latency and improving responsiveness without breaking multiplayer consistency.

![NetworkingRework](https://i.gyazo.com/f555ac106cefcebb03ef7000c3293b52.png)

### Patched Behaviors

| System | Behavior | Fix |
|--------|----------|-----|
| `PhysGrabObject` | Grab/release logic | Ownership, syncing, ping compensation |
| `PhysGrabHinge` | Hinged object physics | Prevents joint destruction on non-hosts |
| `PhotonTransformView` | Transform syncing | Avoids override flicker during ownership transfer |
| `PhysGrabCart` | Pulling and state transitions | Replaces host-only logic, adds ping-buffered grabbing |
| `PhysGrabObjectGrabArea` | Grab region logic | Ensures grab detection works client-side |
| `OwnershipTakeoverHelper` | Client-side authority monitor | Ownership stabilization based on pings and state |
| `ImpactSyncHandler` | Velocity and transform sync | Manual sync of physics after collisions |
| `PhotonView.TransferOwnership` | Ownership transfer | Ensures physics state and grab timers are patched |

---

## Current Known Issues

### Enemies
- Most AI behaviors are still host-authoritative
- Clients may not properly own or simulate enemy interactions
- Enemies fall over non-reactive to item impacts

### Scripted Valuables
- Some valuable objects (e.g. scripted mission-critical ones) may not transfer ownership properly
- State and physics updates may be lost or ignored by clients

### Carts
- Initial state glitches during transfer (e.g. wrong `Dragged` state)
- Ownership handoff mid-frame may cause physics pop
- Cart pulling is inconsistent when grabbed from certain angles

---

## Goals & Future Work

- Finish cart logic, clients cannot maintain an active state with carts and they are continuously in a dragged state.
- Edgecase every enemy to see how the mechanics behave.
- Edgecase every valuable item with a scriptable behavior that interact with players. Most are client related, so they don't need interference, but some do.
- Thorougly test in both a normal/deliberately intensive game environment.

---

## 🔧 Setup

1. Install [BepInEx 5.x](https://github.com/BepInEx/BepInEx/releases) into your REPO installation folder
2. Place this mod's `.dll` into `BepInEx/plugins`
3. Launch the game and join a multiplayer session to see the improvements

---

## Final Notes

This won't be going on Thunderstore until I believe it's in a suitable condition.
