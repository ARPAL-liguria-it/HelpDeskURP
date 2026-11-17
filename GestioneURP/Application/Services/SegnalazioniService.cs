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

using GestioneURP.Components.State;
using GestioneURP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestioneURP.Application.Services;

/// <summary>
/// Offre alle pagine Blazor la lettura e il salvataggio di richieste e reclami con DTO comuni.
/// Le altre parti della classe gestiscono inserimento, validazione della gestione e allegati.
/// </summary>
public partial class SegnalazioniService
{
	private readonly UrpHdDbContext db;
	private readonly IConfiguration configuration;
	private readonly OperatoreState operatoreState;
	private readonly ILogger<SegnalazioniService> logger;

	public SegnalazioniService(
		UrpHdDbContext db,
		IConfiguration configuration,
		OperatoreState operatoreState,
		ILogger<SegnalazioniService> logger)
	{
		this.db = db;
		this.configuration = configuration;
		this.operatoreState = operatoreState;
		this.logger = logger;
	}

	/// <summary>Carica le tematiche nell'ordine configurato per il filtro della lista.</summary>
	public async Task<List<TipoTematica>> GetTipiTematicaAsync()
	{
		return await db.TipiTematica.AsNoTracking()
			.OrderBy(x => x.Ordine)
			.ToListAsync();
	}

	/// <summary>
	/// Filtra richieste e reclami, risolve le descrizioni e restituisce la pagina richiesta con il totale.
	/// L'ordinamento per data decrescente e la paginazione avvengono in memoria dopo l'unione dei due elenchi.
	/// </summary>
	public async Task<(List<SegnalazioneRigaDto> Righe, int TotaleRighe)> GetSegnalazioniAsync(
		FiltriSegnalazione filtri, int pagina, int righePerPagina = 15)
	{
		logger.LogDebug("GetSegnalazioniAsync: filtri={@filtri}, pagina={pagina}, righePerPagina={righePerPagina}", filtri, pagina, righePerPagina);

		// Le tabelle di classificazione sono caricate una volta e usate per descrivere le righe di entrambi i tipi.
		var tipiRichiesta = await db.TipiRichiesta.AsNoTracking()
			.ToDictionaryAsync(x => x.IdTipoRichiesta, x => x.DescrizioneTipoRichiesta);
		var tipiTematica = await db.TipiTematica.AsNoTracking()
			.ToDictionaryAsync(x => x.IdTipoTematica, x => x.DescrizioneTipoTematica);

		var richiesteQuery = db.Richieste.AsNoTracking();

		if (!string.IsNullOrWhiteSpace(filtri.Testo))
		{
			var t = filtri.Testo.ToUpper();
			richiesteQuery = richiesteQuery.Where(r =>
				(r.EmailAutore != null && r.EmailAutore.ToUpper().Contains(t)) ||
				(r.CognomeAutore != null && r.CognomeAutore.ToUpper().Contains(t)) ||
				(r.Testo != null && r.Testo.ToUpper().Contains(t)));
		}
		if (!string.IsNullOrWhiteSpace(filtri.StatoPratica))
			richiesteQuery = richiesteQuery.Where(r => r.StatoPratica == filtri.StatoPratica);
		if (filtri.IdTipoTematica.HasValue)
			richiesteQuery = richiesteQuery.Where(r => r.IdTipoTematica == filtri.IdTipoTematica.Value);

		var richiesteRighe = (await richiesteQuery.ToListAsync())
			.Select(r => new SegnalazioneRigaDto(
				TipoSegnalazione: "Richiesta",
				Id: r.IdRichiesta,
				Codice: r.Codice,
				DataInserimento: r.DataInserimento,
				EmailAutore: r.EmailAutore,
				CognomeNomeAutore: FormatNome(r.CognomeAutore, r.NomeAutore),
				Tipologia: r.IdTipoRichiesta.HasValue && tipiRichiesta.TryGetValue(r.IdTipoRichiesta.Value, out var tr) ? tr : null,
				AreaTematica: r.IdTipoTematica.HasValue && tipiTematica.TryGetValue(r.IdTipoTematica.Value, out var tt) ? tt : null,
				StatoPratica: r.StatoPratica,
				OperatorePresaInCarico: r.OperatorePresaInCarico
			)).ToList();

		List<SegnalazioneRigaDto> reclamiRighe = [];
		// I reclami non hanno area tematica: con questo filtro attivo si considerano soltanto le richieste.
		if (!filtri.IdTipoTematica.HasValue)
		{
			var reclamiQuery = db.Reclami.AsNoTracking();

			if (!string.IsNullOrWhiteSpace(filtri.Testo))
			{
				var t = filtri.Testo.ToUpper();
				reclamiQuery = reclamiQuery.Where(r =>
					(r.EmailAutore != null && r.EmailAutore.ToUpper().Contains(t)) ||
					(r.CognomeAutore != null && r.CognomeAutore.ToUpper().Contains(t)) ||
					(r.Testo != null && r.Testo.ToUpper().Contains(t)));
			}
			if (!string.IsNullOrWhiteSpace(filtri.StatoPratica))
				reclamiQuery = reclamiQuery.Where(r => r.StatoPratica == filtri.StatoPratica);

			reclamiRighe = (await reclamiQuery.ToListAsync())
				.Select(r => new SegnalazioneRigaDto(
					TipoSegnalazione: "Reclamo",
					Id: r.IdReclamo,
					Codice: r.Codice,
					DataInserimento: r.DataInserimento,
					EmailAutore: r.EmailAutore,
					CognomeNomeAutore: FormatNome(r.CognomeAutore, r.NomeAutore),
					Tipologia: r.IdTipoRichiesta.HasValue && tipiRichiesta.TryGetValue(r.IdTipoRichiesta.Value, out var tr) ? tr : null,
					AreaTematica: null,
					StatoPratica: r.StatoPratica,
					OperatorePresaInCarico: r.OperatorePresaInCarico
				)).ToList();
		}

		var tutte = richiesteRighe.Concat(reclamiRighe)
			.OrderByDescending(x => x.DataInserimento)
			.ToList();

		var totale = tutte.Count;
		var righe = tutte.Skip((pagina - 1) * righePerPagina).Take(righePerPagina).ToList();
		return (righe, totale);
	}

