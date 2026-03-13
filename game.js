'use strict';

// ─── Canvas Setup ─────────────────────────────────────────────────────────────
const canvas = document.getElementById('gameCanvas');
const ctx    = canvas.getContext('2d');
const W = canvas.width;
const H = canvas.height;

// ─── Game State ───────────────────────────────────────────────────────────────
const STATE = { MENU: 'menu', PLAYING: 'playing', DEAD: 'dead', WIN: 'win', PAUSED: 'paused' };
let state = STATE.MENU;

let score        = 0;
let wave         = 1;
let lives        = 3;
let stardateTimer = 2267.1;

// ─── Stars Background ─────────────────────────────────────────────────────────
const STARS = Array.from({ length: 200 }, () => ({
  x: Math.random() * W,
  y: Math.random() * H,
  r: Math.random() * 1.5 + 0.3,
  speed: Math.random() * 0.4 + 0.1,
  alpha: Math.random() * 0.7 + 0.3,
}));

// ─── Player (Enterprise) ──────────────────────────────────────────────────────
const player = {
  x: W / 2,
  y: H - 90,
  w: 60,
  h: 36,
  speed: 4,
  shields: 100,
  maxShields: 100,
  energy: 100,
  maxEnergy: 100,
  shieldsOn: true,
  invincible: 0,
  warpCooldown: 0,
  fireRate: 0,
  torpedoReady: true,
  dx: 0,
  dy: 0,
};

// ─── Bullets, Enemies, Effects ────────────────────────────────────────────────
let phasers   = [];   // player shots
let torpedoes = [];   // player torpedoes
let enemyShots = [];  // enemy shots
let enemies   = [];
let particles = [];
let explosions = [];
let warpLines = [];

// ─── Input ────────────────────────────────────────────────────────────────────
const keys = {};
document.addEventListener('keydown', e => {
  keys[e.code] = true;
  if (e.code === 'Space') e.preventDefault();
  if (e.code === 'KeyT') fireT();
  if (e.code === 'KeyS') toggleShields();
  if (e.code === 'KeyW') warpBurst();
});
document.addEventListener('keyup', e => { keys[e.code] = false; });

// ─── Overlay ──────────────────────────────────────────────────────────────────
const overlay    = document.getElementById('overlay');
const overlayTitle = document.getElementById('overlay-title');
const overlayMsg  = document.getElementById('overlay-message');
const overlayBtn  = document.getElementById('overlay-btn');
overlayBtn.addEventListener('click', startGame);

function showOverlay(title, msg, btn = 'ENGAGE') {
  overlayTitle.textContent = title;
  overlayMsg.innerHTML = msg;
  overlayBtn.textContent = btn;
  overlay.classList.remove('hidden');
}
function hideOverlay() { overlay.classList.add('hidden'); }

// ─── Wave Config ──────────────────────────────────────────────────────────────
const WAVE_CONFIG = [
  // wave 1: scouts
  { count: 6,  type: 'klingon',  hp: 2,  speed: 0.8, shotRate: 0.004, points: 100 },
  // wave 2: warbirds
  { count: 8,  type: 'romulan', hp: 3,  speed: 1.0, shotRate: 0.006, points: 150 },
  // wave 3: battle cruisers
  { count: 10, type: 'borg',    hp: 5,  speed: 1.2, shotRate: 0.008, points: 250 },
  // wave 4: mixed assault
  { count: 12, type: 'klingon', hp: 4,  speed: 1.4, shotRate: 0.010, points: 200 },
  // wave 5: boss fleet
  { count: 5,  type: 'borg',    hp: 12, speed: 0.9, shotRate: 0.015, points: 500, boss: true },
];

// ─── Spawn Wave ───────────────────────────────────────────────────────────────
function spawnWave(w) {
  const cfg = WAVE_CONFIG[Math.min(w - 1, WAVE_CONFIG.length - 1)];
  enemies = [];
  const cols = Math.min(cfg.count, 6);
  const rows = Math.ceil(cfg.count / cols);
  let idx = 0;
  for (let r = 0; r < rows && idx < cfg.count; r++) {
    for (let c = 0; c < cols && idx < cfg.count; c++) {
      const ex = 80 + c * (W - 160) / (cols - 1 || 1);
      const ey = 50 + r * 70;
      enemies.push({
        x: ex, y: ey,
        w: cfg.boss ? 64 : 48,
        h: cfg.boss ? 48 : 32,
        hp: cfg.hp,
        maxHp: cfg.hp,
        speed: cfg.speed + (w > 5 ? (w - 5) * 0.1 : 0),
        shotRate: cfg.shotRate,
        points: cfg.points,
        type: cfg.type,
        boss: cfg.boss || false,
        dir: 1,
        moveTimer: 0,
        hitFlash: 0,
      });
      idx++;
    }
  }
}

