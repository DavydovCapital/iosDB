# iOS 60/120 FPS Performance Blueprint

This is a production checklist for modern iPhones and iPads. It applies to native
Metal output from Unity, Godot, or Unreal—not to the current Capacitor canvas
prototype. Choose **one** engine path below, establish measurable budgets, and do
not enable every quality feature simply because the newest phone can run it.

## 1. Define the contract before optimizing

Use device tiers and sustained (not cold-device) measurements:

| Tier | Examples | Display target | GPU budget | CPU main-thread budget | Working-set starting budget |
| --- | --- | ---: | ---: | ---: | ---: |
| A | ProMotion iPhone/iPad Pro | adaptive 120/60 | 6.5 ms / 13 ms | 5.5 ms / 11 ms | 1.2–1.8 GB |
| B | recent 60 Hz iPhone/iPad | 60 | 13 ms | 11 ms | 0.9–1.4 GB |
| C | minimum supported device | 60, with 30 fallback | 13/27 ms | 11/24 ms | measured per device |

The budgets leave compositor and OS headroom. Treat **99th-percentile frame time**,
thermal state, and peak resident memory as release gates; average FPS hides
stutters. Test for 20–30 minutes, unplugged, at realistic brightness and ambient
temperature. Never assume a fixed iOS Jetsam limit: it varies by device, OS, and
system pressure.

Recommended gates:

- 60 Hz: p99 CPU and GPU frame time below 16.67 ms; goal below 14 ms.
- 120 Hz: p99 below 8.33 ms; goal below 7 ms.
- No hitch above 33.3 ms during representative play after warm-up.
- No sustained `ProcessInfo.thermalState` of `.serious` or `.critical`.
- No per-frame managed allocations after scene warm-up.

## 2. Frame pacing and ProMotion

### The model

Rendering, simulation, and presentation are separate clocks:

1. Sample input every rendered frame.
2. Accumulate real elapsed time (clamp after pause/backgrounding).
3. Run zero or more fixed simulation steps.
4. Render an interpolation between the previous and current simulation states.
5. Let the display layer present on an allowed refresh cadence.

Use a 60 Hz physics step (`1/60`) for most action games even when rendering at
120 Hz. A 120 Hz simulation doubles physics/AI cost and is only justified when
measured input/gameplay requirements demand it. At render time:

```text
accumulator += min(frameDelta, 0.1)
while accumulator >= fixedDelta:
    previous = current
    simulate(current, fixedDelta)
    accumulator -= fixedDelta
alpha = accumulator / fixedDelta
renderTransform = lerp(previous, current, alpha)
```

Interpolate visual transforms only. Collision and authoritative gameplay always
read the current physics state. Do not move the same body from both frame and
physics callbacks. Reset the accumulator and previous state after foregrounding,
teleports, or scene loads.

### Native Apple display policy

For a custom Metal loop, drive presentation with `CADisplayLink`, set
`preferredFrameRateRange`, and preserve the system-selected cadence rather than
assuming every callback is 8.33 ms:

```swift
let link = CADisplayLink(target: self, selector: #selector(tick(_:)))
if #available(iOS 15.0, *) {
    link.preferredFrameRateRange = CAFrameRateRange(
        minimum: 60, maximum: 120, preferred: wantsHighRefresh ? 120 : 60)
} else {
    link.preferredFramesPerSecond = wantsHighRefresh ? 120 : 60
}
link.add(to: .main, forMode: .common)

private var lastDisplayTimestamp: CFTimeInterval?

@objc private func tick(_ link: CADisplayLink) {
    // Use observed elapsed time for simulation; targetTimestamp is presentation intent.
    let delta = min(lastDisplayTimestamp.map { link.timestamp - $0 }
                    ?? (link.targetTimestamp - link.timestamp), 0.1)
    lastDisplayTimestamp = link.timestamp
    gameLoop.advance(realDelta: delta)
}
```

Add `CADisableMinimumFrameDuration = true` to the app's `Info.plist` when the
engine/version requires the 120 Hz opt-in. Verify the generated plist after every
engine upgrade. A 120 Hz request is a preference, not a guarantee; Low Power Mode,
thermal policy, animation content, and the OS may lower refresh.

