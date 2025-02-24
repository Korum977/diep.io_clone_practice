using Godot;
using System;

public partial class Shape : Area2D, IDamageable
{
    public enum ShapeType
    {
        Square,     // Yellow, basic
        Triangle,   // Red, medium
        Pentagon,   // Blue, advanced
        AlphaPentagon // Dark Blue, boss-like
    }

    [Export]
    public ShapeType Type { get; set; } = ShapeType.Square;

    [Export]
    public float Health = 20.0f;
    [Export]
    public float ExperienceValue = 10.0f;

    [Export]
    public float ContactDamage = 10.0f; // Damage dealt when colliding with tank

    [Export]
    public float KnockbackResistance = 1.0f;  // Higher values = less knockback
    
    private Color[] ShapeColors = new Color[] {
        new Color(1, 1, 0),     // Yellow for Square
        new Color(1, 0, 0),     // Red for Triangle
        new Color(0, 0, 1),     // Blue for Pentagon
        new Color(0, 0, 0.7f)   // Dark Blue for Alpha Pentagon
    };

    private float _maxHealth;
    private ProgressBar _healthBar;
    private AnimationPlayer _fadeAnimation;
    private float _lastDamageTime;
    private const float FADE_DELAY = 5.0f; // Changed to 5 seconds as requested

    private Vector2 _velocity = Vector2.Zero;
    private const float FRICTION = 1.0f;  // Reduced friction for better movement
    private const float KNOCKBACK_FORCE = 800.0f;  // Increased knockback force
    private const float TANK_COLLISION_KNOCKBACK = 400.0f;
    private const float MIN_COLLISION_VELOCITY = 5.0f; // Reduced minimum velocity threshold
    private const float COLLISION_BOUNCE = 0.8f; // Added bounce factor for collisions

    private Node2D _lastAttacker;  // Track the last tank that damaged this shape

    [Signal]
    public delegate void ShapeDestroyedEventHandler();

    public override void _Ready()
    {
        // Set properties based on shape type
        SetShapeProperties();

        // Random rotation for variety
        var rng = new RandomNumberGenerator();
        rng.Randomize();
        Rotation = rng.RandfRange(0, Mathf.Pi * 2);

        // Connect to tree exited signal to notify GameManager
        TreeExiting += OnTreeExiting;

        // Connect collision signals
        AreaEntered += OnAreaEntered;
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;

        _healthBar = GetNode<ProgressBar>("HealthBar");
        _fadeAnimation = GetNode<AnimationPlayer>("HealthBar/FadeAnimation");
        
        if (_healthBar != null)
        {
            _healthBar.MaxValue = 100;
            _healthBar.Value = 100;
            // Health bar starts invisible
            _healthBar.Modulate = new Color(1, 1, 1, 0);
        }
    }

    private void SetShapeProperties()
    {
        var visual = GetNode<Polygon2D>("ShapeVisual");
        if (visual != null)
        {
            visual.Color = ShapeColors[(int)Type];
            
            // Adjust properties based on type
            switch (Type)
            {
                case ShapeType.Square:
                    Health = 20;
                    ExperienceValue = 10;
                    ContactDamage = 10;
                    KnockbackResistance = 1.0f;
                    break;
                case ShapeType.Triangle:
                    Health = 30;
                    ExperienceValue = 25;
                    ContactDamage = 15;
                    KnockbackResistance = 1.2f;
                    break;
                case ShapeType.Pentagon:
                    Health = 100;
                    ExperienceValue = 130;
                    ContactDamage = 20;
                    KnockbackResistance = 1.5f;
                    break;
                case ShapeType.AlphaPentagon:
                    Health = 3000;
                    ExperienceValue = 3000;
                    ContactDamage = 40;
                    KnockbackResistance = 2.0f;
                    Scale *= 2; // Make it bigger
                    break;
            }
            _maxHealth = Health;
        }
    }

