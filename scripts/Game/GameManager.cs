using Godot;
using System;

public partial class GameManager : Node
{
    [Export]
    public PackedScene ShapeScene { get; set; }
    [Export]
    public int MaxShapes = 50;
    [Export]
    public Vector2 ArenaSize = new Vector2(3000, 3000);

    // Shape spawn weights (higher number = more common)
    private int[] ShapeWeights = new int[] {
        70,  // Square (most common)
        20,  // Triangle
        8,   // Pentagon
        2    // Alpha Pentagon (rare)
    };
    private int TotalWeight;

    private RandomNumberGenerator _rng = new RandomNumberGenerator();
    private int _currentShapes = 0;

    public override void _Ready()
    {
        _rng.Randomize();
        // Calculate total weight for probability distribution
        TotalWeight = 0;
        foreach (int weight in ShapeWeights)
        {
            TotalWeight += weight;
        }
        InitializeGame();
    }

    private void InitializeGame()
    {
        // Spawn initial shapes
        while (_currentShapes < MaxShapes)
        {
            SpawnShape();
        }
    }

    private Shape.ShapeType GetRandomShapeType()
    {
        int roll = _rng.RandiRange(0, TotalWeight - 1);
        int accumulator = 0;
        
        for (int i = 0; i < ShapeWeights.Length; i++)
        {
            accumulator += ShapeWeights[i];
            if (roll < accumulator)
            {
                return (Shape.ShapeType)i;
            }
        }
        
        return Shape.ShapeType.Square; // Fallback
    }

    private void SpawnShape()
    {
        if (ShapeScene == null) return;

        var shape = ShapeScene.Instantiate<Shape>();
        
        // Set random shape type based on weights
        shape.Type = GetRandomShapeType();

        // Random position within arena bounds
        float x = _rng.RandfRange(-ArenaSize.X/2, ArenaSize.X/2);
        float y = _rng.RandfRange(-ArenaSize.Y/2, ArenaSize.Y/2);
        shape.Position = new Vector2(x, y);

        // Add to Shapes group
        shape.AddToGroup("Shapes");

        // Use CallDeferred to add the child when it's safe
        CallDeferred(Node.MethodName.AddChild, shape);
        _currentShapes++;
    }

    public void OnShapeDestroyed()
    {
        _currentShapes--;
        // Use CallDeferred to spawn new shape
        CallDeferred(MethodName.SpawnShape);
    }

    public void ResetGame()
    {
        // Clear all AI tanks
        var aiTanks = GetTree().GetNodesInGroup("AITanks");
        foreach (Node tank in aiTanks)
        {
            tank.QueueFree();
        }

        // Clear all shapes
        var shapes = GetTree().GetNodesInGroup("Shapes");
        foreach (Node shape in shapes)
        {
            shape.QueueFree();
        }

        // Reset AI spawner
        var aiSpawner = GetNode<AISpawner>("AISpawner");
        if (aiSpawner != null)
        {
            aiSpawner.Reset();
        }
    }

    [Signal]
    public delegate void GameOverEventHandler();
} 