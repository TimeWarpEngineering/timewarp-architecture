#!/usr/bin/dotnet --
#:property ImplicitUsings=enable
#:property Nullable=enable
#:property LangVersion=latest
#:property JsonSerializerIsReflectionEnabledByDefault=true
#:property PublishAot=false

#region Purpose
// Kitchen spike: deterministic ActionSet shortlist, then dry-run or live Jev Choice + is-command Noul.
#endregion
#region Design
// Raw HttpClient + kitchen OpenAPI snapshot (taratibu 014 client path). Not a SPA provider, not a TimeWarp SDK.
// Live numbers write to gitignored live-results.json (TypeSafe MCA §2.3(f)).
// TYPESAFE_API_KEY → api.typesafe.ai; else OPENROUTER_API_KEY → OpenRouter Decisions alpha.
// Skill-suggestion shape: code shortlist of {name, description} → Choice over names → Noul "is this a command?"
// Optional per-candidate Nouls on the top 3. Confidence is log-only; auto-dispatch vs show-shortlist stay in code.
// Choice max 255 — this spike shortlists 8 from the opt-in catalog, never every IAction.
// JsonNode only — repo/runfile AOT settings disable reflection System.Text.Json.
#endregion

using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

const int ShortlistSize = 8;
const double GateNoulThreshold = 0.3;
const double FitsNoulThreshold = 0.3;
const double ShowListConfidence = 0.5;
const string NoneOption = "none_of_the_above";

string researchDirectory = Directory.GetCurrentDirectory();
if (!File.Exists(Path.Combine(researchDirectory, "corpus.json")))
{
  string? fromArgs = Path.GetDirectoryName(Path.GetFullPath(Environment.GetCommandLineArgs()[0]));
  if (fromArgs is not null && File.Exists(Path.Combine(fromArgs, "corpus.json")))
  {
    researchDirectory = fromArgs;
  }
}

if (args.Contains("--help", StringComparer.Ordinal))
{
  WriteHelp();
  return 0;
}

bool live = args.Contains("--live", StringComparer.Ordinal);
JsonObject catalog = LoadObject(Path.Combine(researchDirectory, "catalog.json"), "catalog.json");
JsonObject corpus = LoadObject(Path.Combine(researchDirectory, "corpus.json"), "corpus.json");
List<CatalogEntry> catalogEntries = LoadCatalog(catalog);
ValidateCorpus(corpus, catalogEntries);

if (!live)
{
  DryRun(corpus, catalogEntries, researchDirectory);
  Console.WriteLine("Dry-run complete. Pass --live with TYPESAFE_API_KEY or OPENROUTER_API_KEY to call Jev.");
  return 0;
}

AccessPlan? access = ResolveAccess();
if (access is null)
{
  Console.Error.WriteLine(
    "BLOCKER: no TypeSafe or gateway key in this environment. " +
    "Set TYPESAFE_API_KEY (console.typesafe.ai) or OPENROUTER_API_KEY " +
    "(OpenRouter Decisions POST /api/alpha/decisions, model ~typesafe/jev-latest). " +
    "No Vercel/Cloudflare AI Gateway key was found either. Do not fake live results.");
  return 2;
}

await RunLiveAsync(corpus, catalogEntries, access, researchDirectory).ConfigureAwait(false);
return 0;

static void WriteHelp()
{
  Console.WriteLine(
    """
    run-rank-experiment.cs
      (default)  Validate corpus, score C# shortlist, emit dry-run System One request shapes. No network.
      --live     POST each item to TypeSafe or OpenRouter. Requires an API key.
      --help     This text.

    Keys: TYPESAFE_API_KEY (preferred) or OPENROUTER_API_KEY.
    Live output: live-results.json (gitignored).
    """);
}

static JsonObject LoadObject(string path, string label)
{
  if (!File.Exists(path))
  {
    throw new FileNotFoundException($"{label} not found", path);
  }

  JsonNode? node = JsonNode.Parse(File.ReadAllText(path));
  if (node is not JsonObject loaded)
  {
    throw new InvalidOperationException($"{label} is not an object.");
  }

  return loaded;
}

static JsonArray RequireArray(JsonObject root, string name, string label)
{
  return root[name] as JsonArray
    ?? throw new InvalidOperationException($"{label} missing {name} array.");
}

