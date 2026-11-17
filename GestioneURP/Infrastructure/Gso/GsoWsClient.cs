/*
 * Nome del progetto: HelpDesk URP
 * Copyright (C) 2026 Agenzia regionale per la protezione dell'ambiente ligure
 * ---
 * Questo programma è software libero: puoi ridistribuirlo e/o modificarlo
 * secondo i termini della GNU Affero General Public License pubblicata dalla
 * Free Software Foundation, sia la versione 3 della licenza, sia (a tua scelta)
 * qualsiasi versione successiva.
 * ---
 * Questo programma è distribuito nella speranza che possa essere utile,
 * ma SENZA ALCUNA GARANZIA; senza nemmeno la garanzia implicita di
 * COMMERCIABILITÀ o IDONEITÀ PER UNO SCOPO PARTICOLARE. Vedi la
 * GNU Affero General Public License per ulteriori dettagli.
 * ---
 * Dovresti aver ricevuto una copia della GNU Affero General Public License
 * insieme a questo programma. In caso contrario, vedi <https://www.gnu.org/licenses/>.
*/

using Microsoft.Extensions.Logging.Abstractions;
using System.Globalization;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace GsoWsClient;

/// <summary>
/// Client per l'accesso ai servizi GSO esposti tramite endpoint XML su protocollo HTTP.
/// </summary>
public sealed class GsoWsClient
{
	private readonly HttpClient _httpClient;
	private readonly ILogger _logger;
	private readonly string _baseUrl;

	/// <summary>
	/// Inizializza il client GSO con HttpClient, URL base del servizio e logger opzionale.
	/// </summary>
	/// <param name="httpClient">Client HTTP usato per eseguire le chiamate GET.</param>
	/// <param name="baseUrl">URL base del sito che ospita i servizi GSO.</param>
	/// <param name="logger">Logger opzionale per tracciare chiamate, risposte ed errori.</param>
	public GsoWsClient(HttpClient httpClient, string baseUrl, ILogger? logger = null)
	{
		_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

		if (string.IsNullOrWhiteSpace(baseUrl))
			throw new ArgumentException("Il parametro obbligatorio 'baseUrl' non è stato fornito.", nameof(baseUrl));

		if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
			throw new ArgumentException("Il parametro 'baseUrl' non è un URI assoluto valido.", nameof(baseUrl));

		_baseUrl = baseUrl.TrimEnd('/') + "/";
		_logger = logger ?? NullLogger.Instance;
	}

	/// <summary>
	/// Verifica le credenziali utente sul servizio GSO e restituisce l'esito del logon.
	/// </summary>
	/// <param name="userName">Nome utente da validare.</param>
	/// <param name="password">Password associata all'utente; può essere vuota.</param>
	/// <param name="application">Nome dell'applicazione per cui effettuare la verifica.</param>
	/// <param name="trusted">Indica se la richiesta deve essere marcata come trusted.</param>
	/// <param name="cancellationToken">Token di annullamento della chiamata.</param>
	/// <returns>Risultato del logon con id utente in caso di successo o errore tipizzato in caso negativo.</returns>
	public async Task<GsoLogonResult> LogonAsync(
		string userName,
		string? password,
		string application,
		bool trusted = true,
		CancellationToken cancellationToken = default)
	{
		RequireNotEmpty(userName, nameof(userName));
		RequireNotEmpty(application, nameof(application));

		var parameters = new Dictionary<string, string>
		{
			["sUserName"] = userName,
			["sPassword"] = password ?? string.Empty,
			["sApplication"] = application,
			["bTrusted"] = trusted ? "True" : "False"
		};

		var raw = await SendGetAsync("Gso.asmx/Logon", parameters, cancellationToken);

		try
		{
			var value = ParseIntValueFromXml(raw);

			var result = value switch
			{
				> 0 => GsoLogonResult.Success(value),
				-1 => GsoLogonResult.Failure(GsoLogonError.UserNotFound),
				-3 => GsoLogonResult.Failure(GsoLogonError.ApplicationNotFound),
				_ => throw new InvalidOperationException($"Valore inatteso restituito da Logon: '{value}'.")
			};

			_logger.LogInformation("Logon completato con successo.");
			_logger.LogDebug("Risposta raw Logon: {RawResponse}", raw);
			_logger.LogDebug("Dati estratti Logon: {ParsedData}", JsonSerializer.Serialize(result));

			return result;
		}
		catch (XmlException ex)
		{
			throw new FormatException("Errore di parsing XML nella risposta di Logon.", ex);
		}
	}