    public override void _Process(double delta)
    {
        if (_healthBar != null)
        {
            // Update health bar value
            _healthBar.Value = (Health / _maxHealth) * 100;

            // Handle fade out after damage
            float timeSinceLastDamage = Time.GetTicksMsec() / 1000.0f - _lastDamageTime;
            if (timeSinceLastDamage > FADE_DELAY)
            {
                if (_fadeAnimation != null && !_fadeAnimation.IsPlaying() && _healthBar.Modulate.A > 0)
                {
                    _fadeAnimation.Play("fade_out");
                }
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // Apply movement with boundary check
        if (_velocity.LengthSquared() > 0)
        {
            // Apply movement
            Position += _velocity * (float)delta;
            
            // Apply friction with reduced effect
            _velocity = _velocity.MoveToward(Vector2.Zero, FRICTION * (float)delta * 50);

            // Keep shape within arena bounds (assuming GameManager's ArenaSize)
            var gameManager = GetNode<GameManager>("/root/Main/GameManager");
            if (gameManager != null)
            {
                Vector2 halfArena = gameManager.ArenaSize / 2;
                Position = new Vector2(
                    Mathf.Clamp(Position.X, -halfArena.X, halfArena.X),
                    Mathf.Clamp(Position.Y, -halfArena.Y, halfArena.Y)
                );
            }

            // Check for collisions with other shapes
            var overlappingAreas = GetOverlappingAreas();
            foreach (var area in overlappingAreas)
            {
                if (area is Shape otherShape && area != this)
                {
                    // Calculate relative velocity
                    Vector2 relativeVelocity = _velocity - otherShape._velocity;
                    
                    // Only process collision if relative velocity is significant
                    if (relativeVelocity.Length() > MIN_COLLISION_VELOCITY)
                    {
                        // Calculate collision direction and force
                        Vector2 collisionDirection = (otherShape.Position - Position).Normalized();
                        float collisionForce = relativeVelocity.Length() * 2.0f;
                        
                        // Calculate mass ratio based on shape types
                        float thisShapeMass = GetShapeMass(Type);
                        float otherShapeMass = GetShapeMass(otherShape.Type);
                        float totalMass = thisShapeMass + otherShapeMass;
                        float thisImpactRatio = otherShapeMass / totalMass;
                        float otherImpactRatio = thisShapeMass / totalMass;
                        
                        // Calculate velocities after collision using conservation of momentum
                        Vector2 thisNewVelocity = _velocity - collisionDirection * collisionForce * thisImpactRatio * COLLISION_BOUNCE;
                        Vector2 otherNewVelocity = otherShape._velocity + collisionDirection * collisionForce * otherImpactRatio * COLLISION_BOUNCE;
                        
                        // Apply new velocities
                        _velocity = thisNewVelocity;
                        otherShape._velocity = otherNewVelocity;
                        
                        // Apply damage based on impact force
                        float impactForce = collisionForce / 100.0f;
                        otherShape.TakeDamage(ContactDamage * impactForce, this);
                        TakeDamage(otherShape.ContactDamage * impactForce, otherShape);
                    }
                }
            }
        }
    }

    private float GetShapeMass(ShapeType type)
    {
        switch (type)
        {
            case ShapeType.Square:
                return 1.0f;
            case ShapeType.Triangle:
                return 2.0f;  // Increased mass difference
            case ShapeType.Pentagon:
                return 4.0f;  // Increased mass difference
            case ShapeType.AlphaPentagon:
                return 8.0f;  // Increased mass difference
            default:
                return 1.0f;
        }
    }

    private void OnAreaEntered(Area2D area)
    {
        if (area is Shape otherShape && area != this)
        {
            // Calculate relative velocity
            Vector2 relativeVelocity = _velocity - otherShape._velocity;
            
            // Only process collision if relative velocity is significant
            if (relativeVelocity.Length() > MIN_COLLISION_VELOCITY)
            {
                // Calculate collision direction and force
                Vector2 collisionDirection = (otherShape.Position - Position).Normalized();
                float collisionForce = relativeVelocity.Length() * 2.0f;
                
                // Calculate mass ratio
                float thisShapeMass = GetShapeMass(Type);
                float otherShapeMass = GetShapeMass(otherShape.Type);
                float totalMass = thisShapeMass + otherShapeMass;
                float thisImpactRatio = otherShapeMass / totalMass;
                float otherImpactRatio = thisShapeMass / totalMass;
                
                // Calculate and apply velocities after collision
                Vector2 thisNewVelocity = _velocity - collisionDirection * collisionForce * thisImpactRatio * COLLISION_BOUNCE;
                Vector2 otherNewVelocity = otherShape._velocity + collisionDirection * collisionForce * otherImpactRatio * COLLISION_BOUNCE;
                
                _velocity = thisNewVelocity;
                otherShape._velocity = otherNewVelocity;
                
                // Apply damage based on impact force
                float impactForce = collisionForce / 100.0f;
                otherShape.TakeDamage(ContactDamage * impactForce, this);
                TakeDamage(otherShape.ContactDamage * impactForce, otherShape);
            }
        }
    }

    public void TakeDamage(float damage, Node2D attacker = null)
    {
        Health -= damage;
        
        // Update last attacker if provided and it's not another Shape
        if (attacker != null && !(attacker is Shape))
        {
            var tankStats = attacker.GetNodeOrNull<TankStats>("TankStats");
            if (tankStats != null)
            {
                _lastAttacker = attacker;  // Only store if it's a tank (has TankStats)
            }
        }
        
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

        if (Health <= 0)
        {
            // Award experience to the tank that destroyed the shape
            if (_lastAttacker != null)
            {
                TankStats tankStats = null;
                
                // If attacker is a projectile, get the owner's TankStats
                if (_lastAttacker is Projectile projectile && projectile.Owner != null)
                {
                    tankStats = projectile.Owner.GetNodeOrNull<TankStats>("TankStats");
                }
                else
                {
                    // Try to find TankStats by traversing up the node tree
                    Node currentNode = _lastAttacker;
                    
                    // First try to get TankStats directly from the attacker
                    tankStats = currentNode.GetNodeOrNull<TankStats>("TankStats");
                    
                    // If not found, traverse up the tree until we find it or reach root
                    while (tankStats == null && currentNode != null && currentNode.GetParent() != null)
                    {
                        currentNode = currentNode.GetParent();
                        tankStats = currentNode.GetNodeOrNull<TankStats>("TankStats");
                    }
                }
                
                if (tankStats != null)
                {
                    tankStats.AddExperience(ExperienceValue, this);
                }
            }
            
            EmitSignal(SignalName.ShapeDestroyed);
            QueueFree();
        }
    }

    public void ApplyKnockback(Vector2 direction, float force = 1.0f)
    {
        Vector2 knockbackVelocity = direction * (KNOCKBACK_FORCE * force / KnockbackResistance);
        _velocity += knockbackVelocity;
        
        // Cap maximum velocity to prevent excessive speeds
        float maxSpeed = 1500.0f;  // Increased max speed
        if (_velocity.Length() > maxSpeed)
        {
            _velocity = _velocity.Normalized() * maxSpeed;
        }
    }

    private void OnTreeExiting()
    {
        // Notify GameManager when shape is destroyed
        var gameManager = GetNode<GameManager>("/root/Main/GameManager");
        if (gameManager != null)
        {
            gameManager.OnShapeDestroyed();
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is IDamageable damageable)
        {
            damageable.TakeDamage(ContactDamage, this);

            // Apply knockback to both shape and tank
            if (body is CharacterBody2D tank)
            {
                // Calculate knockback direction from tank to shape
                Vector2 knockbackDir = (GlobalPosition - tank.GlobalPosition).Normalized();
                
                // Apply knockback to shape (away from tank)
                ApplyKnockback(knockbackDir, 1.0f);
                
                // Apply velocity to tank (opposite direction)
                tank.Velocity = -knockbackDir * TANK_COLLISION_KNOCKBACK;
            }
        }
    }

    private void OnBodyExited(Node2D body)
    {
        // Could add effects or behavior when tank moves away
    }

    public Node2D GetLastAttacker()
    {
        return _lastAttacker;
    }
} 