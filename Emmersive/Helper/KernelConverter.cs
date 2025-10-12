using Cwl.Helper.String;
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

            using var sb = StringBuilderPool.Get();

            sb.Append(args["system_prompt"]!.ToString());

            // manual render filter
            foreach (var (k, v) in args) {
                sb.StringBuilder.Replace($"{{{{{k}}}}}", v!.ToString());
            }

            history.AddSystemMessage(sb.ToString());
            history.AddUserMessage(args["game_contexts"]!.ToString());

            return history;
        }

        // best not to use template rendering due to escape issue
        // also we'd prefer enforcing system prompt as its own role
        public string ToTemplate()
        {
            using var sb = StringBuilderPool.Get();

            sb.AppendLine("<message role=\"system\">");
            sb.AppendLine(args["system_prompt"]!.ToString());
            sb.AppendLine("</message>");
            sb.AppendLine("<message role=\"user\">");
            sb.AppendLine(args["game_contexts"]!.ToString());
            sb.AppendLine("</message>");

            return sb.ToString();
        }
    }
}