using System.Text;
using TreeSitter;

namespace SpFormatter;

public class AstInspector
{
    public static string FormatTreeStructure(Node node, int depth = 0)
    {
        var sb = new StringBuilder();
        AppendTreeStructure(sb, node, depth);
        return sb.ToString();
    }

    public static void PrintTreeStructure(Node node, int depth = 0)
    {
        Console.Write(FormatTreeStructure(node, depth));
    }

    private static void AppendTreeStructure(StringBuilder sb, Node node, int depth)
    {
        var indent = new string(' ', depth * 2);
        var nodeInfo = $"{node.Type}";

        if (!string.IsNullOrEmpty(node.Text.Trim()))
        {
            var text = node.Text.Replace("\n", "\\n").Replace("\r", "\\r");
            if (text.Length > 50)
                text = text[..47] + "...";
            nodeInfo += $" '{text}'";
        }

        if (node.IsError)
            nodeInfo += " [ERROR]";
        if (node.IsMissing)
            nodeInfo += " [MISSING]";

        sb.Append(indent).AppendLine(nodeInfo);

        foreach (var child in node.Children)
            AppendTreeStructure(sb, child, depth + 1);
    }

    public static void PrintNamedNodesOnly(Node node, int depth = 0)
    {
        if (node.IsNamed)
        {
            var indent = new string(' ', depth * 2);
            Console.WriteLine($"{indent}{node.Type}");
        }
        
        foreach (var child in node.Children)
        {
            PrintNamedNodesOnly(child, node.IsNamed ? depth + 1 : depth);
        }
    }

    public static List<Node> FindNodesByType(Node root, string nodeType)
    {
        var results = new List<Node>();
        FindNodesByTypeRecursive(root, nodeType, results);
        return results;
    }

    private static void FindNodesByTypeRecursive(Node node, string nodeType, List<Node> results)
    {
        if (node.Type == nodeType)
        {
            results.Add(node);
        }

        foreach (var child in node.Children)
        {
            FindNodesByTypeRecursive(child, nodeType, results);
        }
    }

    public static Dictionary<string, int> GetNodeTypeFrequency(Node root)
    {
        var frequency = new Dictionary<string, int>();
        CountNodeTypesRecursive(root, frequency);
        return frequency.OrderByDescending(kvp => kvp.Value).ToDictionary();
    }

    private static void CountNodeTypesRecursive(Node node, Dictionary<string, int> frequency)
    {
        frequency[node.Type] = frequency.GetValueOrDefault(node.Type, 0) + 1;
        
        foreach (var child in node.Children)
        {
            CountNodeTypesRecursive(child, frequency);
        }
    }
}
