using Godot;
using System;
using System.Collections.Generic;

public partial class TankWeapon : Node2D
{
    [Export]
    public PackedScene ProjectileScene { get; set; }
    [Export]
    public float RotationSpeed = 10.0f;
    [Export]
    public float RecoilForce = 100.0f;
    
    private TankStats _stats;
    private Timer _reloadTimer;
    private bool _canShoot = true;
    private bool _autoFire = false;
    private bool _autoSpin = false;
    private CharacterBody2D _tankBody;
    private Node2D _cannonContainer;
    private List<Marker2D> _shootPoints = new();
    private float _autoSpinSpeed = 2.0f;

    // Standard cannon polygon vertices
    private static readonly Vector2[] CANNON_POLYGON = new Vector2[]
    {
        new Vector2(-5, 0),    // Bottom left
        new Vector2(-5, -40),  // Top left
        new Vector2(-3, -45),  // Top left curve
        new Vector2(3, -45),   // Top right curve
        new Vector2(5, -40),   // Top right
        new Vector2(5, 0)      // Bottom right
    };

    // Machine gun polygon vertices (wider at base)
    private static readonly Vector2[] MACHINE_GUN_POLYGON = new Vector2[]
    {
        new Vector2(-8, 0),    // Bottom left
        new Vector2(-6, -35),  // Upper left
        new Vector2(-4, -40),  // Top left
        new Vector2(4, -40),   // Top right
        new Vector2(6, -35),   // Upper right
        new Vector2(8, 0)      // Bottom right
    };

    // Sniper cannon polygon vertices (longer and thinner)
    private static readonly Vector2[] SNIPER_POLYGON = new Vector2[]
    {
        new Vector2(-3, 0),    // Bottom left
        new Vector2(-3, -55),  // Top left
        new Vector2(-2, -60),  // Top left curve
        new Vector2(2, -60),   // Top right curve
        new Vector2(3, -55),   // Top right
        new Vector2(3, 0)      // Bottom right
    };

    public override void _Ready()
    {
        _stats = GetNode<TankStats>("../TankStats");
        _tankBody = GetParent<CharacterBody2D>();
        
        // Verify ProjectileScene is correctly assigned and log parent node info
        GD.Print($"[TankWeapon] Parent node: {GetParent()?.Name}, Parent type: {GetParent()?.GetType()}");
        
        if (ProjectileScene == null)
        {
            GD.PrintErr("[TankWeapon] ERROR: ProjectileScene is null on initialization!");
            GD.PrintErr($"[TankWeapon] Current node path: {GetPath()}");
            return;
        }

        GD.Print($"[TankWeapon] ProjectileScene path: {ProjectileScene.ResourcePath}");
        
        try
        {
            // Load the scene resource to verify it's the correct type
            var sceneInstance = ProjectileScene.Instantiate();
            if (sceneInstance == null)
            {
                GD.PrintErr($"[TankWeapon] ERROR: Failed to instantiate scene from {ProjectileScene.ResourcePath}");
                return;
            }

            GD.Print($"[TankWeapon] Instantiated scene type: {sceneInstance.GetType()}");
            
            if (sceneInstance is not Projectile)
            {
                GD.PrintErr($"[TankWeapon] ERROR: ProjectileScene is not a Projectile! Got {sceneInstance.GetType()} from {ProjectileScene.ResourcePath}");
                GD.PrintErr($"[TankWeapon] Scene inheritance chain: {GetSceneInheritanceChain(sceneInstance)}");
                sceneInstance.QueueFree();
                // Reset the incorrect reference
                ProjectileScene = null;
                return;
            }

            GD.Print($"[TankWeapon] Successfully validated Projectile scene: {ProjectileScene.ResourcePath}");
            sceneInstance.QueueFree();
        }
        catch (Exception e)
        {
            GD.PrintErr($"[TankWeapon] ERROR: Exception while validating ProjectileScene: {e.Message}");
            GD.PrintErr($"[TankWeapon] Stack trace: {e.StackTrace}");
            ProjectileScene = null;
            return;
        }
        
        // Create or get CannonContainer
        _cannonContainer = GetNodeOrNull<Node2D>("CannonContainer");
        if (_cannonContainer == null)
        {
            _cannonContainer = new Node2D();
            _cannonContainer.Name = "CannonContainer";
            AddChild(_cannonContainer);
        }
        
        _reloadTimer = new Timer();
        AddChild(_reloadTimer);
        _reloadTimer.OneShot = true;
        _reloadTimer.Timeout += OnReloadComplete;

        // Initialize basic tank cannon
        AddCannon(0, CannonType.Basic);
    }