	/// <summary>
	/// Recupera i profili applicativi associati a un utente per una specifica applicazione.
	/// </summary>
	/// <param name="userId">Identificativo utente GSO.</param>
	/// <param name="application">Nome dell'applicazione di riferimento.</param>
	/// <param name="cancellationToken">Token di annullamento della chiamata.</param>
	/// <returns>Elenco degli oggetti profilo restituiti dal servizio, inclusi eventuali duplicati presenti nella risposta.</returns>
	public async Task<IReadOnlyList<GsoWsProfileItem>> GetAppUserProfilesAsync(
		decimal userId,
		string application,
		CancellationToken cancellationToken = default)
	{
		RequireNotEmpty(application, nameof(application));

		var parameters = new Dictionary<string, string>
		{
			["nUserId"] = userId.ToString(CultureInfo.InvariantCulture),
			["sApplication"] = application
		};

		var raw = await SendGetAsync("Gso.asmx/GetAppUserProfiles", parameters, cancellationToken);

		try
		{
			var doc = ParseXmlDocument(raw, "GetAppUserProfiles");
			var rows = doc.Descendants()
				.Where(e =>
					e.Name.LocalName == "Table1" &&
					e.Parent?.Name.LocalName == "NewDataSet" &&
					e.Elements().Any(x => x.Name.LocalName == "ID_PROFILO_UTENTE_APP"));

			var list = new List<GsoWsProfileItem>();

			foreach (var row in rows)
			{
				var idText = ChildValue(row, "ID_PROFILO_UTENTE_APP");
				var profiloRaw = ChildValue(row, "PROFILO");

				if (!string.IsNullOrWhiteSpace(idText) && !decimal.TryParse(idText, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
					throw new FormatException($"Valore non valido per ID_PROFILO_UTENTE_APP: '{idText}'.");

				var profilo = ParseEmbeddedProfileXml(profiloRaw);
				if (profilo?.Profili is { Count: > 0 })
					list.AddRange(profilo.Profili);
			}

			_logger.LogInformation("GetAppUserProfiles completato con successo.");
			_logger.LogDebug("Risposta raw GetAppUserProfiles: {RawResponse}", raw);
			_logger.LogDebug("Dati estratti GetAppUserProfiles: {ParsedData}", JsonSerializer.Serialize(list));

			return list;
		}
		catch (XmlException ex)
		{
			throw new FormatException("Errore di parsing XML nella risposta di GetAppUserProfiles.", ex);
		}
	}

	/// <summary>
	/// Recupera l'elenco utenti che hanno accesso all'applicazione indicata.
	/// </summary>
	/// <param name="application">Nome dell'applicazione di cui leggere gli utenti.</param>
	/// <param name="cancellationToken">Token di annullamento della chiamata.</param>
	/// <returns>Elenco utenti restituito dal servizio, mantenendo i record così come ricevuti.</returns>
	public async Task<IReadOnlyList<GsoApplicationUser>> GetApplicationUsersAsync(
		string application,
		CancellationToken cancellationToken = default)
	{
		RequireNotEmpty(application, nameof(application));

		var parameters = new Dictionary<string, string>
		{
			["sApplication"] = application
		};

		var raw = await SendGetAsync("Gso.asmx/GetApplicationUsers", parameters, cancellationToken);

		try
		{
			var doc = XDocument.Parse(raw);
			var rows = doc.Descendants()
				.Where(e => e.Name.LocalName == "Table1" && e.Elements().Any(x => x.Name.LocalName == "ID_UTENTE"));

			var list = new List<GsoApplicationUser>();

			foreach (var row in rows)
			{
				var idText = ChildValue(row, "ID_UTENTE");
				decimal? id = null;
				if (!string.IsNullOrWhiteSpace(idText))
				{
					if (!decimal.TryParse(idText, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
						throw new FormatException($"Valore non valido per ID_UTENTE: '{idText}'.");
					id = parsed;
				}

				list.Add(new GsoApplicationUser
				{
					IdUtente = id,
					Nome = ChildValue(row, "NOME"),
					Cognome = ChildValue(row, "COGNOME"),
					DescrizioneProfilo = ChildValue(row, "DESCRIZIONE_PROFILO")
				});
			}

			_logger.LogInformation("GetApplicationUsers completato con successo.");
			_logger.LogDebug("Risposta raw GetApplicationUsers: {RawResponse}", raw);
			_logger.LogDebug("Dati estratti GetApplicationUsers: {ParsedData}", JsonSerializer.Serialize(list));

			return list;
		}
		catch (XmlException ex)
		{
			throw new FormatException("Errore di parsing XML nella risposta di GetApplicationUsers.", ex);
		}
	}

	/// <summary>
	/// Recupera l'elenco delle strutture disponibili.
	/// </summary>
	/// <param name="cancellationToken">Token di annullamento della chiamata.</param>
	/// <returns>Elenco strutture restituito dal servizio.</returns>
	public async Task<IReadOnlyList<GsoStructure>> GetAllStruttureAsync(
		CancellationToken cancellationToken = default)
	{
		var raw = await SendGetAsync("Gso.asmx/GetAllStrutture", new Dictionary<string, string>(), cancellationToken);

		try
		{
			var doc = ParseXmlDocument(raw, "GetAllStrutture");
			var rows = doc.Descendants()
				.Where(e => e.Name.LocalName == "Table1" && e.Elements().Any(x => x.Name.LocalName == "ID_STRUTTURA"));

			var list = new List<GsoStructure>();

			foreach (var row in rows)
			{
				var idText = ChildValue(row, "ID_STRUTTURA");
				decimal? id = null;
				if (!string.IsNullOrWhiteSpace(idText))
				{
					if (!decimal.TryParse(idText, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
						throw new FormatException($"Valore non valido per ID_STRUTTURA: '{idText}'.");
					id = parsed;
				}

				list.Add(new GsoStructure
				{
					IdStruttura = id,
					DescrizioneStruttura = ChildValue(row, "DESCRIZIONE_STRUTTURA"),
					CodiceStruttura = ChildValue(row, "COD_STRUTTURA_ORGANIZZATIVA")
				});
			}

			_logger.LogInformation("GetAllStrutture completato con successo.");
			_logger.LogDebug("Risposta raw GetAllStrutture: {RawResponse}", raw);
			_logger.LogDebug("Dati estratti GetAllStrutture: {ParsedData}", JsonSerializer.Serialize(list));

			return list;
		}
		catch (XmlException ex)
		{
			throw new FormatException("Errore di parsing XML nella risposta di GetAllStrutture.", ex);
		}
	}

	private async Task<string> SendGetAsync(
		string relativePath,
		IReadOnlyDictionary<string, string> parameters,
		CancellationToken cancellationToken)
	{
		var query = string.Join("&", parameters.Select(p =>
			$"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

		var url = $"{_baseUrl}{relativePath.TrimStart('/')}?{query}";

		_logger.LogInformation("Chiamata API GET: {RelativePath} | Parametri: {Parameters}",
			relativePath, string.Join(", ", parameters.Select(x => $"{x.Key}={x.Value}")));

		try
		{
			using var response = await _httpClient.GetAsync(url, cancellationToken);
			response.EnsureSuccessStatusCode();
			return await response.Content.ReadAsStringAsync(cancellationToken);
		}
		catch (HttpRequestException ex)
		{
			throw new HttpRequestException($"Errore HTTP durante la chiamata a '{url}'. {ex.Message}", ex, ex.StatusCode);
		}
		catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
		{
			throw new TimeoutException($"Timeout durante la chiamata a '{url}'.", ex);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			throw new InvalidOperationException($"Errore imprevisto durante la chiamata a '{url}'.", ex);
		}
	}

	private static XDocument ParseXmlDocument(string xml, string context)
	{
		if (string.IsNullOrWhiteSpace(xml))
			throw new FormatException($"XML vuoto nella risposta di {context}.");

		var normalized = xml.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
		return XDocument.Parse(normalized);
	}

	private static int ParseIntValueFromXml(string rawXml)
	{
		var doc = ParseXmlDocument(rawXml, "Logon");
		var root = doc.Root ?? throw new FormatException("XML senza elemento radice.");

		if (root.Name.LocalName != "int" || root.Name.NamespaceName != "http://tempuri.org/")
			throw new FormatException($"Risposta Logon inattesa: elemento '{root.Name}'.");

		var text = root.Value?.Trim();

		if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
			throw new FormatException($"Impossibile convertire il valore '{text}' in intero.");

		return value;
	}

	private static GsoProfileDocument? ParseEmbeddedProfileXml(string? escapedXml)
	{
		if (string.IsNullOrWhiteSpace(escapedXml))
			return null;

		var decoded = System.Net.WebUtility.HtmlDecode(escapedXml);
		var doc = ParseXmlDocument(decoded, "profilo embedded");
		var root = doc.Root ?? throw new FormatException("Profilo XML senza elemento radice.");

		var result = new GsoProfileDocument
		{
			RootName = root.Name.LocalName
		};

		foreach (var profilo in root.Elements().Where(e => e.Name.LocalName == "profilo"))
		{
			var item = new GsoWsProfileItem
			{
				Nome = (string?)profilo.Attribute("nome")
			};

			foreach (var funz in profilo.Elements().Where(e => e.Name.LocalName == "funz"))
			{
				var abilitText = (string?)funz.Attribute("abilit");
				var abilit = bool.TryParse(abilitText, out var parsedAbilit) && parsedAbilit;

				item.Funzioni.Add(new GsoProfileFunction
				{
					Valore = (string?)funz.Attribute("valore"),
					Abilit = abilit
				});
			}

			result.Profili.Add(item);
		}

		return result;
	}

	private static string? ChildValue(XElement parent, string localName) =>
		parent.Elements().FirstOrDefault(e => e.Name.LocalName == localName)?.Value;

	private static void RequireNotEmpty(string value, string paramName)
	{
		if (string.IsNullOrWhiteSpace(value))
			throw new ArgumentException($"Il parametro obbligatorio '{paramName}' non è stato fornito.", paramName);
	}
}

public enum GsoLogonError
{
	UserNotFound,
	ApplicationNotFound
}

public sealed class GsoLogonResult
{
	public decimal? UserId { get; init; }
	public GsoLogonError? Error { get; init; }
	public bool IsSuccess => UserId.HasValue;

	public static GsoLogonResult Success(decimal userId) => new() { UserId = userId };

	public static GsoLogonResult Failure(GsoLogonError error) => new() { Error = error };
}

public sealed class GsoUserProfile
{
	public decimal? IdProfiloUtenteApp { get; init; }
	public string? Descrizione { get; init; }
	public GsoProfileDocument? Profilo { get; init; }
}

public sealed class GsoApplicationUser
{
	public decimal? IdUtente { get; init; }
	public string? Nome { get; init; }
	public string? Cognome { get; init; }
	public string? DescrizioneProfilo { get; init; }
}

public sealed class GsoStructure
{
	public decimal? IdStruttura { get; init; }
	public string? DescrizioneStruttura { get; init; }
	public string? CodiceStruttura { get; init; }
}

public sealed class GsoProfileDocument
{
	public string? RootName { get; init; }
	public List<GsoWsProfileItem> Profili { get; init; } = [];
}

public sealed class GsoWsProfileItem
{
	public string? Nome { get; init; }
	public List<GsoProfileFunction> Funzioni { get; init; } = [];
}

public sealed class GsoProfileFunction
{
	public string? Valore { get; init; }
	public bool Abilit { get; init; }
}
