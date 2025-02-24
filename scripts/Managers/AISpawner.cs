using Godot;
using System;
using System.Collections.Generic;

public partial class AISpawner : Node
{
    [Export]
    public PackedScene AITankScene { get; set; }
    
    [Export]
    public int MaxAITanks = 5;  // Maximum number of AI tanks allowed at once
    
    [Export]
    public float SpawnInterval = 5.0f;  // Time between spawn attempts
    
    [Export]
    public float MinSpawnDistance = 500.0f;  // Minimum distance from player to spawn
    
    [Export]
    public float MaxSpawnDistance = 1000.0f;  // Maximum distance from player to spawn
    
    private Timer _spawnTimer;
    private Node2D _player;
    private Random _random = new Random();
    private List<Node2D> _activeTanks = new List<Node2D>();
    
    public override void _Ready()
    {
        // Find player reference
        _player = GetNode<Node2D>("/root/Main/Tank");
        
        // Setup spawn timer
        _spawnTimer = new Timer();
        AddChild(_spawnTimer);
        _spawnTimer.WaitTime = SpawnInterval;
        _spawnTimer.Timeout += OnSpawnTimerTimeout;
        _spawnTimer.Start();
    }
    
    private void OnSpawnTimerTimeout()
    {
        // Clean up list of active tanks
        _activeTanks.RemoveAll(tank => tank == null || !IsInstanceValid(tank));
        
        // Only spawn if below max limit and player exists
        if (_activeTanks.Count >= MaxAITanks || _player == null || AITankScene == null)
            return;
            
        // Get spawn position
        Vector2 spawnPos = GetValidSpawnPosition();
        if (spawnPos != Vector2.Zero)
        {
            SpawnTank(spawnPos);
        }
    }
    
    private Vector2 GetValidSpawnPosition()
    {
        if (_player == null) return Vector2.Zero;
        
        // Try to find a valid spawn position
        for (int i = 0; i < 10; i++)  // Max 10 attempts
        {
            // Generate random angle and distance
            float angle = (float)_random.NextDouble() * Mathf.Pi * 2;
            float distance = MinSpawnDistance + (float)_random.NextDouble() * (MaxSpawnDistance - MinSpawnDistance);
            
            // Calculate potential spawn position
            Vector2 spawnPos = _player.GlobalPosition + new Vector2(
                distance * Mathf.Cos(angle),
                distance * Mathf.Sin(angle)
            );
            
            // Check if position is within arena bounds
            var gameManager = GetNode<GameManager>("/root/Main/GameManager");
            if (gameManager != null)
            {
                Vector2 halfArena = gameManager.ArenaSize / 2;
                if (Mathf.Abs(spawnPos.X) > halfArena.X || Mathf.Abs(spawnPos.Y) > halfArena.Y)
                    continue;  // Try again if outside arena
            }
            
            // Valid position found
            return spawnPos;
        }
        
        return Vector2.Zero;  // No valid position found
    }
    
    private void SpawnTank(Vector2 position)
    {
        // Instance new AI tank
        var aiTank = AITankScene.Instantiate<Node2D>();
        GetParent().AddChild(aiTank);
        aiTank.GlobalPosition = position;
        
        // Add to AITanks group
        aiTank.AddToGroup("AITanks");
        
        // Add to active tanks list
        _activeTanks.Add(aiTank);
        
        // Connect to tank destroyed signal to manage active tanks list
        var tankStats = aiTank.GetNode<TankStats>("TankStats");
        if (tankStats != null)
        {
            tankStats.TankDestroyed += () => OnTankDestroyed(aiTank);
        }
    }
    
    public void Reset()
    {
        // Clear active tanks list
        _activeTanks.Clear();
        
        // Reset and restart spawn timer
        if (_spawnTimer != null)
        {
            _spawnTimer.Stop();
            _spawnTimer.Start();
        }
    }

    private void OnTankDestroyed(Node2D tank)
    {
        _activeTanks.Remove(tank);
    }
} 