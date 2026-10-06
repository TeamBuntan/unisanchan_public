using NUnit.Framework;
using unisanchan;

public class ExpressionEvaluatorTests
{
    [TestCase("1 + 2 * 3", 7d)]
    [TestCase("10 - 6 / 2", 7d)]
    [TestCase("10 % 4", 2d)]
    [TestCase("-5 + 2", -3d)]
    [TestCase("1.5 + 2.5", 4d)]
    [TestCase("(1 + 2) * 3", 9d)]
    [TestCase("10 / (2 + 3)", 2d)]
    [TestCase("(2 + 3) * (4 - 1)", 15d)]

    public void TryEvaluateNumber_ValidExpression_ReturnsExpectedResult(
        string expression,
        double expected)
    {
        bool success =
            ExpressionEvaluator.TryEvaluateNumber(
                expression,
                out double result);

        Assert.IsTrue(success);
        Assert.AreEqual(expected, result, 0.000001d);
    }

    [Test]
    public void TryEvaluateNumber_WithScenarioVariableStore_ReturnsExpectedResult()
    {
        ScenarioVariableStore store =
            new ScenarioVariableStore();

        store.Set("Attack", 10);
        store.Set("WeaponBonus", 5);

        bool success =
            ExpressionEvaluator.TryEvaluateNumber(
                "Attack * 2 + WeaponBonus",
                store.ResolveNumber,
                out double result);

        Assert.IsTrue(success);
        Assert.AreEqual(25d, result, 0.000001d);
    }

    [Test]
    public void TryEvaluateNumber_DivideByZero_ReturnsFalse()
    {
        bool success =
            ExpressionEvaluator.TryEvaluateNumber(
                "10 / 0",
                out _);

        Assert.IsFalse(success);
    }

    [Test]
    public void TryEvaluateNumber_WithVariable_ReturnsExpectedResult()
    {
        bool success =
            ExpressionEvaluator.TryEvaluateNumber(
                "Attack + 5",
                variableName =>
                {
                    if (variableName == "Attack")
                        return 10d;

                    return null;
                },
                out double result);

        Assert.IsTrue(success);
        Assert.AreEqual(15d, result, 0.000001d);
    }

    [Test]
    public void TryEvaluateNumber_WithMultipleVariables_ReturnsExpectedResult()
    {
        bool success =
            ExpressionEvaluator.TryEvaluateNumber(
                "Attack * 2 + WeaponBonus",
                variableName =>
                {
                    if (variableName == "Attack")
                        return 10d;

                    if (variableName == "WeaponBonus")
                        return 5d;

                    return null;
                },
                out double result);

        Assert.IsTrue(success);
        Assert.AreEqual(25d, result, 0.000001d);
    }

    [Test]
    public void ScenarioVariableStore_Apply_UsesExpressionEvaluator()
    {
        ScenarioVariableStore store =
            new ScenarioVariableStore();

        store.Set("Attack", 10);
        store.Set("WeaponBonus", 5);
        store.Set("Damage", 0);

        ValueExpression expression =
            new ValueExpression
            {
                VariableID = "Damage",
                Operator = ValueOperator.Set,
                Value = "Attack * 2 + WeaponBonus"
            };

        bool success =
            store.Apply(expression);

        Assert.IsTrue(success);

        bool found =
            store.TryGet(
                "Damage",
                out object value);

        Assert.IsTrue(found);
        Assert.AreEqual(25, value);
    }

    [Test]
    public void ScenarioVariableStore_Apply_AddsExpressionResult()
    {
        ScenarioVariableStore store =
            new ScenarioVariableStore();

        store.Set("EnemyDefeat", 10);
        store.Set("Bonus", 2);

        ValueExpression expression =
            new ValueExpression
            {
                VariableID = "EnemyDefeat",
                Operator = ValueOperator.Add,
                Value = "Bonus + 1"
            };

        bool success =
            store.Apply(expression);

        Assert.IsTrue(success);

        bool found =
            store.TryGet(
                "EnemyDefeat",
                out object value);

        Assert.IsTrue(found);
        Assert.AreEqual(13, value);
    }

    [Test]
    public void ScenarioVariableStore_Apply_SubtractsExpressionResult()
    {
        ScenarioVariableStore store =
            new ScenarioVariableStore();

        store.Set("HP", 100);
        store.Set("EnemyAttack", 20);

        ValueExpression expression =
            new ValueExpression
            {
                VariableID = "HP",
                Operator = ValueOperator.Subtract,
                Value = "EnemyAttack + 5"
            };

        bool success =
            store.Apply(expression);

        Assert.IsTrue(success);

        bool found =
            store.TryGet(
                "HP",
                out object value);

        Assert.IsTrue(found);
        Assert.AreEqual(75, value);
    }

