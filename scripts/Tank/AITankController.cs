using Godot;
using System;

public partial class AITankController : TankController
{
    [Export]
    public float DetectionRange = 800.0f;  // Range to detect and engage player
    [Export]
    public float PreferredCombatDistance = 400.0f;  // Distance AI tries to maintain from player
    [Export]
    public float WanderRadius = 200.0f;  // Radius for random movement when no player detected
    [Export]
    public float AIUpdateInterval = 0.2f;  // How often the AI updates its decision making
    [Export]
    public TankUpgradeManager.TankClass InitialClass = TankUpgradeManager.TankClass.Basic;
    
    private Node2D _player;  // Reference to player tank
    private TankWeapon _weapon;
    private TankStats _stats;
    private TankUpgradeManager _upgradeManager;
    private Timer _aiUpdateTimer;
    private Timer _upgradeTimer;
    private Vector2 _wanderTarget;
    private Random _random = new Random();
    private AIState _currentState = AIState.Wander;
    private float _strafeDirection = 1;
    private float _nextStrategyChange;
    private float _combatTimer;
    private Vector2 _currentVelocity = Vector2.Zero;
    private Vector2 _knockbackVelocity = Vector2.Zero;
    private const float KNOCKBACK_FRICTION = 5.0f;
    
    private enum AIState
    {
        Wander,     // Random movement when no player detected
        Chase,      // Move towards player when too far
        Retreat,    // Move away from player when too close
        Combat,     // Engage in combat while maintaining preferred distance
        Flank       // Flank the player
    }

