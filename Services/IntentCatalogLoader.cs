#nullable enable
using CascadeIDE.Features.Chat;
using CascadeIDE.Models.Intercom;

namespace CascadeIDE.Services;

/// <summary>Сборка <see cref="IntentMelodyCatalogSnapshot"/> из TOML (<c>[[command]]</c>).</summary>
internal static class IntentCatalogLoader
{
    internal static IntentMelodyCatalogSnapshot BuildSnapshot(
        IntentMelodyAliases.IntentMelodyTomlRoot root,
        string bundledRelativePath)
    {
        var wireClasses = LoadTailWireClassTable(root, bundledRelativePath);

        if (root.Command is not { Count: > 0 })
        {
            throw new InvalidOperationException(
                $"{bundledRelativePath}: ожидается intent-catalog с [[command]] (legacy melody_root/slash_route удалены).");
        }

        return BuildFromCommands(root, wireClasses, bundledRelativePath);
    }

    internal static Dictionary<string, string> BuildMelodyAliasMap(IntentMelodyCatalogSnapshot catalog)
    {
        var commandMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in catalog.Roots.Values.OrderBy(e => e.Slug, StringComparer.Ordinal))
        {
            if (!string.IsNullOrWhiteSpace(e.CommandId))
                commandMap[e.Slug] = e.CommandId;
        }

