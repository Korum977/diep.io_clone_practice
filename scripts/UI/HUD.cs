using Godot;
using System;

public partial class HUD : Control
{
    private Label _healthLabel;
    private Label _levelLabel;
    private Label _experienceLabel;
    private ProgressBar _healthBar;
    private ProgressBar _experienceBar;
    private TankStats _tankStats;
    private Control _deathScreen;
    private Label _finalScoreLabel;
    private Button _restartButton;
    private Label _levelUpNotification;
    private Timer _levelUpTimer;

    public override void _Ready()
    {
        // Get basic HUD elements
        _healthLabel = GetNodeOrNull<Label>("HealthLabel");
        _levelLabel = GetNodeOrNull<Label>("LevelLabel");
        _experienceLabel = GetNodeOrNull<Label>("ExperienceLabel");
        _healthBar = GetNodeOrNull<ProgressBar>("HealthBar");
        _experienceBar = GetNodeOrNull<ProgressBar>("ExperienceBar");
        
        // Setup level up notification
        _levelUpNotification = GetNodeOrNull<Label>("LevelUpNotification");
        if (_levelUpNotification != null)
        {
            _levelUpNotification.Visible = false;
        }
        
        _levelUpTimer = new Timer();
        _levelUpTimer.WaitTime = 2.0f; // Show notification for 2 seconds
        _levelUpTimer.OneShot = true;
        _levelUpTimer.Timeout += HideLevelUpNotification;
        AddChild(_levelUpTimer);
        
        // Get death screen elements
        _deathScreen = GetNodeOrNull<Control>("DeathScreen");
        if (_deathScreen != null)
        {
            _finalScoreLabel = _deathScreen.GetNodeOrNull<Label>("VBoxContainer/FinalScore");
            _restartButton = _deathScreen.GetNodeOrNull<Button>("VBoxContainer/RestartButton");
            
            // Connect restart button if it exists
            if (_restartButton != null)
            {
                _restartButton.Pressed += OnRestartPressed;
            }
            
            // Hide death screen initially
            _deathScreen.Visible = false;
        }
        
        // Get tank stats
        _tankStats = GetNode<TankStats>("../../Tank/TankStats");
        if (_tankStats != null)
        {
            _tankStats.LevelUp += OnLevelUp;
            _tankStats.TankDestroyed += OnTankDestroyed;
        }
    }

    public override void _Process(double delta)
    {
        if (_tankStats == null || _tankStats.IsDead) return;

        // Update health display
        if (_healthLabel != null && _healthBar != null)
        {
            _healthLabel.Text = $"Health: {_tankStats.CurrentHealth:F0}/{_tankStats.MaxHealth:F0}";
            _healthBar.Value = (_tankStats.CurrentHealth / _tankStats.MaxHealth) * 100;
        }

        // Update level and experience display
        if (_levelLabel != null && _experienceLabel != null && _experienceBar != null)
        {
            _levelLabel.Text = $"Level: {_tankStats.Level}";
            _experienceLabel.Text = $"XP: {_tankStats.Experience:F0}/{_tankStats.ExperienceToNextLevel:F0}";
            _experienceBar.Value = (_tankStats.Experience / _tankStats.ExperienceToNextLevel) * 100;
        }
    }

    private void OnLevelUp(int level, int availablePoints)
    {
        if (_levelUpNotification != null)
        {
            _levelUpNotification.Text = $"Level Up!\nLevel {level}\nSkill Points: {availablePoints}";
            _levelUpNotification.Visible = true;
            _levelUpTimer.Start();
        }
    }

    private void HideLevelUpNotification()
    {
        if (_levelUpNotification != null)
        {
            _levelUpNotification.Visible = false;
        }
    }

    private void OnTankDestroyed()
    {
        if (_deathScreen != null)
        {
            _deathScreen.Visible = true;
            if (_finalScoreLabel != null)
            {
                _finalScoreLabel.Text = $"Final Score\nLevel: {_tankStats.Level}\nExperience: {_tankStats.Experience:F0}";
            }
        }
    }

    private void OnRestartPressed()
    {
        if (_tankStats != null)
        {
            _tankStats.Respawn();
            if (_deathScreen != null)
            {
                _deathScreen.Visible = false;
            }
        }
    }
} 