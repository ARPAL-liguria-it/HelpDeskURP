using GestioneURP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;

namespace GestioneURP.Application.Services;

public partial class SegnalazioniService
{
	/// <summary>
	/// Carica gli elenchi del form di inserimento, ordinati secondo le tabelle di classificazione.
	/// La voce Reclamo è esclusa dalle tipologie di richiesta perché dispone di un percorso di inserimento distinto.
	/// </summary>
	public async Task<NuovaSegnalazioneLookupDto> GetLookupNuovaSegnalazioneAsync()
	{
		var tipiRichiesta = await db.TipiRichiesta.AsNoTracking()
			.Where(x => x.DescrizioneTipoRichiesta != null && x.DescrizioneTipoRichiesta.ToUpper() != "RECLAMO")
			.OrderBy(x => x.Ordine)
			.ThenBy(x => x.DescrizioneTipoRichiesta)
			.Select(x => new LookupLongDto(x.IdTipoRichiesta, x.DescrizioneTipoRichiesta!))
			.ToListAsync();

		var tipiSorgente = await db.TipiSorgente.AsNoTracking()
			.OrderBy(x => x.Ordine)
			.ThenBy(x => x.TipoSorgenteValue)
			.Select(x => new LookupStringDto(x.TipoSorgenteValue, x.TipoSorgenteValue))
			.ToListAsync();

		var tipiAutore = await db.TipiAutoreRichiesta.AsNoTracking()
			.OrderBy(x => x.Ordine)
			.ThenBy(x => x.DescrizioneTipoAutoreRichiesta)
			.Select(x => new LookupLongDto(x.IdTipoAutoreRichiesta, x.DescrizioneTipoAutoreRichiesta ?? string.Empty))
			.ToListAsync();

		var tipiTematica = await db.TipiTematica.AsNoTracking()
			.OrderBy(x => x.Ordine)
			.ThenBy(x => x.DescrizioneTipoTematica)
			.Select(x => new LookupLongDto(x.IdTipoTematica, x.DescrizioneTipoTematica ?? string.Empty))
			.ToListAsync();

		var tipiLuogo = await db.TipiLuogo.AsNoTracking()
			.OrderBy(x => x.Ordine)
			.ThenBy(x => x.DescrizioneTipoLuogo)
			.Select(x => new LookupLongDto(x.IdTipoLuogo, x.DescrizioneTipoLuogo ?? string.Empty))
			.ToListAsync();

		var tipiStruttura = await db.TipiStruttura.AsNoTracking()
			.OrderBy(x => x.Ordine)
			.ThenBy(x => x.DescrizioneStruttura)
			.Select(x => new LookupStringDto(x.CodiceStruttura, x.DescrizioneStruttura ?? x.CodiceStruttura))
			.ToListAsync();

		return new NuovaSegnalazioneLookupDto(
			tipiRichiesta,
			tipiSorgente,
			tipiAutore,
			tipiTematica,
			tipiLuogo,
			tipiStruttura);
	}

	/// <summary>
	/// Seleziona il flusso di creazione in base al tipo e restituisce l'ID della nuova segnalazione.
	/// Ogni flusso registra lo stato iniziale Aperta, l'attività di inserimento e gli eventuali allegati.
	/// </summary>
	public async Task<long> CreaNuovaSegnalazioneAsync(NuovaSegnalazioneInputDto data, IReadOnlyCollection<AllegatoNuovoUploadDto> nuoviAllegati)
	{
		ArgumentNullException.ThrowIfNull(data);

		var tipo = data.TipoSegnalazione.Trim().ToLowerInvariant();
		if (tipo == "richiesta")
			return await CreaRichiestaAsync(data, nuoviAllegati);

		if (tipo == "reclamo")
			return await CreaReclamoAsync(data, nuoviAllegati);

		throw new InvalidOperationException("Tipo segnalazione non valido. Valori ammessi: richiesta o reclamo.");
	}