    public override void _Ready()
    {
        base._Ready();
        
        // Get required nodes
        _weapon = GetNode<TankWeapon>("TankWeapon");
        _stats = GetNode<TankStats>("TankStats");
        _upgradeManager = GetNode<TankUpgradeManager>("TankUpgradeManager");
        
        // Find player tank
        _player = GetNode<Node2D>("/root/Main/Tank");
        
        // Setup AI update timer
        _aiUpdateTimer = new Timer();
        AddChild(_aiUpdateTimer);
        _aiUpdateTimer.WaitTime = AIUpdateInterval;
        _aiUpdateTimer.Timeout += OnAIUpdate;
        _aiUpdateTimer.Start();

        // Setup upgrade timer
        _upgradeTimer = new Timer();
        AddChild(_upgradeTimer);
        _upgradeTimer.WaitTime = 1.0f;
        _upgradeTimer.Timeout += OnUpgradeTimerTimeout;
        _upgradeTimer.Start();
        
        // Set initial wander target
        SetNewWanderTarget();
        
        // Initialize tank class
        if (_upgradeManager != null)
        {
            _upgradeManager.UpgradeTo(InitialClass);
            AdjustBehaviorForClass(InitialClass);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // Apply knockback and friction
        if (_knockbackVelocity.LengthSquared() > 0)
        {
            _knockbackVelocity = _knockbackVelocity.MoveToward(Vector2.Zero, KNOCKBACK_FRICTION * (float)delta * 100);
        }

        // Combine AI movement and knockback
        Velocity = _currentVelocity + _knockbackVelocity * KnockbackResistance;
        MoveAndSlide();
    }

    public override void ApplyKnockback(Vector2 force)
    {
        _knockbackVelocity += force;
    }

    private void OnUpgradeTimerTimeout()
    {
        if (_stats == null || _upgradeManager == null) return;

        // Automatically spend stat points
        while (_stats.AvailableStatPoints > 0)
        {
            string[] stats = { "health", "damage", "speed", "penetration", "reload", "movement" };
            string randomStat = stats[_random.Next(stats.Length)];
            _stats.UpgradeStat(randomStat);
        }

        // Check for class upgrades at appropriate levels
        if (_stats.Level >= 30)
        {
            TryUpgradeClass(true);  // Try tier 3
        }
        else if (_stats.Level >= 15)
        {
            TryUpgradeClass(false);  // Try tier 2
        }
    }

    private void TryUpgradeClass(bool tier3)
    {
        if (_upgradeManager == null) return;

        var availableUpgrades = _upgradeManager.GetAvailableUpgrades();
        if (availableUpgrades.Length > 0)
        {
            var newClass = availableUpgrades[_random.Next(availableUpgrades.Length)];
            _upgradeManager.UpgradeTo(newClass);
            AdjustBehaviorForClass(newClass);
        }
    }

    private void AdjustBehaviorForClass(TankUpgradeManager.TankClass tankClass)
    {
        switch (tankClass)
        {
            case TankUpgradeManager.TankClass.Sniper:
            case TankUpgradeManager.TankClass.Assassin:
                PreferredCombatDistance = 600.0f;  // Stay further back
                DetectionRange = 1000.0f;  // See farther
                break;

            case TankUpgradeManager.TankClass.MachineGun:
            case TankUpgradeManager.TankClass.Gunner:
                PreferredCombatDistance = 300.0f;  // Get closer
                DetectionRange = 500.0f;  // Focus on closer targets
                break;

            case TankUpgradeManager.TankClass.FlankGuard:
            case TankUpgradeManager.TankClass.TriAngle:
                PreferredCombatDistance = 200.0f;  // Get very close
                break;

            case TankUpgradeManager.TankClass.Smasher:
                PreferredCombatDistance = 0.0f;  // Try to ram
                DetectionRange = 400.0f;  // Focus on close combat
                break;

            default:
                PreferredCombatDistance = 400.0f;
                DetectionRange = 800.0f;
                break;
        }
    }

    private void OnAIUpdate()
    {
        if (_player == null) return;

        Vector2 toPlayer = _player.GlobalPosition - GlobalPosition;
        float distanceToPlayer = toPlayer.Length();

        // Update combat timer and strategy
        _combatTimer += AIUpdateInterval;
        if (_combatTimer >= _nextStrategyChange)
        {
            _combatTimer = 0;
            _nextStrategyChange = (float)_random.NextDouble() * 3.0f + 2.0f;  // Change strategy every 2-5 seconds
            _strafeDirection = _random.NextDouble() > 0.5f ? 1.0f : -1.0f;
            
            // Randomly choose to flank if in combat
            if (_currentState == AIState.Combat && _random.NextDouble() > 0.7f)
            {
                _currentState = AIState.Flank;
            }
        }

        // Update AI state based on player distance and current class
        UpdateAIState(distanceToPlayer, toPlayer.Normalized());

        // Execute behavior based on current state
        switch (_currentState)
        {
            case AIState.Wander:
                HandleWandering();
                break;
            case AIState.Chase:
                HandleChasing(toPlayer.Normalized());
                break;
            case AIState.Retreat:
                HandleRetreating(toPlayer.Normalized());
                break;
            case AIState.Combat:
                HandleCombat(toPlayer.Normalized());
                break;
            case AIState.Flank:
                HandleFlanking(toPlayer.Normalized(), distanceToPlayer);
                break;
        }

        // Always try to aim at player if in range
        if (distanceToPlayer <= DetectionRange)
        {
            AimAtPlayer(distanceToPlayer);
        }
    }

    private void UpdateAIState(float distanceToPlayer, Vector2 toPlayerDir)
    {
        // Special handling for Smasher class
        if (_upgradeManager?.CurrentClass == TankUpgradeManager.TankClass.Smasher)
        {
            _currentState = distanceToPlayer > DetectionRange ? AIState.Wander : AIState.Chase;
            return;
        }

        // Normal state updates
        if (distanceToPlayer > DetectionRange)
        {
            _currentState = AIState.Wander;
        }
        else if (distanceToPlayer < PreferredCombatDistance * 0.7f)
        {
            _currentState = AIState.Retreat;
        }
        else if (distanceToPlayer > PreferredCombatDistance * 1.3f)
        {
            _currentState = AIState.Chase;
        }
        else if (_currentState != AIState.Flank)
        {
            _currentState = AIState.Combat;
        }
    }

    private void HandleWandering()
    {
        // Check if we reached the wander target
        if (GlobalPosition.DistanceTo(_wanderTarget) < 50)
        {
            SetNewWanderTarget();
        }

        // Move towards wander target
        Vector2 direction = (_wanderTarget - GlobalPosition).Normalized();
        _currentVelocity = direction * Speed;
    }

    private void HandleChasing(Vector2 toPlayerDirection)
    {
        _currentVelocity = toPlayerDirection * Speed;
    }

    private void HandleRetreating(Vector2 toPlayerDirection)
    {
        _currentVelocity = -toPlayerDirection * Speed;
    }

    private void HandleCombat(Vector2 toPlayerDirection)
    {
        // Strafe perpendicular to player direction
        Vector2 strafeDirection = new Vector2(-toPlayerDirection.Y, toPlayerDirection.X) * _strafeDirection;
        _currentVelocity = strafeDirection * Speed * 0.7f;
    }

    private void HandleFlanking(Vector2 toPlayerDirection, float distance)
    {
        // Calculate a point to move to that's perpendicular to the player's position
        Vector2 perpendicular = new Vector2(-toPlayerDirection.Y, toPlayerDirection.X) * _strafeDirection;
        Vector2 targetPos = _player.GlobalPosition + perpendicular * PreferredCombatDistance;
        
        // Move towards the flanking position
        Vector2 moveDirection = (targetPos - GlobalPosition).Normalized();
        _currentVelocity = moveDirection * Speed;
        
        // If we're close to the flanking position, switch back to combat
        if (GlobalPosition.DistanceTo(targetPos) < 50)
        {
            _currentState = AIState.Combat;
        }
    }

    private void AimAtPlayer(float distance)
    {
        if (_weapon != null && _player != null)
        {
            // Calculate base inaccuracy based on distance
            float maxInaccuracy = 0.2f;  // Base maximum inaccuracy in radians
            float distanceFactor = Mathf.Clamp(distance / DetectionRange, 0, 1);
            float inaccuracy = (float)_random.NextDouble() * maxInaccuracy * distanceFactor;
            
            // Reduce inaccuracy for sniper classes
            if (_upgradeManager?.CurrentClass == TankUpgradeManager.TankClass.Sniper ||
                _upgradeManager?.CurrentClass == TankUpgradeManager.TankClass.Assassin)
            {
                inaccuracy *= 0.5f;
            }
            
            Vector2 toPlayer = (_player.GlobalPosition - GlobalPosition).Normalized();
            Vector2 inaccurateAim = toPlayer.Rotated(inaccuracy * (_random.NextDouble() > 0.5f ? 1 : -1));
            
            // Set weapon rotation
            _weapon.Rotation = Mathf.Atan2(inaccurateAim.Y, inaccurateAim.X) + Mathf.Pi/2;
            
            // Shoot if aimed well enough
            float accuracyThreshold = _upgradeManager?.CurrentClass == TankUpgradeManager.TankClass.MachineGun ? 0.3f : 0.1f;
            if (Mathf.Abs(inaccuracy) < accuracyThreshold)
            {
                // Use the player's position as the target when shooting
                _weapon.ShootAllCannons(_player.GlobalPosition);
            }
        }
    }

    private void SetNewWanderTarget()
    {
        float angle = (float)_random.NextDouble() * Mathf.Pi * 2;
        float radius = (float)_random.NextDouble() * WanderRadius;
        _wanderTarget = GlobalPosition + new Vector2(
            radius * Mathf.Cos(angle),
            radius * Mathf.Sin(angle)
        );
        
        // Keep target within arena bounds if GameManager exists
        var gameManager = GetNode<GameManager>("/root/Main/GameManager");
        if (gameManager != null)
        {
            Vector2 halfArena = gameManager.ArenaSize / 2;
            _wanderTarget = new Vector2(
                Mathf.Clamp(_wanderTarget.X, -halfArena.X, halfArena.X),
                Mathf.Clamp(_wanderTarget.Y, -halfArena.Y, halfArena.Y)
            );
        }
    }
} 