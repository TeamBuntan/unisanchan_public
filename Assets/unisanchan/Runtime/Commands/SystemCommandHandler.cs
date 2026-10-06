using System;
using UnityEngine;
using UnityEngine.UI;

public class SystemCommandHandler
{
    private readonly Action<string> jumpToLabel;
    private readonly Image fadePanel;
    public static event Action BackToAdvRequested;

    public SystemCommandHandler(
        Action<string> jumpToLabel,
        Image fadePanel)
    {
        this.jumpToLabel = jumpToLabel;
        this.fadePanel = fadePanel;
    }

    /// <summary>
    /// システム進行系コマンドなら実行してtrueを返します。
    /// 担当外のコマンドなら何もせずfalseを返します。
    /// </summary>
    public bool TryExecute(ScenarioRow row)
    {
        if (row == null)
            return false;

        switch (Normalize(row.Command))
        {
            case "Jump":
                ExecuteJump(row);
                return true;

            case "BackToADV":
                ExecuteBackToAdv();
                return true;

            default:
                return false;
        }
    }

    private void ExecuteJump(ScenarioRow row)
    {
        string targetLabel = Normalize(row.Arg1);

        if (string.IsNullOrEmpty(targetLabel))
        {
            Debug.LogWarning(
                "[UnisanChi] Jump先ラベルが指定されていません。");

            return;
        }

        if (jumpToLabel == null)
        {
            Debug.LogWarning(
                "[UnisanChi] Jumpを実行できません。" +
                "ラベル移動処理が設定されていません。");

            return;
        }

        jumpToLabel.Invoke(targetLabel);
    }

    private void ExecuteBackToAdv()
    {
        // 3D側で使用したFadeを透明へ戻してからADVへ復帰する
        if (fadePanel != null)
        {
            Color color = fadePanel.color;
            color.a = 0f;
            fadePanel.color = color;
        }

        Action handler = BackToAdvRequested;
        if (handler != null)
        {
            handler.Invoke();
            return;
        }

        Debug.LogWarning(
        "[unisanchan] BackToADVの受け取り先がありません。" +
        "宴へ戻す場合はu-tanのSceneFlowControllerを有効にしてください。");
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? ""
            : value.Trim();
    }
}