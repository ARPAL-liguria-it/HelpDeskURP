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

using GestioneURP.Infrastructure.Gso;

namespace GestioneURP.Application.Services;

/// <summary>
/// Definisce i passaggi consentiti della pratica, verifica i dati richiesti e la coerenza delle date.
/// Recupera inoltre operatori e strutture dal servizio GSO per le selezioni della UI; non salva la pratica.
/// </summary>
public class GestionePraticaService
{
	private const string StatoAperta = "Aperta";
	private const string StatoPresaInCaricoDaOperatore = "Presa in carico da operatore";
	private const string StatoPresaInCaricoDaAltroOperatore = "Passaggio ad altro operatore";
	private const string StatoInoltratAAltraStruttura = "Richiesto approfondimento ad altra struttura";
	private const string StatoRicevutaRispostaDaStruttura = "Ricevuta risposta da struttura";
	private const string StatoRispostaPerIlCittadino = "Risposta per il cittadino";
	private const string StatoChiusa = "Chiusa";

	/// <summary>Restituisce tutti gli stati riconosciuti, nell'ordine utilizzato per presentarli nella UI.</summary>
	public static List<string> GetStatiDisponibili()
	{
		return
		[
			StatoAperta,
			StatoPresaInCaricoDaOperatore,
			StatoPresaInCaricoDaAltroOperatore,
			StatoInoltratAAltraStruttura,
			StatoRicevutaRispostaDaStruttura,
			StatoRispostaPerIlCittadino,
			StatoChiusa
		];
	}

	/// <summary>
	/// Restituisce soltanto le destinazioni direttamente raggiungibili dallo stato indicato.
	/// Lo stato chiuso e i valori non riconosciuti non consentono ulteriori avanzamenti.
	/// </summary>
	public static IReadOnlyList<string> GetStatiSuccessivi(string? stato) => stato switch
	{
		StatoAperta => [StatoPresaInCaricoDaOperatore],
		StatoPresaInCaricoDaOperatore => [StatoPresaInCaricoDaAltroOperatore, StatoRispostaPerIlCittadino, StatoInoltratAAltraStruttura, StatoChiusa],
		StatoPresaInCaricoDaAltroOperatore => [StatoRispostaPerIlCittadino, StatoChiusa],
		StatoInoltratAAltraStruttura => [StatoRicevutaRispostaDaStruttura, StatoChiusa],
		StatoRicevutaRispostaDaStruttura => [StatoRispostaPerIlCittadino, StatoChiusa],
		StatoRispostaPerIlCittadino => [StatoChiusa],
		_ => []
	};

	/// <summary>
	/// Ricostruisce i passaggi indicati dalle date compilate, mantenendo lo stato corrente
	/// e soltanto i passaggi che possono precederlo nel grafo delle transizioni.
	/// </summary>
	public static IReadOnlyList<string> GetStatiAttraversati(GestionePraticaUpdateDto dati)
	{
		var stati = new List<string> { StatoAperta };
		if (dati.DataPresaInCarico.HasValue) stati.Add(StatoPresaInCaricoDaOperatore);
		if (dati.DataAssegnazioneAltroOperatore.HasValue) stati.Add(StatoPresaInCaricoDaAltroOperatore);
		if (dati.DataInoltroAltraStruttura.HasValue) stati.Add(StatoInoltratAAltraStruttura);
		if (dati.DataRispostaStruttura.HasValue) stati.Add(StatoRicevutaRispostaDaStruttura);
		if (dati.DataRisposta.HasValue) stati.Add(StatoRispostaPerIlCittadino);
		if (dati.DataChiusura.HasValue) stati.Add(StatoChiusa);
		if (dati.StatoPratica is not null && !stati.Contains(dati.StatoPratica)) stati.Add(dati.StatoPratica);
		return stati.Where(stato => stato == dati.StatoPratica || Precede(stato, dati.StatoPratica)).ToList();
	}

	// Esplora ricorsivamente il grafo privo di cicli per verificare se la destinazione è raggiungibile.
	private static bool Precede(string stato, string? destinazione)
		=> GetStatiSuccessivi(stato).Any(successivo => successivo == destinazione || Precede(successivo, destinazione));

