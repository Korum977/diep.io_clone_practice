using Godot;
using System;

public partial class Minimap : SubViewport
{
    private const float MINIMAP_SCALE = 0.1f; // Scale factor for the minimap (10% of actual size)
    private Node2D _player;
    private GameManager _gameManager;
    private Node2D _minimapObjects;
    private Sprite2D _playerMarker;
    private Line2D _arenaBorder;
    private Color _aiTankColor = new Color(1, 0.5f, 0.5f, 1); // Red for AI tanks
    private Color _shapeColor = new Color(1, 1, 0, 1); // Yellow for shapes
    private Color _playerColor = new Color(0.2f, 0.4f, 0.8f, 1); // Blue for player
    private Vector2 _viewportCenter;

    public override void _Ready()
    {
        // Get references
        _gameManager = GetNode<GameManager>("/root/Main/GameManager");
        _player = GetNode<Node2D>("/root/Main/Tank");
        
        // Create container for minimap objects
        _minimapObjects = new Node2D();
        AddChild(_minimapObjects);

        // Calculate viewport center
        _viewportCenter = new Vector2(Size.X / 2, Size.Y / 2);
        
        // Create arena border
        _arenaBorder = new Line2D();
        _arenaBorder.Width = 2.0f;
        _arenaBorder.DefaultColor = new Color(0.3f, 0.3f, 0.3f);
        _minimapObjects.AddChild(_arenaBorder);
        UpdateArenaBorder();

        // Create player marker
        _playerMarker = new Sprite2D();
        var playerTexture = CreateMarkerTexture(_playerColor);
        _playerMarker.Texture = playerTexture;
        _minimapObjects.AddChild(_playerMarker);
    }

    public override void _Process(double delta)
    {
        if (_player == null || _gameManager == null) return;

        // Update player marker position
        _playerMarker.Position = _player.Position * MINIMAP_SCALE + _viewportCenter;

        // Clear existing markers
        foreach (Node child in _minimapObjects.GetChildren())
        {
            if (child != _arenaBorder && child != _playerMarker)
            {
                child.QueueFree();
            }
        }

        // Add markers for AI tanks and shapes
        foreach (Node node in GetTree().GetNodesInGroup("AITanks"))
        {
            if (node is Node2D aiTank)
            {
                var marker = new Sprite2D();
                marker.Texture = CreateMarkerTexture(_aiTankColor);
                marker.Position = aiTank.Position * MINIMAP_SCALE + _viewportCenter;
                _minimapObjects.AddChild(marker);
            }
        }

        foreach (Node node in GetTree().GetNodesInGroup("Shapes"))
        {
            if (node is Node2D shape)
            {
                var marker = new Sprite2D();
                marker.Texture = CreateMarkerTexture(_shapeColor);
                marker.Position = shape.Position * MINIMAP_SCALE + _viewportCenter;
                _minimapObjects.AddChild(marker);
            }
        }
    }

    private void UpdateArenaBorder()
    {
        if (_gameManager == null) return;

        Vector2 halfSize = _gameManager.ArenaSize / 2 * MINIMAP_SCALE;
        Vector2[] points = new Vector2[]
        {
            _viewportCenter + new Vector2(-halfSize.X, -halfSize.Y), // Top-left
            _viewportCenter + new Vector2(halfSize.X, -halfSize.Y),  // Top-right
            _viewportCenter + new Vector2(halfSize.X, halfSize.Y),   // Bottom-right
            _viewportCenter + new Vector2(-halfSize.X, halfSize.Y),  // Bottom-left
            _viewportCenter + new Vector2(-halfSize.X, -halfSize.Y)  // Back to top-left
        };
        _arenaBorder.Points = points;
    }

    private ImageTexture CreateMarkerTexture(Color color)
    {
        // Create a small circular marker
        var image = Image.Create(6, 6, false, Image.Format.Rgba8);
        image.Fill(new Color(0, 0, 0, 0)); // Transparent background

        // Draw a filled circle
        for (int x = 0; x < 6; x++)
        {
            for (int y = 0; y < 6; y++)
            {
                float dx = x - 2.5f;
                float dy = y - 2.5f;
                if (dx * dx + dy * dy <= 9) // Circle with radius 3
                {
                    image.SetPixel(x, y, color);
                }
            }
        }

        var texture = ImageTexture.CreateFromImage(image);
        return texture;
    }
} 