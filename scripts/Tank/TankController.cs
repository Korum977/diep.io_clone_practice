using Godot;
using System;

public partial class TankController : CharacterBody2D, IDamageable
{
    [Export]
    public float Speed = 300.0f;
    [Export]
    public float KnockbackResistance = 0.5f; // How much knockback affects the tank

    private TankStats _tankStats;
    private Vector2 _knockbackVelocity = Vector2.Zero;
    private const float KNOCKBACK_FRICTION = 5.0f;

    public override void _Ready()
    {
        _tankStats = GetNode<TankStats>("TankStats");
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 inputVelocity = Vector2.Zero;

        // Get input direction using WASD controls
        if (Input.IsActionPressed("move_right"))
            inputVelocity.X += 1;
        if (Input.IsActionPressed("move_left"))
            inputVelocity.X -= 1;
        if (Input.IsActionPressed("move_backward"))
            inputVelocity.Y += 1;
        if (Input.IsActionPressed("move_forward"))
            inputVelocity.Y -= 1;

        // Normalize and apply speed
        inputVelocity = inputVelocity.Normalized() * Speed;

        // Apply knockback and friction
        if (_knockbackVelocity.LengthSquared() > 0)
        {
            _knockbackVelocity = _knockbackVelocity.MoveToward(Vector2.Zero, KNOCKBACK_FRICTION * (float)delta * 100);
        }

        // Combine movement and knockback
        Velocity = inputVelocity + _knockbackVelocity * KnockbackResistance;
        MoveAndSlide();
    }

    public void TakeDamage(float damage, Node2D attacker = null)
    {
        if (_tankStats != null)
        {
            _tankStats.TakeDamage(damage, attacker);
        }
    }

    public virtual void ApplyKnockback(Vector2 force)
    {
        _knockbackVelocity += force;
    }
} 