	/// <summary>
	/// Carica gli utenti dell'applicazione da GSO, esclude quelli senza nominativo e li ordina per cognome e nome.
	/// </summary>
	public async Task<List<UtenteApplicazione>> GetOperatoriDisponibiliAsync(
		IConfiguration configuration,
		HttpClient httpClient,
		ILogger? logger = null)
	{
		logger?.LogDebug(nameof(GetOperatoriDisponibiliAsync));

		ArgumentNullException.ThrowIfNull(configuration);
		ArgumentNullException.ThrowIfNull(httpClient);

		var identificativoApplicazione = configuration["IdentificativoApplicazione"];
		var gsoWsBaseUrl = configuration["GsoWsBaseUrl"];

		ArgumentNullException.ThrowIfNull(identificativoApplicazione);
		ArgumentNullException.ThrowIfNull(gsoWsBaseUrl);

		var utenti = await GsoWsClientAdapter.ElencoUtentiApplicazioneAsync(
			identificativoApplicazione,
			gsoWsBaseUrl,
			httpClient,
			logger)
			.ConfigureAwait(false);

		return utenti
			.Where(x => !string.IsNullOrWhiteSpace(x.CognomeNome))
			.OrderBy(x => GetCognome(x.CognomeNome!))
			.ThenBy(x => GetNome(x.CognomeNome!))
			.ToList();
	}

	/// <summary>
	/// Carica le strutture da GSO, esclude le descrizioni vuote e rimuove i duplicati
	/// per descrizione senza distinguere maiuscole e minuscole, poi ordina l'elenco.
	/// </summary>
	public async Task<List<Struttura>> GetStruttureDisponibiliAsync(
		IConfiguration configuration,
		HttpClient httpClient,
		ILogger? logger = null)
	{
		logger?.LogDebug(nameof(GetStruttureDisponibiliAsync));

		ArgumentNullException.ThrowIfNull(configuration);
		ArgumentNullException.ThrowIfNull(httpClient);

		var gsoWsBaseUrl = configuration["GsoWsBaseUrl"];
		ArgumentNullException.ThrowIfNull(gsoWsBaseUrl);

		var strutture = await GsoWsClientAdapter.ElencoStruttureAsync(
			gsoWsBaseUrl,
			httpClient,
			logger)
			.ConfigureAwait(false);

		return strutture
			.Where(x => !string.IsNullOrWhiteSpace(x.DescrizioneStruttura))
			.GroupBy(x => x.DescrizioneStruttura!, StringComparer.OrdinalIgnoreCase)
			.Select(x => x.First())
			.OrderBy(x => x.DescrizioneStruttura)
			.ToList();
	}

	// Per l'ordinamento il nominativo viene interpretato come nome seguito dal cognome nell'ultima parola.
	private static string GetCognome(string valore)
	{
		var parti = valore.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		return parti.Length > 1 ? parti[^1] : valore;
	}

	private static string GetNome(string valore)
	{
		var parti = valore.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		return parti.Length > 1 ? string.Join(' ', parti[..^1]) : valore;
	}

	/// <summary>
	/// Verifica stato e presa in carico, poi i campi obbligatori del passaggio corrente.
	/// Solo in assenza di questi errori controlla le date, restituendo messaggi associati ai campi.
	/// </summary>
	public GestionePraticaValidationResult Validate(
		GestionePraticaUpdateDto data,
		DateTime? dataInserimentoSegnalazione,
		ILogger? logger = null)
	{
		logger?.LogDebug(nameof(Validate));

		var result = new GestionePraticaValidationResult { IsValid = true };
		if (!GetStatiDisponibili().Contains(data.StatoPratica ?? string.Empty))
			result.AddError(nameof(data.StatoPratica), "Stato della pratica non valido.");
		if (data.StatoPratica != StatoAperta && (!data.DataPresaInCarico.HasValue || string.IsNullOrWhiteSpace(data.OperatorePresaInCarico)))
			result.AddError(nameof(data.DataPresaInCarico), "La presa in carico è obbligatoria prima di avanzare.");

		// Validazione obbligatorietà campi per stato
		ValidateObbligatori(data, result);

		// Validazione coerenza temporale delle date (solo se non ci sono altri errori)
		if (result.IsValid)
		{
			ValidateCoerenzaTemporale(data, dataInserimentoSegnalazione, result);
		}

		return result;
	}

