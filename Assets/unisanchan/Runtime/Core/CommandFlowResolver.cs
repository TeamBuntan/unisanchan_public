namespace unisanchan
{
    public static class CommandFlowResolver
    {
        public static CommandFlowType GetFlowType(string command)
        {
            switch (Normalize(command))
            {
                // 実行後、完了待ちや入力待ちをせず、そのままBlockの進行を続ける。
                case "stopvoice":
                case "tips":
                case "bgm":
                case "stopbgm":
                case "ambience":
                case "stopambience":
                case "se":
                case "stopse":
                case "particle":
                case "stopparticle":
                case "setactive":
                case "changevalue":
                case "savepoint":
                case "autosave":
                    return CommandFlowType.PassThrough;

                // ユーザー入力や選択結果など、外部からの解除を待ってBlockの進行を止める。
                case "dialog":
                case "choice":
                case "modal":
                    return CommandFlowType.Hold;

                // 開始した処理が完了するまで、Blockの進行を止める。
                case "animation":
                case "face":
                case "facereset":
                case "camera":
                case "fadein":
                case "fadeout":
                case "wait":
                case "move":
                    return CommandFlowType.CompletionBarrier;

                // 現在のシナリオ位置を変更し、通常とは異なる進行先へ切り替える。
                case "jump":
                case "backtoadv":
                    return CommandFlowType.FlowControl;

                default:
                    // 未知Commandを勝手に通過させるのは危険なので、
                    // 安全側に倒してHold扱いにする。
                    return CommandFlowType.Hold;
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? ""
                : value.Trim().ToLowerInvariant();
        }
    }
}