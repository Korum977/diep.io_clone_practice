using Godot;
using System;

public partial class Projectile : Area2D
{
    public float Speed { get; set; } = 500.0f;
    public float Damage { get; set; } = 10.0f;
    public float Penetration { get; set; } = 1.0f;
    public Vector2 Direction { get; set; } = Vector2.Zero;
    public new Node2D Owner { get; set; } = null;
    
    private float _remainingPenetration;
    private float _lifetime = 2.0f;
    private Timer _lifetimeTimer;

    public override void _Ready()
    {
        _remainingPenetration = Penetration;
        
        _lifetimeTimer = new Timer();
        AddChild(_lifetimeTimer);
        _lifetimeTimer.WaitTime = _lifetime;
        _lifetimeTimer.OneShot = true;
        _lifetimeTimer.Timeout += OnLifetimeExpired;
        _lifetimeTimer.Start();
        
        // Set up collision handling
        AreaEntered += OnAreaEntered;
        BodyEntered += OnBodyEntered;

        // Set collision mask based on owner
        if (Owner != null)
        {
            if (Owner.Name == "Tank") // Player's projectile
            {
                CollisionLayer = 4; // Projectile layer
                CollisionMask = 8 | 2; // Collide with Shapes and AI tanks
            }
            else // AI tank's projectile
            {
                CollisionLayer = 4; // Projectile layer
                CollisionMask = 8 | 2; // Collide with Shapes and player tank
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Position += Direction * Speed * (float)delta;
    }

    private void OnAreaEntered(Area2D area)
    {
        if (area == null || area == this) return;

        // Handle collision with shapes
        if (area is Shape shape)
        {
            HandleCollision(shape);
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body == null || body == Owner) return;

        // Handle collision with tanks
        if (body is TankController tank)
        {
            HandleCollision(tank);
        }
    }

    private void HandleCollision(IDamageable target)
    {
        if (target == null) return;

        // Don't damage owner
        if (target is Node2D node && node == Owner)
        {
            return;
        }

        // Apply damage
        target.TakeDamage(Damage, Owner);

        // Apply knockback if target is a tank
        if (target is TankController tank)
        {
            tank.ApplyKnockback(Direction * 300.0f);
        }
        else if (target is Shape shape)
        {
            shape.ApplyKnockback(Direction);
        }

        // Reduce penetration and destroy if depleted
        _remainingPenetration -= 1;
        if (_remainingPenetration <= 0)
        {
            QueueFree();
        }
    }

    private void OnLifetimeExpired()
    {
        QueueFree();
    }
}

public interface IDamageable
{
    void TakeDamage(float damage, Node2D attacker = null);
} 