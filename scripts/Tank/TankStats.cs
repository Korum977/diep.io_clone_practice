using Godot;
using System;

public partial class TankStats : Node
{
    [Export]
    public float MaxHealth { get; set; } = 100.0f;
    [Export]
    public float CurrentHealth { get; set; } = 100.0f;
    [Export]
    public float ReloadSpeed { get; set; } = 1.0f;
    [Export]
    public float BulletDamage { get; set; } = 10.0f;
    [Export]
    public float BulletSpeed { get; set; } = 500.0f;
    [Export]
    public float BulletPenetration { get; set; } = 1.0f;
    [Export]
    public float BulletSpread { get; set; } = 0.0f;
    [Export]
    public float MovementSpeed { get; set; } = 1.0f;
    [Export]
    public float BodyDamage { get; set; } = 10.0f;
    [Export]
    public float HealthRegen { get; set; } = 1.0f;

    public int Level { get; private set; } = 1;
    public float Experience { get; private set; } = 0;
    public float ExperienceToNextLevel { get; private set; } = 100;
    public bool IsDead { get; private set; } = false;
    public int AvailableStatPoints { get; private set; } = 0;

    [Signal]
    public delegate void ExperienceGainedEventHandler(float amount, Node2D source, string sourceType);

    private Timer _regenTimer;
    private const float REGEN_INTERVAL = 1.0f;
    private const float HEALTH_BAR_FADE_DELAY = 5.0f;
    
    private ProgressBar _healthBar;
    private AnimationPlayer _fadeAnimation;
    private float _lastDamageTime;

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
        
        _regenTimer = new Timer();
        AddChild(_regenTimer);
        _regenTimer.WaitTime = REGEN_INTERVAL;
        _regenTimer.Timeout += OnRegenTick;
        _regenTimer.Start();