### Unity

- Player Settings → iOS: enable **Metal**, disable Auto Graphics API if it could
  add an unwanted fallback, and enable **ProMotion** support where exposed by the
  installed Unity LTS version.
- Set `QualitySettings.vSyncCount = 0` and choose
  `Application.targetFrameRate = 120` only for the high-refresh tier; use 60 for
  other tiers. Confirm pacing on hardware—Unity's exact iOS frame-pacing controls
  vary by LTS release.
- Keep `Time.fixedDeltaTime = 1f / 60f`; enable Rigidbody interpolation for visible
  dynamic bodies. For custom simulation, retain previous/current poses and
  interpolate in `LateUpdate` without writing back to physics.
- Do not use `OnDemandRendering.renderFrameInterval = 2` as a substitute for a
  clean 60 Hz target unless profiling proves pacing is correct; it intentionally
  skips rendered frames.

### Godot 4

- Project Settings → Display/Window: use the Metal-backed renderer supported by
  the chosen Godot iOS release; prefer **Mobile** over Forward+ unless its features
  are required.
- Set the physics tick to 60 and enable physics interpolation. Move physics bodies
  only in `_physics_process`; use `_process` for presentation-only work.
- Request 120 through the current Godot iOS display API/project setting and ensure
  `CADisableMinimumFrameDuration` survives export. APIs have changed between Godot
  minors, so validate against the pinned engine documentation and an exported app.
- Cap to 60 under thermal pressure rather than allowing an unstable 70–100 FPS.

### Unreal Engine 5

- Enable Metal and iOS high-refresh support in the pinned UE version; set the
  device profile rather than relying on editor scalability.
- Start with `t.MaxFPS 120` on a ProMotion profile and `t.MaxFPS 60` elsewhere.
  Disable Smooth Frame Rate if it fights the platform cap; verify with Unreal
  Insights and Metal System Trace.
- Keep the game/physics tick deterministic. Use UE's network/transform smoothing
  or previous/current transform interpolation for visuals. Avoid independent
  component movement from Tick and Chaos in the same frame.
- Mobile Forward is the safe baseline. Treat Lumen, Nanite, virtual shadow maps,
  and TSR as opt-in features backed by on-device evidence, not defaults.

## 3. Adaptive quality without oscillation

Do not react to a single slow frame. Maintain exponential moving averages for CPU
and GPU time and use hysteresis:

- Downshift when GPU time exceeds 90% of budget for 30–60 frames.
- Upshift only when below 70% for 3–5 seconds.
- Wait at least 2 seconds between changes.
- If CPU-bound, reduce AI/update frequency or render rate; resolution will not help.
- On `.serious` thermal state, immediately select 60 Hz, reduce resolution and
  effects one tier. On `.critical`, select the minimum safe tier and persistently
  notify telemetry; never terminate voluntarily.

Resolution ladder: `1.00 → 0.90 → 0.80 → 0.70`, bounded by a useful minimum pixel
height. Prefer engine dynamic-resolution systems backed by Metal; otherwise render
3D into a scaled target and composite native-resolution UI. Change one tier at a
time. Keep HUD/text at native resolution.

Suggested quality ladder:

| Tier | Resolution | Refresh | Shadows | Post |
| --- | ---: | ---: | --- | --- |
| Ultra | 1.00 | 120 | 1 key light, 2 cascades | restrained |
| High | 0.90 | 120/60 | 1 cascade | bloom reduced |
| Sustained | 0.80 | 60 | blob/baked | color grade only |
| Thermal | 0.70 | 60 or 30 | off/baked | off |

Select an intentionally stable target. A locked 60 is better than repeatedly
bursting to 120, heating the SoC, and collapsing below 60.

## 4. Apple tile-based GPU rules

Apple GPUs reward keeping work on-chip and punish bandwidth, overdraw, and render
target churn:

- Use memoryless transient depth/MSAA attachments where the engine exposes them.
- Avoid unnecessary load/store actions, framebuffer copies, camera stacking, and
  full-screen passes. Merge compatible passes.
