import { useCallback, useEffect, useRef, useState } from "react";
import * as THREE from "three";

type Mission = { name: string; objective: string; enemies: number; fog: number; sky: number; ground: number; accent: number; speed: number };
type Hud = { state: "briefing" | "playing" | "clear" | "won" | "lost"; mission: number; kills: number; health: number; score: number; fps: number };
type Enemy = { mesh: THREE.Group; hp: number; speed: number; cooldown: number; kind: "grunt" | "heavy" | "drone" };
type Bolt = { mesh: THREE.Mesh; velocity: THREE.Vector3; hostile: boolean; life: number };

const MISSIONS: Mission[] = [
  { name: "SUNSET OUTPOST", objective: "Eliminate patrol squad", enemies: 8, fog: 0x2a1740, sky: 0xff7a45, ground: 0x241433, accent: 0xffd166, speed: 1 },
  { name: "NEON HARBOR", objective: "Push through dock defenses", enemies: 11, fog: 0x08243a, sky: 0x00c2ff, ground: 0x101826, accent: 0x5ee1ff, speed: 1.08 },
  { name: "VIOLET RIDGE", objective: "Clear ridge positions", enemies: 14, fog: 0x1d1038, sky: 0xc77dff, ground: 0x1a1330, accent: 0xf72585, speed: 1.16 },
  { name: "GRID CITY", objective: "Sweep urban blocks", enemies: 17, fog: 0x061522, sky: 0x22d3ee, ground: 0x111827, accent: 0xff8fab, speed: 1.24 },
  { name: "COMMAND CORE", objective: "Destroy final assault group", enemies: 20, fog: 0x210712, sky: 0xfb7185, ground: 0x17121c, accent: 0xfff1a8, speed: 1.34 },
];

