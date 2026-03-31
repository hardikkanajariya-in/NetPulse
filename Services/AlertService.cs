using System;
using System.Collections.Generic;
using System.Linq;

namespace SpeedoMeter.Services;

public sealed class AlertService
{
    private readonly DatabaseService _db;
    private readonly List<AlertRule> _rules = new();
    private readonly Dictionary<string, bool> _triggeredState = new(StringComparer.OrdinalIgnoreCase);

    public event Action<string, string>? AlertTriggered;

    public AlertService(DatabaseService db)
    {
        _db = db;
        LoadRules();
    }

    public IReadOnlyList<AlertRule> Rules => _rules;

    public void LoadRules()
    {
        _rules.Clear();
        _rules.AddRange(_db.GetAlertRules());
        foreach (var rule in _rules)
            _triggeredState.TryAdd(rule.RuleId, false);
    }

    public void AddRule(AlertRule rule)
    {
        _db.SaveAlertRule(rule);
        _rules.Add(rule);
        _triggeredState[rule.RuleId] = false;
    }

    public void RemoveRule(string ruleId)
    {
        _db.DeleteAlertRule(ruleId);
        _rules.RemoveAll(r => r.RuleId == ruleId);
        _triggeredState.Remove(ruleId);
    }

    public void ToggleRule(string ruleId, bool enabled)
    {
        var rule = _rules.FirstOrDefault(r => r.RuleId == ruleId);
        if (rule == null) return;
        rule.Enabled = enabled;
        _db.SaveAlertRule(rule);
        if (!enabled) _triggeredState[ruleId] = false;
    }

    public void Evaluate(TelemetrySnapshot snapshot, long todayDownloaded, long todayUploaded)
    {
        foreach (var rule in _rules.Where(r => r.Enabled))
        {
            bool triggered = rule.RuleType switch
            {
                "daily-download" => todayDownloaded >= rule.ThresholdBytes,
                "daily-upload" => todayUploaded >= rule.ThresholdBytes,
                "daily-total" => (todayDownloaded + todayUploaded) >= rule.ThresholdBytes,
                "speed-download" => snapshot.DownloadSpeed >= rule.ThresholdBytes,
                "speed-upload" => snapshot.UploadSpeed >= rule.ThresholdBytes,
                _ => false
            };

            _triggeredState.TryGetValue(rule.RuleId, out bool wasTriggered);

            if (triggered && !wasTriggered)
            {
                string msg = $"{rule.RuleName}: threshold of {SpeedFormatter.FormatSize(rule.ThresholdBytes)} reached";
                _db.AddAlertHistoryEntry(rule.RuleId, msg);
                AlertTriggered?.Invoke("NetPulse Alert", msg);
            }

            _triggeredState[rule.RuleId] = triggered;
        }
    }

    public List<AlertHistoryEntry> GetHistory(int limit = 50)
    {
        return _db.GetAlertHistory(limit);
    }
}
