namespace unisanchan
{
    public enum ValueOperator
    {
        Set,
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo
    }

    public struct ValueExpression
    {
        public string VariableID;
        public ValueOperator Operator;
        public string Value;
    }

    public static class ValueExpressionParser
    {
        public static bool TryParse(
            string expression,
            out ValueExpression result)
        {
            result = default;

            if (string.IsNullOrWhiteSpace(expression))
                return false;

            string trimmedExpression = expression.Trim();

            string operatorText;
            ValueOperator valueOperator;

            if (trimmedExpression.Contains("*="))
            {
                operatorText = "*=";
                valueOperator = ValueOperator.Multiply;
            }
            else if (trimmedExpression.Contains("/="))
            {
                operatorText = "/=";
                valueOperator = ValueOperator.Divide;
            }
            else if (trimmedExpression.Contains("%="))
            {
                operatorText = "%=";
                valueOperator = ValueOperator.Modulo;
            }
            else if (trimmedExpression.Contains("+="))
            {
                operatorText = "+=";
                valueOperator = ValueOperator.Add;
            }
            else if (trimmedExpression.Contains("-="))
            {
                operatorText = "-=";
                valueOperator = ValueOperator.Subtract;
            }
            else if (trimmedExpression.Contains("="))
            {
                operatorText = "=";
                valueOperator = ValueOperator.Set;
            }
            else
            {
                return false;
            }

            int operatorIndex =
                trimmedExpression.IndexOf(operatorText);

            string variableId =
                trimmedExpression
                    .Substring(0, operatorIndex)
                    .Trim();

            string value =
                trimmedExpression
                    .Substring(
                        operatorIndex + operatorText.Length)
                    .Trim();

            if (string.IsNullOrEmpty(variableId))
                return false;

            if (string.IsNullOrEmpty(value))
                return false;

            result = new ValueExpression
            {
                VariableID = variableId,
                Operator = valueOperator,
                Value = value
            };

            return true;
        }
    }
}