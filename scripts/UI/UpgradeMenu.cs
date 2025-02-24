using Godot;
using System;

public partial class UpgradeMenu : Control
{
    private TankStats _tankStats;
    private bool _isVisible = false;
    private Label _titleLabel;
    private Label _pointsLabel;

    public override void _Ready()
    {
        _tankStats = GetNode<TankStats>("../../Tank/TankStats");
        _titleLabel = GetNode<Label>("Panel/VBoxContainer/Title");
        _pointsLabel = GetNode<Label>("Panel/VBoxContainer/PointsLabel");
        
        if (_tankStats != null)
        {
            _tankStats.LevelUp += OnLevelUp;
            _tankStats.StatUpgraded += OnStatUpgraded;
        }
        
        Visible = false;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("upgrade_menu"))
        {
            ToggleMenu();
        }
    }

    private void ToggleMenu()
    {
        if (_tankStats.AvailableStatPoints <= 0)
        {
            // Don't show menu if no points available
            _isVisible = false;
            Visible = false;
            Engine.TimeScale = 1.0f;
            return;
        }

        _isVisible = !_isVisible;
        Visible = _isVisible;
        
        if (_isVisible)
        {
            // Pause game or slow down time
            Engine.TimeScale = 0.2f;
            UpdateUI();
        }
        else
        {
            Engine.TimeScale = 1.0f;
        }
    }

    private void UpdateUI()
    {
        if (_titleLabel != null)
        {
            _titleLabel.Text = $"Level Up! (Level {_tankStats.Level})";
        }
        if (_pointsLabel != null)
        {
            _pointsLabel.Text = $"Available Points: {_tankStats.AvailableStatPoints}";
        }
    }

    private void OnLevelUp(int level, int availablePoints)
    {
        if (availablePoints > 0)
        {
            // Show upgrade options
            _isVisible = true;
            Visible = true;
            Engine.TimeScale = 0.2f;
            UpdateUI();
        }
    }

    private void OnStatUpgraded(string statName, int remainingPoints)
    {
        UpdateUI();
        if (remainingPoints <= 0)
        {
            ToggleMenu();
        }
    }

    public void OnUpgradeHealth()
    {
        if (_tankStats != null)
        {
            _tankStats.UpgradeStat("health");
        }
    }

    public void OnUpgradeReload()
    {
        if (_tankStats != null)
        {
            _tankStats.UpgradeStat("reload");
        }
    }

    public void OnUpgradeDamage()
    {
        if (_tankStats != null)
        {
            _tankStats.UpgradeStat("damage");
        }
    }
} 