- Minimize transparent layers and large particles; front-to-back opaque ordering
  helps early rejection. Measure overdraw on device.
- Prefer baked lighting, probes, one shadowed key light, short shadow distance, and
  one or two cascades. Atlas shadow maps and update static shadows infrequently.
- Keep mobile shaders branch-light with `half` precision where visually safe.
  Avoid discard/alpha test across large screen areas, dependent texture reads,
  excessive texture samples, and high-frequency noise.
- Prewarm shader/pipeline variants at a loading boundary. Strip unused variants.
  Runtime pipeline compilation is a common first-use hitch.
- MSAA 2× is often a better mobile trade than expensive temporal post-processing,
  but only on-device GPU counters decide.

Engine starting points:

- **Unity URP:** SRP Batcher on, GPU instancing where batching actually improves,
  HDR off unless required, opaque/depth textures off unless sampled, 2× MSAA,
  render scale 0.9, additional-light shadows off, shader variant stripping on.
- **Godot Mobile:** clustered features and screen-space effects off unless proven,
  occlusion culling for meaningful indoor scenes, MultiMesh for repeated props,
  particles capped and pooled.
- **UE Mobile:** Mobile Forward, Mobile HDR off if the art pipeline permits, baked
  lighting, material quality tiers/device profiles, PSO precaching, HLOD/instancing,
  conservative shadow and post-process CVars.

## 5. Memory, Jetsam, and zero-allocation play

### Asset rules

- Use ASTC for color/normal textures. Start at ASTC 6×6 for general art, 4×4 for
  high-detail UI/characters/normals, and 8×8 for low-frequency backgrounds. Inspect
  artifacts; do not blindly apply one block size.
- Size textures to their maximum on-screen footprint. Generate mipmaps for 3D
  assets; consider no mipmaps only for pixel-perfect UI/sprites where sampling is
  controlled.
- Stream by level/zone and unload deterministically. Put a hard cap on resident
  audio voices, particles, decals, render targets, and pooled objects.
- Prefer AAC/ADPCM/engine-appropriate compressed audio for longer clips; reserve
  uncompressed PCM for tiny, latency-sensitive effects.
- Never allocate a second full asset set during scene transition. Load the next
  chunk incrementally, release the old chunk, then compact only at a safe loading
  boundary.

### Hot-loop rules

No LINQ, closures, string formatting, per-frame arrays/lists/dictionaries, dynamic
material creation, synchronous asset loads, or logging in the gameplay path.
Pre-size collections. Cache component/node pointers and shader property IDs.
Return particles/projectiles/enemies to pools; do not destroy them during combat.

### Bounded Unity C# object pool

This pool prewarms, has a hard maximum, performs no steady-state collection
allocation, prevents double-return, and resets lifecycle state explicitly:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

public interface IPoolable
{
    void OnRent();
    void OnReturn();
}

public sealed class ComponentPool<T> where T : Component, IPoolable
{
    private readonly T prefab;
    private readonly Transform root;
    private readonly Stack<T> free;
    private readonly HashSet<T> rented;
    private readonly int capacity;

    public ComponentPool(T prefab, Transform root, int prewarm, int capacity)
    {
        if (prewarm < 0 || capacity < 1 || prewarm > capacity)
            throw new ArgumentOutOfRangeException();
        this.prefab = prefab;
        this.root = root;
        this.capacity = capacity;
        free = new Stack<T>(capacity);
        rented = new HashSet<T>(capacity);
        for (int i = 0; i < prewarm; ++i) free.Push(Create());
    }

    public bool TryRent(out T item)
    {
        if (free.Count != 0) item = free.Pop();
        else if (free.Count + rented.Count < capacity) item = Create();
        else { item = null; return false; } // Apply gameplay back-pressure.

        rented.Add(item);
        item.gameObject.SetActive(true);
        item.OnRent();
        return true;
    }

    public void Return(T item)
    {
        if (item == null || !rented.Remove(item))
            throw new InvalidOperationException("Foreign object or double return");
        item.OnReturn();                 // Stop VFX/audio; clear callbacks/velocity.
        item.transform.SetParent(root, false);
        item.gameObject.SetActive(false);
        free.Push(item);
    }