	// Ogni passaggio richiede i propri dati; la presa in carico necessaria all'avanzamento è verificata da Validate.
	private void ValidateObbligatori(GestionePraticaUpdateDto data, GestionePraticaValidationResult result)
	{
		switch (data.StatoPratica)
		{
			case StatoPresaInCaricoDaOperatore:
				if (!data.DataPresaInCarico.HasValue)
				{
					result.AddError("DataPresaInCarico", "La data presa in carico è obbligatoria per questo stato.");
					result.IsValid = false;
				}
				if (string.IsNullOrWhiteSpace(data.OperatorePresaInCarico))
				{
					result.AddError("OperatorePresaInCarico", "L'operatore presa in carico è obbligatorio per questo stato.");
					result.IsValid = false;
				}
				break;

			case StatoPresaInCaricoDaAltroOperatore:
				if (!data.DataAssegnazioneAltroOperatore.HasValue)
				{
					result.AddError("DataAssegnazioneAltroOperatore", "La data assegnazione è obbligatoria per questo stato.");
					result.IsValid = false;
				}
				if (string.IsNullOrWhiteSpace(data.OperatoreAssegnatario))
				{
					result.AddError("OperatoreAssegnatario", "L'operatore assegnatario è obbligatorio per questo stato.");
					result.IsValid = false;
				}
				break;

			case StatoInoltratAAltraStruttura:
				if (!data.DataInoltroAltraStruttura.HasValue)
				{
					result.AddError("DataInoltroAltraStruttura", "La data inoltro è obbligatoria per questo stato.");
					result.IsValid = false;
				}
				if (string.IsNullOrWhiteSpace(data.StrutturaDestinataria))
				{
					result.AddError("StrutturaDestinataria", "La struttura destinataria è obbligatoria per questo stato.");
					result.IsValid = false;
				}
				break;

			case StatoRicevutaRispostaDaStruttura:
				if (!data.DataRispostaStruttura.HasValue)
				{
					result.AddError("DataRispostaStruttura", "La data risposta della struttura è obbligatoria per questo stato.");
					result.IsValid = false;
				}
				if (string.IsNullOrWhiteSpace(data.RispostaDaStruttura))
				{
					result.AddError("RispostaDaStruttura", "La risposta da struttura è obbligatoria per questo stato.");
					result.IsValid = false;
				}
				break;

			case StatoRispostaPerIlCittadino:
				if (!data.DataRisposta.HasValue)
					result.AddError(nameof(data.DataRisposta), "La data risposta è obbligatoria per questo stato.");
				if (string.IsNullOrWhiteSpace(data.TestoRisposta))
					result.AddError(nameof(data.TestoRisposta), "Il testo della risposta è obbligatorio per questo stato.");
				break;

			case StatoChiusa:
				if (!data.DataChiusura.HasValue)
				{
					result.AddError("DataChiusura", "La data chiusura è obbligatoria per questo stato.");
					result.IsValid = false;
				}
				break;
		}
	}