// ─── Start / Reset ────────────────────────────────────────────────────────────
function startGame() {
  hideOverlay();
  score = 0;
  wave  = 1;
  lives = 3;
  stardateTimer = 2267.1;

  player.x = W / 2;
  player.y = H - 90;
  player.shields = 100;
  player.energy  = 100;
  player.shieldsOn = true;
  player.invincible = 0;
  player.warpCooldown = 0;

  phasers = []; torpedoes = []; enemyShots = []; particles = []; explosions = []; warpLines = [];
  spawnWave(wave);
  state = STATE.PLAYING;
}

// ─── Player Actions ───────────────────────────────────────────────────────────
function fireT() {
  if (state !== STATE.PLAYING) return;
  if (!player.torpedoReady) return;
  torpedoes.push({ x: player.x, y: player.y - player.h / 2, vy: -9, w: 6, h: 16, life: 1 });
  player.torpedoReady = false;
  setTimeout(() => { player.torpedoReady = true; }, 1200);
}

function toggleShields() {
  if (state !== STATE.PLAYING) return;
  player.shieldsOn = !player.shieldsOn;
}

function warpBurst() {
  if (state !== STATE.PLAYING) return;
  if (player.warpCooldown > 0 || player.energy < 30) return;
  player.energy -= 30;
  player.warpCooldown = 300;
  // spawn warp effect lines
  for (let i = 0; i < 20; i++) {
    warpLines.push({ x: Math.random() * W, y: Math.random() * H, life: 30 + Math.random() * 20 });
  }
  // push all enemies down briefly
  enemies.forEach(e => { e.y += 80; });
}

