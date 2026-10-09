using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Emmersive.Helper;

public static class KernelConverter
{
    extension(KernelArguments args)
    {
        public ChatHistory ToHistory()
        {
            ChatHistory history = [];

            var sb = new StringBuilder();

            sb.Append(args["system_prompt"]!);

            // manual render filter
            foreach (var (k, v) in args) {
                sb.Replace($"{{{{{k}}}}}", v!.ToString());
            }

            history.AddSystemMessage(sb.ToString());
            history.AddUserMessage(args["game_contexts"]!.ToString());

            return history;
        }
    }
}