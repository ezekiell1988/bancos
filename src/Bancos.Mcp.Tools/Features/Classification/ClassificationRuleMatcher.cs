using Bancos.Mcp.Domain;
using Bancos.Mcp.Features.Parsing;

namespace Bancos.Mcp.Features.Classification;

internal static class ClassificationRuleMatcher
{
    public static ClassificationRule? FindBestMatch(
        IEnumerable<ClassificationRule> rules,
        string description,
        string operationType) =>
        rules
            .Where(rule => Matches(rule, description, operationType))
            .OrderByDescending(Specificity)
            .ThenByDescending(rule => rule.Priority)
            .FirstOrDefault();

    private static bool Matches(ClassificationRule rule, string description, string operationType)
    {
        if (rule.OperationType is not null && !string.Equals(rule.OperationType, operationType, StringComparison.Ordinal))
            return false;

        var normalizedDescription = TextNormalizer.Normalize(description);
        var normalizedPattern = TextNormalizer.Normalize(rule.DescriptionPattern);

        return rule.MatchType switch
        {
            "exact" => normalizedDescription == normalizedPattern,
            "starts-with" => normalizedDescription.StartsWith(normalizedPattern, StringComparison.Ordinal),
            _ => normalizedDescription.Contains(normalizedPattern, StringComparison.Ordinal)
        };
    }

    // Regla más específica gana: cuenta explícita > tipo de operación > ninguna.
    private static int Specificity(ClassificationRule rule) =>
        (rule.BankAccountId is not null ? 2 : 0) + (rule.OperationType is not null ? 1 : 0);
}
