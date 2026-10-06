using System;
using System.Globalization;

namespace unisanchan
{
    public static class ExpressionEvaluator
    {
        public static bool TryEvaluateNumber(
            string expression,
            out double result)
        {
            result = 0d;

            if (string.IsNullOrWhiteSpace(expression))
                return false;

            string normalized =
                expression
                    .Trim()
                    .Replace(" ", "");

            int index = 0;

            if (!TryParseAddSubtract(
                    normalized,
                    ref index,
                    null,
                    out result))
            {
                return false;
            }

            return index == normalized.Length;
        }

        public static bool TryEvaluateNumber(
            string expression,
            Func<string, double?> variableResolver,
            out double result)
        {
            result = 0d;

            if (string.IsNullOrWhiteSpace(expression))
                return false;

            string normalized =
                expression
                    .Trim()
                    .Replace(" ", "");

            int index = 0;

            if (!TryParseAddSubtract(
                    normalized,
                    ref index,
                    variableResolver,
                    out result))
            {
                return false;
            }

            return index == normalized.Length;
        }

        public static bool TryEvaluateCondition(
            string expression,
            Func<string, double?> variableResolver,
            Func<string, bool?> boolResolver,
            out bool result)
        {
            result = false;

            if (string.IsNullOrWhiteSpace(expression))
                return false;

            string normalized =
                expression
                    .Trim()
                    .Replace(" ", "");
            
            if (normalized.StartsWith("(") &&
                normalized.EndsWith(")"))
            {
                string innerCondition =
                    normalized.Substring(
                        1,
                        normalized.Length - 2);

                if (TryEvaluateCondition(
                        innerCondition,
                        variableResolver,
                        boolResolver,
                        out bool innerResult))
                {
                    result = innerResult;
                    return true;
                }
            }
            
            if (normalized.StartsWith("!"))
            {
                string innerCondition =
                    normalized.Substring(1);

                if (innerCondition.StartsWith("(") &&
                    innerCondition.EndsWith(")"))
                {
                    innerCondition =
                        innerCondition.Substring(
                            1,
                            innerCondition.Length - 2);
                }

                if (!TryEvaluateCondition(
                        innerCondition,
                        variableResolver,
                        boolResolver,
                        out bool innerResult))
                {
                    return false;
                }

                result = !innerResult;
                return true;
            }

            int orIndex =
                FindOperatorOutsideParentheses(
                    normalized,
                    "||");

            if (orIndex >= 0)
            {
                string leftCondition =
                    normalized.Substring(
                        0,
                        orIndex);

                string rightCondition =
                    normalized.Substring(
                        orIndex + 2);

                if (!TryEvaluateCondition(
                        leftCondition,
                        variableResolver,
                        boolResolver,
                        out bool leftResult))
                {
                    return false;
                }

                if (!TryEvaluateCondition(
                        rightCondition,
                        variableResolver,
                        boolResolver,
                        out bool rightResult))
                {
                    return false;
                }

                result = leftResult || rightResult;
                return true;
            }

            int andIndex =
                FindOperatorOutsideParentheses(
                    normalized,
                    "&&");

            if (andIndex >= 0)
            {
                string leftCondition =
                    normalized.Substring(
                        0,
                        andIndex);

                string rightCondition =
                    normalized.Substring(
                        andIndex + 2);

                if (!TryEvaluateCondition(
                        leftCondition,
                        variableResolver,
                        boolResolver,
                        out bool leftResult))
                {
                    return false;
                }

                if (!TryEvaluateCondition(
                        rightCondition,
                        variableResolver,
                        boolResolver,
                        out bool rightResult))
                {
                    return false;
                }

                result = leftResult && rightResult;
                return true;
            }

            if (boolResolver != null)
            {
                bool? boolValue =
                    boolResolver(normalized);

                if (boolValue.HasValue)
                {
                    result = boolValue.Value;
                    return true;
                }
            }

            int operatorIndex =
                normalized.IndexOf(">=");

            if (operatorIndex >= 0)
            {
                string leftExpression =
                    normalized.Substring(
                        0,
                        operatorIndex);

                string rightExpression =
                    normalized.Substring(
                        operatorIndex + 2);

                if (!TryEvaluateNumber(
                        leftExpression,
                        variableResolver,
                        out double leftValue))
                {
                    return false;
                }

                if (!TryEvaluateNumber(
                        rightExpression,
                        variableResolver,
                        out double rightValue))
                {
                    return false;
                }

                result = leftValue >= rightValue;
                return true;
            }

            operatorIndex =
                normalized.IndexOf("<=");

            if (operatorIndex >= 0)
            {
                string leftExpression =
                    normalized.Substring(
                        0,
                        operatorIndex);

                string rightExpression =
                    normalized.Substring(
                        operatorIndex + 2);

                if (!TryEvaluateNumber(
                        leftExpression,
                        variableResolver,
                        out double leftValue))
                {
                    return false;
                }

                if (!TryEvaluateNumber(
                        rightExpression,
                        variableResolver,
                        out double rightValue))
                {
                    return false;
                }

                result = leftValue <= rightValue;
                return true;
            }

            operatorIndex =
                normalized.IndexOf("==");

            if (operatorIndex >= 0)
            {
                string leftExpression =
                    normalized.Substring(
                        0,
                        operatorIndex);

                string rightExpression =
                    normalized.Substring(
                        operatorIndex + 2);

                if (!TryEvaluateNumber(
                        leftExpression,
                        variableResolver,
                        out double leftValue))
                {
                    return false;
                }

                if (!TryEvaluateNumber(
                        rightExpression,
                        variableResolver,
                        out double rightValue))
                {
                    return false;
                }

                result = leftValue == rightValue;
                return true;
            }

            operatorIndex =
                normalized.IndexOf("!=");

            if (operatorIndex >= 0)
            {
                string leftExpression =
                    normalized.Substring(
                        0,
                        operatorIndex);

                string rightExpression =
                    normalized.Substring(
                        operatorIndex + 2);

                if (!TryEvaluateNumber(
                        leftExpression,
                        variableResolver,
                        out double leftValue))
                {
                    return false;
                }

                if (!TryEvaluateNumber(
                        rightExpression,
                        variableResolver,
                        out double rightValue))
                {
                    return false;
                }

                result = leftValue != rightValue;
                return true;
            }

            operatorIndex =
                normalized.IndexOf('<');

            if (operatorIndex >= 0)
            {
                string leftExpression =
                    normalized.Substring(
                        0,
                        operatorIndex);

                string rightExpression =
                    normalized.Substring(
                        operatorIndex + 1);

                if (!TryEvaluateNumber(
                        leftExpression,
                        variableResolver,
                        out double leftValue))
                {
                    return false;
                }

                if (!TryEvaluateNumber(
                        rightExpression,
                        variableResolver,
                        out double rightValue))
                {
                    return false;
                }

                result = leftValue < rightValue;
                return true;
            }

            operatorIndex =
                normalized.IndexOf('>');          

            if (operatorIndex < 0)
                return false;

            {
                string leftExpression =
                    normalized.Substring(
                        0,
                        operatorIndex);

                string rightExpression =
                    normalized.Substring(
                        operatorIndex + 1);

                if (!TryEvaluateNumber(
                        leftExpression,
                        variableResolver,
                        out double leftValue))
                {
                    return false;
                }

                if (!TryEvaluateNumber(
                        rightExpression,
                        variableResolver,
                        out double rightValue))
                {
                    return false;
                }

                result = leftValue > rightValue;
                return true;
            }
        }