static JsonObject RequireItem(JsonNode? node, string label)
{
  return node as JsonObject ?? throw new InvalidOperationException($"{label} item is not an object.");
}

static string Text(JsonObject item, string name)
{
  return item[name]?.GetValue<string>() ?? "";
}

static List<CatalogEntry> LoadCatalog(JsonObject catalog)
{
  JsonArray items = RequireArray(catalog, "items", "catalog.json");
  List<CatalogEntry> entries = [];
  HashSet<string> names = [];
  foreach (JsonNode? node in items)
  {
    JsonObject item = RequireItem(node, "catalog");
    string name = Text(item, "name");
    string description = Text(item, "description");
    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description))
    {
      throw new InvalidOperationException("catalog item missing name or description.");
    }

    if (!names.Add(name))
    {
      throw new InvalidOperationException($"Duplicate catalog name '{name}'.");
    }

    entries.Add(new CatalogEntry(name, description));
  }

  if (entries.Count == 0)
  {
    throw new InvalidOperationException("catalog.json has no items.");
  }

  if (entries.Count > 255)
  {
    throw new InvalidOperationException("catalog exceeds Choice max 255; shortlist first.");
  }

  return entries;
}

static void ValidateCorpus(JsonObject corpus, List<CatalogEntry> catalogEntries)
{
  HashSet<string> catalogNames = catalogEntries.Select(static e => e.Name).ToHashSet(StringComparer.Ordinal);
  JsonArray items = RequireArray(corpus, "items", "corpus.json");
  if (items.Count < 30)
  {
    throw new InvalidOperationException($"corpus.json has {items.Count} items; need at least 30 labeled phrases.");
  }

  int commands = 0;
  int negatives = 0;
  HashSet<string> labeledCommands = [];
  foreach (JsonNode? node in items)
  {
    JsonObject item = RequireItem(node, "corpus");
    string id = Text(item, "id");
    string phrase = Text(item, "phrase");
    string labeledArm = Text(item, "labeled_arm");
    if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(phrase))
    {
      throw new InvalidOperationException("corpus item missing id or phrase.");
    }

    if (labeledArm == "none")
    {
      negatives++;
      continue;
    }

    if (!catalogNames.Contains(labeledArm))
    {
      throw new InvalidOperationException($"Item '{id}' labeled_arm '{labeledArm}' is not in the catalog.");
    }

    commands++;
    labeledCommands.Add(labeledArm);
  }

  if (negatives < 6)
  {
    throw new InvalidOperationException("corpus needs at least 6 'just looking' / navigate / question negatives.");
  }

  if (commands < 16)
  {
    throw new InvalidOperationException("corpus needs at least 16 command phrases.");
  }

  if (labeledCommands.Count < 12)
  {
    throw new InvalidOperationException("corpus command labels cover too few catalog entries.");
  }
}