    private string GetSceneInheritanceChain(Node node)
    {
        var chain = new System.Collections.Generic.List<string>();
        var current = node;
        
        while (current != null)
        {
            chain.Add($"{current.GetType()}");
            current = current.GetParent();
        }
        
        return string.Join(" -> ", chain);
    }

    public enum CannonType
    {
        Basic,
        MachineGun,
        Sniper,
        Twin,
        Flank
    }

    public void AddCannon(float angle, CannonType type = CannonType.Basic, float scale = 1.0f)
    {
        var cannon = new Node2D();
        var polygon = new Polygon2D();
        var shootPoint = new Marker2D();
        
        // Set up cannon polygon based on type
        Vector2[] vertices;
        Color cannonColor;
        float length;
        
        switch (type)
        {
            case CannonType.MachineGun:
                vertices = ScalePolygon(MACHINE_GUN_POLYGON, scale);
                cannonColor = new Color(0.6f, 0.6f, 0.6f); // Darker gray
                length = 40 * scale;
                break;
            case CannonType.Sniper:
                vertices = ScalePolygon(SNIPER_POLYGON, scale);
                cannonColor = new Color(0.3f, 0.3f, 0.3f); // Very dark gray
                length = 60 * scale;
                break;
            default:
                vertices = ScalePolygon(CANNON_POLYGON, scale);
                cannonColor = new Color(0.5f, 0.5f, 0.5f); // Medium gray
                length = 45 * scale;
                break;
        }

        polygon.Polygon = vertices;
        polygon.Color = cannonColor;
        
        cannon.AddChild(polygon);
        
        // Set up shoot point at the tip of the cannon
        shootPoint.Position = new Vector2(0, -length);
        cannon.AddChild(shootPoint);
        
        // Position cannon
        cannon.RotationDegrees = angle;
        _cannonContainer.AddChild(cannon);
        _shootPoints.Add(shootPoint);
    }

    private Vector2[] ScalePolygon(Vector2[] original, float scale)
    {
        var scaled = new Vector2[original.Length];
        for (int i = 0; i < original.Length; i++)
        {
            scaled[i] = original[i] * scale;
        }
        return scaled;
    }

    public void ClearCannons()
    {
        foreach (Node child in _cannonContainer.GetChildren())
        {
            child.QueueFree();
        }
        _shootPoints.Clear();
    }

    public void UpdateCannonType(CannonType type)
    {
        ClearCannons();
        
        switch (type)
        {
            case CannonType.Twin:
                AddCannon(-15, CannonType.Basic, 0.9f);
                AddCannon(15, CannonType.Basic, 0.9f);
                break;
            case CannonType.MachineGun:
                AddCannon(0, CannonType.MachineGun, 1.2f);
                break;
            case CannonType.Sniper:
                AddCannon(0, CannonType.Sniper, 1.0f);
                break;
            case CannonType.Flank:
                AddCannon(0, CannonType.Basic, 1.0f);
                AddCannon(180, CannonType.Basic, 1.0f);
                break;
            default:
                AddCannon(0, CannonType.Basic, 1.0f);
                break;
        }
    }

