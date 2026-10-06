public class ScenarioChoiceOption
{
    public string Text;
    public string TargetLabel;
    public string Condition;

    public ScenarioChoiceOption(
        string text,
        string targetLabel,
        string condition)
    {
        Text = text;
        TargetLabel = targetLabel;
        Condition = condition;
    }
}