// ─── Update ───────────────────────────────────────────────────────────────────
function update() {
  if (state !== STATE.PLAYING) return;

  stardateTimer += 0.001;
  document.getElementById('stardate').textContent =
    'STARDATE: ' + stardateTimer.toFixed(1);

  // Stars scroll
  STARS.forEach(s => { s.y += s.speed; if (s.y > H) { s.y = 0; s.x = Math.random() * W; } });

  // Warp lines decay
  warpLines = warpLines.filter(l => { l.life--; return l.life > 0; });

  // Player movement
  let dx = 0, dy = 0;
  if (keys['ArrowLeft']  || keys['KeyA']) dx = -player.speed;
  if (keys['ArrowRight'] || keys['KeyD']) dx =  player.speed;
  if (keys['ArrowUp']    || keys['KeyW'] && player.warpCooldown > 0) dy = -player.speed;
  if (keys['ArrowUp']    || keys['KeyK']) dy = -player.speed;
  if (keys['ArrowDown']  || keys['KeyJ']) dy =  player.speed;

  player.x = Math.max(player.w / 2, Math.min(W - player.w / 2, player.x + dx));
  player.y = Math.max(player.h / 2 + 40, Math.min(H - player.h / 2, player.y + dy));

  // Auto-fire phasers
  if (keys['Space']) {
    player.fireRate--;
    if (player.fireRate <= 0 && player.energy > 0) {
      player.fireRate = 8;
      player.energy = Math.max(0, player.energy - 2);
      phasers.push({ x: player.x - 18, y: player.y - player.h / 2, vy: -10 });
      phasers.push({ x: player.x + 18, y: player.y - player.h / 2, vy: -10 });
    }
  }

  // Energy regen
  if (!keys['Space'] && player.energy < player.maxEnergy) {
    player.energy = Math.min(player.maxEnergy, player.energy + 0.15);
  }

  // Shield regen (slow)
  if (player.shieldsOn && player.shields < player.maxShields && player.energy > 20) {
    player.shields = Math.min(player.maxShields, player.shields + 0.05);
    player.energy = Math.max(0, player.energy - 0.02);
  }

  if (player.warpCooldown > 0) player.warpCooldown--;
  if (player.invincible > 0) player.invincible--;

  // Move phasers
  phasers = phasers.filter(b => b.y > -10);
  phasers.forEach(b => { b.y += b.vy; });

  // Move torpedoes
  torpedoes = torpedoes.filter(t => t.y > -20);
  torpedoes.forEach(t => { t.y += t.vy; });

  // Enemy movement + shots
  enemies.forEach(e => {
    e.moveTimer++;
    e.x += e.speed * e.dir;
    if (e.x > W - e.w || e.x < e.w) {
      e.dir *= -1;
      e.y += 20;
    }
    if (e.hitFlash > 0) e.hitFlash--;

    // Enemy fires
    if (Math.random() < e.shotRate + wave * 0.001) {
      enemyShots.push({ x: e.x, y: e.y + e.h / 2, vy: 3 + wave * 0.3 });
    }
  });

  // Move enemy shots
  enemyShots = enemyShots.filter(s => s.y < H + 10);
  enemyShots.forEach(s => { s.y += s.vy; });

  // Phaser hits enemies
  phasers.forEach((b, bi) => {
    enemies.forEach((e, ei) => {
      if (rectsOverlap(b.x - 2, b.y - 6, 4, 12, e.x - e.w/2, e.y - e.h/2, e.w, e.h)) {
        spawnParticles(b.x, b.y, '#00ccff', 4);
        phasers.splice(bi, 1);
        e.hp--;
        e.hitFlash = 8;
        if (e.hp <= 0) killEnemy(ei);
      }
    });
  });

  // Torpedo hits enemies (big AOE)
  torpedoes.forEach((t, ti) => {
    enemies.forEach((e, ei) => {
      if (rectsOverlap(t.x - t.w/2, t.y - t.h/2, t.w, t.h, e.x - e.w/2, e.y - e.h/2, e.w, e.h)) {
        spawnParticles(t.x, t.y, '#ff8800', 20);
        spawnExplosion(t.x, t.y);
        torpedoes.splice(ti, 1);
        e.hp -= 4;
        e.hitFlash = 12;
        if (e.hp <= 0) killEnemy(ei);
      }
    });
  });

  // Enemy shots hit player
  if (player.invincible <= 0) {
    enemyShots.forEach((s, si) => {
      if (rectsOverlap(s.x - 2, s.y - 4, 4, 8,
                       player.x - player.w/2, player.y - player.h/2, player.w, player.h)) {
        enemyShots.splice(si, 1);
        spawnParticles(s.x, s.y, '#ff3300', 8);
        if (player.shieldsOn && player.shields > 0) {
          player.shields = Math.max(0, player.shields - 12);
        } else {
          player.shields = Math.max(0, player.shields - 3);
          player.energy  = Math.max(0, player.energy  - 8);
          if (player.shields <= 0) { hitPlayer(); }
        }
      }
    });
  }

  // Update particles & explosions
  particles = particles.filter(p => p.life > 0);
  particles.forEach(p => {
    p.x += p.vx; p.y += p.vy; p.life--;
    p.vx *= 0.96; p.vy *= 0.96;
  });
  explosions = explosions.filter(e => e.r < e.maxR);
  explosions.forEach(e => { e.r += 3; e.alpha = 1 - e.r / e.maxR; });

  // Wave clear?
  if (enemies.length === 0) {
    wave++;
    score += wave * 500;
    if (wave > WAVE_CONFIG.length + 2) {
      state = STATE.WIN;
      showOverlay('MISSION COMPLETE',
        `The Federation is safe, Captain!<br/>Final Score: <b>${score}</b><br/>Waves Survived: ${wave - 1}`,
        'PLAY AGAIN');
    } else {
      setTimeout(() => spawnWave(wave), 1200);
    }
  }

  // HUD
  updateHUD();
}

function killEnemy(idx) {
  const e = enemies[idx];
  score += e.points;
  spawnExplosion(e.x, e.y);
  spawnParticles(e.x, e.y, enemyColor(e.type), 16);
  enemies.splice(idx, 1);
}

function hitPlayer() {
  lives--;
  player.invincible = 120;
  player.shields = 60;
  spawnParticles(player.x, player.y, '#ff0044', 30);
  spawnExplosion(player.x, player.y);
  if (lives <= 0) {
    state = STATE.DEAD;
    showOverlay('SHIP DESTROYED',
      `The Enterprise has been lost...<br/>Score: <b>${score}</b><br/>Wave Reached: ${wave}`,
      'RETURN TO SPACEDOCK');
  }
}

function rectsOverlap(ax, ay, aw, ah, bx, by, bw, bh) {
  return ax < bx + bw && ax + aw > bx && ay < by + bh && ay + ah > by;
}