	/// <summary>Seleziona la lettura del dettaglio in base al tipo, rifiutando valori diversi da richiesta e reclamo.</summary>
	public async Task<SegnalazioneDettaglioDto> GetDettaglioSegnalazioneAsync(long id, string tipo)
	{
		logger.LogDebug("GetDettaglioSegnalazioneAsync: id={id}, tipo={tipo}", id, tipo);

		var tipoNormalizzato = tipo.Trim().ToLowerInvariant();

		if (tipoNormalizzato == "richiesta")
			return await GetDettaglioRichiestaAsync(id);

		if (tipoNormalizzato == "reclamo")
			return await GetDettaglioReclamoAsync(id);

		throw new InvalidOperationException("Parametro 'tipo' non valido. Valori ammessi: richiesta o reclamo.");
	}

	// Risolve le classificazioni e gli allegati della richiesta, quindi prepara i campi comuni e quelli specifici della UI.
	private async Task<SegnalazioneDettaglioDto> GetDettaglioRichiestaAsync(long id)
	{
		logger.LogDebug("GetDettaglioRichiestaAsync: id={Id}", id);

		var richiesta = await db.Richieste.AsNoTracking()
			.FirstOrDefaultAsync(x => x.IdRichiesta == id)
			?? throw new InvalidOperationException("Segnalazione non trovata.");

		var tipologia = await ResolveLookupAsync(
			richiesta.IdTipoRichiesta,
			value => db.TipiRichiesta.AsNoTracking().FirstOrDefaultAsync(x => x.IdTipoRichiesta == value),
			x => x?.DescrizioneTipoRichiesta,
			"tipologia");
		var categoria = await ResolveLookupAsync(
			richiesta.IdTipoAutoreRichiesta,
			value => db.TipiAutoreRichiesta.AsNoTracking().FirstOrDefaultAsync(x => x.IdTipoAutoreRichiesta == value),
			x => x?.DescrizioneTipoAutoreRichiesta,
			"categoria");
		var tematica = await ResolveLookupAsync(
			richiesta.IdTipoTematica,
			value => db.TipiTematica.AsNoTracking().FirstOrDefaultAsync(x => x.IdTipoTematica == value),
			x => x?.DescrizioneTipoTematica,
			"tematica");
		var luogo = await ResolveLookupAsync(
			richiesta.IdTipoLuogo,
			value => db.TipiLuogo.AsNoTracking().FirstOrDefaultAsync(x => x.IdTipoLuogo == value),
			x => x?.DescrizioneTipoLuogo,
			"luogo");

		var allegati = await GetAllegatiRichiestaAsync(id);

		var mostraSezioneRichiesta = tipologia is not null && tipologia != "Reclamo";

		return new SegnalazioneDettaglioDto(
			Id: richiesta.IdRichiesta,
			Codice: richiesta.Codice,
			TipoSegnalazione: "richiesta",
			DataInserimento: richiesta.DataInserimento,
			Tipologia: tipologia ?? "",
			Email: richiesta.EmailAutore,
			Cognome: richiesta.CognomeAutore,
			Nome: richiesta.NomeAutore,
			Pec: richiesta.PecAutore,
			Categoria: categoria,
			Telefono: richiesta.TelefonoAutore,
			MostraSezioneRichiesta: mostraSezioneRichiesta,
			MostraSezioneReclamo: false,
			Tematica: tematica,
			Luogo: luogo,
			ComuneSpecifico: richiesta.ComuneLuogo,
			Richiesta: richiesta.Testo,
			Oggetto: null,
			Struttura: null,
			Allegati: allegati,
			StatoPratica: richiesta.StatoPratica ?? "Aperta",
			DataPresaInCarico: richiesta.DataPresaInCarico,
			OperatorePresaInCarico: richiesta.OperatorePresaInCarico,
			IdOperatorePresaInCarico: richiesta.IdOperatorePresaInCarico,
			AnnotazioniPresaInCarico: richiesta.AnnotazioniPresaInCarico,
			DataAssegnazioneAltroOperatore: richiesta.DataAssegnazioneAltroOperatore,
			OperatoreAssegnatario: richiesta.OperatoreAssegnatario,
			IdOperatoreAssegnatario: richiesta.IdOperatoreAssegnatario,
			AnnotazioniAssegnazione: richiesta.AnnotazioniAssegnazione,
			DataInoltroAltraStruttura: richiesta.DataInoltroAltraStruttura,
			StrutturaDestinataria: richiesta.StrutturaDestinataria,
			CodiceStrutturaDestinataria: richiesta.CodiceStrutturaDestinataria,
			AnnotazioniInoltro: richiesta.AnnotazioniInoltro,
			DataRispostaStruttura: richiesta.DataRispostaStruttura,
			RispostaDaStruttura: richiesta.RispostaDaStruttura,
			DataRisposta: richiesta.DataRisposta,
			TestoRisposta: richiesta.TestoRisposta,
			AnnotazioniRisposta: richiesta.AnnotazioniRisposta,
			DataChiusura: richiesta.DataChiusura,
			AnnotazioniChiusura: richiesta.AnnotazioniChiusura,
			UltimoAggiornamento: richiesta.UltimoAggiornamento,
			OperatoreUltimoAggiornamento: richiesta.OperatoreUltimoAggiornamento,
			Sorgente: richiesta.Sorgente,
			RagioneSociale: richiesta.RagioneSociale,
			Indirizzo: richiesta.Indirizzo
		);
	}

