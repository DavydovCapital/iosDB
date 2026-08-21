import { useCallback, useEffect, useRef, useState } from "react";

type EnemyKind = "scout" | "heavy";
type Enemy = { id: number; x: number; y: number; hp: number; maxHp: number; speed: number; shot: number; kind: EnemyKind; hit: number };
type Shot = { x: number; y: number; vx: number; vy: number; hostile: boolean };
type Particle = { x: number; y: number; vx: number; vy: number; life: number; color: string };
type GameState = "briefing" | "playing" | "won" | "lost";
type Game = {
  player: { x: number; y: number; vy: number; lives: number; cooldown: number; invincible: number; facing: 1 | -1 };
  enemies: Enemy[]; shots: Shot[]; particles: Particle[];
  score: number; defeated: number; wave: number; spawn: number; nextId: number; shake: number; state: GameState;
};

const W = 844;
const H = 390;
const FLOOR = 310;
const GOAL = 24;
const WAVE_SIZE = 8;

const newGame = (): Game => ({
  player: { x: 130, y: FLOOR, vy: 0, lives: 3, cooldown: 0, invincible: 0, facing: 1 },
  enemies: [], shots: [], particles: [], score: 0, defeated: 0, wave: 1, spawn: 0.6, nextId: 1, shake: 0, state: "briefing",
});