function spawnParticles(x, y, color, n) {
  for (let i = 0; i < n; i++) {
    const angle = Math.random() * Math.PI * 2;
    const speed = Math.random() * 3 + 0.5;
    particles.push({
      x, y,
      vx: Math.cos(angle) * speed,
      vy: Math.sin(angle) * speed,
      color,
      r: Math.random() * 3 + 1,
      life: 20 + Math.random() * 20,
      maxLife: 40,
    });
  }
}

function spawnExplosion(x, y) {
  explosions.push({ x, y, r: 2, maxR: 50, alpha: 1 });
}

function updateHUD() {
  const sBar = document.getElementById('shields-bar');
  const eBar = document.getElementById('energy-bar');
  const sPct = player.shields / player.maxShields;
  const ePct = player.energy  / player.maxEnergy;
  sBar.style.width = (sPct * 100) + '%';
  eBar.style.width = (ePct * 100) + '%';
  sBar.style.background = sPct > 0.5 ? 'linear-gradient(90deg,#0044ff,#00aaff)'
                        : sPct > 0.25 ? 'linear-gradient(90deg,#aa4400,#ff8800)'
                        : 'linear-gradient(90deg,#880000,#ff2200)';
  document.getElementById('shields-val').textContent = Math.round(player.shields) + '%';
  document.getElementById('energy-val').textContent  = Math.round(player.energy)  + '%';
  document.getElementById('score-val').textContent   = score;
  document.getElementById('wave-val').textContent    = wave;
  document.getElementById('warp-val').textContent    = player.warpCooldown > 0 ? 'COOLDOWN' : 'NOMINAL';
  document.getElementById('torps-val').textContent   = player.torpedoReady ? '● READY' : '○ LOADING';
  const hearts = '❤️ '.repeat(lives).trim() || '💀';
  document.getElementById('lives-val').textContent = hearts;
}

// ─── Enemy Colors ─────────────────────────────────────────────────────────────
function enemyColor(type) {
  return type === 'klingon' ? '#cc4400'
       : type === 'romulan' ? '#006622'
       : '#44ff44'; // borg
}

// ─── Draw ─────────────────────────────────────────────────────────────────────
function draw() {
  ctx.clearRect(0, 0, W, H);

  // Space background
  ctx.fillStyle = '#000008';
  ctx.fillRect(0, 0, W, H);

  // Stars
  STARS.forEach(s => {
    ctx.save();
    ctx.globalAlpha = s.alpha;
    ctx.fillStyle = '#ffffff';
    ctx.beginPath();
    ctx.arc(s.x, s.y, s.r, 0, Math.PI * 2);
    ctx.fill();
    ctx.restore();
  });

  // Warp lines
  warpLines.forEach(l => {
    ctx.save();
    ctx.globalAlpha = l.life / 50;
    ctx.strokeStyle = '#00ffff';
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(l.x, 0);
    ctx.lineTo(l.x, H);
    ctx.stroke();
    ctx.restore();
  });

  if (state === STATE.PLAYING || state === STATE.DEAD || state === STATE.WIN) {
    drawEnemies();
    drawEnemyShots();
    drawPhasers();
    drawTorpedoes();
    drawPlayer();
    drawParticles();
    drawExplosions();
  }

  if (state === STATE.MENU) drawTitle();
}

function drawTitle() {
  ctx.save();
  ctx.font = 'bold 36px Courier New';
  ctx.fillStyle = '#00ffcc';
  ctx.textAlign = 'center';
  ctx.shadowColor = '#00ffcc';
  ctx.shadowBlur = 20;
  ctx.fillText('USS ENTERPRISE', W / 2, H / 2 - 20);
  ctx.font = '16px Courier New';
  ctx.fillStyle = '#009977';
  ctx.shadowBlur = 0;
  ctx.fillText('STARSHIP COMMAND', W / 2, H / 2 + 20);
  ctx.restore();
}