	// Il reclamo usa oggetto e struttura; i campi specifici della richiesta non sono valorizzati nel DTO.
	private async Task<SegnalazioneDettaglioDto> GetDettaglioReclamoAsync(long id)
	{
		logger.LogDebug("GetDettaglioReclamoAsync: id={Id}", id);

		var reclamo = await db.Reclami.AsNoTracking()
			.FirstOrDefaultAsync(x => x.IdReclamo == id)
			?? throw new InvalidOperationException("Segnalazione non trovata.");

		if (reclamo.IdTipoRichiesta.HasValue)
		{
			var tipoRichiesta = await db.TipiRichiesta.AsNoTracking()
				.FirstOrDefaultAsync(x => x.IdTipoRichiesta == reclamo.IdTipoRichiesta.Value);
			if (tipoRichiesta is null)
				throw CreateLookupException("tipologia");
		}

		var categoria = await ResolveLookupAsync(
			reclamo.IdTipoAutoreRichiesta,
			value => db.TipiAutoreRichiesta.AsNoTracking().FirstOrDefaultAsync(x => x.IdTipoAutoreRichiesta == value),
			x => x?.DescrizioneTipoAutoreRichiesta,
			"categoria");

		var struttura = await ResolveLookupAsync(
			reclamo.CodiceStruttura,
			value => db.TipiStruttura.AsNoTracking().FirstOrDefaultAsync(x => x.CodiceStruttura == value),
			x => x?.DescrizioneStruttura,
			"struttura");

		var allegati = await GetAllegatiReclamoAsync(id);

		return new SegnalazioneDettaglioDto(
			Id: reclamo.IdReclamo,
			Codice: reclamo.Codice,
			TipoSegnalazione: "reclamo",
			DataInserimento: reclamo.DataInserimento,
			Tipologia: "Reclamo",
			Email: reclamo.EmailAutore,
			Cognome: reclamo.CognomeAutore,
			Nome: reclamo.NomeAutore,
			Pec: reclamo.PecAutore,
			Categoria: categoria,
			Telefono: reclamo.TelefonoAutore,
			MostraSezioneRichiesta: false,
			MostraSezioneReclamo: true,
			Tematica: null,
			Luogo: null,
			ComuneSpecifico: null,
			Richiesta: null,
			Oggetto: reclamo.Testo,
			Struttura: struttura,
			Allegati: allegati,
			StatoPratica: reclamo.StatoPratica ?? "Aperta",
			DataPresaInCarico: reclamo.DataPresaInCarico,
			OperatorePresaInCarico: reclamo.OperatorePresaInCarico,
			IdOperatorePresaInCarico: reclamo.IdOperatorePresaInCarico,
			AnnotazioniPresaInCarico: reclamo.AnnotazioniPresaInCarico,
			DataAssegnazioneAltroOperatore: reclamo.DataAssegnazioneAltroOperatore,
			OperatoreAssegnatario: reclamo.OperatoreAssegnatario,
			IdOperatoreAssegnatario: reclamo.IdOperatoreAssegnatario,
			AnnotazioniAssegnazione: reclamo.AnnotazioniAssegnazione,
			DataInoltroAltraStruttura: reclamo.DataInoltroAltraStruttura,
			StrutturaDestinataria: reclamo.StrutturaDestinataria,
			CodiceStrutturaDestinataria: reclamo.CodiceStrutturaDestinataria,
			AnnotazioniInoltro: reclamo.AnnotazioniInoltro,
			DataRispostaStruttura: reclamo.DataRispostaStruttura,
			RispostaDaStruttura: reclamo.RispostaDaStruttura,
			DataRisposta: reclamo.DataRisposta,
			TestoRisposta: reclamo.TestoRisposta,
			AnnotazioniRisposta: reclamo.AnnotazioniRisposta,
			DataChiusura: reclamo.DataChiusura,
			AnnotazioniChiusura: reclamo.AnnotazioniChiusura,
			UltimoAggiornamento: reclamo.UltimoAggiornamento,
			OperatoreUltimoAggiornamento: reclamo.OperatoreUltimoAggiornamento,
			Sorgente: reclamo.Sorgente,
			RagioneSociale: reclamo.RagioneSociale,
			Indirizzo: null
		);
	}