static void DryRun(JsonObject corpus, List<CatalogEntry> catalogEntries, string researchDirectory)
{
  JsonArray rows = [];
  JsonArray requests = [];
  int commandCount = 0;
  int negativeCount = 0;
  int shortlistHit = 0;
  int shortlistTop1 = 0;
  int shortlistMiss = 0;
  int previewAuto = 0;
  int previewShowList = 0;
  int previewNone = 0;
  int previewMiss = 0;

  foreach (JsonNode? node in RequireArray(corpus, "items", "corpus.json"))
  {
    JsonObject item = RequireItem(node, "corpus");
    string id = Text(item, "id");
    string phrase = Text(item, "phrase");
    string labeledArm = Text(item, "labeled_arm");
    List<ScoredEntry> shortlist = Shortlist(phrase, catalogEntries);
    JsonObject request = BuildRequest(phrase, shortlist);
    requests.Add(
      new JsonObject
      {
        ["id"] = id,
        ["phrase"] = phrase,
        ["labeled_arm"] = labeledArm,
        ["intent"] = Text(item, "intent"),
        ["request"] = request.DeepClone()
      });

    bool goldInShortlist = shortlist.Any(s => s.Entry.Name == labeledArm);
    bool goldTop1 = shortlist.Count > 0 && shortlist[0].Entry.Name == labeledArm;
    int topScore = shortlist.Count > 0 ? shortlist[0].Score : 0;
    int secondScore = shortlist.Count > 1 ? shortlist[1].Score : 0;
    string preview;
    if (labeledArm == "none")
    {
      negativeCount++;
      preview = topScore < 2 ? "none" : "show-shortlist";
      if (preview == "none")
      {
        previewNone++;
      }
      else
      {
        previewShowList++;
      }
    }
    else
    {
      commandCount++;
      if (goldInShortlist)
      {
        shortlistHit++;
      }
      else
      {
        shortlistMiss++;
      }

      if (goldTop1)
      {
        shortlistTop1++;
      }

      if (goldTop1 && topScore - secondScore >= 2)
      {
        preview = "auto-dispatch";
        previewAuto++;
      }
      else if (goldInShortlist)
      {
        preview = "show-shortlist";
        previewShowList++;
      }
      else
      {
        preview = "miss";
        previewMiss++;
      }
    }

    JsonArray shortlistJson = [];
    foreach (ScoredEntry scored in shortlist)
    {
      shortlistJson.Add(
        new JsonObject
        {
          ["name"] = scored.Entry.Name,
          ["description"] = scored.Entry.Description,
          ["score"] = scored.Score
        });
    }

    rows.Add(
      new JsonObject
      {
        ["id"] = id,
        ["phrase"] = phrase,
        ["labeled_arm"] = labeledArm,
        ["intent"] = Text(item, "intent"),
        ["shortlist_hit"] = labeledArm != "none" && goldInShortlist,
        ["shortlist_top1"] = labeledArm != "none" && goldTop1,
        ["deterministic_preview"] = preview,
        ["shortlist"] = shortlistJson
      });
  }

  JsonObject summary = new()
  {
    ["mode"] = "dry-run",
    ["item_count"] = rows.Count,
    ["command_count"] = commandCount,
    ["negative_count"] = negativeCount,
    ["catalog_count"] = catalogEntries.Count,
    ["shortlist_size"] = ShortlistSize,
    ["choice_max"] = 255,
    ["gate_noul_threshold"] = GateNoulThreshold,
    ["fits_noul_threshold"] = FitsNoulThreshold,
    ["choice_confidence_show_list"] = ShowListConfidence,
    ["shortlist_hit"] = shortlistHit,
    ["shortlist_top1"] = shortlistTop1,
    ["shortlist_miss"] = shortlistMiss,
    ["deterministic_preview_auto_dispatch"] = previewAuto,
    ["deterministic_preview_show_shortlist"] = previewShowList,
    ["deterministic_preview_none"] = previewNone,
    ["deterministic_preview_miss"] = previewMiss,
    ["note"] = "deterministic_preview is C# shortlist only, not Jev. Live Jev Choice + Noul is --live.",
    ["rows"] = rows,
    ["requests"] = requests
  };

  string outputPath = Path.Combine(researchDirectory, "dry-run-requests.json");
  File.WriteAllText(outputPath, summary.ToJsonString(WriteJsonOptions()));
  Console.WriteLine($"Wrote {outputPath}");
  Console.WriteLine($"Items: {rows.Count} (commands={commandCount} negatives={negativeCount})");
  Console.WriteLine($"Catalog: {catalogEntries.Count} opt-in ActionSets; shortlist={ShortlistSize} (Choice max 255)");
  Console.WriteLine($"Shortlist hit={shortlistHit}/{commandCount} top1={shortlistTop1}/{commandCount} miss={shortlistMiss}");
  Console.WriteLine(
    $"Deterministic preview (not Jev): auto-dispatch={previewAuto} show-shortlist={previewShowList} none={previewNone} miss={previewMiss}");
}