function drawPlayer() {
  const { x, y, w, h, invincible, shieldsOn, shields } = player;

  if (invincible > 0 && Math.floor(invincible / 6) % 2 === 0) return;

  ctx.save();

  // Nacelles (warp engines) — left
  ctx.fillStyle = '#2255aa';
  ctx.fillRect(x - w/2 - 10, y - 6, 12, 24);
  // Engine glow
  ctx.fillStyle = player.warpCooldown > 0 ? '#00ffff' : '#0044ff';
  ctx.shadowColor = player.warpCooldown > 0 ? '#00ffff' : '#0033cc';
  ctx.shadowBlur = 12;
  ctx.fillRect(x - w/2 - 8, y + 14, 8, 6);

  // Right nacelle
  ctx.shadowBlur = 0;
  ctx.fillStyle = '#2255aa';
  ctx.fillRect(x + w/2 - 2, y - 6, 12, 24);
  ctx.fillStyle = player.warpCooldown > 0 ? '#00ffff' : '#0044ff';
  ctx.shadowColor = player.warpCooldown > 0 ? '#00ffff' : '#0033cc';
  ctx.shadowBlur = 12;
  ctx.fillRect(x + w/2, y + 14, 8, 6);

  // Secondary hull (connector)
  ctx.shadowBlur = 0;
  ctx.fillStyle = '#334477';
  ctx.beginPath();
  ctx.ellipse(x, y + 8, 16, 10, 0, 0, Math.PI * 2);
  ctx.fill();

  // Primary saucer hull
  ctx.fillStyle = '#445588';
  ctx.shadowColor = '#00aaff';
  ctx.shadowBlur = 8;
  ctx.beginPath();
  ctx.ellipse(x, y - 4, w/2, h/2, 0, 0, Math.PI * 2);
  ctx.fill();

  // Hull detail
  ctx.strokeStyle = '#6688bb';
  ctx.lineWidth = 1;
  ctx.shadowBlur = 0;
  ctx.beginPath();
  ctx.ellipse(x, y - 4, w/2 - 8, h/2 - 4, 0, 0, Math.PI * 2);
  ctx.stroke();

  // Bridge dome
  ctx.fillStyle = '#aabbdd';
  ctx.shadowColor = '#ffffff';
  ctx.shadowBlur = 6;
  ctx.beginPath();
  ctx.arc(x, y - 8, 5, 0, Math.PI * 2);
  ctx.fill();

  // Shield bubble
  if (shieldsOn && shields > 0) {
    ctx.shadowBlur = 0;
    ctx.globalAlpha = 0.15 + (shields / 100) * 0.15;
    ctx.strokeStyle = '#4499ff';
    ctx.lineWidth = 2;
    ctx.shadowColor = '#4499ff';
    ctx.shadowBlur = 10;
    ctx.beginPath();
    ctx.ellipse(x, y, w/2 + 18, h/2 + 18, 0, 0, Math.PI * 2);
    ctx.stroke();
    ctx.globalAlpha = 1;
  }

  ctx.restore();
}

function drawEnemies() {
  enemies.forEach(e => {
    ctx.save();
    const flash = e.hitFlash > 0;
    const col = flash ? '#ffffff' : enemyColor(e.type);

    if (e.boss) {
      // Boss — borg cube
      ctx.fillStyle = flash ? '#ffffff' : '#004400';
      ctx.shadowColor = '#00ff00';
      ctx.shadowBlur = flash ? 20 : 8;
      ctx.fillRect(e.x - e.w/2, e.y - e.h/2, e.w, e.h);
      // Grid lines (borg aesthetic)
      ctx.strokeStyle = '#00ff66';
      ctx.lineWidth = 1;
      for (let i = 1; i < 4; i++) {
        ctx.beginPath();
        ctx.moveTo(e.x - e.w/2 + i * e.w/4, e.y - e.h/2);
        ctx.lineTo(e.x - e.w/2 + i * e.w/4, e.y + e.h/2);
        ctx.stroke();
        ctx.beginPath();
        ctx.moveTo(e.x - e.w/2, e.y - e.h/2 + i * e.h/4);
        ctx.lineTo(e.x + e.w/2, e.y - e.h/2 + i * e.h/4);
        ctx.stroke();
      }
    } else if (e.type === 'klingon') {
      // Bird-of-prey shape
      ctx.fillStyle = col;
      ctx.shadowColor = col;
      ctx.shadowBlur = 6;
      // Wings
      ctx.beginPath();
      ctx.moveTo(e.x, e.y - e.h/2);
      ctx.lineTo(e.x + e.w/2, e.y + e.h/2);
      ctx.lineTo(e.x, e.y);
      ctx.lineTo(e.x - e.w/2, e.y + e.h/2);
      ctx.closePath();
      ctx.fill();
      // Body
      ctx.fillStyle = '#882200';
      ctx.beginPath();
      ctx.ellipse(e.x, e.y, 8, 14, 0, 0, Math.PI * 2);
      ctx.fill();
    } else {
      // Romulan warbird
      ctx.fillStyle = col;
      ctx.shadowColor = col;
      ctx.shadowBlur = 6;
      ctx.beginPath();
      ctx.moveTo(e.x, e.y - e.h/2);
      ctx.lineTo(e.x + e.w/2, e.y);
      ctx.lineTo(e.x + e.w/3, e.y + e.h/2);
      ctx.lineTo(e.x, e.y + e.h/4);
      ctx.lineTo(e.x - e.w/3, e.y + e.h/2);
      ctx.lineTo(e.x - e.w/2, e.y);
      ctx.closePath();
      ctx.fill();
      ctx.fillStyle = '#003311';
      ctx.beginPath();
      ctx.arc(e.x, e.y, 6, 0, Math.PI * 2);
      ctx.fill();
    }

    // HP bar above enemy
    const barW = e.w;
    const hpFrac = e.hp / e.maxHp;
    ctx.shadowBlur = 0;
    ctx.fillStyle = '#220000';
    ctx.fillRect(e.x - barW/2, e.y - e.h/2 - 8, barW, 4);
    ctx.fillStyle = hpFrac > 0.5 ? '#00cc44' : hpFrac > 0.25 ? '#ffaa00' : '#ff2200';
    ctx.fillRect(e.x - barW/2, e.y - e.h/2 - 8, barW * hpFrac, 4);

    ctx.restore();
  });
}

