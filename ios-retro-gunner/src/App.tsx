import { useCallback, useEffect, useRef, useState } from "react";

type EnemyKind = "scout" | "heavy" | "turret" | "boss";
type Enemy = { id: number; x: number; y: number; hp: number; maxHp: number; speed: number; shot: number; kind: EnemyKind; hit: number; phase: number };
type Shot = { x: number; y: number; vx: number; vy: number; hostile: boolean; spread: number };
type Particle = { x: number; y: number; vx: number; vy: number; life: number; color: string; size: number };
type GameState = "briefing" | "playing" | "levelclear" | "won" | "lost";
type Level = {
  name: string; subtitle: string; kills: number; goal: number; maxEnemies: number; spawn: number;
  sky: [string, string, string]; sun: string; far: string; near: string; ground: string; accent: string;
  heavy: number; turrets: number; boss: boolean; speed: number;
};
type Game = {
  player: { x: number; y: number; vy: number; lives: number; cooldown: number; invincible: number; facing: 1 | -1 };
  enemies: Enemy[]; shots: Shot[]; particles: Particle[];
  score: number; kills: number; level: number; spawn: number; nextId: number; shake: number; state: GameState; time: number;
};

const W = 844;
const H = 390;
const FLOOR = 310;
const TOTAL_LEVELS = 5;
const LEVELS: Level[] = [
  { name: "SUNSET OUTPOST", subtitle: "Break the siege", kills: 10, goal: 10, maxEnemies: 4, spawn: .78, sky: ["#150b2e", "#7c2d91", "#ff7a45"], sun: "#ffd166", far: "#3f2a6d", near: "#271944", ground: "#241433", accent: "#ff9f4a", heavy: 0, turrets: 0, boss: false, speed: 1 },
  { name: "NEON HARBOR", subtitle: "Hold the docks", kills: 14, goal: 14, maxEnemies: 5, spawn: .68, sky: ["#031d35", "#006d77", "#ff4d8d"], sun: "#7bf1a8", far: "#16456b", near: "#0b2d47", ground: "#101826", accent: "#5ee1ff", heavy: .18, turrets: 0, boss: false, speed: 1.08 },
  { name: "VIOLET RIDGE", subtitle: "Cross the ridge", kills: 16, goal: 16, maxEnemies: 5, spawn: .62, sky: ["#12031f", "#4c1d95", "#f72585"], sun: "#ffca3a", far: "#3b2f77", near: "#211946", ground: "#1a1330", accent: "#c77dff", heavy: .24, turrets: .1, boss: false, speed: 1.16 },
  { name: "GRID CITY", subtitle: "Clear the streets", kills: 18, goal: 18, maxEnemies: 6, spawn: .56, sky: ["#020617", "#0f766e", "#f97316"], sun: "#f8f7a1", far: "#155e75", near: "#0f3b4c", ground: "#111827", accent: "#22d3ee", heavy: .3, turrets: .18, boss: false, speed: 1.24 },
  { name: "COMMAND CORE", subtitle: "Destroy the core", kills: 21, goal: 21, maxEnemies: 6, spawn: .5, sky: ["#18020d", "#7f1d1d", "#f43f5e"], sun: "#fff1a8", far: "#4a1d2f", near: "#2d1224", ground: "#17121c", accent: "#fb7185", heavy: .34, turrets: .22, boss: true, speed: 1.34 },
];

const newGame = (level = 1, score = 0, lives = 3): Game => ({
  player: { x: 118, y: FLOOR, vy: 0, lives, cooldown: 0, invincible: 1.2, facing: 1 },
  enemies: [], shots: [], particles: [], score, kills: 0, level, spawn: .7, nextId: 1, shake: 0, state: "briefing", time: 0,
});

class Synthwave {
  private ctx?: AudioContext;
  private master?: GainNode;
  private timer?: number;
  private step = 0;
  enabled = true;