    [Test]
    public void ScenarioVariableStore_Apply_MultipliesExpressionResult()
    {
        ScenarioVariableStore store =
            new ScenarioVariableStore();

        store.Set("Score", 10);
        store.Set("Multiplier", 2);

        ValueExpression expression =
            new ValueExpression
            {
                VariableID = "Score",
                Operator = ValueOperator.Multiply,
                Value = "Multiplier + 1"
            };

        bool success =
            store.Apply(expression);

        Assert.IsTrue(success);

        bool found =
            store.TryGet(
                "Score",
                out object value);

        Assert.IsTrue(found);
        Assert.AreEqual(30, value);
    }

    [Test]
    public void ScenarioVariableStore_Apply_DividesExpressionResult()
    {
        ScenarioVariableStore store =
            new ScenarioVariableStore();

        store.Set("HP", 100);
        store.Set("Divider", 4);

        ValueExpression expression =
            new ValueExpression
            {
                VariableID = "HP",
                Operator = ValueOperator.Divide,
                Value = "Divider"
            };

        bool success =
            store.Apply(expression);

        Assert.IsTrue(success);

        bool found =
            store.TryGet(
                "HP",
                out object value);

        Assert.IsTrue(found);
        Assert.AreEqual(25, value);
    }

    [Test]
    public void ScenarioVariableStore_Apply_ModuloExpressionResult()
    {
        ScenarioVariableStore store =
            new ScenarioVariableStore();

        store.Set("Counter", 17);
        store.Set("Divider", 5);

        ValueExpression expression =
            new ValueExpression
            {
                VariableID = "Counter",
                Operator = ValueOperator.Modulo,
                Value = "Divider"
            };

        bool success =
            store.Apply(expression);

        Assert.IsTrue(success);

        bool found =
            store.TryGet(
                "Counter",
                out object value);

        Assert.IsTrue(found);
        Assert.AreEqual(2, value);
    }

    [TestCase("10 > 5", true)]
    [TestCase("10 < 5", false)]
    [TestCase("10 >= 10", true)]
    [TestCase("10 <= 10", true)]
    [TestCase("10 == 10", true)]
    [TestCase("10 != 5", true)]
    public void TryEvaluateCondition_ComparisonExpression_ReturnsExpectedResult(
        string expression,
        bool expected)
    {
        bool success =
            ExpressionEvaluator.TryEvaluateCondition(
                expression,
                null,
                null,
                out bool result);

        Assert.IsTrue(success);
        Assert.AreEqual(expected, result);
    }

    [TestCase("10 > 5 && 3 < 4", true)]
    [TestCase("10 > 5 && 3 > 4", false)]
    [TestCase("10 > 20 || 3 < 4", true)]
    [TestCase("10 > 20 || 3 > 4", false)]
    [TestCase("!(10 > 5)", false)]
    [TestCase("!(10 < 5)", true)]
    public void TryEvaluateCondition_LogicalExpression_ReturnsExpectedResult(
        string expression,
        bool expected)
    {
        bool success =
                ExpressionEvaluator.TryEvaluateCondition(
                    expression,
                    null,
                    null,
                    out bool result);

        Assert.IsTrue(success);
        Assert.AreEqual(expected, result);
    }

    [Test]
    public void TryEvaluateCondition_WithVariablesAndLogicalOperators_ReturnsExpectedResult()
    {
        ScenarioVariableStore store =
            new ScenarioVariableStore();

        store.Set("Affection", 60);
        store.Set("Chapter", 4);
        store.Set("HP", 100);

        bool success =
            ExpressionEvaluator.TryEvaluateCondition(
                "Affection >= 50 && Chapter >= 3 && HP > 0",
                store.ResolveNumber,
                null,
                out bool result);

        Assert.IsTrue(success);
        Assert.IsTrue(result);
    }

    [Test]
    public void TryEvaluateCondition_WithBoolVariable_ReturnsExpectedResult()
    {
        ScenarioVariableStore store =
            new ScenarioVariableStore();

        store.Set("HasKey", true);

        bool success =
            ExpressionEvaluator.TryEvaluateCondition(
                "HasKey",
                store.ResolveNumber,
                store.ResolveBool,
                out bool result);

        Assert.IsTrue(success);
        Assert.IsTrue(result);
    }

    [Test]
    public void TryEvaluateCondition_WithBoolAndNumberVariables_ReturnsExpectedResult()
    {
        ScenarioVariableStore store =
            new ScenarioVariableStore();

        store.Set("HasKey", true);
        store.Set("HP", 100);

        bool success =
            ExpressionEvaluator.TryEvaluateCondition(
                "HasKey && HP > 0",
                store.ResolveNumber,
                store.ResolveBool,
                out bool result);

        Assert.IsTrue(success);
        Assert.IsTrue(result);
    }

