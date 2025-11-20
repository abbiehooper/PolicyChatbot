using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolicyChatbot.Shared.Models;

public class ChatRequest
{
    public string ProductId { get; set; } = "";
    public string Question { get; set; } = "";
}
