namespace unisanchan
{
    public class CommandCompletionTicket
    {
        public bool IsCompleted { get; private set; }

        public void Complete()
        {
            IsCompleted = true;
        }
    }
}