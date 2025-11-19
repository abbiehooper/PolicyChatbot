using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolicyChatbot.Shared;

public class ChatMessage
{
    public string Content { get; set; } = "";
    public bool IsUser { get; set; }
}