static async Task RunLiveAsync(
  JsonObject corpus,
  List<CatalogEntry> catalogEntries,
  AccessPlan access,
  string researchDirectory)
{
  using HttpClient httpClient = new();
  httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access.ApiKey);
  httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
  httpClient.Timeout = TimeSpan.FromSeconds(60);

  JsonArray rows = [];
  int autoDispatchMatch = 0;
  int autoDispatchMismatch = 0;
  int showShortlist = 0;
  int noneCorrect = 0;
  int noneFalsePositive = 0;
  int noneMissed = 0;
  int errors = 0;

  foreach (JsonNode? node in RequireArray(corpus, "items", "corpus.json"))
  {
    JsonObject item = RequireItem(node, "corpus");
    string id = Text(item, "id");
    string phrase = Text(item, "phrase");
    string labeledArm = Text(item, "labeled_arm");
    List<ScoredEntry> shortlist = Shortlist(phrase, catalogEntries);
    JsonObject request = BuildRequest(phrase, shortlist);
    request["model"] = access.Model;
    Stopwatch stopwatch = Stopwatch.StartNew();
    HttpResponseMessage response;
    try
    {
      response = await httpClient.PostAsync(
        access.Endpoint,
        new StringContent(request.ToJsonString(), Encoding.UTF8, "application/json")).ConfigureAwait(false);
    }
    catch (Exception exception)
    {
      errors++;
      rows.Add(new JsonObject { ["id"] = id, ["error"] = exception.GetType().Name });
      Console.WriteLine($"{id}: transport error {exception.GetType().Name}");
      continue;
    }

    stopwatch.Stop();
    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    if (!response.IsSuccessStatusCode)
    {
      errors++;
      rows.Add(new JsonObject { ["id"] = id, ["http_status"] = (int)response.StatusCode });
      Console.WriteLine($"{id}: HTTP {(int)response.StatusCode}");
      continue;
    }

    using JsonDocument document = JsonDocument.Parse(body);
    JsonElement answers = document.RootElement.GetProperty("answers");
    JsonElement which = answers.GetProperty("which");
    string choice = which.GetProperty("choice").GetString() ?? "";
    double confidence = which.TryGetProperty("confidence", out JsonElement confidenceElement)
      ? confidenceElement.GetDouble()
      : 0;
    double isCommand = 0;
    if (answers.TryGetProperty("is_command", out JsonElement isCommandElement)
        && isCommandElement.TryGetProperty("noul", out JsonElement noulElement))
    {
      isCommand = noulElement.GetDouble();
    }

    JsonObject fits = [];
    foreach (JsonProperty property in answers.EnumerateObject())
    {
      if (property.Name.StartsWith("fits::", StringComparison.Ordinal)
          && property.Value.TryGetProperty("noul", out JsonElement fitNoul))
      {
        fits[property.Name["fits::".Length..]] = fitNoul.GetDouble();
      }
    }

    int? inputTokens = null;
    if (document.RootElement.TryGetProperty("usage", out JsonElement usage)
        && usage.TryGetProperty("input_tokens", out JsonElement inputTokensElement))
    {
      inputTokens = inputTokensElement.GetInt32();
    }

    string disposition = LiveDisposition(labeledArm, choice, isCommand, confidence, fits);
    switch (disposition)
    {
      case "auto-dispatch-match":
        autoDispatchMatch++;
        break;
      case "auto-dispatch-mismatch":
        autoDispatchMismatch++;
        break;
      case "show-shortlist":
        showShortlist++;
        break;
      case "none-correct":
        noneCorrect++;
        break;
      case "none-false-positive":
        noneFalsePositive++;
        break;
      default:
        noneMissed++;
        break;
    }

    rows.Add(
      new JsonObject
      {
        ["id"] = id,
        ["phrase"] = phrase,
        ["labeled_arm"] = labeledArm,
        ["choice"] = choice,
        ["confidence"] = confidence,
        ["is_command"] = isCommand,
        ["fits"] = fits,
        ["disposition"] = disposition,
        ["latency_ms"] = stopwatch.ElapsedMilliseconds,
        ["input_tokens"] = inputTokens,
        ["model"] = document.RootElement.TryGetProperty("model", out JsonElement modelElement)
          ? modelElement.GetString()
          : access.Model
      });

    Console.WriteLine(
      $"{id}: choice={choice} label={labeledArm} command={isCommand:0.00} conf={confidence:0.00} {disposition}");
  }

  JsonObject live = new()
  {
    ["mode"] = "live",
    ["endpoint"] = access.Endpoint,
    ["model"] = access.Model,
    ["item_count"] = RequireArray(corpus, "items", "corpus.json").Count,
    ["auto_dispatch_match"] = autoDispatchMatch,
    ["auto_dispatch_mismatch"] = autoDispatchMismatch,
    ["show_shortlist"] = showShortlist,
    ["none_correct"] = noneCorrect,
    ["none_false_positive"] = noneFalsePositive,
    ["none_missed"] = noneMissed,
    ["errors"] = errors,
    ["gate_noul_threshold"] = GateNoulThreshold,
    ["fits_noul_threshold"] = FitsNoulThreshold,
    ["choice_confidence_show_list"] = ShowListConfidence,
    ["rows"] = rows
  };

  string outputPath = Path.Combine(researchDirectory, "live-results.json");
  File.WriteAllText(outputPath, live.ToJsonString(WriteJsonOptions()));
  Console.WriteLine(
    $"Wrote {outputPath} (gitignored). auto-match={autoDispatchMatch} auto-mismatch={autoDispatchMismatch} " +
    $"show-list={showShortlist} none-ok={noneCorrect} none-fp={noneFalsePositive} none-miss={noneMissed} errors={errors}");
}

