using Godot;
using System;

public partial class ArenaBorder : Node2D
{
    private Line2D _border;
    private Polygon2D _background;
    private GameManager _gameManager;

    public override void _Ready()
    {
        _border = GetNode<Line2D>("Border");
        _background = GetNode<Polygon2D>("Background");
        _gameManager = GetParent<GameManager>();

        if (_gameManager != null)
        {
            UpdateBorder();
        }
    }

    private void UpdateBorder()
    {
        Vector2 halfSize = _gameManager.ArenaSize / 2;
        
        // Set up border points (clockwise from top-left)
        Vector2[] points = new Vector2[]
        {
            new Vector2(-halfSize.X, -halfSize.Y),  // Top-left
            new Vector2(halfSize.X, -halfSize.Y),   // Top-right
            new Vector2(halfSize.X, halfSize.Y),    // Bottom-right
            new Vector2(-halfSize.X, halfSize.Y),   // Bottom-left
            new Vector2(-halfSize.X, -halfSize.Y)   // Back to top-left to close the shape
        };

        // Update Line2D points
        _border.Points = points;

        // Update background polygon (same points but without the last one since Polygon2D auto-closes)
        _background.Polygon = points[..^1];
    }
} 