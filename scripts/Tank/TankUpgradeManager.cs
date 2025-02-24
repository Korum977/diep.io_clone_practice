using Godot;
using System;
using System.Collections.Generic;

public partial class TankUpgradeManager : Node
{
    public enum TankClass
    {
        Basic,
        // Tier 2 (Level 15)
        Twin,
        Sniper,
        MachineGun,
        FlankGuard,
        // Tier 3 (Level 30)
        TripleShot,
        QuadTank,
        TwinFlank,
        Assassin,
        Overseer,
        Hunter,
        Destroyer,
        Gunner,
        TriAngle,
        Auto3,
        Smasher
    }

    public TankClass CurrentClass => _currentClass;

    private Dictionary<TankClass, int> _requiredLevels = new()
    {
        { TankClass.Basic, 1 },
        // Tier 2
        { TankClass.Twin, 15 },
        { TankClass.Sniper, 15 },
        { TankClass.MachineGun, 15 },
        { TankClass.FlankGuard, 15 },
        // Tier 3
        { TankClass.TripleShot, 30 },
        { TankClass.QuadTank, 30 },
        { TankClass.TwinFlank, 30 },
        { TankClass.Assassin, 30 },
        { TankClass.Overseer, 30 },
        { TankClass.Hunter, 30 },
        { TankClass.Destroyer, 30 },
        { TankClass.Gunner, 30 },
        { TankClass.TriAngle, 30 },
        { TankClass.Auto3, 30 },
        { TankClass.Smasher, 30 }
    };

    private Dictionary<TankClass, TankClass[]> _upgradeTree = new();
    private TankStats _tankStats;
    private TankWeapon _tankWeapon;
    private TankClass _currentClass = TankClass.Basic;

    public override void _Ready()
    {
        _tankStats = GetNode<TankStats>("../TankStats");
        _tankWeapon = GetNode<TankWeapon>("../TankWeapon");
        InitializeUpgradeTree();
    }

    private void InitializeUpgradeTree()
    {
        // Basic Tank upgrades (Tier 2)
        _upgradeTree[TankClass.Basic] = new[] {
            TankClass.Twin,
            TankClass.Sniper,
            TankClass.MachineGun,
            TankClass.FlankGuard
        };

        // Twin upgrades
        _upgradeTree[TankClass.Twin] = new[] {
            TankClass.TripleShot,
            TankClass.QuadTank,
            TankClass.TwinFlank
        };

        // Sniper upgrades
        _upgradeTree[TankClass.Sniper] = new[] {
            TankClass.Assassin,
            TankClass.Overseer,
            TankClass.Hunter
        };

        // Machine Gun upgrades
        _upgradeTree[TankClass.MachineGun] = new[] {
            TankClass.Destroyer,
            TankClass.Gunner
        };

        // Flank Guard upgrades
        _upgradeTree[TankClass.FlankGuard] = new[] {
            TankClass.TriAngle,
            TankClass.QuadTank,
            TankClass.TwinFlank,
            TankClass.Auto3
        };
    }

    public TankClass[] GetAvailableUpgrades()
    {
        if (!_upgradeTree.ContainsKey(_currentClass))
            return Array.Empty<TankClass>();

        var availableUpgrades = new List<TankClass>();
        foreach (var upgrade in _upgradeTree[_currentClass])
        {
            if (_tankStats.Level >= _requiredLevels[upgrade])
            {
                availableUpgrades.Add(upgrade);
            }
        }
        return availableUpgrades.ToArray();
    }

    public bool CanUpgradeTo(TankClass targetClass)
    {
        if (!_upgradeTree.ContainsKey(_currentClass))
            return false;

        return Array.Exists(_upgradeTree[_currentClass], upgrade => upgrade == targetClass) &&
               _tankStats.Level >= _requiredLevels[targetClass];
    }

    public void UpgradeTo(TankClass targetClass)
    {
        if (!CanUpgradeTo(targetClass))
            return;

        _currentClass = targetClass;
        ApplyTankClassStats();
        EmitSignal(SignalName.TankClassChanged, (int)targetClass);
    }