        // Get health bar components
        _healthBar = GetNode<ProgressBar>("TankHealthBar");
        if (_healthBar != null)
        {
            _healthBar.MaxValue = MaxHealth;
            _healthBar.Value = CurrentHealth;
            _healthBar.Modulate = new Color(1, 1, 1, 0);
            _fadeAnimation = _healthBar.GetNode<AnimationPlayer>("FadeAnimation");
        }
    }

    public override void _Process(double delta)
    {
        if (_healthBar != null)
        {
            // Update health bar position to follow tank
            var parent = GetParent<Node2D>();
            if (parent != null)
            {
                _healthBar.GlobalPosition = parent.GlobalPosition + new Vector2(0, -45);
            }

            // Update health bar value
            _healthBar.MaxValue = MaxHealth;
            _healthBar.Value = CurrentHealth;

            // Handle fade out after damage
            float timeSinceLastDamage = Time.GetTicksMsec() / 1000.0f - _lastDamageTime;
            if (timeSinceLastDamage > HEALTH_BAR_FADE_DELAY)
            {
                if (_fadeAnimation != null && !_fadeAnimation.IsPlaying() && _healthBar.Modulate.A > 0)
                {
                    _fadeAnimation.Play("fade_out");
                }
            }
        }
    }

    private void OnRegenTick()
    {
        if (CurrentHealth < MaxHealth && !IsDead)
        {
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + HealthRegen);
        }
    }

    public void AddExperience(float amount, Node2D source = null)
    {
        if (IsDead) return;

        // Log experience gain with source information
        string sourceType = "Unknown";
        string sourceName = source?.Name ?? "Unknown";

        if (source is Shape shape)
        {
            sourceType = $"Shape ({shape.Type})";
        }
        else if (source is AITankController)
        {
            sourceType = "AI Tank";
            var sourceStats = source.GetNodeOrNull<TankStats>("TankStats");
            if (sourceStats != null)
            {
                sourceName = $"Level {sourceStats.Level} AI Tank";
            }
        }

        // Log the experience gain
        GD.Print($"[XP] {GetParent().Name} gained {amount:F0} XP from {sourceType} ({sourceName})");
        
        // Emit signal for UI updates
        EmitSignal(SignalName.ExperienceGained, amount, source, sourceType);

        // Add the experience
        Experience += amount;
        CheckLevelUp();
    }

    private void CheckLevelUp()
    {
        bool leveledUp = false;
        int startLevel = Level;
        float totalXPGained = 0;

        while (Experience >= ExperienceToNextLevel && Level < 45)
        {
            leveledUp = true;
            Level++;
            float xpToNextLevel = ExperienceToNextLevel;
            Experience -= ExperienceToNextLevel;
            ExperienceToNextLevel *= 1.5f;
            totalXPGained += xpToNextLevel;
            
            // Award stat points
            AvailableStatPoints += 1;
            
            EmitSignal(SignalName.LevelUp, Level, AvailableStatPoints);
            
            // Special level notifications
            if (Level == 15 || Level == 30 || Level == 45)
            {
                EmitSignal(SignalName.UpgradeAvailable, Level);
            }
        }

        if (leveledUp)
        {
            GD.Print($"[LEVEL UP] {GetParent().Name} leveled up from {startLevel} to {Level}! (Total XP gained: {totalXPGained:F0})");
        }
    }

    public void UpgradeStat(string statName)
    {
        if (AvailableStatPoints <= 0) return;

        switch (statName.ToLower())
        {
            case "health":
                MaxHealth *= 1.1f;
                CurrentHealth = MaxHealth;
                break;
            case "regen":
                HealthRegen *= 1.1f;
                break;
            case "damage":
                BulletDamage *= 1.1f;
                break;
            case "penetration":
                BulletPenetration *= 1.1f;
                break;
            case "speed":
                BulletSpeed *= 1.1f;
                break;
            case "reload":
                ReloadSpeed *= 1.1f;
                break;
            case "movement":
                MovementSpeed *= 1.1f;
                break;
            case "bodydamage":
                BodyDamage *= 1.1f;
                break;
            default:
                return;
        }

        AvailableStatPoints--;
        EmitSignal(SignalName.StatUpgraded, statName, AvailableStatPoints);
    }

    public void TakeDamage(float damage, Node2D attacker = null)
    {
        if (IsDead) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
        
        // Show and reset health bar
        if (_healthBar != null)
        {
            _healthBar.Modulate = new Color(1, 1, 1, 1); // Make fully visible
            if (_fadeAnimation != null && _fadeAnimation.IsPlaying())
            {
                _fadeAnimation.Stop();
            }
        }
        _lastDamageTime = Time.GetTicksMsec() / 1000.0f;
        
        if (CurrentHealth <= 0)
        {
            Die(attacker);
        }
    }

    private void Die(Node2D attacker = null)
    {
        IsDead = true;
        CurrentHealth = 0;
        EmitSignal(SignalName.TankDestroyed);
        
        var parent = GetParent();
        if (parent != null)
        {
            if (parent.Name == "Tank")  // "Tank" is the player's tank node name
            {
                GetTree().Paused = true;
            }
            else if (parent is AITankController && attacker != null)
            {
                // For AI tanks, award experience to the player that destroyed it
                TankStats attackerStats = null;
                
                // If attacker is a Shape, try to get the TankStats from its last attacker
                if (attacker is Shape shape)
                {
                    var lastAttacker = shape.GetLastAttacker();
                    if (lastAttacker != null)
                    {
                        attackerStats = lastAttacker.GetNode<TankStats>("TankStats");
                    }
                }
                else
                {
                    // Direct tank-to-tank combat
                    attackerStats = attacker.GetNode<TankStats>("TankStats");
                }

                if (attackerStats != null)
                {
                    // Award experience based on AI tank's level
                    float experienceValue = 100 + (Level * 50); // Base 100 XP + 50 per level
                    attackerStats.AddExperience(experienceValue, GetParent() as Node2D);
                }
            }
        }
    }

    public void Respawn()
    {
        IsDead = false;
        Level = 1;
        Experience = 0;
        ExperienceToNextLevel = 100;
        AvailableStatPoints = 0;
        
        // Reset all stats to base values
        MaxHealth = 100.0f;
        CurrentHealth = MaxHealth;
        ReloadSpeed = 1.0f;
        BulletDamage = 10.0f;
        BulletSpeed = 500.0f;
        BulletPenetration = 1.0f;
        BulletSpread = 0.0f;
        MovementSpeed = 1.0f;
        BodyDamage = 10.0f;
        HealthRegen = 1.0f;

        // Reset tank position to center
        var parent = GetParent<Node2D>();
        if (parent != null)
        {
            parent.GlobalPosition = Vector2.Zero;
            parent.Rotation = 0;
        }

        // Reset tank weapon
        var weapon = GetParent().GetNodeOrNull<TankWeapon>("TankWeapon");
        if (weapon != null)
        {
            weapon.UpdateCannonType(TankWeapon.CannonType.Basic);
        }

        // Clear all AI tanks and shapes
        var gameManager = GetNode<GameManager>("/root/Main/GameManager");
        if (gameManager != null)
        {
            // Signal game manager to reset the game state
            gameManager.CallDeferred("ResetGame");
        }
        
        GetTree().Paused = false;
    }

    [Signal]
    public delegate void LevelUpEventHandler(int level, int availablePoints);
    
    [Signal]
    public delegate void UpgradeAvailableEventHandler(int level);
    
    [Signal]
    public delegate void StatUpgradedEventHandler(string statName, int remainingPoints);
    
    [Signal]
    public delegate void HealthChangedEventHandler(float currentHealth, float maxHealth);
    
    [Signal]
    public delegate void TankDestroyedEventHandler();
} 