	// I confronti usano il giorno, ignorando l'orario; la chiusura non può precedere nessuna data compilata.
	private void ValidateCoerenzaTemporale(
		GestionePraticaUpdateDto data,
		DateTime? dataInserimentoSegnalazione,
		GestionePraticaValidationResult result)
	{
		// Raccogliamo tutte le date compilate con i loro nomi per le regole di validazione
		var dateCompilate = new List<(DateTime Data, string Nome, string FieldName)>();

		if (dataInserimentoSegnalazione.HasValue)
			dateCompilate.Add((dataInserimentoSegnalazione.Value, "inserimento", "DataInserimento"));

		if (data.DataPresaInCarico.HasValue)
			dateCompilate.Add((data.DataPresaInCarico.Value, "presa in carico", "DataPresaInCarico"));

		if (data.DataAssegnazioneAltroOperatore.HasValue)
			dateCompilate.Add((data.DataAssegnazioneAltroOperatore.Value, "assegnazione ad altro operatore", "DataAssegnazioneAltroOperatore"));

		if (data.DataInoltroAltraStruttura.HasValue)
			dateCompilate.Add((data.DataInoltroAltraStruttura.Value, "inoltro ad altra struttura", "DataInoltroAltraStruttura"));

		if (data.DataRispostaStruttura.HasValue)
			dateCompilate.Add((data.DataRispostaStruttura.Value, "risposta della struttura", "DataRispostaStruttura"));

		if (data.DataRisposta.HasValue)
			dateCompilate.Add((data.DataRisposta.Value, "risposta", "DataRisposta"));

		if (data.DataChiusura.HasValue)
			dateCompilate.Add((data.DataChiusura.Value, "chiusura", "DataChiusura"));

		// Validazione: Data presa in carico >= Data inserimento
		if (data.DataPresaInCarico.HasValue && dataInserimentoSegnalazione.HasValue)
		{
			if (data.DataPresaInCarico.Value.Date < dataInserimentoSegnalazione.Value.Date)
			{
				result.AddError("DataPresaInCarico",
					"La data presa in carico non può essere antecedente alla data di inserimento.");
				result.IsValid = false;
			}
		}

		// Validazione: Data assegnazione >= Data presa in carico
		if (data.DataAssegnazioneAltroOperatore.HasValue && data.DataPresaInCarico.HasValue)
		{
			if (data.DataAssegnazioneAltroOperatore.Value.Date < data.DataPresaInCarico.Value.Date)
			{
				result.AddError("DataAssegnazioneAltroOperatore",
					"La data assegnazione ad altro operatore non può essere antecedente alla data presa in carico.");
				result.IsValid = false;
			}
		}

		// Validazione: Data inoltro >= Data presa in carico
		if (data.DataInoltroAltraStruttura.HasValue && data.DataPresaInCarico.HasValue)
		{
			if (data.DataInoltroAltraStruttura.Value.Date < data.DataPresaInCarico.Value.Date)
			{
				result.AddError("DataInoltroAltraStruttura",
					"La data inoltro ad altra struttura non può essere antecedente alla data presa in carico.");
				result.IsValid = false;
			}
		}

		// Validazione: Data risposta struttura >= Data inoltro
		if (data.DataRispostaStruttura.HasValue && data.DataInoltroAltraStruttura.HasValue)
		{
			if (data.DataRispostaStruttura.Value.Date < data.DataInoltroAltraStruttura.Value.Date)
			{
				result.AddError("DataRispostaStruttura",
					"La data risposta della struttura non può essere antecedente alla data inoltro ad altra struttura.");
				result.IsValid = false;
			}
		}

		// F003: la risposta segue tutte le date di riferimento presenti, confrontate per giorno.
		if (data.DataRisposta.HasValue)
		{
			DateTime?[] riferimenti = [data.DataPresaInCarico, data.DataAssegnazioneAltroOperatore, data.DataRispostaStruttura];
			if (riferimenti.Any(d => d.HasValue && data.DataRisposta.Value.Date < d.Value.Date))
			{
				result.AddError("DataRisposta",
					"La data risposta non può precedere la presa in carico, l'assegnazione o la risposta della struttura.");
				result.IsValid = false;
			}
		}

		// Validazione: Data chiusura >= tutte le altre date compilate
		if (data.DataChiusura.HasValue)
		{
			var datePerChiusura = dateCompilate.Where(d => d.FieldName != "DataChiusura").ToList();
			var dataMaxAltra = datePerChiusura.Any() ? datePerChiusura.Max(d => d.Data) : (DateTime?)null;

			if (dataMaxAltra.HasValue && data.DataChiusura.Value.Date < dataMaxAltra.Value.Date)
			{
				result.AddError("DataChiusura",
					"La data chiusura deve essere pari o successiva a tutte le altre date presenti.");
				result.IsValid = false;
			}
		}
	}
}