    public override void _Process(double delta)
    {
        // Only handle mouse input for player tanks
        if (GetParent() is AITankController)
            return;

        // Handle auto-fire toggle
        if (Input.IsActionJustPressed("auto_fire"))
        {
            _autoFire = !_autoFire;
        }

        // Handle auto-spin toggle
        if (Input.IsActionJustPressed("auto_spin"))
        {
            _autoSpin = !_autoSpin;
        }

        if (_autoSpin)
        {
            Rotation += _autoSpinSpeed * (float)delta;
        }
        else
        {
            // Get mouse position and calculate direction
            Vector2 mousePos = GetGlobalMousePosition();
            Vector2 toMouse = (mousePos - GlobalPosition).Normalized();
            
            // Calculate rotation to point towards mouse
            float targetRotation = Mathf.Atan2(toMouse.Y, toMouse.X) + Mathf.Pi/2;
            
            // Use LerpAngle for smooth rotation
            Rotation = Mathf.LerpAngle(Rotation, targetRotation, RotationSpeed * (float)delta);
        }

        // Handle shooting
        if (_canShoot && (_autoFire || Input.IsActionPressed("shoot")))
        {
            TryShoot();
        }
    }

    public bool TryShoot()
    {
        if (!_canShoot || ProjectileScene == null) return false;
        ShootAllCannons();
        return true;
    }

    public void ShootAllCannons(Vector2? targetPosition = null)
    {
        if (ProjectileScene == null)
        {
            GD.PrintErr("ProjectileScene is null in TankWeapon!");
            return;
        }

        Vector2 shootTarget = targetPosition ?? GetGlobalMousePosition();
        bool appliedRecoil = false;

        foreach (var shootPoint in _shootPoints)
        {
            // Create projectile instance with detailed error checking
            Node instance;
            try
            {
                instance = ProjectileScene.Instantiate();
                if (instance == null)
                {
                    GD.PrintErr("Failed to instantiate ProjectileScene - returned null!");
                    continue;
                }
            }
            catch (Exception e)
            {
                GD.PrintErr($"Exception while instantiating ProjectileScene: {e.Message}");
                continue;
            }

            // Verify the instance type
            if (instance is not Projectile projectile)
            {
                GD.PrintErr($"Failed to instantiate Projectile! Got {instance.GetType()} instead. Scene path: {ProjectileScene.ResourcePath}");
                instance.QueueFree();
                continue;
            }

            // Add to scene tree
            try
            {
                GetTree().Root.AddChild(projectile);
            }
            catch (Exception e)
            {
                GD.PrintErr($"Failed to add Projectile to scene tree: {e.Message}");
                projectile.QueueFree();
                continue;
            }
            
            // Set projectile properties from tank stats
            projectile.Speed = _stats.BulletSpeed;
            projectile.Damage = _stats.BulletDamage;
            projectile.Penetration = _stats.BulletPenetration;
            projectile.Owner = _tankBody;  // Set the tank as the projectile's owner
            
            // Position projectile at shoot point
            projectile.GlobalPosition = shootPoint.GlobalPosition;
            
            // Calculate shoot direction with spread
            float spread = _stats.BulletSpread;
            float randomSpread = (float)GD.RandRange(-spread, spread);
            Vector2 shootDirection = (shootTarget - shootPoint.GlobalPosition).Normalized();
            shootDirection = shootDirection.Rotated(Mathf.DegToRad(randomSpread));
            
            // Set projectile direction and rotation
            projectile.Direction = shootDirection;
            projectile.Rotation = shootDirection.Angle() + Mathf.Pi/2;

            // Apply recoil only once
            if (!appliedRecoil && _tankBody != null)
            {
                Vector2 recoilDirection = -shootDirection;
                _tankBody.Velocity += recoilDirection * RecoilForce;
                appliedRecoil = true;
            }
        }

        // Start reload timer
        _canShoot = false;
        _reloadTimer.Start(_stats.ReloadSpeed);
    }

    private void OnReloadComplete()
    {
        _canShoot = true;
    }
} 