using System.Collections.Generic;
using RoboterLego.Domain;

namespace RoboterLego.Generation
{
    public sealed class CompatibilityGraph
    {
        private readonly Dictionary<string, bool> map = new Dictionary<string, bool>();

        public CompatibilityGraph(IReadOnlyList<CompatibilityRule> rules)
        {
            if (rules == null)
            {
                return;
            }

            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                map[Key(rule.SocketTypeA, rule.SocketTypeB)] = rule.Allowed;
                map[Key(rule.SocketTypeB, rule.SocketTypeA)] = rule.Allowed;
            }
        }

        public bool IsAllowed(string socketA, string socketB)
        {
            if (string.IsNullOrEmpty(socketA) || string.IsNullOrEmpty(socketB))
            {
                return false;
            }

            return map.TryGetValue(Key(socketA, socketB), out bool allowed) && allowed;
        }

        private static string Key(string a, string b)
        {
            return $"{a}::{b}";
        }
    }
}