	private static InvalidOperationException CreateLookupException(string lookup)
		=> new($"Incoerenza dei dati di riferimento: lookup '{lookup}' non risolto.");

	// Unisce soltanto le parti del nominativo presenti; restituisce null se entrambe mancano.
	private static string? FormatNome(string? cognome, string? nome)
	{
		var parts = new[] { cognome, nome }.Where(s => !string.IsNullOrWhiteSpace(s));
		var result = string.Join(" ", parts);
		return string.IsNullOrWhiteSpace(result) ? null : result;
	}

	// Un riferimento assente è ammesso; un ID valorizzato ma non risolvibile segnala un'incoerenza dei dati.
	private async Task<string?> ResolveLookupAsync<TLookup>(
		long? lookupId,
		Func<long, Task<TLookup?>> loadLookup,
		Func<TLookup?, string?> valueSelector,
		string lookupName)
		where TLookup : class
	{
		if (!lookupId.HasValue)
			return null;

		var lookup = await loadLookup(lookupId.Value);
		if (lookup is null)
			throw CreateLookupException(lookupName);

		return valueSelector(lookup);
	}

	// Applica la stessa distinzione tra riferimento assente e inesistente ai lookup con codice testuale.
	private async Task<string?> ResolveLookupAsync<TLookup>(
		string? lookupCode,
		Func<string, Task<TLookup?>> loadLookup,
		Func<TLookup?, string?> valueSelector,
		string lookupName)
		where TLookup : class
	{
		if (string.IsNullOrWhiteSpace(lookupCode))
			return null;

		var lookup = await loadLookup(lookupCode);
		if (lookup is null)
			throw CreateLookupException(lookupName);

		return valueSelector(lookup);
	}