function App() {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const gameRef = useRef<Game>(newGame());
  const held = useRef(new Set<string>());
  const [hud, setHud] = useState({ lives: 3, score: 0, defeated: 0, wave: 1, state: "briefing" as GameState });

  const publish = useCallback(() => {
    const g = gameRef.current;
    setHud({ lives: g.player.lives, score: g.score, defeated: g.defeated, wave: g.wave, state: g.state });
  }, []);

  const start = useCallback(() => {
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

    const burst = (g: Game, x: number, y: number, color: string, count = 9) => {
      for (let i = 0; i < count; i++) g.particles.push({ x, y, vx: (Math.random() - .5) * 190, vy: -40 - Math.random() * 160, life: .35 + Math.random() * .35, color });
    };
    const hit = (ax: number, ay: number, bx: number, by: number, rx = 20, ry = 25) => Math.abs(ax - bx) < rx && Math.abs(ay - by) < ry;

    const update = (g: Game, dt: number) => {
      const p = g.player;
      const move = Number(held.current.has("right")) - Number(held.current.has("left"));
      if (move) p.facing = move as 1 | -1;
      p.x = Math.max(42, Math.min(W - 42, p.x + move * 215 * dt));
      if (held.current.has("jump") && p.y >= FLOOR) p.vy = -390;
      p.vy += 930 * dt;
      p.y = Math.min(FLOOR, p.y + p.vy * dt);
      if (p.y === FLOOR) p.vy = 0;
      p.cooldown -= dt; p.invincible -= dt; g.shake = Math.max(0, g.shake - dt);

      if (held.current.has("fire") && p.cooldown <= 0) {
        g.shots.push({ x: p.x + p.facing * 28, y: p.y - 35, vx: p.facing * 540, vy: 0, hostile: false });
        p.cooldown = .14;
      }

      const waveLimit = Math.min(GOAL, g.wave * WAVE_SIZE);
      g.spawn -= dt;
      if (g.defeated + g.enemies.length < waveLimit && g.spawn <= 0 && g.enemies.length < 5) {
        const heavy = g.wave > 1 && Math.random() < .22;
        const hp = heavy ? 3 : 1;
        const fromRight = Math.random() > .12;
        g.enemies.push({ id: g.nextId++, x: fromRight ? W + 30 : -30, y: FLOOR, hp, maxHp: hp, speed: heavy ? 36 : 63 + Math.random() * 24, shot: .8 + Math.random() * 1.2, kind: heavy ? "heavy" : "scout", hit: 0 });
        g.spawn = .46 + Math.random() * .5;
      }

      for (const enemy of g.enemies) {
        const direction = Math.sign(p.x - enemy.x) || 1;
        if (Math.abs(p.x - enemy.x) > (enemy.kind === "heavy" ? 250 : 145)) enemy.x += direction * enemy.speed * dt;
        enemy.shot -= dt; enemy.hit -= dt;
        if (enemy.shot <= 0 && Math.abs(p.x - enemy.x) < 520) {
          const dx = p.x - enemy.x, dy = p.y - 34 - (enemy.y - 38), length = Math.hypot(dx, dy);
          g.shots.push({ x: enemy.x + direction * 22, y: enemy.y - 38, vx: dx / length * (enemy.kind === "heavy" ? 245 : 205), vy: dy / length * 205, hostile: true });
          enemy.shot = (enemy.kind === "heavy" ? .9 : 1.55) + Math.random() * .8;
        }
        if (hit(p.x, p.y - 25, enemy.x, enemy.y - 25, 25, 34) && p.invincible <= 0) damagePlayer(g);
      }

      for (const shot of g.shots) {
        shot.x += shot.vx * dt; shot.y += shot.vy * dt;
        if (shot.hostile && hit(p.x, p.y - 31, shot.x, shot.y, 17, 25) && p.invincible <= 0) { damagePlayer(g); shot.x = -999; }
        if (!shot.hostile) {
          const target = g.enemies.find(e => e.hp > 0 && hit(e.x, e.y - 32, shot.x, shot.y, e.kind === "heavy" ? 27 : 20, 31));
          if (target) {
            target.hp--; target.hit = .1; shot.x = 9999;
            burst(g, shot.x > W ? target.x : shot.x, target.y - 35, "#ffd45c", 4);
            if (target.hp <= 0) { g.defeated++; g.score += target.kind === "heavy" ? 350 : 100; burst(g, target.x, target.y - 30, "#ff6038", 13); g.shake = .16; }
          }
        }
      }
      g.enemies = g.enemies.filter(e => e.hp > 0 && e.x > -80 && e.x < W + 80);
      g.shots = g.shots.filter(s => s.x > -50 && s.x < W + 50 && s.y > -30 && s.y < H + 20);
      for (const particle of g.particles) { particle.x += particle.vx * dt; particle.y += particle.vy * dt; particle.vy += 450 * dt; particle.life -= dt; }
      g.particles = g.particles.filter(particle => particle.life > 0);

      if (g.defeated >= waveLimit && g.enemies.length === 0 && g.wave < 3) { g.wave++; g.spawn = 1.1; g.score += 500; }
      if (p.lives <= 0) g.state = "lost";
      if (g.defeated >= GOAL && g.enemies.length === 0) { g.state = "won"; g.score += 2000; }
    };

    const damagePlayer = (g: Game) => {
      g.player.lives--; g.player.invincible = 1.6; g.player.vy = -210; g.shake = .28;
      burst(g, g.player.x, g.player.y - 30, "#f04435", 12);
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

  const progress = Math.min(100, hud.defeated / GOAL * 100);
  return (
    <main className="game-shell">
      <section className="game-card" aria-label="Cobra Strike game">
        <canvas ref={canvasRef} width={W} height={H} />
        <header className="hud">
          <div className="identity"><small>OPERATIVE</small><strong>COBRA</strong><b>{Array.from({ length: Math.max(0, hud.lives) }, (_, i) => <i key={i} />)}</b></div>
          <div className="mission"><small>WAVE {hud.wave} · OUTPOST ZERO</small><strong>{hud.defeated}<em>/ {GOAL}</em></strong><span><i style={{ width: `${progress}%` }} /></span></div>
          <div className="score"><small>COMBAT SCORE</small><strong>{String(hud.score).padStart(6, "0")}</strong></div>
        </header>

        {hud.state === "briefing" && <div className="briefing">
          <div className="mission-tag">MISSION 01 · RED SECTOR</div>
          <p>PRIMARY OBJECTIVE</p><h1>BREAK THE SIEGE</h1>
          <div className="goal-card"><b>24</b><span>HOSTILES<br />TO ELIMINATE</span></div>
          <p className="orders">Push through three assault waves. Heavy units take three hits.</p>
          <button onClick={start}>DEPLOY <span>›</span></button>
        </div>}

        {(hud.state === "won" || hud.state === "lost") && <div className={`result ${hud.state}`}>
          <p>{hud.state === "won" ? "MISSION COMPLETE" : "OPERATIVE DOWN"}</p>
          <h1>{hud.state === "won" ? "SIEGE BROKEN" : "THE OUTPOST HOLDS"}</h1>
          <strong>{String(hud.score).padStart(6, "0")} <small>PTS</small></strong>
          <button onClick={reset}>{hud.state === "won" ? "RUN AGAIN" : "REDEPLOY"}</button>
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
  ctx.save();
  if (g.shake > 0) ctx.translate((Math.random() - .5) * 8, (Math.random() - .5) * 6);
  const sky = ctx.createLinearGradient(0, 0, 0, FLOOR);
  sky.addColorStop(0, "#07171b"); sky.addColorStop(.58, "#17443f"); sky.addColorStop(1, "#d16b2d"); ctx.fillStyle = sky; ctx.fillRect(-10, -10, W + 20, H + 20);
  ctx.fillStyle = "#ffb047"; ctx.beginPath(); ctx.arc(690, 92, 45, 0, Math.PI * 2); ctx.fill();
  ctx.fillStyle = "#12332f"; ctx.beginPath(); ctx.moveTo(0, 235); for (let x = 0; x <= W; x += 70) ctx.lineTo(x, 175 + ((x / 70) % 3) * 22); ctx.lineTo(W, FLOOR); ctx.lineTo(0, FLOOR); ctx.fill();
  ctx.fillStyle = "#091f20"; for (let x = 15; x < W; x += 105) { const h = 62 + (x % 4) * 11; ctx.fillRect(x, FLOOR - h, 45, h); ctx.fillRect(x + 11, FLOOR - h - 18, 5, 18); }
  ctx.fillStyle = "rgba(255,189,88,.34)"; ctx.fillRect(0, 223, W, 2);
  ctx.fillStyle = "#29352c"; ctx.fillRect(0, FLOOR, W, H - FLOOR); ctx.fillStyle = "#728060"; ctx.fillRect(0, FLOOR, W, 5);
  ctx.fillStyle = "#1b241e"; for (let x = 0; x < W; x += 44) ctx.fillRect(x, 340 + (x % 3) * 8, 27, 6);
  ctx.fillStyle = "rgba(255,178,68,.08)"; for (let i = 0; i < 14; i++) ctx.fillRect((i * 83 + now * .008) % W, 130 + (i % 5) * 23, 2, 2);

  for (const shot of g.shots) { ctx.fillStyle = shot.hostile ? "#ff4a35" : "#fff17a"; ctx.shadowColor = ctx.fillStyle; ctx.shadowBlur = 8; ctx.fillRect(shot.x - 8, shot.y - 2, 16, 4); ctx.shadowBlur = 0; }
  for (const enemy of g.enemies) soldier(ctx, enemy.x, enemy.y, enemy.kind === "heavy" ? "#7c2524" : "#ad4335", false, enemy.kind === "heavy", enemy.hit > 0);
  const flicker = g.player.invincible > 0 && Math.floor(now / 80) % 2 === 0;
  if (!flicker) soldier(ctx, g.player.x, g.player.y, "#d5ab55", true, false, false, g.player.facing);
  for (const p of g.particles) { ctx.globalAlpha = Math.min(1, p.life * 3); ctx.fillStyle = p.color; ctx.fillRect(p.x - 2, p.y - 2, 5, 5); } ctx.globalAlpha = 1;
  ctx.restore();
}

function soldier(ctx: CanvasRenderingContext2D, x: number, y: number, color: string, hero: boolean, heavy = false, isHit = false, facing: 1 | -1 = -1) {
  ctx.save(); ctx.translate(x, y); ctx.scale(facing, 1);
  ctx.fillStyle = "rgba(0,0,0,.4)"; ctx.beginPath(); ctx.ellipse(0, 4, heavy ? 26 : 21, 5, 0, 0, Math.PI * 2); ctx.fill();
  ctx.fillStyle = isHit ? "#fff" : color; ctx.fillRect(-11, -43, heavy ? 25 : 21, 29); ctx.fillRect(-10, -14, 8, 18); ctx.fillRect(5, -14, 8, 18);
  ctx.fillStyle = hero ? "#e4b47e" : "#b47e64"; ctx.fillRect(-8, -58, 17, 16);
  ctx.fillStyle = hero ? "#c9342d" : "#3d2825"; ctx.fillRect(-11, -61, 22, 7); if (hero) ctx.fillRect(7, -55, 9, 4);
  ctx.fillStyle = "#111a18"; ctx.fillRect(7, -39, heavy ? 31 : 27, 6); ctx.fillRect(30, -36, heavy ? 13 : 9, 3);
  if (heavy) { ctx.fillStyle = "#d59039"; ctx.fillRect(-14, -47, 29, 6); }
  ctx.restore();
}

export default App;
