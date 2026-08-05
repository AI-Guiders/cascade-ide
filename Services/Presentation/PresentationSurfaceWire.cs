#nullable enable
using System.Globalization;
using System.Text;

namespace CascadeIDE.Services.Presentation;

/// <summary>
/// Physical/scan packing role for a window slot.
/// <see cref="PmOneOf"/> = one physical window hosting P/M channel stack via <c>/</c>.
/// </summary>
public enum PresentationScanRole
{
    P,
    F,
    M,
    /// <summary>Shared P/M geography on one physical TopLevel (OneOf channel stack).</summary>
    PmOneOf,
}

/// <summary>One window slot: Scan Pattern role + surface/channel stack.</summary>
public sealed record PresentationScanSlot(
    PresentationScanRole Role,
    PresentationZoneCompose Compose,
    IReadOnlyList<string> Stack,
    string Active);

/// <summary>Surface-wire pack → Scan anchors + channel stacks (topology-oneof-slash-v1).</summary>
public sealed class PresentationSurfacePack
{
    PresentationSurfacePack(IReadOnlyList<PresentationScanSlot> slots, string? error)
    {
        Slots = slots;
        Error = error;
    }

    public IReadOnlyList<PresentationScanSlot> Slots { get; }
    public string? Error { get; }
    public bool IsSuccess => Error is null;

    public static PresentationSurfacePack Ok(IReadOnlyList<PresentationScanSlot> slots) => new(slots, null);
    public static PresentationSurfacePack Fail(string error) => new(Array.Empty<PresentationScanSlot>(), error);
}

/// <summary>
/// Parse surface topology e.g. <c>(intercom)(sit/world/alert)</c> → F=intercom, P/M=OneOf stack.
/// P|F|M tokens still accepted as legacy surface aliases. Scan labels ≠ physical monitors.
/// </summary>
public static class PresentationSurfaceWire
{
    public static PresentationSurfacePack Parse(string? wire)
    {
        if (string.IsNullOrWhiteSpace(wire))
            return PresentationSurfacePack.Ok(Array.Empty<PresentationScanSlot>());

        var groups = new List<(List<string> Stack, PresentationZoneCompose Compose)>();
        var text = wire.Trim();
        var i = 0;
        SkipWs(text, ref i);

        while (i < text.Length)
        {
            if (text[i] != '(')
                return PresentationSurfacePack.Fail($"Expected '(' at {i}.");

            i++;
            var start = i;
            while (i < text.Length && text[i] != ')')
                i++;
            if (i >= text.Length)
                return PresentationSurfacePack.Fail("Missing ')'.");

            var inner = CollapseWs(text.AsSpan(start, i - start));
            i++;
            var parsed = ParseGroup(inner);
            if (parsed.Error is { } err)
                return PresentationSurfacePack.Fail(err);
            groups.Add((parsed.Stack!, parsed.Compose));
            SkipWs(text, ref i);
        }

        return AssignScan(groups);
    }

    /// <summary>Compat: legacy <c>(F)(P/M)</c> speaks scan glyphs as surface aliases.</summary>
    public static PresentationSurfacePack FromLegacyMetaWire(PresentationParseResult parse)
    {
        if (!parse.IsSuccess)
            return PresentationSurfacePack.Fail(parse.Error ?? "legacy parse failed");

        var groups = new List<(List<string> Stack, PresentationZoneCompose Compose)>();
        for (var s = 0; s < parse.Screens.Count; s++)
        {
            var stack = new List<string>();
            foreach (var slot in parse.Screens[s])
            {
                var name = slot.Kind switch
                {
                    PresentationAnchorKind.Pfd => "p",
                    PresentationAnchorKind.Forward => "f",
                    PresentationAnchorKind.Mfd => "m",
                    _ => "?",
                };
                stack.Add(name);
            }

            var compose = s < parse.ScreenComposes.Count
                ? parse.ScreenComposes[s]
                : PresentationZoneCompose.Split;
            groups.Add((stack, compose));
        }

        return AssignScan(groups);
    }