        private static int FindOperatorOutsideParentheses(
            string expression,
            string operatorText)
        {
            int depth = 0;

            for (int i = 0;
                i <= expression.Length - operatorText.Length;
                i++)
            {
                char c = expression[i];

                if (c == '(')
                {
                    depth++;
                    continue;
                }

                if (c == ')')
                {
                    depth--;
                    continue;
                }

                if (depth == 0 &&
                    expression.Substring(
                        i,
                        operatorText.Length) == operatorText)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool TryParseAddSubtract(
            string expression,
            ref int index,
            Func<string, double?> variableResolver,
            out double result)
        {
            result = 0d;

            if (!TryParseMultiplyDivide(
                    expression,
                    ref index,
                    variableResolver,
                    out result))
            {
                return false;
            }

            while (index < expression.Length)
            {
                char op = expression[index];

                if (op != '+' && op != '-')
                    break;

                index++;

                if (!TryParseMultiplyDivide(
                        expression,
                        ref index,
                        variableResolver,
                        out double rightValue))
                {
                    return false;
                }

                result =
                    op == '+'
                        ? result + rightValue
                        : result - rightValue;
            }

            return true;
        }

        private static bool TryParseMultiplyDivide(
            string expression,
            ref int index,
            Func<string, double?> variableResolver,
            out double result)
        {
            if (!TryParsePrimary(
                    expression,
                    ref index,
                    variableResolver,
                    out result))
            {
                return false;
            }

            while (index < expression.Length)
            {
                char op = expression[index];

                if (op != '*' &&
                    op != '/' &&
                    op != '%')
                {
                    break;
                }

                index++;

                if (!TryParsePrimary(
                        expression,
                        ref index,
                        variableResolver,
                        out double rightValue))
                {
                    return false;
                }

                switch (op)
                {
                    case '*':
                        result *= rightValue;
                        break;

                    case '/':
                        if (rightValue == 0d)
                            return false;

                        result /= rightValue;
                        break;

                    case '%':
                        if (rightValue == 0d)
                            return false;

                        result %= rightValue;
                        break;
                }
            }

            return true;
        }

        private static bool TryParsePrimary(
            string expression,
            ref int index,
            Func<string, double?> variableResolver,
            out double result)
        {
            result = 0d;

            if (index >= expression.Length)
                return false;

            if (expression[index] == '(')
            {
                index++;

                if (!TryParseAddSubtract(
                        expression,
                        ref index,
                        variableResolver,
                        out result))
                {
                    return false;
                }

                if (index >= expression.Length ||
                    expression[index] != ')')
                {
                    return false;
                }

                index++;

                return true;
            }

            int startIndex = index;

            if (index < expression.Length &&
                (char.IsLetter(expression[index]) ||
                expression[index] == '_'))
            {
                index++;

                while (index < expression.Length)
                {
                    char c = expression[index];

                    if (char.IsLetterOrDigit(c) || c == '_')
                    {
                        index++;
                        continue;
                    }

                    break;
                }
            }

            if (index > startIndex)
            {
                string variableName =
                    expression.Substring(
                        startIndex,
                        index - startIndex);

                if (variableResolver == null)
                    return false;

                double? resolvedValue =
                    variableResolver(variableName);

                if (!resolvedValue.HasValue)
                    return false;

                result = resolvedValue.Value;
                return true;
            }

            return TryParseNumber(
                expression,
                ref index,
                out result);
        }

        private static bool TryParseNumber(
            string expression,
            ref int index,
            out double result)
        {
            result = 0d;

            int startIndex = index;

            if (index < expression.Length &&
                (expression[index] == '+' ||
                expression[index] == '-'))
            {
                index++;
            }

            bool hasDigit = false;
            bool hasDecimalPoint = false;

            while (index < expression.Length)
            {
                char c = expression[index];

                if (char.IsDigit(c))
                {
                    hasDigit = true;
                    index++;
                    continue;
                }

                if (c == '.' && !hasDecimalPoint)
                {
                    hasDecimalPoint = true;
                    index++;
                    continue;
                }

                break;
            }

            if (!hasDigit)
                return false;

            string numberText =
                expression.Substring(
                    startIndex,
                    index - startIndex);

            return double.TryParse(
                numberText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out result);
        }
    }
}