	/// <summary>
	/// Legge gli eventi della segnalazione dal più recente al più vecchio;
	/// a parità di data usa l'identificativo del log per stabilire l'ordine.
	/// </summary>
	public async Task<List<RegistroAttivitaDto>> GetRegistroAttivitaAsync(long id, string tipo)
	{
		var tipoRegistro = GetTipoSegnalazioneRegistroDb(tipo);

		return await db.RegistroAttivita.AsNoTracking()
			.Where(x => x.TipoSegnalazione == tipoRegistro && x.IdSegnalazione == id)
			.OrderByDescending(x => x.DataOraEvento)
			.ThenByDescending(x => x.IdLog)
			.Select(x => new RegistroAttivitaDto(
				x.IdLog,
				x.TipoSegnalazione,
				x.IdSegnalazione,
				x.DataOraEvento,
				x.Operatore,
				x.StatoPratica,
				x.Dettagli))
			.ToListAsync();
	}

	// Il registro memorizza il tipo in maiuscolo, indipendentemente dal formato ricevuto dalla pagina.
	private static string GetTipoSegnalazioneRegistroDb(string tipo)
	{
		var tipoNormalizzato = tipo.Trim().ToLowerInvariant();

		if (tipoNormalizzato == "richiesta")
			return "RICHIESTA";

		if (tipoNormalizzato == "reclamo")
			return "RECLAMO";

		throw new InvalidOperationException("Parametro 'tipo' non valido. Valori ammessi: richiesta o reclamo.");
	}

	/// <summary>Riutilizza il flusso completo di salvataggio senza aggiungere o rimuovere allegati.</summary>
	public async Task UpdateGestionePraticaAsync(long id, string tipo, GestionePraticaUpdateDto data)
	{
		logger.LogDebug("UpdateGestionePraticaAsync: id={Id}, tipo={Tipo}, data={@Data}", id, tipo, data);
		await UpdateGestionePraticaConAllegatiAsync(id, tipo, data, [], []);
	}

	// I file sono conservati nelle cartelle configurate; una configurazione assente impedisce di ricostruire il percorso.
	private string GetCartellaAllegatiRichieste()
		=> configuration["CartellaAllegatiRichieste"] ?? throw new InvalidOperationException("Configurazione 'CartellaAllegatiRichieste' mancante.");

	private string GetCartellaAllegatiReclami()
		=> configuration["CartellaAllegatiReclami"] ?? throw new InvalidOperationException("Configurazione 'CartellaAllegatiReclami' mancante.");
}
