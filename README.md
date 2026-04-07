# Futaba's Adventure

使用Unity开发的3D平台跳跃动作游戏，核心聚焦于高精度角色操控系统与模块化状态机架构，实现了类《超级马力欧》的丰富动作体系。
<img width="1544" height="867" alt="image" src="https://github.com/user-attachments/assets/b1b8260d-a372-4896-b9d4-01ea26e19cd4" />
---


## 🎮 玩法介绍 && 实鸡展示

玩家操控角色在3D关卡中奔跑、跳跃、攀爬，利用多样动作技能穿越障碍、击败敌人。核心循环包含：探索关卡收集金币与星星、利用地形与技能组合抵达终点、在存档点保存进度。特色机制包括多段跳/土狼跳/蹬墙跳等精细化跳跃系统、轨道滑行与攀爬的边缘交互、冲刺/旋转攻击/踩踏等战斗动作。

1.异步加载

![futaba异步加载](https://github.com/user-attachments/assets/e49c613e-831a-48da-9076-7c12c823c3d8)

2.UI 界面（选择存档、选择关卡）

---

## 🔧 技术点

### 架构设计

| 技术点 | 实现方案 | 达成效果 |
|--------|----------|----------|
| **泛型状态机框架** | 基于`EntityState<T>`与`EntityStateManager<T>`实现双层泛型架构，通过反射字符串数组动态实例化状态对象，状态切换通过`Change<TState>()`泛型方法实现类型安全 | Player/Enemy共享同一套状态机框架，新增状态只需继承基类无需修改管理器代码，状态切换零字符串硬编码 |
| **数据驱动配置** | 采用ScriptableObject（`PlayerStats`/`EnemyStats`）存储角色属性，支持多段跳次数、冲刺冷却、土狼跳窗口等50+参数配置 | 策划可独立调整手感参数无需改代码，支持同一角色多套属性模板（如水中/地面不同属性） |
| **事件驱动通信** | `PlayerEvents`/`EnemyEvents`/`EntityEvents`分层事件系统，结合UnityEvent实现零耦合订阅（如OnJump/OnHurt/OnDie） | UI层与Gameplay层完全解耦，动画/音效/粒子通过事件自动响应逻辑变化 |
| **MVP架构的UI系统** | `HUD`作为View实现`IHudView`接口，`HudPresenter`处理业务逻辑，通过`LevelScore`/`Game`等Model层获取数据 | UI逻辑与表现分离，便于单元测试与多平台UI复用 |

### 核心系统实现

| 技术点 | 实现方案 | 达成效果 |
|--------|----------|----------|
| **精细化跳跃系统** | 实现多段跳计数器、coyoteJumpThreshold（离地后0.15s仍可跳）、min/max跳跃高度（根据按键时长决定跳跃高度）三重机制 | 跳跃手感响应精准，支持高阶技巧如边缘起跳、轻按小跳 |
| **3C角色控制器** | 基于Unity CharacterController自定义封装`Entity<T>`基类，分离横向/纵向速度（lateralVelocity/verticalVelocity），支持自定义碰撞检测（SphereCast/CapsuleCast） | 实现蹬墙跳、边缘悬挂、斜坡滑行等复杂交互，物理响应可精确调控 |
| **轨道滑行系统** | 利用Unity Splines包检测轨道碰撞，进入后沿Spline切线方向移动，结合坡度计算加减速 | 实现流畅的滑轨体验，下坡自动加速、上坡减速，可衔接冲刺动作 |
| **边缘交互检测** | 通过Raycast组合检测（向前+向下+侧向）判定可悬挂边缘，结合`DetectingLedge()`算法排除球体/胶囊体误检测 | 实现准确的边缘抓取与攀爬，避免错误吸附到不合法碰撞体 |

### 存档系统

| 技术点 | 实现方案 | 达成效果 |
|--------|----------|----------|
| **多格式存档支持** | `GameSaver`单例支持Binary/JSON/PlayerPrefs三种模式切换，采用BinaryFormatter或JSON序列化 | 开发阶段使用JSON便于调试，发布切换Binary防篡改，支持多槽位存档管理 |
| **数据持久化架构** | `GameData`/`LevelData`纯数据类配合`Game.ToData()`/`GameLevel.ToData()`方法实现全量状态导出 | 关卡进度、收集状态、重试次数完整保存，支持跨场景状态恢复 |

---

## 🏗️ 核心架构设计

```mermaid
classDiagram
    %% 核心控制器层
    class Game {
        <<Singleton>>
        -List~GameLevel~ levels
        +HandleRetry()
        +SaveGame()
        +DontDestroyOnLoad()
    }
    Game --> GameSaver : 依赖
    Game --> GameLoader : 依赖
    Game --> GameController : 依赖

    %% 实体系统
    class Entity~T~ {
        <<Generic>>
        +CharacterController controller
        +Vector3 lateralVelocity
        +Vector3 verticalVelocity
        +GroundCheck()
        +UseCustomCollision()
        +SplineSupport()
    }
    
    class Player {
        +PlayerStateManager stateManager
        +PlayerInputManager inputManager
        +PlayerStatsManager statsManager
        +Health health
        +ManageJumpCount()
        +PickAndThrow()
        +WallSlideAndHang()
    }
    
    class Enemy {
        +EnemyStateManager stateManager
        +WaypointManager waypointManager
        +OverlapSphereVision()
        +ContactAttack()
    }
    
    Entity~Player~ <|-- Player
    Entity~Enemy~ <|-- Enemy

    %% 状态机系统
    class EntityStateManager~T~ {
        <<Generic>>
        -Dictionary~Type, EntityState~ states
        +EntityState current
        +EntityState last
        +Change~TState~()
    }
    
    Player *-- EntityStateManager~Player~ : 包含
    Enemy *-- EntityStateManager~Enemy~ : 包含
    
    class EntityState~T~ {
        <<Generic>>
        +float timeSinceEntered
        +OnEnter()
        +OnStep()
        +OnExit()
        +OnContact()
        +CreateFromString()
    }
    
    EntityStateManager~T~ o-- EntityState~T~ : 管理

    %% 玩家状态实现
    class PlayerState {
        <<Abstract>>
    }
    EntityState~Player~ <|-- PlayerState
    
    PlayerState <|-- Walk
    PlayerState <|-- Fall
    PlayerState <|-- Dash
    PlayerState <|-- Spin
    PlayerState <|-- Swim
    PlayerState <|-- WallDrag
    PlayerState <|-- LedgeHang
    note for Walk "20+ 状态类，均继承 PlayerState"

    %% 辅助系统
    class PlayerStats {
        <<ScriptableObject>>
        +50+ Parameters
    }
    class Health {
        +Damage()
        +Heal()
    }
    class GameTags {
        <<Constants>>
    }
    Player ..> PlayerStats : 配置
    Player ..> Health : 依赖

    %% UI MVP系统
    class IHudView {
        <<Interface>>
    }
    class HUD {
        <<MonoBehaviour>>
    }
    class HudPresenter {
        <<Presenter>>
    }
    
    IHudView <|.. HUD : 实现
    HudPresenter --> IHudView : 驱动View
    HudPresenter --> Game : 监听 Model (LevelScore/Game)