  start() {
    if (this.timer || !this.enabled) return;
    const AudioCtor = window.AudioContext || (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (!AudioCtor) return;
    this.ctx = this.ctx ?? new AudioCtor();
    this.master = this.ctx.createGain();
    this.master.gain.value = .16;
    this.master.connect(this.ctx.destination);
    void this.ctx.resume();
    this.step = 0;
    this.timer = window.setInterval(() => this.tick(), 120);
  }

  stop() {
    if (this.timer) window.clearInterval(this.timer);
    this.timer = undefined;
  }

  toggle() {
    this.enabled = !this.enabled;
    if (!this.enabled) this.stop(); else this.start();
  }

  private tone(freq: number, dur: number, type: OscillatorType, gain: number, when = 0) {
    if (!this.ctx || !this.master) return;
    const t = this.ctx.currentTime + when;
    const osc = this.ctx.createOscillator();
    const amp = this.ctx.createGain();
    osc.type = type;
    osc.frequency.setValueAtTime(freq, t);
    amp.gain.setValueAtTime(0.0001, t);
    amp.gain.exponentialRampToValueAtTime(gain, t + .01);
    amp.gain.exponentialRampToValueAtTime(0.0001, t + dur);
    osc.connect(amp).connect(this.master);
    osc.start(t); osc.stop(t + dur + .03);
  }

  private tick() {
    const bass = [55, 55, 65.4, 55, 73.4, 55, 49, 61.7];
    const lead = [220, 261.6, 329.6, 392, 329.6, 440, 392, 329.6, 261.6, 329.6, 392, 523.2, 392, 329.6, 293.7, 261.6];
    const s = this.step++;
    this.tone(bass[s % bass.length], .12, "sawtooth", .12);
    if (s % 2 === 0) this.tone(lead[(s / 2) % lead.length], .09, "square", .055);
    if (s % 4 === 0) this.tone(140, .03, "triangle", .08);
    if (s % 8 === 4) this.tone(1900, .02, "sawtooth", .025);
  }
}

function App() {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const gameRef = useRef<Game>(newGame());
  const held = useRef(new Set<string>());
  const musicRef = useRef(new Synthwave());
  const [hud, setHud] = useState({ lives: 3, score: 0, kills: 0, level: 1, state: "briefing" as GameState });

  const publish = useCallback(() => {
    const g = gameRef.current;
    setHud({ lives: g.player.lives, score: g.score, kills: g.kills, level: g.level, state: g.state });
  }, []);

  const start = useCallback(() => {
    gameRef.current.state = "playing";
    musicRef.current.start();
    publish();
  }, [publish]);

  const next = useCallback(() => {
    const g = gameRef.current;
    if (g.level >= TOTAL_LEVELS) { g.state = "won"; publish(); return; }
    gameRef.current = newGame(g.level + 1, g.score + 1000, Math.min(4, g.player.lives + 1));
    gameRef.current.state = "playing";
    publish();
  }, [publish]);

  const reset = useCallback(() => {
    gameRef.current = newGame();
    held.current.clear();
    publish();
  }, [publish]);

  const press = (control: string) => (event: React.PointerEvent<HTMLButtonElement>) => {
    event.preventDefault();
    event.currentTarget.setPointerCapture(event.pointerId);
    held.current.add(control);
  };
  const release = (control: string) => () => held.current.delete(control);

  useEffect(() => {
    const down = (event: KeyboardEvent) => {
      const key = ({ ArrowLeft: "left", ArrowRight: "right", ArrowUp: "jump", KeyA: "left", KeyD: "right", KeyW: "jump", Space: "fire" } as Record<string, string>)[event.code];
      if (key) { event.preventDefault(); held.current.add(key); }
      if (event.code === "Enter" && gameRef.current.state === "briefing") start();
      if (event.code === "KeyM") musicRef.current.toggle();
    };
    const up = (event: KeyboardEvent) => {
      const key = ({ ArrowLeft: "left", ArrowRight: "right", ArrowUp: "jump", KeyA: "left", KeyD: "right", KeyW: "jump", Space: "fire" } as Record<string, string>)[event.code];
      if (key) held.current.delete(key);
    };
    window.addEventListener("keydown", down);
    window.addEventListener("keyup", up);
    return () => { window.removeEventListener("keydown", down); window.removeEventListener("keyup", up); };
  }, [start]);

  useEffect(() => {
    const canvas = canvasRef.current!;
    const ctx = canvas.getContext("2d")!;
    let animation = 0;
    let last = performance.now();
    let hudClock = 0;

    const burst = (g: Game, x: number, y: number, color: string, count = 12) => {
      for (let i = 0; i < count; i++) g.particles.push({ x, y, vx: (Math.random() - .5) * 230, vy: -50 - Math.random() * 190, life: .35 + Math.random() * .5, color, size: 2 + Math.random() * 5 });
    };
    const hit = (ax: number, ay: number, bx: number, by: number, rx = 20, ry = 25) => Math.abs(ax - bx) < rx && Math.abs(ay - by) < ry;

    const damagePlayer = (g: Game) => {
      g.player.lives--; g.player.invincible = 1.5; g.player.vy = -235; g.shake = .28;
      burst(g, g.player.x, g.player.y - 30, "#ff3d81", 14);
    };

    const spawnEnemy = (g: Game, level: Level) => {
      const roll = Math.random();
      const kind: EnemyKind = g.level === TOTAL_LEVELS && level.boss && g.kills >= level.goal - 4 && !g.enemies.some(e => e.kind === "boss")
        ? "boss"
        : roll < level.turrets ? "turret" : roll < level.turrets + level.heavy ? "heavy" : "scout";
      const hp = kind === "boss" ? 10 : kind === "heavy" ? 3 : kind === "turret" ? 2 : 1;
      const fromRight = Math.random() > .14;
      const speed = (kind === "heavy" ? 38 : kind === "turret" ? 0 : kind === "boss" ? 28 : 72) * level.speed + Math.random() * 16;
      g.enemies.push({ id: g.nextId++, x: fromRight ? W + 40 : -40, y: FLOOR, hp, maxHp: hp, speed, shot: .6 + Math.random() * 1.1, kind, hit: 0, phase: Math.random() * 10 });
    };

    const update = (g: Game, dt: number) => {
      const level = LEVELS[g.level - 1];
      const p = g.player;
      g.time += dt;
      const move = Number(held.current.has("right")) - Number(held.current.has("left"));
      if (move) p.facing = move as 1 | -1;
      p.x = Math.max(34, Math.min(W - 34, p.x + move * 245 * dt));
      if (held.current.has("jump") && p.y >= FLOOR) p.vy = -430;
      p.vy += 980 * dt;
      p.y = Math.min(FLOOR, p.y + p.vy * dt);
      if (p.y === FLOOR) p.vy = 0;
      p.cooldown -= dt; p.invincible -= dt; g.shake = Math.max(0, g.shake - dt);

      if (held.current.has("fire") && p.cooldown <= 0) {
        const spread = g.level >= 3 ? [0, -.08, .08] : [0];
        for (const angle of spread) g.shots.push({ x: p.x + p.facing * 30, y: p.y - 34, vx: p.facing * 590, vy: angle * 520, hostile: false, spread: angle });
        p.cooldown = .12;
      }

      g.spawn -= dt;
      if (g.kills + g.enemies.length < level.goal && g.spawn <= 0 && g.enemies.length < level.maxEnemies) {
        spawnEnemy(g, level);
        g.spawn = level.spawn + Math.random() * .38;
      }

      for (const enemy of g.enemies) {
        const direction = Math.sign(p.x - enemy.x) || 1;
        const desired = enemy.kind === "boss" ? 300 : enemy.kind === "heavy" ? 235 : enemy.kind === "turret" ? 390 : 150;
        if (enemy.kind !== "turret" && Math.abs(p.x - enemy.x) > desired) enemy.x += direction * enemy.speed * dt;
        if (enemy.kind === "boss") enemy.y = FLOOR - 18 + Math.sin(g.time * 2 + enemy.phase) * 18;
        enemy.shot -= dt; enemy.hit -= dt;
        if (enemy.shot <= 0 && Math.abs(p.x - enemy.x) < 560) {
          const dx = p.x - enemy.x, dy = p.y - 34 - (enemy.y - 40), length = Math.hypot(dx, dy) || 1;
          const speed = enemy.kind === "boss" ? 275 : enemy.kind === "heavy" ? 250 : enemy.kind === "turret" ? 260 : 215;
          g.shots.push({ x: enemy.x + direction * 23, y: enemy.y - 40, vx: dx / length * speed, vy: dy / length * speed, hostile: true, spread: 0 });
          if (enemy.kind === "boss") {
            g.shots.push({ x: enemy.x, y: enemy.y - 40, vx: -speed, vy: -70, hostile: true, spread: 0 });
            g.shots.push({ x: enemy.x, y: enemy.y - 40, vx: -speed, vy: 70, hostile: true, spread: 0 });
          }
          enemy.shot = (enemy.kind === "boss" ? .48 : enemy.kind === "heavy" ? .9 : enemy.kind === "turret" ? 1.05 : 1.45) + Math.random() * .65;
        }
        if (hit(p.x, p.y - 25, enemy.x, enemy.y - 27, enemy.kind === "boss" ? 34 : 25, 36) && p.invincible <= 0) damagePlayer(g);
      }

      for (const shot of g.shots) {
        shot.x += shot.vx * dt; shot.y += shot.vy * dt;
        if (shot.hostile && hit(p.x, p.y - 31, shot.x, shot.y, 16, 25) && p.invincible <= 0) { damagePlayer(g); shot.x = -999; }
        if (!shot.hostile) {
          const target = g.enemies.find(e => e.hp > 0 && hit(e.x, e.y - 32, shot.x, shot.y, e.kind === "boss" ? 34 : e.kind === "heavy" ? 27 : e.kind === "turret" ? 24 : 20, 32));
          if (target) {
            target.hp--; target.hit = .1; shot.x = 9999;
            burst(g, target.x, target.y - 36, "#ffe066", 5);
            if (target.hp <= 0) {
              g.kills++;
              g.score += target.kind === "boss" ? 2500 : target.kind === "heavy" ? 350 : target.kind === "turret" ? 250 : 100;
              burst(g, target.x, target.y - 32, target.kind === "boss" ? "#7c3aed" : "#ff477e", target.kind === "boss" ? 26 : 15);
              g.shake = target.kind === "boss" ? .34 : .14;
            }
          }
        }
      }
      g.enemies = g.enemies.filter(e => e.hp > 0 && e.x > -90 && e.x < W + 90);
      g.shots = g.shots.filter(s => s.x > -60 && s.x < W + 60 && s.y > -40 && s.y < H + 30);
      for (const particle of g.particles) { particle.x += particle.vx * dt; particle.y += particle.vy * dt; particle.vy += 470 * dt; particle.life -= dt; }
      g.particles = g.particles.filter(particle => particle.life > 0);

      if (p.lives <= 0) { g.state = "lost"; musicRef.current.stop(); }
      if (g.kills >= level.goal && g.enemies.length === 0) {
        g.score += 1200;
        g.state = g.level >= TOTAL_LEVELS ? "won" : "levelclear";
        if (g.state === "won") musicRef.current.stop();
      }
    };

    const loop = (now: number) => {
      const dt = Math.min((now - last) / 1000, .034); last = now;
      const g = gameRef.current;
      if (g.state === "playing") {
        update(g, dt); hudClock += dt;
        if (hudClock > .08 || g.state !== "playing") { publish(); hudClock = 0; }
      }
      draw(ctx, g, now);
      animation = requestAnimationFrame(loop);
    };
    animation = requestAnimationFrame(loop);
    return () => cancelAnimationFrame(animation);
  }, [publish]);

  const level = LEVELS[hud.level - 1];
  const progress = Math.min(100, hud.kills / level.goal * 100);
  return (
    <main className="game-shell">
      <section className="game-card" aria-label="Cobra Strike game">
        <canvas ref={canvasRef} width={W} height={H} />
        <header className="hud">
          <div className="identity"><small>OPERATIVE</small><strong>COBRA</strong><b>{Array.from({ length: Math.max(0, hud.lives) }, (_, i) => <i key={i} />)}</b></div>
          <div className="mission"><small>LEVEL {hud.level} · {level.name}</small><strong>{hud.kills}<em>/ {level.goal}</em></strong><span><i style={{ width: `${progress}%` }} /></span></div>
          <div className="score"><small>COMBAT SCORE</small><strong>{String(hud.score).padStart(6, "0")}</strong></div>
        </header>

        {hud.state === "briefing" && <div className="briefing">
          <div className="mission-tag">LEVEL {hud.level} · {level.name}</div>
          <p>PRIMARY OBJECTIVE</p><h1>{level.subtitle.toUpperCase()}</h1>
          <div className="goal-card"><b>{level.goal}</b><span>HOSTILES<br />TO ELIMINATE</span></div>
          <p className="orders">Smooth run-and-gun controls. Heavy armor takes three hits. Spread fire unlocks in later levels.</p>
          <button onClick={start}>DEPLOY <span>›</span></button>
        </div>}

        {hud.state === "levelclear" && <div className="result levelclear">
          <p>LEVEL CLEAR</p><h1>{level.name}</h1>
          <strong>{String(hud.score).padStart(6, "0")} <small>PTS</small></strong>
          <button onClick={next}>NEXT LEVEL</button>
        </div>}

        {(hud.state === "won" || hud.state === "lost") && <div className={`result ${hud.state}`}>
          <p>{hud.state === "won" ? "CAMPAIGN COMPLETE" : "OPERATIVE DOWN"}</p>
          <h1>{hud.state === "won" ? "CORE DESTROYED" : "THE OUTPOST HOLDS"}</h1>
          <strong>{String(hud.score).padStart(6, "0")} <small>PTS</small></strong>
          <button onClick={reset}>{hud.state === "won" ? "PLAY AGAIN" : "REDEPLOY"}</button>
        </div>}

        <div className="controls" aria-label="Touch controls">
          <div className="dpad"><button aria-label="Move left" onPointerDown={press("left")} onPointerUp={release("left")} onPointerCancel={release("left")}>◀</button><button aria-label="Move right" onPointerDown={press("right")} onPointerUp={release("right")} onPointerCancel={release("right")}>▶</button></div>
          <div className="actions"><button className="jump" onPointerDown={press("jump")} onPointerUp={release("jump")} onPointerCancel={release("jump")}><b>↑</b><span>JUMP</span></button><button className="fire" onPointerDown={press("fire")} onPointerUp={release("fire")} onPointerCancel={release("fire")}><b>✦</b><span>FIRE</span></button></div>
        </div>
      </section>
      <aside className="rotate"><span>↻</span><b>ROTATE TO LANDSCAPE</b><small>Cobra Strike is built for iPhone</small></aside>
    </main>
  );
}

function draw(ctx: CanvasRenderingContext2D, g: Game, now: number) {
  const level = LEVELS[g.level - 1];
  ctx.save();
  if (g.shake > 0) ctx.translate((Math.random() - .5) * 9, (Math.random() - .5) * 7);
  const t = now * .001;
  const sky = ctx.createLinearGradient(0, 0, 0, FLOOR);
  sky.addColorStop(0, level.sky[0]); sky.addColorStop(.5, level.sky[1]); sky.addColorStop(1, level.sky[2]);
  ctx.fillStyle = sky; ctx.fillRect(-12, -12, W + 24, H + 24);

  ctx.fillStyle = "rgba(255,255,255,.9)";
  for (let i = 0; i < 34; i++) { const y = 14 + (i * 37) % 190; ctx.fillRect((i * 89 + t * 7) % W, y, i % 3 === 0 ? 2 : 1, i % 4 === 0 ? 2 : 1); }

  const sunGlow = ctx.createRadialGradient(686, 96, 8, 686, 96, 92);
  sunGlow.addColorStop(0, level.sun); sunGlow.addColorStop(.35, level.accent); sunGlow.addColorStop(1, "rgba(255,255,255,0)");
  ctx.fillStyle = sunGlow; ctx.beginPath(); ctx.arc(686, 96, 92, 0, Math.PI * 2); ctx.fill();
  ctx.fillStyle = level.sun; ctx.beginPath(); ctx.arc(686, 96, 38, 0, Math.PI * 2); ctx.fill();
  ctx.fillStyle = level.sky[2]; for (let y = 104; y < 144; y += 9) ctx.fillRect(640, y, 92, 4);

  ridge(ctx, level.far, 0.12, 205, 64, t);
  ridge(ctx, level.near, 0.24, 244, 92, t);

  ctx.strokeStyle = "rgba(255,255,255,.1)"; ctx.lineWidth = 1;
  for (let x = -120 + (t * 70) % 120; x < W + 120; x += 120) { ctx.beginPath(); ctx.moveTo(x, FLOOR); ctx.lineTo(x + 80, 178); ctx.stroke(); }

  const ground = ctx.createLinearGradient(0, FLOOR, 0, H);
  ground.addColorStop(0, level.ground); ground.addColorStop(1, "#05070d");
  ctx.fillStyle = ground; ctx.fillRect(0, FLOOR, W, H - FLOOR);
  ctx.fillStyle = level.accent; ctx.shadowColor = level.accent; ctx.shadowBlur = 12; ctx.fillRect(0, FLOOR, W, 3); ctx.shadowBlur = 0;
  ctx.fillStyle = "rgba(255,255,255,.08)"; for (let x = -60 + (t * 170) % 76; x < W; x += 76) ctx.fillRect(x, 344, 40, 4);
  ctx.fillStyle = "rgba(0,0,0,.28)"; for (let x = 0; x < W; x += 42) ctx.fillRect(x, 356 + (x % 4) * 4, 24, 5);

  for (const shot of g.shots) {
    ctx.fillStyle = shot.hostile ? "#ff477e" : "#fff06a";
    ctx.shadowColor = ctx.fillStyle; ctx.shadowBlur = 12;
    ctx.fillRect(shot.x - 9, shot.y - 2, 18, shot.hostile ? 4 : 5);
    ctx.shadowBlur = 0;
  }
  for (const enemy of g.enemies) soldier(ctx, enemy, false);
  const flicker = g.player.invincible > 0 && Math.floor(now / 70) % 2 === 0;
  if (!flicker) {
    soldier(ctx, { id: 0, x: g.player.x, y: g.player.y, hp: 1, maxHp: 1, speed: 0, shot: 0, kind: "scout", hit: 0, phase: 0 }, true, g.player.facing);
  }
  for (const p of g.particles) {
    ctx.globalAlpha = Math.min(1, p.life * 3);
    ctx.fillStyle = p.color; ctx.shadowColor = p.color; ctx.shadowBlur = 8;
    ctx.fillRect(p.x - p.size / 2, p.y - p.size / 2, p.size, p.size);
  }
  ctx.globalAlpha = 1; ctx.shadowBlur = 0;
  ctx.restore();
}

function ridge(ctx: CanvasRenderingContext2D, color: string, speed: number, base: number, amp: number, t: number) {
  ctx.fillStyle = color;
  ctx.beginPath(); ctx.moveTo(0, FLOOR);
  for (let x = 0; x <= W + 20; x += 36) {
    const y = base - Math.abs(Math.sin((x * .018) + t * speed)) * amp - Math.sin((x * .041) + t * speed * 1.7) * 18;
    ctx.lineTo(x, y);
  }
  ctx.lineTo(W, FLOOR); ctx.closePath(); ctx.fill();
}

function soldier(ctx: CanvasRenderingContext2D, enemy: Enemy, hero: boolean, facing: 1 | -1 = enemy.x < W / 2 ? 1 : -1) {
  const heavy = enemy.kind === "heavy" || enemy.kind === "boss";
  const turret = enemy.kind === "turret";
  const boss = enemy.kind === "boss";
  const x = enemy.x, y = enemy.y;
  ctx.save(); ctx.translate(x, y); ctx.scale(facing, 1);
  ctx.fillStyle = "rgba(0,0,0,.45)"; ctx.beginPath(); ctx.ellipse(0, 5, boss ? 34 : heavy ? 27 : 22, 6, 0, 0, Math.PI * 2); ctx.fill();
  if (turret) {
    ctx.fillStyle = enemy.hit > 0 ? "#fff" : "#26324a"; ctx.fillRect(-18, -30, 36, 30);
    ctx.fillStyle = "#10b5e9"; ctx.fillRect(-10, -38, 20, 10);
    ctx.fillStyle = "#f8fafc"; ctx.fillRect(9, -34, 26, 5);
    ctx.fillStyle = "#ff477e"; ctx.fillRect(30, -35, 6, 7);
    ctx.restore(); return;
  }
  const suit = enemy.hit > 0 ? "#ffffff" : hero ? "#ffd166" : boss ? "#7c3aed" : heavy ? "#ef476f" : "#06d6a0";
  const trim = hero ? "#118ab2" : boss ? "#f72585" : heavy ? "#ffd166" : "#ff8fab";
  ctx.fillStyle = suit; ctx.fillRect(-11, -45, heavy ? 25 : 21, 30);
  ctx.fillStyle = "rgba(255,255,255,.28)"; ctx.fillRect(-11, -45, 5, 30);
  ctx.fillStyle = trim; ctx.fillRect(-11, -32, heavy ? 25 : 21, 4);
  ctx.fillStyle = suit; ctx.fillRect(-10, -15, 8, 19); ctx.fillRect(5, -15, 8, 19);
  ctx.fillStyle = hero ? "#ffcf9f" : "#b47e64"; ctx.fillRect(-8, -60, 17, 16);
  ctx.fillStyle = hero ? "#ef476f" : boss ? "#111827" : "#27324a"; ctx.fillRect(-11, -63, 22, 7);
  ctx.fillStyle = trim; ctx.fillRect(7, -56, 9, 4);
  ctx.fillStyle = "#e5e7eb"; ctx.fillRect(8, -41, heavy ? 32 : 28, 6);
  ctx.fillStyle = "#111827"; ctx.fillRect(31, -38, heavy ? 13 : 9, 3);
  if (heavy) { ctx.fillStyle = trim; ctx.fillRect(-15, -49, 31, 7); }
  if (boss) {
    ctx.strokeStyle = "#f72585"; ctx.lineWidth = 3; ctx.strokeRect(-18, -66, 38, 56);
    ctx.fillStyle = "#f72585"; ctx.fillRect(-4, -73, 8, 8);
  }
  ctx.restore();
}

export default App;