function drawPhasers() {
  phasers.forEach(b => {
    ctx.save();
    ctx.fillStyle = '#00eeff';
    ctx.shadowColor = '#00eeff';
    ctx.shadowBlur = 10;
    ctx.fillRect(b.x - 2, b.y - 8, 4, 14);
    ctx.restore();
  });
}

function drawTorpedoes() {
  torpedoes.forEach(t => {
    ctx.save();
    ctx.fillStyle = '#ff8800';
    ctx.shadowColor = '#ff8800';
    ctx.shadowBlur = 18;
    ctx.beginPath();
    ctx.ellipse(t.x, t.y, t.w/2, t.h/2, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.fillStyle = '#ffdd00';
    ctx.shadowBlur = 6;
    ctx.beginPath();
    ctx.ellipse(t.x, t.y, t.w/3, t.h/3, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.restore();
  });
}

function drawEnemyShots() {
  enemyShots.forEach(s => {
    ctx.save();
    ctx.fillStyle = '#ff3300';
    ctx.shadowColor = '#ff4400';
    ctx.shadowBlur = 8;
    ctx.fillRect(s.x - 2, s.y - 5, 4, 10);
    ctx.restore();
  });
}

function drawParticles() {
  particles.forEach(p => {
    ctx.save();
    ctx.globalAlpha = p.life / p.maxLife;
    ctx.fillStyle = p.color;
    ctx.shadowColor = p.color;
    ctx.shadowBlur = 4;
    ctx.beginPath();
    ctx.arc(p.x, p.y, p.r, 0, Math.PI * 2);
    ctx.fill();
    ctx.restore();
  });
}

function drawExplosions() {
  explosions.forEach(e => {
    ctx.save();
    ctx.globalAlpha = e.alpha * 0.7;
    const grad = ctx.createRadialGradient(e.x, e.y, 0, e.x, e.y, e.r);
    grad.addColorStop(0, '#ffffff');
    grad.addColorStop(0.3, '#ffaa00');
    grad.addColorStop(1, 'transparent');
    ctx.fillStyle = grad;
    ctx.beginPath();
    ctx.arc(e.x, e.y, e.r, 0, Math.PI * 2);
    ctx.fill();
    ctx.restore();
  });
}

// ─── Game Loop ────────────────────────────────────────────────────────────────
function loop() {
  update();
  draw();
  requestAnimationFrame(loop);
}

// ─── Init ─────────────────────────────────────────────────────────────────────
showOverlay(
  '⚡ STARSHIP COMMAND ⚡',
  `You are Captain Kirk of the USS Enterprise.<br/>
   Defend the Federation against Klingons,<br/>
   Romulans, and the Borg!<br/><br/>
   <b>SPACE</b> — Phasers &nbsp; <b>T</b> — Torpedo<br/>
   <b>S</b> — Toggle Shields &nbsp; <b>W</b> — Warp Burst<br/>
   <b>Arrow Keys</b> — Navigate`,
  'ENGAGE ENGINES'
);

requestAnimationFrame(loop);
