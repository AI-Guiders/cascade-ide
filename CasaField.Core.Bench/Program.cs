using System.Diagnostics;
using System.Globalization;
using System.Text;
using CasaField.Core;

var store = args.Length > 0
    ? args[0]
    : Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..",
        "..", "casa-ontology-payload", "examples", "agent-stores", "research-agent-lab-v0"));

var query = args.Length > 1 ? args[1] : "import knowledge delta";

if (!File.Exists(Path.Combine(store, "field_state.json")))
{
    Console.Error.WriteLine($"missing field_state in {store}");
    return 1;
}

var sw = Stopwatch.StartNew();
var first = CasaFieldHotPath.Run(store, query);
sw.Stop();
var coldMs = sw.Elapsed.TotalMilliseconds;

var times = new List<double>(30);
for (var i = 0; i < 30; i++)
{
    sw.Restart();
    _ = CasaFieldHotPath.Run(store, query);
    sw.Stop();
    times.Add(sw.Elapsed.TotalMilliseconds);
}

times.Sort();
var sb = new StringBuilder();
sb.AppendLine("{");
sb.AppendLine("  \"csharp\": {");
var inv = CultureInfo.InvariantCulture;
sb.AppendLine($"    \"cold_ms\": {coldMs.ToString("F3", inv)},");
sb.AppendLine($"    \"hot_loop_median_ms\": {times[times.Count / 2].ToString("F3", inv)},");
sb.AppendLine($"    \"hot_loop_p95_ms\": {times[(int)(times.Count * 0.95)].ToString("F3", inv)},");
sb.AppendLine($"    \"hot_loop_min_ms\": {times[0].ToString("F3", inv)},");
sb.AppendLine($"    \"first_field_version\": {first.FieldVersion},");
sb.AppendLine($"    \"first_stale\": {(first.Stale ? "true" : "false")},");
sb.AppendLine($"    \"first_concept_count\": {first.Decoded.ConceptIds.Count}");
sb.AppendLine("  }");
sb.AppendLine("}");
Console.WriteLine(sb.ToString());
return 0;