    static PresentationSurfacePack AssignScan(List<(List<string> Stack, PresentationZoneCompose Compose)> groups)
    {
        if (groups.Count == 0)
            return PresentationSurfacePack.Ok(Array.Empty<PresentationScanSlot>());

        // Single TopLevel OneOf — e.g. (P/F/M) or (sit/world/alert). Slash = XOR on one window.
        // Not spatial (P+F+M); that stays legacy Split / 2|3 groups.
        if (groups.Count == 1)
        {
            var g = groups[0];
            if (g.Compose != PresentationZoneCompose.OneOf)
            {
                return PresentationSurfacePack.Fail(
                    "Single () group with '+' is spatial Split — use legacy (P+F+M) or 2|3 window groups; '/' for OneOf on one TopLevel.");
            }

            if (g.Stack.Count < 2)
                return PresentationSurfacePack.Fail("OneOf group needs ≥2 surfaces.");

            return PresentationSurfacePack.Ok(
            [
                new PresentationScanSlot(
                    PresentationScanRole.PmOneOf,
                    PresentationZoneCompose.OneOf,
                    g.Stack,
                    g.Stack[0])
            ]);
        }

        if (groups.Count == 2)
        {
            var a = groups[0];
            var b = groups[1];
            var aOne = a.Compose == PresentationZoneCompose.OneOf;
            var bOne = b.Compose == PresentationZoneCompose.OneOf;
            if (aOne == bOne)
                return PresentationSurfacePack.Fail("2-window pack needs one dedicated + one OneOf (/) group.");

            var ded = aOne ? b : a;
            if (ded.Stack.Count != 1)
                return PresentationSurfacePack.Fail("Dedicated scan slot must be a single surface.");

            // Window order preserved: dedicated → F (forward surface); OneOf → P/M stack.
            var slots = new PresentationScanSlot[2];
            for (var w = 0; w < 2; w++)
            {
                var g = groups[w];
                if (g.Compose == PresentationZoneCompose.OneOf)
                    slots[w] = new(PresentationScanRole.PmOneOf, PresentationZoneCompose.OneOf, g.Stack, g.Stack[0]);
                else
                    slots[w] = new(PresentationScanRole.F, PresentationZoneCompose.Split, g.Stack, g.Stack[0]);
            }

            return PresentationSurfacePack.Ok(slots);
        }

        if (groups.Count == 3)
        {
            if (groups.Any(g => g.Compose == PresentationZoneCompose.OneOf && g.Stack.Count < 2))
                return PresentationSurfacePack.Fail("OneOf group needs ≥2 surfaces.");

            var remaining = new HashSet<PresentationScanRole>
            {
                PresentationScanRole.P,
                PresentationScanRole.F,
                PresentationScanRole.M,
            };
            var assigned = new PresentationScanSlot?[3];

            // Prefer class of first surface; then fill leftovers left-to-right.
            for (var pass = 0; pass < 2; pass++)
            {
                for (var g = 0; g < 3; g++)
                {
                    if (assigned[g] is not null)
                        continue;
                    var role = PreferRole(groups[g].Stack[0]);
                    if (pass == 0 && !remaining.Contains(role))
                        continue;
                    if (pass == 1)
                    {
                        role = remaining.Contains(PresentationScanRole.F) ? PresentationScanRole.F
                            : remaining.Contains(PresentationScanRole.P) ? PresentationScanRole.P
                            : PresentationScanRole.M;
                    }

                    if (!remaining.Remove(role))
                        continue;

                    var stack = groups[g].Stack;
                    var compose = groups[g].Compose;
                    var scanRole = compose == PresentationZoneCompose.OneOf && stack.Count > 1
                        ? (role == PresentationScanRole.F ? PresentationScanRole.F : PresentationScanRole.PmOneOf)
                        : role;
                    // Three dedicated groups: keep P|F|M even if stack later grows; OneOf on non-F → PmOneOf only when that group is the shared P/M — for v1 keep per-seat role when dedicated.
                    if (compose == PresentationZoneCompose.Split || stack.Count == 1)
                        scanRole = role;

                    assigned[g] = new PresentationScanSlot(scanRole, compose, stack, stack[0]);
                }
            }

            if (assigned.Any(x => x is null))
                return PresentationSurfacePack.Fail("Could not assign P/F/M scan roles.");

            return PresentationSurfacePack.Ok(assigned.Select(x => x!).ToArray());
        }

        return PresentationSurfacePack.Fail(
            "Surface wire supports 1 OneOf group (single TopLevel), or 2|3 window groups (Scan Pattern pack).");
    }

    static PresentationScanRole PreferRole(string surface)
    {
        var s = Normalize(surface);
        if (IsForward(s))
            return PresentationScanRole.F;
        if (IsP(s))
            return PresentationScanRole.P;
        if (IsM(s))
            return PresentationScanRole.M;
        // Unknown / alert-only: default M stack (instruments) unless clearly forward.
        return s is "alert" or "ecl" or "eicas" ? PresentationScanRole.P : PresentationScanRole.M;
    }

    static bool IsForward(string s) =>
        s is "intercom" or "editor" or "work" or "f" or "forward" or "fwd";

    static bool IsP(string s) =>
        s is "sit" or "report" or "plan" or "p" or "pfd";

    static bool IsM(string s) =>
        s is "world" or "probe" or "shell" or "git" or "browser" or "mcp" or "m" or "mfd";

    static (List<string>? Stack, PresentationZoneCompose Compose, string? Error) ParseGroup(string inner)
    {
        if (inner.Length == 0)
            return (null, PresentationZoneCompose.Split, "Empty () group.");

        var stack = new List<string>();
        PresentationZoneCompose? compose = null;
        var i = 0;
        while (i < inner.Length)
        {
            if (!TryReadToken(inner, ref i, out var tok))
                return (null, PresentationZoneCompose.Split, $"Bad surface token near {i}.");
            stack.Add(Normalize(tok));

            if (i >= inner.Length)
                break;

            if (inner[i] == '/')
            {
                if (compose is PresentationZoneCompose.Split)
                    return (null, default, "Mixed '+' and '/' in one group.");
                compose = PresentationZoneCompose.OneOf;
                i++;
                continue;
            }

            if (inner[i] == '+')
            {
                if (compose is PresentationZoneCompose.OneOf)
                    return (null, default, "Mixed '+' and '/' in one group.");
                compose = PresentationZoneCompose.Split;
                i++;
                continue;
            }

            return (null, default, $"Expected '/' or '+' after '{tok}'.");
        }

        compose ??= PresentationZoneCompose.Split;
        if (compose == PresentationZoneCompose.OneOf && stack.Count < 2)
            return (null, default, "OneOf needs ≥2 surfaces.");
        return (stack, compose.Value, null);
    }

    static bool TryReadToken(string s, ref int i, out string tok)
    {
        var start = i;
        while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] is '_' or '-'))
            i++;
        if (i == start)
        {
            tok = "";
            return false;
        }

        tok = s[start..i];
        return true;
    }

    static string Normalize(string s) => s.Trim().ToLowerInvariant();

    static string CollapseWs(ReadOnlySpan<char> span)
    {
        var sb = new StringBuilder(span.Length);
        foreach (var c in span)
        {
            if (!char.IsWhiteSpace(c))
                sb.Append(c);
        }

        return sb.ToString();
    }

    static void SkipWs(string text, ref int i)
    {
        while (i < text.Length && char.IsWhiteSpace(text[i]))
            i++;
    }
}