static string LiveDisposition(
  string labeledArm,
  string choice,
  double isCommand,
  double confidence,
  JsonObject fits)
{
  bool commandWanted = isCommand >= GateNoulThreshold;
  double bestFit = 0;
  foreach (KeyValuePair<string, JsonNode?> pair in fits)
  {
    if (pair.Value is not null)
    {
      bestFit = Math.Max(bestFit, pair.Value.GetValue<double>());
    }
  }

  bool noneChoice = choice == NoneOption || string.IsNullOrWhiteSpace(choice);
  if (!commandWanted || noneChoice || (fits.Count > 0 && bestFit < FitsNoulThreshold))
  {
    return labeledArm == "none" ? "none-correct" : "none-missed";
  }

  if (confidence < ShowListConfidence)
  {
    return "show-shortlist";
  }

  if (labeledArm == "none")
  {
    return "none-false-positive";
  }

  return string.Equals(choice, labeledArm, StringComparison.Ordinal)
    ? "auto-dispatch-match"
    : "auto-dispatch-mismatch";
}

static List<ScoredEntry> Shortlist(string phrase, List<CatalogEntry> catalogEntries)
{
  return RankCatalog(phrase, catalogEntries)
    .Where(static s => s.Score > 0)
    .Take(ShortlistSize)
    .ToList();
}

static List<ScoredEntry> RankCatalog(string phrase, List<CatalogEntry> catalogEntries)
{
  HashSet<string> tokens = Tokens(phrase);
  List<ScoredEntry> scored = [];
  foreach (CatalogEntry entry in catalogEntries)
  {
    scored.Add(new ScoredEntry(entry, Score(phrase, tokens, entry)));
  }

  return scored
    .OrderByDescending(static s => s.Score)
    .ThenBy(static s => s.Entry.Name, StringComparer.Ordinal)
    .ToList();
}

static int Score(string phrase, HashSet<string> tokens, CatalogEntry entry)
{
  string haystack = $"{entry.Name} {entry.Description}".ToLowerInvariant();
  string phraseLower = phrase.ToLowerInvariant();
  int score = 0;
  if (haystack.Contains(phraseLower, StringComparison.Ordinal))
  {
    score += 10;
  }

  foreach (string token in tokens)
  {
    if (entry.Name.Contains(token, StringComparison.OrdinalIgnoreCase))
    {
      score += 4;
    }

    if (entry.Description.Contains(token, StringComparison.OrdinalIgnoreCase))
    {
      score += 2;
    }
  }

  string methodName = entry.Name.Contains('.', StringComparison.Ordinal)
    ? entry.Name[(entry.Name.LastIndexOf('.') + 1)..]
    : entry.Name;
  List<string> methodTokens = IdentifierTokens(methodName);
  int methodHits = 0;
  foreach (string identifierToken in IdentifierTokens(entry.Name))
  {
    if (tokens.Contains(identifierToken))
    {
      score += 3;
      if (methodTokens.Contains(identifierToken))
      {
        methodHits++;
      }
    }
  }

  if (methodHits > 0)
  {
    foreach (string methodToken in methodTokens)
    {
      if (!tokens.Contains(methodToken))
      {
        score -= 1;
      }
    }
  }

  return score < 0 ? 0 : score;
}

