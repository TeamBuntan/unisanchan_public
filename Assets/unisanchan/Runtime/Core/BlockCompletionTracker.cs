using System.Collections.Generic;

namespace unisanchan
{
    public class BlockCompletionTracker
    {
        private readonly List<CommandCompletionTicket> tickets =
            new List<CommandCompletionTicket>();

        public void Add(CommandCompletionTicket ticket)
        {
            if (ticket == null)
                return;

            tickets.Add(ticket);
        }

        public bool IsCompleted
        {
            get
            {
                foreach (CommandCompletionTicket ticket in tickets)
                {
                    if (!ticket.IsCompleted)
                        return false;
                }

                return true;
            }
        }

        public int Count => tickets.Count;
    }
}