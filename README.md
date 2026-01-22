# Dunk Contest 2D Pro

A stylish 2D "dunk contest" game built in .NET 9 with MonoGame. Auto-run toward the hoop, jump into **slow-motion**, perform trick moves mid-air, and watch the **automatic slam dunk** with style!

## 🎮 Features

- **Auto-Run Gameplay**: Press SPACE to start running, SPACE again to jump — no manual movement needed!
- **EPIC SLOW-MO**: Jumping triggers slow-motion so you have time to perform tricks!
- **AUTO-DUNK**: Reach the hoop and the dunk happens automatically!
- **Stunning 2D Graphics**: Animated basketball player with trick poses, detailed hoop with swaying net, particle effects, and an animated crowd
- **Trick System**: 4 different moves (Hand Up, Hand Down, Between Legs, Spin) that can be chained mid-air
- **Dynamic Scoring**: Bonuses for variety, timing, creativity, and penalties for repetition
- **Style Meter**: Visual feedback bar showing combo quality
- **Particle Effects**: Dust when running, sparks for tricks, confetti for dunks!
- **In-Game Instructions**: Press **H** anytime for full help overlay

## 🚀 Quick Start

### Prerequisites
- .NET 9 SDK ([Download](https://dotnet.microsoft.com/download/dotnet/9.0))
- Windows (or Linux/macOS with MonoGame support)

### Installation & Run

```bash
cd c:\repos\DunkPro2D
dotnet run
```

That's it! The game window opens immediately — no external assets needed.

## 🎯 How to Play

### Basic Flow
1. **Press SPACE** to start running toward the hoop
2. **Press SPACE again** to JUMP — this triggers **SLOW-MOTION!**
3. **Do tricks** (J, K, L, I) while in slow-mo — you have plenty of time!
4. **Reach the hoop** and the **AUTO-DUNK** happens automatically!
5. **Watch** your stylish dunk in dramatic slow motion!
6. **Results**: See your score breakdown and rating
7. **Press R** to try again

### Controls

| Key | Action |
|-----|--------|
| **Space** | Start running / Jump (triggers slow-mo!) |
| **J** | ✋ Hand Up trick (+15 pts) |
| **K** | 👇 Hand Down trick (+15 pts) |
| **L** | 🦵 Between Legs trick (+15 pts) |
| **I** | 🌀 SPIN trick (+15 pts + creativity bonus!) |
| **H** | Toggle full instructions overlay |
| **R** | Restart attempt (on results screen) |
| **Esc** | Exit game |

### 💡 Pro Tips

1. **Mix up your tricks** — Using different moves earns a Variety Bonus (+20 per unique trick)
2. **Use the slow-mo wisely** — You have time to chain multiple tricks!
3. **Always include a SPIN** — The I key spin gives +25 creativity bonus
4. **Chain 3+ moves** — Landing 3 or more tricks before dunking gives +30 creativity
5. **Don't spam!** — Repeating the same move consecutively incurs a -10 penalty each time
6. **Auto-dunk is automatic** — Just focus on your tricks, the dunk happens when you reach the hoop!

## 📊 Scoring System

| Component | Points |
|-----------|--------|
| Base Dunk Score | 100 |
| Per Trick | +15 |
| Per Unique Trick Type | +20 |
| Good Timing (200-800ms window) | +25 |
| Mid-jump Tricks | +5 each |
| 3+ Moves Before Dunk | +30 |
| Including a Spin | +25 |
| Good Alternation | +20 |
| **Penalties** | |
| Consecutive Repeats | -10 each |

### Ratings
- 🏆 **LEGENDARY!** — 300+ points
- 🔥 **INSANE!** — 250+ points  
- ⭐ **EXCELLENT!** — 200+ points
- 👍 **GREAT!** — 150+ points
- 👌 **GOOD** — 100+ points

## 🏗️ Architecture

The codebase is designed for clarity and extensibility:

### Core Components

- **Game1**: Main game loop with state machine, auto-run system, slow-mo, and screen shake
- **Player**: Animated character with physics, auto-run, spin animation, and trick poses
- **Hoop**: Realistic hoop with animated swaying net and auto-dunk zone
- **TrickTracker**: Records moves, enforces cooldowns, manages style meter
- **ScoringEngine**: Pure scoring function with detailed breakdown
- **ParticleSystem**: Manages dust, sparks, spin trails, and confetti effects
- **CrowdSystem**: Animated background crowd that reacts to gameplay
- **ColorHelper**: Utility for creating colors without constructor ambiguity

### Game States

```
Ready State (Title Screen)
  ↓ (Press SPACE)
InAttempt State (Playing)
  ↓ (Player dunks → SLOW-MO → or lands without dunking)
Results State (Score Screen)
  ↓ (Press R)
Ready State (repeat)
```

### Slow Motion System

When you successfully dunk:
1. Time scale drops to 15% (configurable `SLOW_MO_SCALE`)
2. Radial light rays emanate from dunk position
3. "SLAM!" text appears with fade effect
4. Vignette effect darkens screen edges
5. Confetti particles burst out
6. After 1.5 seconds, time returns to normal with screen shake

## 🔧 Technical Details

- **Framework**: MonoGame 3.8.4 (DesktopGL)
- **.NET Version**: .NET 9
- **Resolution**: 1280×720 (60 FPS)
- **Physics**: Discrete Euler integration with time-scaled delta
- **Rendering**: Immediate-mode with transform matrix for screen shake

## 📝 License

Personal project. Feel free to modify and extend!

---

**🏀 Go for LEGENDARY! Mix tricks, include spins, and time your dunks perfectly!**