    [Test]
    public void TryEvaluateCondition_WithNotBoolVariable_ReturnsExpectedResult()
    {
        ScenarioVariableStore store =
            new ScenarioVariableStore();

        store.Set("HasKey", false);

        bool success =
            ExpressionEvaluator.TryEvaluateCondition(
                "!HasKey",
                store.ResolveNumber,
                store.ResolveBool,
                out bool result);

        Assert.IsTrue(success);
        Assert.IsTrue(result);
    }

    [Test]
    public void TryEvaluateCondition_WithMultipleBoolVariables_ReturnsExpectedResult()
    {
        ScenarioVariableStore store =
            new ScenarioVariableStore();

        store.Set("HasKey", true);
        store.Set("DoorUnlocked", false);

        bool success =
            ExpressionEvaluator.TryEvaluateCondition(
                "HasKey || DoorUnlocked",
                store.ResolveNumber,
                store.ResolveBool,
                out bool result);

        Assert.IsTrue(success);
        Assert.IsTrue(result);
    }

    [Test]
    public void TryEvaluateCondition_LogicalPrecedence_ReturnsExpectedResult()
    {
        ScenarioVariableStore store =
            new ScenarioVariableStore();

        store.Set("A", true);
        store.Set("B", false);
        store.Set("C", false);

        bool success =
            ExpressionEvaluator.TryEvaluateCondition(
                "A || B && C",
                store.ResolveNumber,
                store.ResolveBool,
                out bool result);

        Assert.IsTrue(success);
        Assert.IsTrue(result);
    }

    [Test]
    public void TryEvaluateCondition_WithLogicalParentheses_ReturnsExpectedResult()
    {
        ScenarioVariableStore store =
            new ScenarioVariableStore();

        store.Set("HasKey", false);
        store.Set("DoorUnlocked", true);
        store.Set("HP", 100);

        bool success =
            ExpressionEvaluator.TryEvaluateCondition(
                "(HasKey || DoorUnlocked) && HP > 0",
                store.ResolveNumber,
                store.ResolveBool,
                out bool result);

        Assert.IsTrue(success);
        Assert.IsTrue(result);
    }

    [Test]
    public void TryEvaluateCondition_ParenthesesDoNotSwallowOuterOperator()
    {
        ScenarioVariableStore store =
            new ScenarioVariableStore();

        store.Set("A", false);
        store.Set("B", true);

        bool success =
            ExpressionEvaluator.TryEvaluateCondition(
                "(A) || B",
                store.ResolveNumber,
                store.ResolveBool,
                out bool result);

        Assert.IsTrue(success);
        Assert.IsTrue(result);
    }

    [Test]
    public void ChoiceCondition_FalseCondition_IsRejected()
    {
        ScenarioVariableStore store =
            new ScenarioVariableStore();

        store.Set("Affection", 10);

        bool success =
            unisanchan.ExpressionEvaluator.TryEvaluateCondition(
                "Affection >= 50",
                store.ResolveNumber,
                store.ResolveBool,
                out bool result);

        Assert.IsTrue(success);
        Assert.IsFalse(result);
    }

    [Test]
    public void JumpCondition_TrueCondition_IsAccepted()
    {
        ScenarioVariableStore store =
            new ScenarioVariableStore();

        store.Set("Affection", 60);

        bool success =
            unisanchan.ExpressionEvaluator.TryEvaluateCondition(
                "Affection >= 50",
                store.ResolveNumber,
                store.ResolveBool,
                out bool result);

        Assert.IsTrue(success);
        Assert.IsTrue(result);
    }

    [Test]
    public void JumpCondition_FalseCondition_IsRejected()
    {
        ScenarioVariableStore store =
            new ScenarioVariableStore();

        store.Set("Affection", 10);

        bool success =
            unisanchan.ExpressionEvaluator.TryEvaluateCondition(
                "Affection >= 50",
                store.ResolveNumber,
                store.ResolveBool,
                out bool result);

        Assert.IsTrue(success);
        Assert.IsFalse(result);
    }

    [Test]
    public void TryEvaluateCondition_InvalidExpression_ReturnsFalse()
    {
        ScenarioVariableStore store =
            new ScenarioVariableStore();

        store.Set("Affection", 60);

        bool success =
            unisanchan.ExpressionEvaluator.TryEvaluateCondition(
                "Affection >> 50",
                store.ResolveNumber,
                store.ResolveBool,
                out _);

        Assert.IsFalse(success);
    }
}