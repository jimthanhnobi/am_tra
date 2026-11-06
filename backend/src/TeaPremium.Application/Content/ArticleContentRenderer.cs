using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Ganss.Xss;

namespace TeaPremium.Application.Content;

public sealed class ArticleContentRenderer(HtmlSanitizer sanitizer)
{
    public string Render(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Length > 100_000)
            throw new ContentValidationException("Content JSON is required and must be at most 100 KB.");
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 32 });
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("type", out var type) ||
                type.ValueKind != JsonValueKind.String || type.GetString() != "doc")
                throw new ContentValidationException("Content must be a Tiptap document.");
            var html = new StringBuilder();
            var count = 0;
            AppendChildren(document.RootElement, html, ref count, 0);
            if (count > 2000 || html.Length > 200_000)
                throw new ContentValidationException("Content is too large.");
            // Render only known nodes, then sanitize again at the trust boundary.
            return sanitizer.Sanitize(html.ToString());
        }
        catch (JsonException)
        {
            throw new ContentValidationException("Content JSON is invalid.");
        }
    }

    private static void AppendChildren(JsonElement parent, StringBuilder html, ref int count, int depth)
    {
        if (depth > 32 || ++count > 2000)
            throw new ContentValidationException("Content is too complex.");
        if (!parent.TryGetProperty("content", out var children)) return;
        if (children.ValueKind != JsonValueKind.Array)
            throw new ContentValidationException("Content nodes must be an array.");
        foreach (var child in children.EnumerateArray()) AppendNode(child, html, ref count, depth + 1);
    }

    private static void AppendNode(JsonElement node, StringBuilder html, ref int count, int depth)
    {
        if (node.ValueKind != JsonValueKind.Object || !node.TryGetProperty("type", out var typeNode) ||
            typeNode.ValueKind != JsonValueKind.String)
            throw new ContentValidationException("Content node is invalid.");
        var type = typeNode.GetString();
        if (type == "text")
        {
            var value = node.TryGetProperty("text", out var text)
                ? text.ValueKind == JsonValueKind.String ? text.GetString() ?? "" : throw new ContentValidationException("Text node is invalid.")
                : "";
            if (value.Length > 20_000) throw new ContentValidationException("Text node is too long.");
            var marks = new List<(string Open, string Close)>();
            if (node.TryGetProperty("marks", out var markNodes) && markNodes.ValueKind == JsonValueKind.Array)
            {
                foreach (var mark in markNodes.EnumerateArray())
                {
                    if (mark.ValueKind != JsonValueKind.Object || !mark.TryGetProperty("type", out var markType) ||
                        markType.ValueKind != JsonValueKind.String) continue;
                    switch (markType.GetString())
                    {
                        case "bold": marks.Add(("<strong>", "</strong>")); break;
                        case "italic": marks.Add(("<em>", "</em>")); break;
                        case "strike": marks.Add(("<s>", "</s>")); break;
                        case "code": marks.Add(("<code>", "</code>")); break;
                        case "link":
                            if (mark.TryGetProperty("attrs", out var attrs) && attrs.ValueKind == JsonValueKind.Object &&
                                attrs.TryGetProperty("href", out var hrefNode) &&
                                hrefNode.ValueKind == JsonValueKind.String &&
                                Uri.TryCreate(hrefNode.GetString(), UriKind.Absolute, out var href) &&
                                href.Scheme is "https" or "http")
                                marks.Add(($"<a href=\"{HtmlEncoder.Default.Encode(href.ToString())}\" rel=\"nofollow noopener noreferrer\">", "</a>"));
                            break;
                    }
                }
            }
            foreach (var mark in marks) html.Append(mark.Open);
            html.Append(HtmlEncoder.Default.Encode(value));
            for (var i = marks.Count - 1; i >= 0; i--) html.Append(marks[i].Close);
            return;
        }
        if (type is "hardBreak" or "horizontalRule")
        { html.Append(type == "hardBreak" ? "<br>" : "<hr>"); return; }
        var tag = type switch
        {
            "paragraph" => "p", "bulletList" => "ul", "orderedList" => "ol",
            "listItem" => "li", "blockquote" => "blockquote", "codeBlock" => "pre",
            "heading" => HeadingTag(node), _ => null,
        };
        if (tag is null) return; // Unknown extensions never produce HTML.
        html.Append('<').Append(tag).Append('>');
        AppendChildren(node, html, ref count, depth);
        html.Append("</").Append(tag).Append('>');
    }

    private static string HeadingTag(JsonElement node)
    {
        if (node.TryGetProperty("attrs", out var attrs) && attrs.ValueKind == JsonValueKind.Object &&
            attrs.TryGetProperty("level", out var level) && level.ValueKind == JsonValueKind.Number &&
            level.TryGetInt32(out var number))
            return number switch { 2 => "h2", 3 => "h3", _ => "h2" };
        return "h2";
    }
}

public sealed class ContentValidationException(string message) : Exception(message);
public sealed class ContentNotFoundException(string message) : Exception(message);
public sealed class ContentConflictException(string message) : Exception(message);