    private void ApplyTankClassStats()
    {
        switch (_currentClass)
        {
            case TankClass.Twin:
                _tankStats.BulletDamage *= 0.8f;
                _tankStats.ReloadSpeed *= 1.5f;
                _tankWeapon.UpdateCannonType(TankWeapon.CannonType.Twin);
                break;

            case TankClass.Sniper:
                _tankStats.BulletDamage *= 1.5f;
                _tankStats.BulletSpeed *= 1.5f;
                _tankStats.ReloadSpeed *= 0.7f;
                _tankWeapon.UpdateCannonType(TankWeapon.CannonType.Sniper);
                break;

            case TankClass.MachineGun:
                _tankStats.BulletDamage *= 1.2f;
                _tankStats.ReloadSpeed *= 2f;
                _tankStats.BulletSpread = 30f;
                _tankWeapon.UpdateCannonType(TankWeapon.CannonType.MachineGun);
                break;

            case TankClass.FlankGuard:
                _tankWeapon.UpdateCannonType(TankWeapon.CannonType.Flank);
                break;

            case TankClass.TripleShot:
                _tankStats.BulletDamage *= 0.7f;
                _tankStats.ReloadSpeed *= 1.3f;
                _tankWeapon.ClearCannons();
                _tankWeapon.AddCannon(-30, TankWeapon.CannonType.Basic, 0.9f);
                _tankWeapon.AddCannon(0, TankWeapon.CannonType.Basic, 0.9f);
                _tankWeapon.AddCannon(30, TankWeapon.CannonType.Basic, 0.9f);
                break;

            case TankClass.QuadTank:
                _tankStats.BulletDamage *= 0.7f;
                _tankWeapon.ClearCannons();
                _tankWeapon.AddCannon(0, TankWeapon.CannonType.Basic, 0.9f);
                _tankWeapon.AddCannon(90, TankWeapon.CannonType.Basic, 0.9f);
                _tankWeapon.AddCannon(180, TankWeapon.CannonType.Basic, 0.9f);
                _tankWeapon.AddCannon(270, TankWeapon.CannonType.Basic, 0.9f);
                break;

            case TankClass.TwinFlank:
                _tankStats.BulletDamage *= 0.7f;
                _tankStats.ReloadSpeed *= 1.5f;
                _tankWeapon.ClearCannons();
                _tankWeapon.AddCannon(-15, TankWeapon.CannonType.Basic, 0.9f);
                _tankWeapon.AddCannon(15, TankWeapon.CannonType.Basic, 0.9f);
                _tankWeapon.AddCannon(165, TankWeapon.CannonType.Basic, 0.9f);
                _tankWeapon.AddCannon(195, TankWeapon.CannonType.Basic, 0.9f);
                break;

            case TankClass.Assassin:
                _tankStats.BulletDamage *= 2f;
                _tankStats.BulletSpeed *= 2f;
                _tankStats.ReloadSpeed *= 0.5f;
                _tankWeapon.UpdateCannonType(TankWeapon.CannonType.Sniper);
                break;

            case TankClass.Hunter:
                _tankStats.BulletDamage *= 1.3f;
                _tankStats.BulletSpeed *= 1.7f;
                _tankStats.ReloadSpeed *= 0.6f;
                _tankWeapon.ClearCannons();
                _tankWeapon.AddCannon(0, TankWeapon.CannonType.Sniper, 1.0f);
                _tankWeapon.AddCannon(0, TankWeapon.CannonType.Basic, 0.7f);
                break;

            case TankClass.Destroyer:
                _tankStats.BulletDamage *= 3f;
                _tankStats.ReloadSpeed *= 0.3f;
                _tankWeapon.ClearCannons();
                _tankWeapon.AddCannon(0, TankWeapon.CannonType.Basic, 2.0f);
                break;

            case TankClass.Gunner:
                _tankStats.BulletDamage *= 0.4f;
                _tankStats.ReloadSpeed *= 3f;
                _tankWeapon.ClearCannons();
                _tankWeapon.AddCannon(-8, TankWeapon.CannonType.Basic, 0.4f);
                _tankWeapon.AddCannon(-2.5f, TankWeapon.CannonType.Basic, 0.4f);
                _tankWeapon.AddCannon(2.5f, TankWeapon.CannonType.Basic, 0.4f);
                _tankWeapon.AddCannon(8, TankWeapon.CannonType.Basic, 0.4f);
                break;

            case TankClass.TriAngle:
                _tankStats.BulletDamage *= 0.8f;
                _tankWeapon.ClearCannons();
                _tankWeapon.AddCannon(0, TankWeapon.CannonType.Basic, 1.0f);
                _tankWeapon.AddCannon(150, TankWeapon.CannonType.Basic, 0.8f);
                _tankWeapon.AddCannon(210, TankWeapon.CannonType.Basic, 0.8f);
                break;

            case TankClass.Auto3:
                _tankStats.BulletDamage *= 0.7f;
                _tankStats.ReloadSpeed *= 1.2f;
                _tankWeapon.ClearCannons();
                for (int i = 0; i < 3; i++)
                {
                    float angle = i * 120;
                    _tankWeapon.AddCannon(angle, TankWeapon.CannonType.Basic, 0.9f);
                }
                break;

            case TankClass.Smasher:
                _tankStats.BulletDamage = 0;
                _tankStats.BodyDamage *= 4.0f;
                _tankWeapon.ClearCannons();
                break;
        }
    }

    [Signal]
    public delegate void TankClassChangedEventHandler(int tankClass);
} 