    private T Create()
    {
        T item = UnityEngine.Object.Instantiate(prefab, root);
        item.gameObject.SetActive(false);
        return item;
    }
}
```

Instantiate growth only at controlled boundaries if possible. If `TryRent` fails,
drop a cosmetic effect or recycle the oldest noncritical item—never create an
unbounded emergency object. In Godot, preinstantiate nodes, call explicit
`activate/reset/deactivate`, and reparent to a pool node instead of `queue_free()`.
In Unreal, pre-spawn actors/components, disable tick/collision/rendering while
inactive, and use a fixed free-index array rather than spawning in combat.

Subscribe to memory warnings in the native shell and engine callback. Shed
reconstructible caches, distant streamed content, and optional render targets; do
not run a giant synchronous garbage collection in response. Capture Xcode memory
graphs and test repeated enter/exit cycles—baseline memory must return to a stable
plateau.

## 6. Xcode Instruments: finding p99 spikes

Always profile a **Release/Profile** build on a physical device, without the
debugger attached when measuring final pacing. Add `os_signpost` ranges around
input, simulation, AI, streaming, render submission, and scene transitions so
engine and platform timelines correlate.

### Time Profiler (CPU hitch triage)

1. Xcode → Product → Profile, choose **Time Profiler**, select the physical device,
   and record a scripted 3–5 minute route after warm-up.
2. Mark the route with signposts. Reproduce spawning, combat, streaming, UI, and
   background/foreground transitions.
3. In the track, select individual hitch windows—not the entire capture. In Call
   Tree enable **Separate by Thread**, **Invert Call Tree**, and **Hide System
   Libraries**; inspect Main Thread and engine job/render threads separately.
4. Look for asset decompression, shader creation, lock contention, managed GC,
   synchronous file I/O, object construction/destruction, layout, and logging.
5. Export the trace, fix one cause, and repeat the identical route. Compare p50,
   p95, p99, and worst frame rather than average CPU percentage.

### Metal System Trace (CPU/GPU/present correlation)

1. Profile with **Metal System Trace** on a supported physical device. Capture a
   short 15–30 second interval around a known hitch; long traces become noisy.
2. Align display presents, CPU command-buffer submission, GPU execution, and engine
   signposts. Determine whether the late frame is CPU-bound (late submission),
   GPU-bound (long command buffer), synchronization-bound (idle gap/wait), or a
   pacing/present issue.
3. Expand GPU intervals and inspect render/compute/blit encoders. Look for long
   full-screen passes, attachment stores/loads, bandwidth, many tiny encoders,
   pipeline compilation, and CPU/GPU synchronization.
4. Use Metal frame capture for a representative bad frame; inspect attachments,
   load/store actions, shader timing/counters, draw count, overdraw, and resource
   residency. Validate optimizations on the same device and thermal state.

Also run **Allocations** for growth and transient spikes, **VM Tracker** for dirty
and compressed memory, and the engine profiler/Unreal Insights alongside signposts.
Profile without Metal validation/API capture for final numbers; those diagnostics
distort timing.

## 7. Shipping checklist

- [ ] Every device profile explicitly selects refresh, resolution, shadows, and post.
- [ ] 60/120 requests verified on-device; no hard-coded callback delta.
- [ ] 60 Hz simulation with visual interpolation; teleport/foreground reset tested.
- [ ] 30-minute thermal soak passes at p99 budget and realistic brightness.
- [ ] Dynamic resolution uses hysteresis and reacts only when GPU-bound.
- [ ] No steady-state allocations; pools are bounded and prewarmed.
- [ ] ASTC quality inspected per asset class; peak working set measured repeatedly.
- [ ] PSO/shader variants prewarmed; first-use combat produces no compilation hitch.
- [ ] Time Profiler and Metal System Trace captures archived for each release tier.
- [ ] Low Power Mode, interruptions, background/foreground, and memory warnings tested.

Pin the exact Unity/Godot/Unreal version in the project and revalidate every toggle
against that version's documentation: names and platform behavior change between
releases. Hardware traces—not editor FPS—are the source of truth.
