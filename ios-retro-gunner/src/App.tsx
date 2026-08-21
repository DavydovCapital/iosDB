import { useCallback, useEffect, useRef, useState } from "react";
import * as THREE from "three";

type Mission = { name: string; objective: string; enemies: number; fog: number; sky: number; ground: number; accent: number; speed: number };
type Hud = { state: "briefing" | "playing" | "clear" | "won" | "lost"; mission: number; kills: number; health: number; score: number; ammo: number };
type EnemyKind = "grunt" | "heavy" | "drone";
type Enemy = { mesh: THREE.Group; hp: number; speed: number; cooldown: number; kind: EnemyKind; strafe: number; baseY: number };
type Bolt = { mesh: THREE.Mesh; velocity: THREE.Vector3; hostile: boolean; life: number; damage: number };
type Particle = { mesh: THREE.Object3D; velocity: THREE.Vector3; life: number; grow?: number };
type FloatingText = { el: HTMLDivElement; world: THREE.Vector3; life: number };

const MISSIONS: Mission[] = [
  { name: "BLACKSITE DAWN", objective: "Breach the outer yard", enemies: 10, fog: 0x111827, sky: 0x475569, ground: 0x1f2937, accent: 0x7dd3fc, speed: 1 },
  { name: "IRON HARBOR", objective: "Sweep the loading docks", enemies: 13, fog: 0x0f172a, sky: 0x334155, ground: 0x18202b, accent: 0x38bdf8, speed: 1.08 },
  { name: "ASH RIDGE", objective: "Clear the fortified ridge", enemies: 16, fog: 0x1c1917, sky: 0x57534e, ground: 0x1d1a17, accent: 0xf59e0b, speed: 1.16 },
  { name: "DEAD GRID", objective: "Push through the city block", enemies: 19, fog: 0x0b1220, sky: 0x1e293b, ground: 0x10141c, accent: 0x60a5fa, speed: 1.26 },
  { name: "COMMAND CORE", objective: "Annihilate the final guard", enemies: 24, fog: 0x140b12, sky: 0x2f1d27, ground: 0x17121a, accent: 0xfb7185, speed: 1.38 },
];

const tmpVec = new THREE.Vector3();

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
    this.master.gain.value = .11;
    this.master.connect(this.ctx.destination);
    void this.ctx.resume();
    this.timer = window.setInterval(() => this.tick(), 124);
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
    this.tone(bass[s % bass.length], .1, "sawtooth", .08);
    if (s % 2 === 0) this.tone(lead[(s / 2) % lead.length], .07, "square", .035);
    if (s % 4 === 0) this.tone(1500, .018, "sawtooth", .018);
  }
}