class Synth {
  private ctx?: AudioContext;
  private master?: GainNode;
  private timer?: number;
  private step = 0;
  start() {
    if (this.timer) return;
    const Ctor = window.AudioContext || (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (!Ctor) return;
    this.ctx = this.ctx ?? new Ctor();
    this.master = this.ctx.createGain();
    this.master.gain.value = .13;
    this.master.connect(this.ctx.destination);
    void this.ctx.resume();
    this.timer = window.setInterval(() => this.tick(), 128);
  }
  stop() { if (this.timer) window.clearInterval(this.timer); this.timer = undefined; }
  private tone(freq: number, dur: number, type: OscillatorType, gain: number) {
    if (!this.ctx || !this.master) return;
    const t = this.ctx.currentTime;
    const osc = this.ctx.createOscillator();
    const amp = this.ctx.createGain();
    osc.type = type; osc.frequency.setValueAtTime(freq, t);
    amp.gain.setValueAtTime(.0001, t);
    amp.gain.exponentialRampToValueAtTime(gain, t + .01);
    amp.gain.exponentialRampToValueAtTime(.0001, t + dur);
    osc.connect(amp).connect(this.master); osc.start(t); osc.stop(t + dur + .02);
  }
  private tick() {
    const bass = [55, 55, 65.41, 55, 73.42, 55, 49, 61.74];
    const lead = [220, 261.63, 329.63, 392, 329.63, 440, 392, 329.63, 261.63, 329.63, 392, 523.25, 392, 329.63, 293.66, 261.63];
    const s = this.step++;
    this.tone(bass[s % bass.length], .11, "sawtooth", .1);
    if (s % 2 === 0) this.tone(lead[(s / 2) % lead.length], .08, "square", .045);
    if (s % 4 === 0) this.tone(1800, .02, "sawtooth", .02);
  }
}

function App() {
  const mountRef = useRef<HTMLDivElement>(null);
  const hudRef = useRef<Hud>({ state: "briefing", mission: 1, kills: 0, health: 100, score: 0, fps: 60 });
  const [hud, setHud] = useState<Hud>(hudRef.current);
  const input = useRef({ forward: 0, strafe: 0, firing: false, lookX: 0, lookY: 0 });
  const music = useRef(new Synth());

  const setState = useCallback((patch: Partial<Hud>) => {
    hudRef.current = { ...hudRef.current, ...patch };
    setHud(hudRef.current);
  }, []);

  useEffect(() => {
    const mount = mountRef.current!;
    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(72, window.innerWidth / window.innerHeight, .1, 500);
    const renderer = new THREE.WebGLRenderer({ antialias: true, powerPreference: "high-performance" });
    renderer.setSize(window.innerWidth, window.innerHeight);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    renderer.shadowMap.enabled = true;
    renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    mount.appendChild(renderer.domElement);

    const clock = new THREE.Clock();
    const ray = new THREE.Raycaster();
    const center = new THREE.Vector2(0, 0);
    const world = new THREE.Group(); scene.add(world);
    const enemies: Enemy[] = [];
    const bolts: Bolt[] = [];
    const particles: { mesh: THREE.Mesh; velocity: THREE.Vector3; life: number }[] = [];

    let mission = 1;
    let kills = 0;
    let spawned = 0;
    let health = 100;
    let score = 0;
    let yaw = 0;
    let pitch = 0;
    let fireCooldown = 0;
    let damageFlash = 0;
    let won = false;

    const hemi = new THREE.HemisphereLight(0xdfefff, 0x1a1030, 2.2); scene.add(hemi);
    const sun = new THREE.DirectionalLight(0xffffff, 3.2); sun.position.set(-8, 16, 7); sun.castShadow = true; sun.shadow.mapSize.set(2048, 2048); scene.add(sun);
    const muzzle = new THREE.PointLight(0x7de8ff, 0, 18); scene.add(muzzle);

    const gun = new THREE.Group();
    const gunBody = new THREE.Mesh(new THREE.BoxGeometry(.28, .16, .7), new THREE.MeshStandardMaterial({ color: 0x1f2937, roughness: .38, metalness: .75 }));
    const gunGlow = new THREE.Mesh(new THREE.BoxGeometry(.29, .03, .5), new THREE.MeshBasicMaterial({ color: 0x22d3ee }));
    gunGlow.position.y = .09; gun.add(gunBody, gunGlow); gun.position.set(.42, -.34, -.72); camera.add(gun); scene.add(camera);

    const makeMaterial = (color: number, emissive = 0x000000, roughness = .65, metalness = .08) => new THREE.MeshStandardMaterial({ color, emissive, emissiveIntensity: emissive ? .25 : 0, roughness, metalness });

    const buildMission = (index: number) => {
      world.clear(); enemies.length = 0; bolts.length = 0; particles.length = 0;
      const m = MISSIONS[index - 1];
      scene.background = new THREE.Color(m.fog);
      scene.fog = new THREE.Fog(m.fog, 16, 130);
      const ground = new THREE.Mesh(new THREE.PlaneGeometry(260, 260), makeMaterial(m.ground));
      ground.rotation.x = -Math.PI / 2; ground.receiveShadow = true; world.add(ground);

      const grid = new THREE.GridHelper(240, 48, m.accent, 0x334155);
      (grid.material as THREE.Material).transparent = true; (grid.material as THREE.Material).opacity = .16; world.add(grid);

      for (let i = 0; i < 46; i++) {
        const size = 2 + Math.random() * 8;
        const b = new THREE.Mesh(new THREE.BoxGeometry(size, 4 + Math.random() * 18, size), makeMaterial(i % 4 === 0 ? 0x263247 : 0x151d2d, i % 5 === 0 ? m.accent : 0x000000, .8, .12));
        const side = Math.random() > .5 ? 1 : -1;
        b.position.set(side * (14 + Math.random() * 74), b.scale.y / 2, -Math.random() * 150 + 25);
        b.castShadow = true; b.receiveShadow = true; world.add(b);
      }

      for (let i = 0; i < 90; i++) {
        const star = new THREE.Mesh(new THREE.SphereGeometry(.08 + Math.random() * .08, 6, 6), new THREE.MeshBasicMaterial({ color: i % 3 ? 0xffffff : m.accent }));
        star.position.set((Math.random() - .5) * 220, 18 + Math.random() * 80, -Math.random() * 160);
        world.add(star);
      }

      const gate = new THREE.Mesh(new THREE.TorusGeometry(8, .25, 12, 80), new THREE.MeshBasicMaterial({ color: m.accent }));
      gate.position.set(0, 7, -58); world.add(gate);
      camera.position.set(0, 1.7, 8); yaw = 0; pitch = 0;
      kills = 0; spawned = 0; health = 100; won = false;
      setState({ state: "briefing", mission: index, kills: 0, health: 100, score });
    };

    const spawnEnemy = () => {
      const m = MISSIONS[mission - 1];
      const roll = Math.random();
      const kind: Enemy["kind"] = mission >= 3 && roll < .18 ? "drone" : mission >= 2 && roll < .32 ? "heavy" : "grunt";
      const g = new THREE.Group();
      const color = kind === "heavy" ? 0xef476f : kind === "drone" ? 0xc77dff : 0x06d6a0;
      const body = new THREE.Mesh(new THREE.CapsuleGeometry(.55, kind === "heavy" ? 1.3 : .95, 4, 10), makeMaterial(color, color, .5, .18)); body.castShadow = true; body.position.y = kind === "drone" ? 2.1 : 1.05; g.add(body);
      const head = new THREE.Mesh(new THREE.SphereGeometry(.28, 12, 12), makeMaterial(0xf8fafc)); head.position.y = kind === "drone" ? 2.82 : 1.98; head.castShadow = true; g.add(head);
      const visor = new THREE.Mesh(new THREE.BoxGeometry(.42, .09, .08), new THREE.MeshBasicMaterial({ color: m.accent })); visor.position.set(0, kind === "drone" ? 2.82 : 1.98, -.22); g.add(visor);
      const gunMesh = new THREE.Mesh(new THREE.BoxGeometry(.9, .12, .12), makeMaterial(0x111827, 0x000000, .32, .8)); gunMesh.position.set(.36, kind === "drone" ? 2.12 : 1.24, -.2); g.add(gunMesh);
      g.position.set((Math.random() - .5) * 28, 0, -24 - Math.random() * 32);
      world.add(g); enemies.push({ mesh: g, hp: kind === "heavy" ? 3 : kind === "drone" ? 2 : 1, speed: (kind === "heavy" ? 1.8 : kind === "drone" ? 2.8 : 2.4) * m.speed, cooldown: .6 + Math.random() * 1.4, kind });
      spawned++;
    };

    const burst = (position: THREE.Vector3, color: number, count = 10) => {
      for (let i = 0; i < count; i++) {
        const p = new THREE.Mesh(new THREE.SphereGeometry(.05 + Math.random() * .08, 6, 6), new THREE.MeshBasicMaterial({ color }));
        p.position.copy(position);
        world.add(p);
        particles.push({ mesh: p, velocity: new THREE.Vector3((Math.random() - .5) * 5, Math.random() * 4, (Math.random() - .5) * 5), life: .5 + Math.random() * .4 });
      }
    };

    const shoot = () => {
      if (fireCooldown > 0 || hudRef.current.state !== "playing") return;
      fireCooldown = .16;
      muzzle.intensity = 6;
      const mesh = new THREE.Mesh(new THREE.SphereGeometry(.07, 8, 8), new THREE.MeshBasicMaterial({ color: 0x7de8ff }));
      const origin = new THREE.Vector3(); camera.getWorldPosition(origin);
      const dir = new THREE.Vector3(); camera.getWorldDirection(dir);
      mesh.position.copy(origin.clone().add(dir.clone().multiplyScalar(.9)));
      world.add(mesh); bolts.push({ mesh, velocity: dir.multiplyScalar(34), hostile: false, life: 1.6 });
    };

    const enemyShoot = (enemy: Enemy) => {
      const mesh = new THREE.Mesh(new THREE.SphereGeometry(.08, 8, 8), new THREE.MeshBasicMaterial({ color: 0xff477e }));
      const start = enemy.mesh.position.clone().add(new THREE.Vector3(0, enemy.kind === "drone" ? 2.2 : 1.35, 0));
      const target = camera.position.clone().add(new THREE.Vector3(0, -.15, 0));
      mesh.position.copy(start);
      world.add(mesh); bolts.push({ mesh, velocity: target.sub(start).normalize().multiplyScalar(15), hostile: true, life: 2.4 });
    };

    buildMission(1);

    const onResize = () => { camera.aspect = window.innerWidth / window.innerHeight; camera.updateProjectionMatrix(); renderer.setSize(window.innerWidth, window.innerHeight); };
    window.addEventListener("resize", onResize);

    const onLook = (e: PointerEvent) => {
      if (e.pointerType !== "mouse" && e.clientX < window.innerWidth * .45) return;
      input.current.lookX += e.movementX * .0024;
      input.current.lookY += e.movementY * .0024;
    };
    window.addEventListener("pointermove", onLook);

    let raf = 0;
    const animate = () => {
      raf = requestAnimationFrame(animate);
      const dt = Math.min(clock.getDelta(), .033);
      const st = hudRef.current.state;
      const m = MISSIONS[mission - 1];
      yaw -= input.current.lookX; pitch -= input.current.lookY; pitch = Math.max(-1.2, Math.min(1.2, pitch));
      input.current.lookX = 0; input.current.lookY = 0;
      camera.rotation.set(pitch, yaw, 0, "YXZ");

      if (st === "playing") {
        const move = new THREE.Vector3(input.current.strafe, 0, -input.current.forward).normalize().multiplyScalar(7 * dt);
        move.applyAxisAngle(new THREE.Vector3(0, 1, 0), yaw);
        camera.position.add(move);
        camera.position.x = Math.max(-22, Math.min(22, camera.position.x));
        camera.position.z = Math.max(-70, Math.min(10, camera.position.z));
        camera.position.y = 1.7 + Math.sin(clock.elapsedTime * 10) * .015;

        if (spawned < m.enemies && enemies.length < Math.min(6, 3 + mission)) spawnEnemy();
        if (input.current.firing) shoot();
        fireCooldown = Math.max(0, fireCooldown - dt);
        damageFlash = Math.max(0, damageFlash - dt);
        muzzle.intensity = Math.max(0, muzzle.intensity - 30 * dt);

        for (const enemy of enemies) {
          const toPlayer = camera.position.clone().sub(enemy.mesh.position);
          const dist = toPlayer.length();
          enemy.mesh.lookAt(camera.position.x, enemy.mesh.position.y + 1, camera.position.z);
          if (dist > (enemy.kind === "drone" ? 11 : 8)) enemy.mesh.position.add(toPlayer.normalize().multiplyScalar(enemy.speed * dt));
          if (enemy.kind === "drone") enemy.mesh.position.y = 1.1 + Math.sin(clock.elapsedTime * 3 + enemy.mesh.id) * .35;
          enemy.cooldown -= dt;
          if (enemy.cooldown <= 0 && dist < 34) { enemyShoot(enemy); enemy.cooldown = (enemy.kind === "heavy" ? .9 : 1.35) + Math.random() * .6; }
        }

        for (const bolt of bolts) {
          bolt.mesh.position.add(bolt.velocity.clone().multiplyScalar(dt));
          bolt.life -= dt;
          if (!bolt.hostile) {
            ray.set(bolt.mesh.position, bolt.velocity.clone().normalize());
            for (const enemy of enemies) {
              if (bolt.mesh.position.distanceTo(enemy.mesh.position.clone().add(new THREE.Vector3(0, enemy.kind === "drone" ? 2.2 : 1.3, 0))) < 1) {
                enemy.hp--; bolt.life = 0;
                burst(bolt.mesh.position, m.accent, 7);
                if (enemy.hp <= 0) {
                  burst(enemy.mesh.position.clone().add(new THREE.Vector3(0, 1.2, 0)), enemy.kind === "heavy" ? 0xef476f : 0x06d6a0, 16);
                  world.remove(enemy.mesh);
                  enemies.splice(enemies.indexOf(enemy), 1);
                  kills++; score += enemy.kind === "heavy" ? 300 : enemy.kind === "drone" ? 250 : 100;
                  setState({ kills, score });
                }
                break;
              }
            }
          } else if (bolt.mesh.position.distanceTo(camera.position) < .8) {
            bolt.life = 0; health -= enemyDamage(mission); damageFlash = .2;
            setState({ health });
            if (health <= 0) { setState({ state: "lost" }); music.current.stop(); }
          }
        }
        for (let i = bolts.length - 1; i >= 0; i--) if (bolts[i].life <= 0) { world.remove(bolts[i].mesh); bolts.splice(i, 1); }
        for (let i = particles.length - 1; i >= 0; i--) {
          const p = particles[i]; p.mesh.position.add(p.velocity.clone().multiplyScalar(dt)); p.velocity.y -= 8 * dt; p.life -= dt;
          (p.mesh.material as THREE.MeshBasicMaterial).transparent = true; (p.mesh.material as THREE.MeshBasicMaterial).opacity = Math.max(0, p.life * 2);
          if (p.life <= 0) { world.remove(p.mesh); particles.splice(i, 1); }
        }
        if (!won && kills >= m.enemies && enemies.length === 0) {
          won = true;
          score += 1000;
          if (mission >= MISSIONS.length) { setState({ state: "won", score }); music.current.stop(); }
          else setState({ state: "clear", score });
        }
      }
      renderer.render(scene, camera);
    };
    animate();

    const api = {
      start() { music.current.start(); setState({ state: "playing" }); },
      next() { mission++; score += 500; buildMission(mission); music.current.start(); setState({ state: "playing", mission, kills: 0, health: 100, score }); },
      reset() { mission = 1; score = 0; buildMission(1); setState({ state: "briefing", mission: 1, kills: 0, health: 100, score: 0 }); },
      input,
    };
    (window as unknown as { cobra3d?: typeof api }).cobra3d = api;

    return () => {
      cancelAnimationFrame(raf);
      window.removeEventListener("resize", onResize);
      window.removeEventListener("pointermove", onLook);
      mount.removeChild(renderer.domElement);
      renderer.dispose();
    };
  }, [setState]);

  const begin = () => (window as unknown as { cobra3d?: { start(): void } }).cobra3d?.start();
  const next = () => (window as unknown as { cobra3d?: { next(): void } }).cobra3d?.next();
  const reset = () => (window as unknown as { cobra3d?: { reset(): void } }).cobra3d?.reset();
  const setMove = (forward: number, strafe: number) => { input.current.forward = forward; input.current.strafe = strafe; };
  const setFire = (v: boolean) => { input.current.firing = v; };

  return (
    <main className="game3d-shell">
      <div ref={mountRef} className="game3d-viewport" />
      <div className="crosshair">+</div>
      <header className="hud3d">
        <div><small>MISSION {hud.mission}</small><strong>{MISSIONS[hud.mission - 1].name}</strong></div>
        <div><small>KILLS</small><strong>{hud.kills}/{MISSIONS[hud.mission - 1].enemies}</strong></div>
        <div><small>HEALTH</small><strong>{hud.health}</strong></div>
        <div><small>SCORE</small><strong>{String(hud.score).padStart(6, "0")}</strong></div>
      </header>

      {hud.state === "briefing" && <section className="overlay3d">
        <small>OPERATION {hud.mission}</small>
        <h1>{MISSIONS[hud.mission - 1].name}</h1>
        <p>{MISSIONS[hud.mission - 1].objective}</p>
        <button onClick={begin}>DEPLOY</button>
      </section>}

      {hud.state === "clear" && <section className="overlay3d">
        <small>MISSION CLEAR</small>
        <h1>{MISSIONS[hud.mission - 1].name}</h1>
        <p>Score {String(hud.score).padStart(6, "0")}</p>
        <button onClick={next}>NEXT MISSION</button>
      </section>}

      {(hud.state === "won" || hud.state === "lost") && <section className="overlay3d">
        <small>{hud.state === "won" ? "CAMPAIGN COMPLETE" : "OPERATIVE DOWN"}</small>
        <h1>{hud.state === "won" ? "CORE DESTROYED" : "MISSION FAILED"}</h1>
        <p>Final score {String(hud.score).padStart(6, "0")}</p>
        <button onClick={reset}>RESTART</button>
      </section>}

      <div className="touch3d">
        <div className="movepad">
          <button onPointerDown={() => setMove(1, 0)} onPointerUp={() => setMove(0, 0)}>▲</button>
          <div><button onPointerDown={() => setMove(0, -1)} onPointerUp={() => setMove(0, 0)}>◀</button><button onPointerDown={() => setMove(-1, 0)} onPointerUp={() => setMove(0, 0)}>▼</button><button onPointerDown={() => setMove(0, 1)} onPointerUp={() => setMove(0, 0)}>▶</button></div>
        </div>
        <button className="fire3d" onPointerDown={() => setFire(true)} onPointerUp={() => setFire(false)} onPointerCancel={() => setFire(false)}>FIRE</button>
      </div>
    </main>
  );
}

function enemyDamage(mission: number) { return 6 + mission * 2; }

export default App;