	// Verifica classificazioni e comune quando richiesto; transazione e compensazione dei file coordinano il salvataggio.
	private async Task<long> CreaRichiestaAsync(NuovaSegnalazioneInputDto data, IReadOnlyCollection<AllegatoNuovoUploadDto> nuoviAllegati)
	{
		ValidaNuoviAllegati([], nuoviAllegati);
		await using var transaction = await db.Database.BeginTransactionAsync();
		var fileDaRipristinare = new List<(string PercorsoOriginale, string PercorsoBackup)>();
		var fileCreati = new List<string>();

		try
		{
			var tipoRichiesta = await db.TipiRichiesta.AsNoTracking()
				.FirstOrDefaultAsync(x => x.IdTipoRichiesta == data.IdTipoRichiesta)
				?? throw CreateLookupException("tipologia richiesta");

			if (string.Equals(tipoRichiesta.DescrizioneTipoRichiesta, "Reclamo", StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("La tipologia selezionata non è valida per una richiesta.");

			var tipoSorgente = await db.TipiSorgente.AsNoTracking()
				.FirstOrDefaultAsync(x => x.TipoSorgenteValue == data.Sorgente)
				?? throw CreateLookupException("sorgente");

			_ = await db.TipiAutoreRichiesta.AsNoTracking()
				.FirstOrDefaultAsync(x => x.IdTipoAutoreRichiesta == data.IdTipoAutoreRichiesta)
				?? throw CreateLookupException("categoria");

			_ = await db.TipiTematica.AsNoTracking()
				.FirstOrDefaultAsync(x => x.IdTipoTematica == data.IdTipoTematica)
				?? throw CreateLookupException("tematica");

			var tipoLuogo = await db.TipiLuogo.AsNoTracking()
				.FirstOrDefaultAsync(x => x.IdTipoLuogo == data.IdTipoLuogo)
				?? throw CreateLookupException("luogo");

			var comuneObbligatorio = string.Equals(tipoLuogo.DescrizioneTipoLuogo, "Comune specifico", StringComparison.Ordinal);
			var comuneLuogo = comuneObbligatorio ? PulisciValore(data.ComuneLuogo) : null;
			if (comuneObbligatorio && string.IsNullOrWhiteSpace(comuneLuogo))
				throw new InvalidOperationException("Il campo 'Comune specifico' è obbligatorio per il luogo selezionato.");

			var timestamp = DateTime.Now;
			var operatore = GetOperatoreCorrente();

			var richiesta = new Richiesta
			{
				Codice = await GetCodice("richiesta"),
				DataInserimento = timestamp,
				UltimoAggiornamento = timestamp,
				OperatoreUltimoAggiornamento = operatore,
				StatoPratica = "Aperta",
				IdTipoRichiesta = tipoRichiesta.IdTipoRichiesta,
				Sorgente = tipoSorgente.TipoSorgenteValue,
				CognomeAutore = PulisciValore(data.Cognome),
				NomeAutore = PulisciValore(data.Nome),
				EmailAutore = PulisciValore(data.Email),
				PecAutore = PulisciValore(data.Pec),
				TelefonoAutore = PulisciValore(data.Telefono),
				RagioneSociale = PulisciValore(data.RagioneSociale),
				IdTipoAutoreRichiesta = data.IdTipoAutoreRichiesta,
				IdTipoTematica = data.IdTipoTematica,
				IdTipoLuogo = data.IdTipoLuogo,
				ComuneLuogo = comuneLuogo,
				Indirizzo = PulisciValore(data.Indirizzo),
				Testo = PulisciValore(data.Testo)
			};

			// Il primo salvataggio ottiene l'ID necessario per collegare allegati ed evento iniziale nella stessa transazione.
			db.Richieste.Add(richiesta);
			await db.SaveChangesAsync();

			var nuovi = CreaAllegatiRichiesta(richiesta.IdRichiesta, nuoviAllegati);
			if (nuovi.Count > 0)
				db.AllegatiRichieste.AddRange(nuovi);

			db.RegistroAttivita.Add(new RegistroAttivita
			{
				TipoSegnalazione = "RICHIESTA",
				IdSegnalazione = richiesta.IdRichiesta,
				DataOraEvento = timestamp,
				Operatore = operatore,
				StatoPratica = "Aperta",
				Dettagli = "Inserimento iniziale"
			});

			await db.SaveChangesAsync();

			await GestisciFileAllegatiAsync(
				GetCartellaAllegatiRichieste(),
				[],
				MapNuoviAllegatiPerSalvataggio(nuovi, nuoviAllegati),
				fileDaRipristinare,
				fileCreati);

			await transaction.CommitAsync();
			return richiesta.IdRichiesta;
		}
		catch
		{
			await transaction.RollbackAsync();
			RipristinaFileAllegati(fileCreati, fileDaRipristinare);
			throw;
		}
		finally
		{
			PulisciBackup(fileDaRipristinare);
		}
	}

	// Per il reclamo risolve automaticamente la tipologia Reclamo e verifica la struttura indicata dal form.
	private async Task<long> CreaReclamoAsync(NuovaSegnalazioneInputDto data, IReadOnlyCollection<AllegatoNuovoUploadDto> nuoviAllegati)
	{
		ValidaNuoviAllegati([], nuoviAllegati);
		await using var transaction = await db.Database.BeginTransactionAsync();
		var fileDaRipristinare = new List<(string PercorsoOriginale, string PercorsoBackup)>();
		var fileCreati = new List<string>();

		try
		{
			var tipoSorgente = await db.TipiSorgente.AsNoTracking()
				.FirstOrDefaultAsync(x => x.TipoSorgenteValue == data.Sorgente)
				?? throw CreateLookupException("sorgente");

			_ = await db.TipiAutoreRichiesta.AsNoTracking()
				.FirstOrDefaultAsync(x => x.IdTipoAutoreRichiesta == data.IdTipoAutoreRichiesta)
				?? throw CreateLookupException("categoria");

			_ = await db.TipiStruttura.AsNoTracking()
				.FirstOrDefaultAsync(x => x.CodiceStruttura == data.CodiceStruttura)
				?? throw CreateLookupException("struttura");

			var tipoReclamo = await db.TipiRichiesta.AsNoTracking()
				.FirstOrDefaultAsync(x => x.DescrizioneTipoRichiesta != null && x.DescrizioneTipoRichiesta.ToUpper() == "RECLAMO")
				?? throw CreateLookupException("tipologia reclamo");

			var timestamp = DateTime.Now;
			var operatore = GetOperatoreCorrente();

			var reclamo = new Reclamo
			{
				Codice = await GetCodice("reclamo"),
				DataInserimento = timestamp,
				UltimoAggiornamento = timestamp,
				OperatoreUltimoAggiornamento = operatore,
				StatoPratica = "Aperta",
				IdTipoRichiesta = tipoReclamo.IdTipoRichiesta,
				Sorgente = tipoSorgente.TipoSorgenteValue,
				CognomeAutore = PulisciValore(data.Cognome),
				NomeAutore = PulisciValore(data.Nome),
				EmailAutore = PulisciValore(data.Email),
				PecAutore = PulisciValore(data.Pec),
				TelefonoAutore = PulisciValore(data.Telefono),
				RagioneSociale = PulisciValore(data.RagioneSociale),
				IdTipoAutoreRichiesta = data.IdTipoAutoreRichiesta,
				CodiceStruttura = PulisciValore(data.CodiceStruttura),
				Testo = PulisciValore(data.Testo)
			};

			db.Reclami.Add(reclamo);
			await db.SaveChangesAsync();

			var nuovi = CreaAllegatiReclamo(reclamo.IdReclamo, nuoviAllegati);
			if (nuovi.Count > 0)
				db.AllegatiReclami.AddRange(nuovi);

			db.RegistroAttivita.Add(new RegistroAttivita
			{
				TipoSegnalazione = "RECLAMO",
				IdSegnalazione = reclamo.IdReclamo,
				DataOraEvento = timestamp,
				Operatore = operatore,
				StatoPratica = "Aperta",
				Dettagli = "Inserimento iniziale"
			});

			await db.SaveChangesAsync();

			await GestisciFileAllegatiAsync(
				GetCartellaAllegatiReclami(),
				[],
				MapNuoviAllegatiPerSalvataggio(nuovi, nuoviAllegati),
				fileDaRipristinare,
				fileCreati);

			await transaction.CommitAsync();
			return reclamo.IdReclamo;
		}
		catch
		{
			await transaction.RollbackAsync();
			RipristinaFileAllegati(fileCreati, fileDaRipristinare);
			throw;
		}
		finally
		{
			PulisciBackup(fileDaRipristinare);
		}
	}

	// Gli allegati iniziali sono distinti da quelli della risposta mediante la sorgente Richiesta o Reclamo.
	private static List<AllegatoRichiesta> CreaAllegatiRichiesta(long idRichiesta, IReadOnlyCollection<AllegatoNuovoUploadDto> nuoviAllegati)
		=> nuoviAllegati.Select(upload => new AllegatoRichiesta
		{
			IdRichiesta = idRichiesta,
			NomeFile = NormalizzaNomeFile(upload.NomeFile),
			Sorgente = "Richiesta"
		}).ToList();

	private static List<AllegatoReclamo> CreaAllegatiReclamo(long idReclamo, IReadOnlyCollection<AllegatoNuovoUploadDto> nuoviAllegati)
		=> nuoviAllegati.Select(upload => new AllegatoReclamo
		{
			IdReclamo = idReclamo,
			NomeFile = NormalizzaNomeFile(upload.NomeFile),
			Sorgente = "Reclamo"
		}).ToList();

	// Dopo il salvataggio dei metadati associa gli ID assegnati ai byte dell'upload tramite il nome normalizzato.
	private static List<(long IdAllegato, byte[] Contenuto)> MapNuoviAllegatiPerSalvataggio<TAllegato>(
		IReadOnlyCollection<TAllegato> allegatiCreati,
		IReadOnlyCollection<AllegatoNuovoUploadDto> upload)
		where TAllegato : class
	{
		if (allegatiCreati.Count == 0)
			return [];

		var mappaUpload = upload.ToDictionary(x => NormalizzaNomeFile(x.NomeFile), x => x.Contenuto, StringComparer.OrdinalIgnoreCase);
		var risultato = new List<(long IdAllegato, byte[] Contenuto)>();

		foreach (var allegato in allegatiCreati)
		{
			if (allegato is AllegatoRichiesta richiesta)
			{
				if (!mappaUpload.TryGetValue(richiesta.NomeFile!, out var contenuto))
					throw new InvalidOperationException("Errore interno durante la gestione degli allegati.");
				risultato.Add((richiesta.IdAllegato, contenuto));
			}
			else if (allegato is AllegatoReclamo reclamo)
			{
				if (!mappaUpload.TryGetValue(reclamo.NomeFile!, out var contenuto))
					throw new InvalidOperationException("Errore interno durante la gestione degli allegati.");
				risultato.Add((reclamo.IdAllegato, contenuto));
			}
		}

		return risultato;
	}

	// Il flusso di inserimento usa n/d come ripiego se lo UserName non è disponibile.
	private string GetOperatoreCorrente()
		=> string.IsNullOrWhiteSpace(operatoreState.UserName) ? "n/d" : operatoreState.UserName!;

	// Uniforma i valori del form rimuovendo gli spazi esterni e trasformando i testi vuoti in null.
	private static string? PulisciValore(string? value)
		=> string.IsNullOrWhiteSpace(value) ? null : value.Trim();

	/// <summary>
	/// Incrementa il contatore Oracle del tipo indicato e ne legge il nuovo valore con UPDATE RETURNING,
	/// evitando una lettura separata del progressivo. Compone quindi il codice con prefisso RI o RC.
	/// </summary>
	private async Task<string> GetCodice(string tipo)
	{
		var tipoContatore = tipo.ToLowerInvariant() switch
		{
			"richiesta" => "Richieste",
			"reclamo" => "Reclami",
			_ => throw new InvalidOperationException("Parametro 'tipo' non valido. Valori ammessi: richiesta o reclamo.")
		};
		var connection = db.Database.GetDbConnection();

		if (connection.State != System.Data.ConnectionState.Open)
			await connection.OpenAsync();

		await using var cmd = connection.CreateCommand();

		if (cmd is OracleCommand oracleCmd)
		{
			oracleCmd.BindByName = true;
		}

		cmd.CommandText = @"
			UPDATE CONTATORI
			SET valore = valore + 1
			WHERE contatore = :tipoContatore
			RETURNING valore INTO :nuovovalore";

		var nuovoValoreParam = new OracleParameter(
			"nuovovalore",
			OracleDbType.Decimal)
		{
			Direction = ParameterDirection.Output
		};

		var tipoContatoreParam = new OracleParameter(
			"tipoContatore",
			OracleDbType.Varchar2,
			tipoContatore,
			ParameterDirection.Input);

		// ordine coerente con la query
		cmd.Parameters.Add(tipoContatoreParam);
		cmd.Parameters.Add(nuovoValoreParam);

		await cmd.ExecuteNonQueryAsync();

		var oracleValue = (OracleDecimal)nuovoValoreParam.Value;

		if (oracleValue.IsNull)
		{
			throw new InvalidOperationException(
				$"Il contatore '{tipoContatore}' non ha restituito un valore.");
		}

		int progressivo = oracleValue.ToInt32();

		return tipo.ToLowerInvariant() switch
		{
			"richiesta" => $"RI{progressivo}",
			"reclamo" => $"RC{progressivo}",
			_ => throw new InvalidOperationException("Parametro 'tipo' non valido. Valori ammessi: richiesta o reclamo.")
		};
	}
}