        return commandMap;
    }

    private static IntentMelodyCatalogSnapshot BuildFromCommands(
        IntentMelodyAliases.IntentMelodyTomlRoot root,
        Dictionary<string, TailWireClassEntry> wireClasses,
        string path)
    {
        var merged = new Dictionary<string, MelodyRootEntry>(StringComparer.OrdinalIgnoreCase);
        var slashRoutes = new Dictionary<string, SlashRouteEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var block in root.Command!)
        {
            if (block.Enabled == false)
                continue;

            var cmdId = block.CommandId?.Trim() ?? "";
            var defaultSlashGroup = NormOptional(block.SlashGroup);

            if (TryResolveMelodyForm(block, out var melodyForm))
                AddMelodyForm(merged, melodyForm, cmdId, path);

            foreach (var slash in EnumerateSlashForms(block))
            {
                AddSlashForm(
                    slashRoutes,
                    slash,
                    cmdId,
                    path,
                    defaultSlashGroup);
            }
        }

        ValidateMelodyCatalog(merged, wireClasses, path);

        if (merged.Count == 0 && slashRoutes.Count == 0)
        {
            throw new InvalidOperationException(
                $"{path}: нет ни одной активной формы в [[command]] (melody или slash).");
        }

        return new IntentMelodyCatalogSnapshot(merged, wireClasses, slashRoutes);
    }

    private static IEnumerable<SlashFormToml> EnumerateSlashForms(CommandToml block)
    {
        foreach (var list in new[]
                 {
                     block.Form?.Slash,
                     block.Slash,
                 })
        {
            if (list is null)
                continue;

            foreach (var row in list)
            {
                if (row.Enabled != false)
                    yield return row;
            }
        }
    }

    private static bool TryResolveMelodyForm(CommandToml block, out MelodyFormToml form)
    {
        if (!string.IsNullOrWhiteSpace(block.MelodySlug))
        {
            form = new MelodyFormToml
            {
                Slug = block.MelodySlug,
                Shape = block.MelodyShape,
                ShowUsageHintIfBareSlug = block.MelodyShowUsageHintIfBareSlug,
                TailSignature = block.MelodyTailSignature,
                WireClass = block.MelodyWireClass,
                ChordCommit = block.MelodyChordCommit,
                PaletteHintSlug = block.MelodyPaletteHintSlug,
                PaletteUsageHint = block.MelodyPaletteUsageHint,
                PaletteUsageCategory = block.MelodyPaletteUsageCategory,
            };
            return true;
        }

        if (block.Form?.Melody is { } formMelody && !string.IsNullOrWhiteSpace(formMelody.Slug))
        {
            form = formMelody;
            return true;
        }

        if (block.Melody is { } nested && !string.IsNullOrWhiteSpace(nested.Slug))
        {
            form = nested;
            return true;
        }

        form = null!;
        return false;
    }

    private static void AddMelodyForm(
        Dictionary<string, MelodyRootEntry> merged,
        MelodyFormToml row,
        string commandIdFromBlock,
        string path)
    {
        var slug = row.Slug?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(slug))
            return;

        if (merged.TryGetValue(slug, out var existing))
        {
            if (!string.Equals(existing.CommandId, commandIdFromBlock, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"{path}: slug '{slug}' уже привязан к command_id '{existing.CommandId}', повтор: '{commandIdFromBlock}'.");
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(commandIdFromBlock))
        {
            throw new InvalidOperationException(
                $"{path}: [[command]] с melody slug '{slug}' требует command_id.");
        }

        var shape = ShapeFromRow(row, path);
        var showUsageHintIfBareSlug = row.ShowUsageHintIfBareSlug ?? InferShowUsageHintIfBareSlug(shape, row.TailSignature);

        merged[slug] = new MelodyRootEntry(
            slug,
            commandIdFromBlock,
            shape,
            showUsageHintIfBareSlug,
            NormOptional(row.TailSignature),
            NormOptional(row.WireClass),
            NormOptional(row.ChordCommit),
            NormOptional(row.PaletteHintSlug),
            NormOptional(row.PaletteUsageHint),
            NormOptional(row.PaletteUsageCategory));
    }

    private static void AddSlashForm(
        Dictionary<string, SlashRouteEntry> routes,
        SlashFormToml row,
        string commandIdFromBlock,
        string path,
        string? defaultSlashGroup)
    {
        var slashPath = IntentSlashCatalog.NormalizeSlashPath(row.Path);
        if (slashPath.Length < 2)
            return;

        if (routes.ContainsKey(slashPath))
        {
            throw new InvalidOperationException(
                $"{path}: duplicate slash path '{slashPath}'.");
        }

        var help = row.Help?.Trim() ?? "";
        if (help.Length == 0)
        {
            throw new InvalidOperationException(
                $"{path}: slash '{slashPath}' requires help.");
        }

        var kind = ParseSlashKind(row.Kind, path);
        var cmdId = commandIdFromBlock;
        if (kind == ChatSlashCommandExecutionKind.IdeCommand && cmdId.Length == 0)
        {
            throw new InvalidOperationException(
                $"{path}: slash '{slashPath}' requires command_id on [[command]].");
        }

        if ((kind is ChatSlashCommandExecutionKind.LocalHelp
                or ChatSlashCommandExecutionKind.LocalReport
                or ChatSlashCommandExecutionKind.LocalIntercom
                or ChatSlashCommandExecutionKind.LocalAgent)
            && cmdId.Length > 0)
        {
            throw new InvalidOperationException(
                $"{path}: slash '{slashPath}' kind={row.Kind?.Trim().ToLowerInvariant()} must not set command_id.");
        }

        var group = NormOptional(row.Group) ?? defaultSlashGroup;
        ResolveSlashStaticArgs(row, out var mfdPage, out var primarySurface, out var mapLevel);
        var completion = ParseSlashCompletion(row.Completion, path, slashPath);
        var reportHandler = ResolveReportHandler(row, kind, path, slashPath);
        var intercomHandler = ResolveIntercomHandler(row, kind, path, slashPath);
        var audience = ParseSlashAudience(row.Audience, path, slashPath);
        var autoRunOnCommit = row.AutoRunOnCommit ?? false;
        var autoRunRequiresArgs = row.AutoRunRequiresArgs ?? true;
        var argTailKindExplicit = ParseSlashArgTail(row.ArgTail, path, slashPath);

        routes[slashPath] = new SlashRouteEntry(
            slashPath,
            cmdId,
            help,
            kind,
            mfdPage,
            primarySurface,
            mapLevel,
            group,
            completion,
            reportHandler,
            intercomHandler,
            audience,
            autoRunOnCommit,
            autoRunRequiresArgs,
            argTailKindExplicit);
    }

    private static SlashArgTailKind? ParseSlashArgTail(string? argTail, string path, string slashPath)
    {
        if (string.IsNullOrWhiteSpace(argTail))
            return null;

        return argTail.Trim().ToLowerInvariant() switch
        {
            "none" => SlashArgTailKind.None,
            "optional" => SlashArgTailKind.Optional,
            "required" => SlashArgTailKind.Required,
            _ => throw new InvalidOperationException(
                $"{path}: slash '{slashPath}' arg_tail must be none|optional|required, got '{argTail}'."),
        };
    }

    private static IntercomMessageAudience ParseSlashAudience(string? raw, string path, string slashPath)
    {
        // ADR 0119 §7: слэш-команды local — не расширяют промпт агента; channel только явно в TOML.
        if (string.IsNullOrWhiteSpace(raw))
            return IntercomMessageAudience.SelfOnly;

        return raw.Trim().ToLowerInvariant() switch
        {
            "channel" => IntercomMessageAudience.Channel,
            "self" or "self_only" or "local" => IntercomMessageAudience.SelfOnly,
            _ => throw new InvalidOperationException(
                $"{path}: slash '{slashPath}' unknown audience '{raw}' (channel | self)."),
        };
    }

    private static string? ResolveReportHandler(
        SlashFormToml row,
        ChatSlashCommandExecutionKind kind,
        string path,
        string slashPath)
    {
        var handler = NormOptional(row.ReportHandler);
        if (kind == ChatSlashCommandExecutionKind.LocalReport)
        {
            if (handler is null)
            {
                throw new InvalidOperationException(
                    $"{path}: slash '{slashPath}' kind=report requires report_handler.");
            }

            if (!ChatSlashReportHandlers.IsKnown(handler))
            {
                throw new InvalidOperationException(
                    $"{path}: slash '{slashPath}' unknown report_handler '{handler}'.");
            }

            return handler;
        }

        if (handler is not null)
        {
            throw new InvalidOperationException(
                $"{path}: slash '{slashPath}' report_handler is only allowed for kind=report.");
        }

        return null;
    }

    private static string? ResolveIntercomHandler(
        SlashFormToml row,
        ChatSlashCommandExecutionKind kind,
        string path,
        string slashPath)
    {
        var handler = NormOptional(row.IntercomHandler);
        if (kind == ChatSlashCommandExecutionKind.LocalIntercom)
        {
            if (handler is null)
            {
                throw new InvalidOperationException(
                    $"{path}: slash '{slashPath}' kind=intercom requires intercom_handler.");
            }

            if (!ChatSlashIntercomHandlers.IsKnown(handler))
            {
                throw new InvalidOperationException(
                    $"{path}: slash '{slashPath}' unknown intercom_handler '{handler}'.");
            }

            return handler;
        }

        if (handler is not null)
        {
            throw new InvalidOperationException(
                $"{path}: slash '{slashPath}' intercom_handler is only allowed for kind=intercom.");
        }

        return null;
    }

    private static SlashCompletionKind ParseSlashCompletion(string? raw, string path, string slashPath)
    {
        var v = NormOptional(raw);
        if (v is null)
            return SlashCompletionKind.None;

        if (string.Equals(v, "workspace_files", StringComparison.OrdinalIgnoreCase))
            return SlashCompletionKind.WorkspaceFiles;

        if (string.Equals(v, "session_topics", StringComparison.OrdinalIgnoreCase))
            return SlashCompletionKind.SessionTopics;

        if (string.Equals(v, "message_anchors", StringComparison.OrdinalIgnoreCase))
            return SlashCompletionKind.MessageAnchors;

        throw new InvalidOperationException(
            $"{path}: slash '{slashPath}' has unknown completion '{v}' (expected workspace_files | session_topics | message_anchors).");
    }

    private static void ResolveSlashStaticArgs(
        SlashFormToml row,
        out string? mfdPage,
        out string? primarySurface,
        out string? mapLevel)
    {
        mfdPage = NormOptional(row.MfdPage) ?? NormOptional(row.Args?.Page);
        primarySurface = NormOptional(row.PrimarySurface) ?? NormOptional(row.Args?.Surface);
        mapLevel = NormOptional(row.Args?.Level);
    }

    private static void ValidateMelodyCatalog(
        Dictionary<string, MelodyRootEntry> merged,
        Dictionary<string, TailWireClassEntry> wireClasses,
        string path)
    {
        foreach (var e in merged.Values.OrderBy(e => e.Slug, StringComparer.Ordinal))
            ValidateChordCommitField(e.Slug, e.ChordCommit, e.Shape, path);

        ValidateParametricWireBindings(merged, wireClasses, path);
    }

    private static string? NormOptional(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static IntentMelodyShape ShapeFromRow(MelodyFormToml row, string path) =>
        string.IsNullOrWhiteSpace(row.Shape)
            ? (string.IsNullOrWhiteSpace(row.TailSignature)
                ? IntentMelodyShape.Simple
                : IntentMelodyShape.Parametric)
            : ParseShapeMandatory(row.Shape!, path);

    private static IntentMelodyShape ParseShapeMandatory(string raw, string path)
    {
        var x = raw.Trim();
        if (string.Equals(x, "simple", StringComparison.OrdinalIgnoreCase))
            return IntentMelodyShape.Simple;
        if (string.Equals(x, "parametric", StringComparison.OrdinalIgnoreCase))
            return IntentMelodyShape.Parametric;
        throw new InvalidOperationException($"{path}: unknown melody shape '{raw}'.");
    }

    private static bool InferShowUsageHintIfBareSlug(IntentMelodyShape shape, string? tailSignatureRaw)
    {
        if (shape != IntentMelodyShape.Parametric)
            return false;
        var ts = tailSignatureRaw?.Trim();
        if (string.IsNullOrEmpty(ts))
            return false;
        if (IntentMelodyTailSemantics.HasUrlSlot(ts))
            return false;
        return IntentMelodyTailSemantics.CountDelimitedNumericSlots(ts) >= 2;
    }

    private static ChatSlashCommandExecutionKind ParseSlashKind(string? raw, string path)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return ChatSlashCommandExecutionKind.IdeCommand;

        return raw.Trim().ToLowerInvariant() switch
        {
            "help" => ChatSlashCommandExecutionKind.LocalHelp,
            "report" => ChatSlashCommandExecutionKind.LocalReport,
            "intercom" => ChatSlashCommandExecutionKind.LocalIntercom,
            "agent" => ChatSlashCommandExecutionKind.LocalAgent,
            "ide" or "command" => ChatSlashCommandExecutionKind.IdeCommand,
            _ => throw new InvalidOperationException(
                $"{path}: slash unknown kind '{raw}' (help | report | intercom | agent | ide)."),
        };
    }

    private static Dictionary<string, TailWireClassEntry> LoadTailWireClassTable(
        IntentMelodyAliases.IntentMelodyTomlRoot tomlRoot,
        string path)
    {
        var tables = new Dictionary<string, TailWireClassEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in tomlRoot.TailWireClass ?? [])
        {
            var idNorm = row.Id?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(idNorm))
                continue;
            if (tables.ContainsKey(idNorm))
            {
                throw new InvalidOperationException(
                    $"{path}: duplicate [[tail_wire_class]] id '{idNorm}'.");
            }

            var kind =
                (row.Kind ?? "").Trim().ToLowerInvariant() switch
                {
                    "single_remainder" => TailWireKind.SingleRemainder,
                    "delimited_slots" => TailWireKind.DelimitedSlots,
                    _ => throw new InvalidOperationException(
                        $"{path}: [[tail_wire_class]] '{idNorm}' unknown kind '{row.Kind}'.")
                };

            var seps = NormalizeBetweenSlots(kind, row.BetweenSlotsAnyOf ?? [], idNorm, path);
            tables[idNorm] = new TailWireClassEntry(idNorm, kind, seps);
        }

        return tables;

        static string[] NormalizeBetweenSlots(TailWireKind kind, string[] rawArr, string idNorm, string path)
        {
            if (kind == TailWireKind.SingleRemainder)
                return [];

            if (rawArr.Length == 0)
            {
                throw new InvalidOperationException(
                    $"{path}: [[tail_wire_class]] '{idNorm}' DelimitedSlots requires between_slots_any_of.");
            }

            List<string> acc = [];
            foreach (var s in rawArr)
            {
                if (string.IsNullOrEmpty(s))
                {
                    throw new InvalidOperationException(
                        $"{path}: [[tail_wire_class]] '{idNorm}' empty separator entry.");
                }

                if (s.Length != 1)
                {
                    throw new InvalidOperationException(
                        $"{path}: [[tail_wire_class]] '{idNorm}' separator '{s}' must be a single character.");
                }

                acc.Add(s);
            }

            acc.Sort(StringComparer.Ordinal);
            for (var i = 1; i < acc.Count; i++)
            {
                if (!string.Equals(acc[i], acc[i - 1], StringComparison.Ordinal))
                    continue;
                acc.RemoveAt(i);
                i--;
            }

            return acc.ToArray();
        }
    }

    private static void ValidateChordCommitField(string slug, string? chordCommit, IntentMelodyShape shape, string path)
    {
        if (string.IsNullOrWhiteSpace(chordCommit))
            return;

        var cc = chordCommit.Trim().ToLowerInvariant();
        if (cc != "enter" && cc != "immediate" && cc != "instant")
        {
            throw new InvalidOperationException(
                $"{path}: melody '{slug}' unknown chord_commit '{chordCommit}' (enter, immediate, instant).");
        }

        if (shape == IntentMelodyShape.Simple && cc != "enter")
        {
            throw new InvalidOperationException(
                $"{path}: melody '{slug}' shape simple — chord_commit пустой или enter.");
        }
    }

    private static void ValidateParametricWireBindings(
        IReadOnlyDictionary<string, MelodyRootEntry> roots,
        Dictionary<string, TailWireClassEntry> wireClasses,
        string path)
    {
        foreach (var e in roots.Values.OrderBy(x => x.Slug, StringComparer.Ordinal))
        {
            if (e.Shape != IntentMelodyShape.Parametric)
                continue;

            var hasTail = !string.IsNullOrWhiteSpace(e.TailSignature);
            var wireRef = string.IsNullOrWhiteSpace(e.WireClass) ? null : e.WireClass.Trim().ToLowerInvariant();
            if (hasTail && wireRef is null)
            {
                throw new InvalidOperationException(
                    $"{path}: parametric melody '{e.Slug}' с tail_signature требует wire_class.");
            }

            if (wireRef is null)
                continue;

            if (!wireClasses.TryGetValue(wireRef, out var row))
            {
                throw new InvalidOperationException(
                    $"{path}: unknown wire_class '{e.WireClass}' for melody '{e.Slug}'.");
            }

            IntentMelodyTailSemantics.ValidateMelodyAgainstWireClass(e, row);
        }
    }
}