static HashSet<string> Tokens(string phrase)
{
  HashSet<string> tokens = new(StringComparer.OrdinalIgnoreCase);
  StringBuilder current = new();
  foreach (char character in phrase)
  {
    if (char.IsLetterOrDigit(character))
    {
      current.Append(char.ToLowerInvariant(character));
      continue;
    }

    FlushToken(tokens, current);
  }

  FlushToken(tokens, current);
  return tokens;
}

static List<string> IdentifierTokens(string name)
{
  List<string> tokens = [];
  StringBuilder current = new();
  foreach (char character in name)
  {
    if (character == '.')
    {
      FlushIdentifier(tokens, current);
      continue;
    }

    if (char.IsUpper(character) && current.Length > 0)
    {
      FlushIdentifier(tokens, current);
    }

    current.Append(character);
  }

  FlushIdentifier(tokens, current);
  return tokens;
}

static void FlushToken(HashSet<string> tokens, StringBuilder current)
{
  if (current.Length >= 3)
  {
    tokens.Add(current.ToString());
  }

  current.Clear();
}

static void FlushIdentifier(List<string> tokens, StringBuilder current)
{
  if (current.Length >= 2)
  {
    tokens.Add(current.ToString().ToLowerInvariant());
  }

  current.Clear();
}

static JsonObject BuildRequest(string phrase, List<ScoredEntry> shortlist)
{
  JsonObject criteria = [];
  JsonArray shortlistState = [];
  foreach (ScoredEntry scored in shortlist)
  {
    criteria[scored.Entry.Name] = scored.Entry.Description;
    shortlistState.Add(
      new JsonObject
      {
        ["name"] = scored.Entry.Name,
        ["description"] = scored.Entry.Description
      });
  }

  criteria[NoneOption] = "The user is browsing, asking a question, navigating to a page, or does not want to run a catalog command.";
  if (shortlist.Count == 0)
  {
    criteria["no_catalog_match"] = "Nothing in the opt-in catalog matches; do not invent a command.";
  }

  JsonObject questions = new()
  {
    ["which"] = new JsonObject
    {
      ["type"] = "choice",
      ["instructions"] = "Which catalog command, if any, should run for this phrase? Pick none_of_the_above when the user is only looking, asking, or navigating.",
      ["criteria"] = criteria
    },
    ["is_command"] = Noul(
      "Does the user want to run a command in the app right now, rather than browse, ask a question, or open a page?",
      "An imperative request to change theme, sign out, add a passkey, create a role, increment a counter, or similar.",
      "Looking around, asking what something is, or wanting to go to a page without running an ActionSet.")
  };

  foreach (ScoredEntry scored in shortlist.Take(3))
  {
    questions[$"fits::{scored.Entry.Name}"] = Noul(
      $"Does the command '{scored.Entry.Name}' do the specific thing this phrase asks for? It is described as: {scored.Entry.Description}",
      "This command is the action the user wants executed.",
      "This command is unrelated, a near-miss, or the user is not asking to run a command.");
  }

  return new JsonObject
  {
    ["model"] = "jev-latest",
    ["state"] = new JsonObject
    {
      ["phrase"] = phrase,
      ["shortlist"] = shortlistState
    },
    ["questions"] = questions
  };
}

static JsonObject Noul(string instructions, string yes, string no)
{
  return new JsonObject
  {
    ["type"] = "noul",
    ["instructions"] = instructions,
    ["criteria"] = new JsonObject
    {
      ["true"] = yes,
      ["false"] = no
    }
  };
}

static AccessPlan? ResolveAccess()
{
  string? typeSafe = Environment.GetEnvironmentVariable("TYPESAFE_API_KEY");
  if (!string.IsNullOrWhiteSpace(typeSafe))
  {
    return new AccessPlan(typeSafe, "https://api.typesafe.ai/v1/systemone", "jev-latest");
  }

  string? openRouter = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
  if (!string.IsNullOrWhiteSpace(openRouter))
  {
    return new AccessPlan(openRouter, "https://openrouter.ai/api/alpha/decisions", "~typesafe/jev-latest");
  }

  return null;
}

static JsonSerializerOptions WriteJsonOptions()
{
  return new JsonSerializerOptions { WriteIndented = true };
}

sealed record CatalogEntry(string Name, string Description);
sealed record ScoredEntry(CatalogEntry Entry, int Score);
sealed record AccessPlan(string ApiKey, string Endpoint, string Model);