function App() {
  const mountRef = useRef<HTMLDivElement>(null);
  const labelsRef = useRef<HTMLDivElement>(null);
  const hudRef = useRef<Hud>({ state: "briefing", mission: 1, kills: 0, health: 100, score: 0, ammo: 30 });
  const [hud, setHud] = useState<Hud>(hudRef.current);
  const input = useRef({ forward: 0, strafe: 0, firing: false, aiming: false, reload: false, lookX: 0, lookY: 0 });
  const music = useRef(new Synth());

  const setState = useCallback((patch: Partial<Hud>) => {
    hudRef.current = { ...hudRef.current, ...patch };
    setHud(hudRef.current);
  }, []);

  useEffect(() => {
    const mount = mountRef.current!;
    const labels = labelsRef.current!;
    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(70, window.innerWidth / window.innerHeight, .08, 400);
    const renderer = new THREE.WebGLRenderer({ antialias: true, powerPreference: "high-performance" });
    renderer.setSize(window.innerWidth, window.innerHeight);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    renderer.shadowMap.enabled = true;
    renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    renderer.toneMapping = THREE.ACESFilmicToneMapping;
    renderer.toneMappingExposure = 1.15;
    mount.appendChild(renderer.domElement);

    const clock = new THREE.Clock();
    const world = new THREE.Group(); scene.add(world);
    const enemies: Enemy[] = [];
    const bolts: Bolt[] = [];
    const particles: Particle[] = [];
    const texts: FloatingText[] = [];

    let mission = 1, kills = 0, spawned = 0, health = 100, score = 0, ammo = 30;
    let yaw = 0, pitch = 0, fireCooldown = 0, reloadTimer = 0, shake = 0, won = false;

    const hemi = new THREE.HemisphereLight(0xdbeafe, 0x111827, 1.6); scene.add(hemi);
    const sun = new THREE.DirectionalLight(0xffffff, 2.9); sun.position.set(-12, 18, 10); sun.castShadow = true; sun.shadow.mapSize.set(2048, 2048); sun.shadow.camera.near = 1; sun.shadow.camera.far = 90; scene.add(sun);
    const fill = new THREE.DirectionalLight(0x93c5fd, 1.1); fill.position.set(8, 10, -8); scene.add(fill);
    const muzzle = new THREE.PointLight(0xfbbf24, 0, 16); scene.add(muzzle);

    const gun = new THREE.Group();
    const gunMat = new THREE.MeshStandardMaterial({ color: 0x111827, roughness: .34, metalness: .82 });
    const darkMat = new THREE.MeshStandardMaterial({ color: 0x020617, roughness: .58, metalness: .35 });
    const accentMat = new THREE.MeshBasicMaterial({ color: 0x38bdf8 });
    const receiver = new THREE.Mesh(new THREE.BoxGeometry(.34, .18, .82), gunMat);
    const barrel = new THREE.Mesh(new THREE.CylinderGeometry(.035, .035, .72, 12), darkMat); barrel.rotation.x = Math.PI / 2; barrel.position.z = -.55;
    const stock = new THREE.Mesh(new THREE.BoxGeometry(.26, .2, .28), darkMat); stock.position.set(0, -.01, .42);
    const mag = new THREE.Mesh(new THREE.BoxGeometry(.18, .34, .16), gunMat); mag.position.set(0, -.23, .06);
    const sight = new THREE.Mesh(new THREE.BoxGeometry(.18, .08, .18), accentMat); sight.position.set(0, .16, -.12);
    gun.add(receiver, barrel, stock, mag, sight); gun.position.set(.36, -.3, -.72); camera.add(gun); scene.add(camera);

    const mat = (color: number, roughness = .72, metalness = .12, emissive = 0x000000, intensity = .15) => new THREE.MeshStandardMaterial({ color, roughness, metalness, emissive, emissiveIntensity: intensity });

    const addText = (value: string, worldPos: THREE.Vector3, color = "#fff") => {
      const el = document.createElement("div");
      el.textContent = value;
      el.style.position = "absolute"; el.style.color = color; el.style.font = "900 12px ui-monospace,monospace"; el.style.textShadow = "0 2px 0 #000,0 0 10px currentColor"; el.style.pointerEvents = "none";
      labels.appendChild(el);
      texts.push({ el, world: worldPos.clone(), life: .7 });
    };

    const burst = (position: THREE.Vector3, color: number, count = 12, size = .08, smoke = false) => {
      for (let i = 0; i < count; i++) {
        const mesh = smoke
          ? new THREE.Mesh(new THREE.SphereGeometry(size + Math.random() * size * 1.8, 8, 8), new THREE.MeshStandardMaterial({ color, transparent: true, opacity: .5, roughness: 1 }))
          : new THREE.Mesh(new THREE.SphereGeometry(size + Math.random() * size, 6, 6), new THREE.MeshBasicMaterial({ color }));
        mesh.position.copy(position);
        world.add(mesh);
        particles.push({ mesh, velocity: new THREE.Vector3((Math.random() - .5) * 7, Math.random() * 4.5, (Math.random() - .5) * 7), life: smoke ? 1.1 + Math.random() * .8 : .45 + Math.random() * .35, grow: smoke ? 1.5 : 0 });
      }
    };

    const crate = (x: number, z: number, s: number, c = 0x334155) => {
      const m = new THREE.Mesh(new THREE.BoxGeometry(s, s, s), mat(c, .8, .08));
      m.position.set(x, s / 2, z); m.castShadow = true; m.receiveShadow = true; world.add(m);
      const trim = new THREE.Mesh(new THREE.BoxGeometry(s * 1.02, s * .08, s * 1.02), new THREE.MeshBasicMaterial({ color: 0x0ea5e9 }));
      trim.position.set(x, s * .52, z); world.add(trim);
    };

    const barrier = (x: number, z: number, rot = 0) => {
      const g = new THREE.Group();
      const base = new THREE.Mesh(new THREE.BoxGeometry(3.4, 1.2, .5), mat(0x475569, .84, .18)); base.castShadow = true; base.receiveShadow = true; g.add(base);
      const stripe = new THREE.Mesh(new THREE.BoxGeometry(3.45, .12, .52), new THREE.MeshBasicMaterial({ color: 0xf59e0b })); stripe.position.y = .26; g.add(stripe);
      g.position.set(x, .6, z); g.rotation.y = rot; world.add(g);
    };

    const tower = (x: number, z: number, h: number, c: number) => {
      const g = new THREE.Group();
      const body = new THREE.Mesh(new THREE.BoxGeometry(3, h, 3), mat(c, .82, .16)); body.position.y = h / 2; body.castShadow = true; body.receiveShadow = true; g.add(body);
      for (let i = 1; i < h / 2; i++) {
        const win = new THREE.Mesh(new THREE.BoxGeometry(3.04, .08, .18), new THREE.MeshBasicMaterial({ color: 0x93c5fd }));
        win.position.set(0, i * 2, -1.53); g.add(win);
      }
      g.position.set(x, 0, z); world.add(g);
    };

    const makeEnemy = (kind: EnemyKind, m: Mission): Enemy => {
      const g = new THREE.Group();
      const color = kind === "heavy" ? 0x7f1d1d : kind === "drone" ? 0x334155 : 0x365314;
      const accent = kind === "heavy" ? 0xf97316 : kind === "drone" ? 0x60a5fa : 0x84cc16;
      const body = new THREE.Mesh(new THREE.CapsuleGeometry(.55, kind === "heavy" ? 1.4 : 1.05, 5, 12), mat(color, .6, .22)); body.position.y = kind === "drone" ? 2.15 : 1.1; body.castShadow = true; g.add(body);
      const vest = new THREE.Mesh(new THREE.BoxGeometry(.95, .7, .32), mat(0x111827, .74, .18)); vest.position.set(0, kind === "drone" ? 2.05 : 1.18, -.18); g.add(vest);
      const head = new THREE.Mesh(new THREE.SphereGeometry(.3, 12, 12), mat(0x9ca3af, .48, .28)); head.position.y = kind === "drone" ? 2.85 : 2.02; head.castShadow = true; g.add(head);
      const visor = new THREE.Mesh(new THREE.BoxGeometry(.44, .09, .08), new THREE.MeshBasicMaterial({ color: accent })); visor.position.set(0, kind === "drone" ? 2.85 : 2.02, -.24); g.add(visor);
      const rifle = new THREE.Mesh(new THREE.BoxGeometry(1.05, .12, .14), mat(0x020617, .34, .8)); rifle.position.set(.42, kind === "drone" ? 2.14 : 1.26, -.2); g.add(rifle);
      if (kind === "drone") {
        const ring = new THREE.Mesh(new THREE.TorusGeometry(.72, .05, 8, 20), new THREE.MeshBasicMaterial({ color: accent })); ring.rotation.x = Math.PI / 2; ring.position.y = 2.15; g.add(ring);
      }
      g.position.set((Math.random() - .5) * 30, 0, -28 - Math.random() * 36);
      world.add(g);
      return { mesh: g, hp: kind === "heavy" ? 4 : kind === "drone" ? 2 : 2, speed: (kind === "heavy" ? 1.5 : kind === "drone" ? 2.9 : 2.2) * m.speed, cooldown: .5 + Math.random() * 1.2, kind, strafe: Math.random() > .5 ? 1 : -1, baseY: 0 };
    };

    const buildMission = (index: number) => {
      world.clear(); enemies.length = 0; bolts.length = 0; particles.length = 0;
      for (const t of texts) t.el.remove(); texts.length = 0;
      const m = MISSIONS[index - 1];
      scene.background = new THREE.Color(m.fog);
      scene.fog = new THREE.FogExp2(m.fog, .024);
      const ground = new THREE.Mesh(new THREE.PlaneGeometry(280, 280), mat(m.ground, .92, .04)); ground.rotation.x = -Math.PI / 2; ground.receiveShadow = true; world.add(ground);
      const runway = new THREE.Mesh(new THREE.PlaneGeometry(10, 220), mat(0x111827, .88, .06)); runway.rotation.x = -Math.PI / 2; runway.position.y = .01; world.add(runway);
      for (let z = -110; z < 90; z += 8) {
        const lane = new THREE.Mesh(new THREE.BoxGeometry(.12, .02, 3), new THREE.MeshBasicMaterial({ color: 0x64748b })); lane.position.set(0, .03, z); world.add(lane);
      }
      for (let i = 0; i < 14; i++) barrier((Math.random() - .5) * 34, -12 - i * 6 - Math.random() * 4, Math.random() * Math.PI);
      for (let i = 0; i < 22; i++) crate((Math.random() - .5) * 60, -18 - Math.random() * 95, 1 + Math.random() * 2.8, i % 3 ? 0x334155 : 0x3f3f46);
      for (let i = 0; i < 16; i++) tower((Math.random() > .5 ? 1 : -1) * (18 + Math.random() * 70), -20 - Math.random() * 120, 7 + Math.random() * 20, i % 2 ? 0x1f2937 : 0x273449);
      for (let i = 0; i < 24; i++) {
        const post = new THREE.Mesh(new THREE.CylinderGeometry(.05, .05, 2.6, 6), mat(0x111827, .4, .5));
        post.position.set((Math.random() > .5 ? 1 : -1) * (10 + Math.random() * 30), 1.3, -Math.random() * 120);
        world.add(post);
        const lamp = new THREE.Mesh(new THREE.SphereGeometry(.12, 8, 8), new THREE.MeshBasicMaterial({ color: m.accent })); lamp.position.set(post.position.x, 2.7, post.position.z); world.add(lamp);
      }
      camera.position.set(0, 1.72, 8); yaw = 0; pitch = 0; ammo = 30;
      kills = 0; spawned = 0; health = 100; won = false;
      setState({ state: "briefing", mission: index, kills: 0, health: 100, score, ammo });
    };

    const spawnEnemy = () => {
      const m = MISSIONS[mission - 1];
      const roll = Math.random();
      const kind: EnemyKind = mission >= 3 && roll < .2 ? "drone" : mission >= 2 && roll < .35 ? "heavy" : "grunt";
      enemies.push(makeEnemy(kind, m)); spawned++;
    };

    const reload = () => {
      if (reloadTimer > 0 || ammo === 30) return;
      reloadTimer = 1.1;
      addText("RELOADING", camera.position.clone().add(new THREE.Vector3(0, -.2, -2)), "#fbbf24");
    };

    const shoot = () => {
      if (fireCooldown > 0 || reloadTimer > 0 || hudRef.current.state !== "playing") return;
      if (ammo <= 0) { reload(); return; }
      fireCooldown = input.current.aiming ? .19 : .13;
      ammo--;
      setState({ ammo });
      muzzle.intensity = 8;
      shake = Math.max(shake, input.current.aiming ? .04 : .08);
      pitch += input.current.aiming ? .012 : .022;
      yaw += (Math.random() - .5) * (input.current.aiming ? .006 : .014);
      const mesh = new THREE.Mesh(new THREE.SphereGeometry(.055, 8, 8), new THREE.MeshBasicMaterial({ color: 0xfef08a }));
      const origin = new THREE.Vector3(); camera.getWorldPosition(origin);
      const dir = new THREE.Vector3(); camera.getWorldDirection(dir);
      mesh.position.copy(origin.clone().add(dir.clone().multiplyScalar(.9)));
      world.add(mesh); bolts.push({ mesh, velocity: dir.multiplyScalar(42), hostile: false, life: 1.5, damage: 1 });
      burst(origin.clone().add(dir.clone().multiplyScalar(.8)), 0xfbbf24, 4, .03);
    };

    const enemyShoot = (enemy: Enemy) => {
      const mesh = new THREE.Mesh(new THREE.SphereGeometry(.07, 8, 8), new THREE.MeshBasicMaterial({ color: 0xfb7185 }));
      const start = enemy.mesh.position.clone().add(new THREE.Vector3(0, enemy.kind === "drone" ? 2.2 : 1.35, 0));
      const target = camera.position.clone().add(new THREE.Vector3((Math.random() - .5) * .5, -.08, (Math.random() - .5) * .5));
      mesh.position.copy(start); world.add(mesh);
      bolts.push({ mesh, velocity: target.sub(start).normalize().multiplyScalar(16 + mission), hostile: true, life: 2.4, damage: 7 + mission * 2 });
      burst(start, 0xfb7185, 3, .025);
    };

    buildMission(1);

    const onResize = () => { camera.aspect = window.innerWidth / window.innerHeight; camera.updateProjectionMatrix(); renderer.setSize(window.innerWidth, window.innerHeight); };
    const onLook = (e: PointerEvent) => {
      if (e.pointerType !== "mouse" && e.clientX < window.innerWidth * .42) return;
      input.current.lookX += e.movementX * .0022;
      input.current.lookY += e.movementY * .0022;
    };
    const onKey = (e: KeyboardEvent) => {
      if (e.code === "KeyR") reload();
      if (e.code === "ShiftLeft") input.current.aiming = e.type === "keydown";
    };
    window.addEventListener("resize", onResize);
    window.addEventListener("pointermove", onLook);
    window.addEventListener("keydown", onKey);
    window.addEventListener("keyup", onKey);

    let raf = 0;
    const animate = () => {
      raf = requestAnimationFrame(animate);
      const dt = Math.min(clock.getDelta(), .033);
      const st = hudRef.current.state;
      const m = MISSIONS[mission - 1];
      yaw -= input.current.lookX; pitch -= input.current.lookY; pitch = Math.max(-1.1, Math.min(1.1, pitch));
      input.current.lookX = 0; input.current.lookY = 0;
      if (shake > 0) { yaw += (Math.random() - .5) * shake; pitch += (Math.random() - .5) * shake * .5; shake = Math.max(0, shake - dt * .16); }
      camera.rotation.set(pitch, yaw, 0, "YXZ");
      camera.fov = input.current.aiming ? 50 : 70; camera.updateProjectionMatrix();
      gun.position.lerp(input.current.aiming ? new THREE.Vector3(.12, -.22, -.55) : new THREE.Vector3(.36, -.3, -.72), 12 * dt);

      if (st === "playing") {
        const move = new THREE.Vector3(input.current.strafe, 0, -input.current.forward).normalize().multiplyScalar((input.current.aiming ? 4.2 : 7.4) * dt);
        move.applyAxisAngle(new THREE.Vector3(0, 1, 0), yaw);
        camera.position.add(move);
        camera.position.x = Math.max(-26, Math.min(26, camera.position.x));
        camera.position.z = Math.max(-86, Math.min(10, camera.position.z));
        camera.position.y = 1.72 + Math.sin(clock.elapsedTime * 11) * .012;

        if (spawned < m.enemies && enemies.length < Math.min(7, 4 + mission)) spawnEnemy();
        if (input.current.firing) shoot();
        if (input.current.reload) { reload(); input.current.reload = false; }
        if (reloadTimer > 0) {
          reloadTimer -= dt;
          if (reloadTimer <= 0) { ammo = 30; setState({ ammo }); }
        }
        fireCooldown = Math.max(0, fireCooldown - dt);
        muzzle.intensity = Math.max(0, muzzle.intensity - 34 * dt);

        for (const enemy of enemies) {
          const toPlayer = camera.position.clone().sub(enemy.mesh.position);
          const dist = toPlayer.length();
          enemy.mesh.lookAt(camera.position.x, enemy.mesh.position.y + 1.2, camera.position.z);
          const lateral = new THREE.Vector3(-toPlayer.z, 0, toPlayer.x).normalize().multiplyScalar(enemy.strafe * enemy.speed * .35 * dt);
          if (dist > (enemy.kind === "drone" ? 12 : 9)) enemy.mesh.position.add(toPlayer.normalize().multiplyScalar(enemy.speed * dt)).add(lateral);
          if (Math.random() < .01) enemy.strafe *= -1;
          if (enemy.kind === "drone") enemy.mesh.position.y = 1.1 + Math.sin(clock.elapsedTime * 3 + enemy.mesh.id) * .35;
          enemy.cooldown -= dt;
          if (enemy.cooldown <= 0 && dist < 38) { enemyShoot(enemy); enemy.cooldown = (enemy.kind === "heavy" ? .85 : enemy.kind === "drone" ? 1 : 1.25) + Math.random() * .55; }
        }

        for (const bolt of bolts) {
          bolt.mesh.position.add(bolt.velocity.clone().multiplyScalar(dt));
          bolt.life -= dt;
          if (!bolt.hostile) {
            for (const enemy of enemies) {
              const targetPos = enemy.mesh.position.clone().add(new THREE.Vector3(0, enemy.kind === "drone" ? 2.2 : 1.3, 0));
              if (bolt.mesh.position.distanceTo(targetPos) < (enemy.kind === "heavy" ? 1.15 : .95)) {
                enemy.hp -= bolt.damage; bolt.life = 0;
                burst(bolt.mesh.position, 0xfef08a, 5, .035);
                addText("HIT", targetPos, "#fbbf24");
                if (enemy.hp <= 0) {
                  burst(targetPos, enemy.kind === "heavy" ? 0xf97316 : 0xef4444, enemy.kind === "heavy" ? 22 : 14, .08);
                  burst(targetPos, 0x525252, enemy.kind === "heavy" ? 8 : 5, .12, true);
                  addText(enemy.kind === "heavy" ? "+300" : enemy.kind === "drone" ? "+250" : "+100", targetPos, "#86efac");
                  world.remove(enemy.mesh);
                  enemies.splice(enemies.indexOf(enemy), 1);
                  kills++; score += enemy.kind === "heavy" ? 300 : enemy.kind === "drone" ? 250 : 100;
                  setState({ kills, score });
                }
                break;
              }
            }
          } else if (bolt.mesh.position.distanceTo(camera.position) < .85) {
            bolt.life = 0; health -= bolt.damage; shake = Math.max(shake, .16);
            burst(camera.position.clone().add(new THREE.Vector3(0, -.1, -.6)), 0xef4444, 6, .04);
            setState({ health });
            if (health <= 0) { setState({ state: "lost" }); music.current.stop(); }
          }
        }
        for (let i = bolts.length - 1; i >= 0; i--) if (bolts[i].life <= 0) { world.remove(bolts[i].mesh); bolts.splice(i, 1); }
        for (let i = particles.length - 1; i >= 0; i--) {
          const p = particles[i]; p.mesh.position.add(p.velocity.clone().multiplyScalar(dt)); p.velocity.y -= 7.5 * dt; p.life -= dt;
          if (p.grow) p.mesh.scale.addScalar(p.grow * dt);
          const material = (p.mesh as THREE.Mesh).material as THREE.Material & { opacity?: number };
          material.transparent = true; if (typeof material.opacity === "number") material.opacity = Math.max(0, Math.min(1, p.life));
          if (p.life <= 0) { world.remove(p.mesh); particles.splice(i, 1); }
        }
        for (let i = texts.length - 1; i >= 0; i--) {
          const t = texts[i]; t.life -= dt;
          const screen = t.world.clone().project(camera);
          t.el.style.left = `${(screen.x * .5 + .5) * window.innerWidth}px`;
          t.el.style.top = `${(-screen.y * .5 + .5) * window.innerHeight}px`;
          t.el.style.opacity = String(Math.max(0, t.life * 1.4));
          if (t.life <= 0) { t.el.remove(); texts.splice(i, 1); }
        }
        if (!won && kills >= m.enemies && enemies.length === 0) {
          won = true; score += 1000;
          if (mission >= MISSIONS.length) { setState({ state: "won", score }); music.current.stop(); }
          else setState({ state: "clear", score });
        }
      }
      renderer.render(scene, camera);
    };
    animate();

    const api = {
      start() { music.current.start(); setState({ state: "playing" }); },
      next() { mission++; score += 500; buildMission(mission); music.current.start(); setState({ state: "playing", mission, kills: 0, health: 100, score, ammo: 30 }); },
      reset() { mission = 1; score = 0; buildMission(1); setState({ state: "briefing", mission: 1, kills: 0, health: 100, score: 0, ammo: 30 }); },
    };
    (window as unknown as { cobra3d?: typeof api }).cobra3d = api;

    return () => {
      cancelAnimationFrame(raf);
      window.removeEventListener("resize", onResize);
      window.removeEventListener("pointermove", onLook);
      window.removeEventListener("keydown", onKey);
      window.removeEventListener("keyup", onKey);
      mount.removeChild(renderer.domElement);
      renderer.dispose();
    };
  }, [setState]);

  const begin = () => (window as unknown as { cobra3d?: { start(): void } }).cobra3d?.start();
  const next = () => (window as unknown as { cobra3d?: { next(): void } }).cobra3d?.next();
  const reset = () => (window as unknown as { cobra3d?: { reset(): void } }).cobra3d?.reset();
  const setMove = (forward: number, strafe: number) => { input.current.forward = forward; input.current.strafe = strafe; };
  const setFire = (v: boolean) => { input.current.firing = v; };
  const setAim = (v: boolean) => { input.current.aiming = v; };
  const doReload = () => { input.current.reload = true; };

  return (
    <main className="game3d-shell">
      <div ref={mountRef} className="game3d-viewport" />
      <div ref={labelsRef} className="labels3d" />
      <div className="crosshair">+</div>
      <header className="hud3d">
        <div><small>MISSION {hud.mission}</small><strong>{MISSIONS[hud.mission - 1].name}</strong></div>
        <div><small>KILLS</small><strong>{hud.kills}/{MISSIONS[hud.mission - 1].enemies}</strong></div>
        <div><small>HEALTH</small><strong>{hud.health}</strong></div>
        <div><small>AMMO</small><strong>{hud.ammo}/30</strong></div>
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
        <div className="combatpad">
          <button onPointerDown={() => setAim(true)} onPointerUp={() => setAim(false)} onPointerCancel={() => setAim(false)}>ADS</button>
          <button onClick={doReload}>RELOAD</button>
          <button className="fire3d" onPointerDown={() => setFire(true)} onPointerUp={() => setFire(false)} onPointerCancel={() => setFire(false)}>FIRE</button>
        </div>
      </div>
    </main>
  );
}

export default App;