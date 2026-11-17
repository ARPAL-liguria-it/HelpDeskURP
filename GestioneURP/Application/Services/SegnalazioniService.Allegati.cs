using GestioneURP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestioneURP.Application.Services;

public partial class SegnalazioniService
{
	// I metadati provengono dal database; la data di caricamento è l'ultima modifica del file nominato con l'ID.
	private async Task<List<AllegatoDettaglioDto>> GetAllegatiRichiestaAsync(long id)
	{
		var allegati = await db.AllegatiRichieste.AsNoTracking()
			.Where(x => x.IdRichiesta == id && !string.IsNullOrWhiteSpace(x.NomeFile))
			.OrderBy(x => x.IdAllegato)
			.Select(x => new { x.IdAllegato, x.NomeFile, x.Sorgente })
			.ToListAsync();

		var cartella = GetCartellaAllegatiRichieste();
		return allegati
			.Select(x => new AllegatoDettaglioDto(x.IdAllegato, x.NomeFile!, GetDataCaricamentoAllegato(cartella, x.IdAllegato), x.Sorgente))
			.ToList();
	}

	// Applica la stessa ricostruzione ai reclami usando la loro tabella e cartella dedicate.
	private async Task<List<AllegatoDettaglioDto>> GetAllegatiReclamoAsync(long id)
	{
		var allegati = await db.AllegatiReclami.AsNoTracking()
			.Where(x => x.IdReclamo == id && !string.IsNullOrWhiteSpace(x.NomeFile))
			.OrderBy(x => x.IdAllegato)
			.Select(x => new { x.IdAllegato, x.NomeFile, x.Sorgente })
			.ToListAsync();

		var cartella = GetCartellaAllegatiReclami();
		return allegati
			.Select(x => new AllegatoDettaglioDto(x.IdAllegato, x.NomeFile!, GetDataCaricamentoAllegato(cartella, x.IdAllegato), x.Sorgente))
			.ToList();
	}

	/// <summary>
	/// Rilegge e valida la pratica, quindi salva i dati di gestione insieme alle aggiunte e rimozioni degli allegati.
	/// I metadati sono gestiti in transazione; le modifiche ai file sono compensate in caso di errore.
	/// </summary>
	public async Task UpdateGestionePraticaConAllegatiAsync(
		long id,
		string tipo,
		GestionePraticaUpdateDto data,
		IReadOnlyCollection<AllegatoNuovoUploadDto> nuoviAllegati,
		IReadOnlyCollection<long> allegatiDaRimuovere,
		string? statoAtteso = null)
	{
		var tipoNormalizzato = tipo.Trim().ToLowerInvariant();
		// Si parte da entità rilette dal database, senza riutilizzare modifiche rimaste nel contesto da operazioni precedenti.
		db.ChangeTracker.Clear();
		try
		{
			if (tipoNormalizzato == "richiesta")
			{
				var richiesta = await db.Richieste.Include(x => x.Allegati)
					.FirstOrDefaultAsync(x => x.IdRichiesta == id)
					?? throw new InvalidOperationException("Segnalazione non trovata.");
				data = ValidaGestioneDaSalvare(richiesta, data, statoAtteso);
				await SalvaRichiestaConAllegatiAsync(richiesta, data, nuoviAllegati, allegatiDaRimuovere);
				return;
			}

			if (tipoNormalizzato == "reclamo")
			{
				var reclamo = await db.Reclami.Include(x => x.Allegati)
					.FirstOrDefaultAsync(x => x.IdReclamo == id)
					?? throw new InvalidOperationException("Segnalazione non trovata.");
				data = ValidaGestioneDaSalvare(reclamo, data, statoAtteso);
				await SalvaReclamoConAllegatiAsync(reclamo, data, nuoviAllegati, allegatiDaRimuovere);
				return;
			}

			throw new InvalidOperationException("Parametro 'tipo' non valido. Valori ammessi: richiesta o reclamo.");
		}
		finally
		{
			// Un rollback non annulla lo stato delle entità nel ChangeTracker.
			db.ChangeTracker.Clear();
		}
	}

	/// <summary>
	/// Marca la segnalazione come eliminata e aggiorna data e operatore dell'ultima modifica.
	/// Non rimuove fisicamente la riga, i metadati degli allegati o i relativi file.
	/// </summary>
	public async Task EliminaLogicamenteSegnalazioneAsync(long id, string tipo)
	{
		var tipoNormalizzato = tipo.Trim().ToLowerInvariant();

		if (tipoNormalizzato == "richiesta")
		{
			var richiesta = await db.Richieste
				.FirstOrDefaultAsync(x => x.IdRichiesta == id)
				?? throw new InvalidOperationException("Segnalazione non trovata.");

			await EliminaLogicamenteRichiestaAsync(richiesta);
			return;
		}

		if (tipoNormalizzato == "reclamo")
		{
			var reclamo = await db.Reclami
				.FirstOrDefaultAsync(x => x.IdReclamo == id)
				?? throw new InvalidOperationException("Segnalazione non trovata.");

			await EliminaLogicamenteReclamoAsync(reclamo);
			return;
		}

		throw new InvalidOperationException("Parametro 'tipo' non valido. Valori ammessi: richiesta o reclamo.");
	}

	private async Task EliminaLogicamenteRichiestaAsync(Richiesta richiesta)
	{
		await using var transaction = await db.Database.BeginTransactionAsync();
		try
		{
			var timestamp = DateTime.Now;
			richiesta.Eliminata = "S";
			richiesta.UltimoAggiornamento = timestamp;
			richiesta.OperatoreUltimoAggiornamento = operatoreState.UserName;

			await db.SaveChangesAsync();
			await transaction.CommitAsync();
		}
		catch
		{
			await transaction.RollbackAsync();
			throw;
		}
	}

	private async Task EliminaLogicamenteReclamoAsync(Reclamo reclamo)
	{
		await using var transaction = await db.Database.BeginTransactionAsync();
		try
		{
			var timestamp = DateTime.Now;
			reclamo.Eliminato = "S";
			reclamo.UltimoAggiornamento = timestamp;
			reclamo.OperatoreUltimoAggiornamento = operatoreState.UserName;

			await db.SaveChangesAsync();
			await transaction.CommitAsync();
		}
		catch
		{
			await transaction.RollbackAsync();
			throw;
		}
	}

	// Salva gestione, eventuale evento di cambio stato e metadati; gli ID assegnati servono poi per i file fisici.
	private async Task SalvaRichiestaConAllegatiAsync(
		Richiesta richiesta,
		GestionePraticaUpdateDto data,
		IReadOnlyCollection<AllegatoNuovoUploadDto> nuoviAllegati,
		IReadOnlyCollection<long> allegatiDaRimuovere)
	{
		var allegatiDaEliminare = richiesta.Allegati.Where(x => allegatiDaRimuovere.Contains(x.IdAllegato)).ToList();
		var allegatiAttivi = richiesta.Allegati.Where(x => !allegatiDaRimuovere.Contains(x.IdAllegato)).ToList();
		ValidaNuoviAllegati(allegatiAttivi.Select(x => x.NomeFile), nuoviAllegati);

		await using var transaction = await db.Database.BeginTransactionAsync();
		var allegatiNuoviDaPersistire = new List<AllegatoRichiesta>();
		var fileDaRipristinare = new List<(string PercorsoOriginale, string PercorsoBackup)>();
		var fileCreati = new List<string>();

		try
		{
			var statoPrecedente = richiesta.StatoPratica ?? "Aperta";

			ApplicaDatiGestione(richiesta, data);
			var statoCorrente = richiesta.StatoPratica ?? "Aperta";
			if (StatoModificato(statoPrecedente, statoCorrente))
				db.RegistroAttivita.Add(CreaVoceRegistroAttivita("RICHIESTA", richiesta.IdRichiesta, statoCorrente, data, operatoreState.UserName!));

			foreach (var upload in nuoviAllegati)
			{
				var entity = new AllegatoRichiesta
				{
					IdRichiesta = richiesta.IdRichiesta,
					NomeFile = NormalizzaNomeFile(upload.NomeFile),
					Sorgente = "Risposta"
				};

				db.AllegatiRichieste.Add(entity);
				allegatiNuoviDaPersistire.Add(entity);
			}

			foreach (var allegato in allegatiDaEliminare)
				db.AllegatiRichieste.Remove(allegato);

			await db.SaveChangesAsync();

			await GestisciFileAllegatiAsync(
				GetCartellaAllegatiRichieste(),
				allegatiDaEliminare.Select(x => x.IdAllegato).ToList(),
				allegatiNuoviDaPersistire.Select(x => (x.IdAllegato, nuoviAllegati.First(a => NormalizzaNomeFile(a.NomeFile) == x.NomeFile).Contenuto)).ToList(),
				fileDaRipristinare,
				fileCreati);

			await transaction.CommitAsync();
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

	// Ripete il coordinamento tra database e filesystem per le entità e la cartella degli allegati del reclamo.
	private async Task SalvaReclamoConAllegatiAsync(
		Reclamo reclamo,
		GestionePraticaUpdateDto data,
		IReadOnlyCollection<AllegatoNuovoUploadDto> nuoviAllegati,
		IReadOnlyCollection<long> allegatiDaRimuovere)
	{
		var allegatiDaEliminare = reclamo.Allegati.Where(x => allegatiDaRimuovere.Contains(x.IdAllegato)).ToList();
		var allegatiAttivi = reclamo.Allegati.Where(x => !allegatiDaRimuovere.Contains(x.IdAllegato)).ToList();
		ValidaNuoviAllegati(allegatiAttivi.Select(x => x.NomeFile), nuoviAllegati);

		await using var transaction = await db.Database.BeginTransactionAsync();
		var allegatiNuoviDaPersistire = new List<AllegatoReclamo>();
		var fileDaRipristinare = new List<(string PercorsoOriginale, string PercorsoBackup)>();
		var fileCreati = new List<string>();

		try
		{
			var statoPrecedente = reclamo.StatoPratica ?? "Aperta";

			ApplicaDatiGestione(reclamo, data);
			var statoCorrente = reclamo.StatoPratica ?? "Aperta";
			if (StatoModificato(statoPrecedente, statoCorrente))
				db.RegistroAttivita.Add(CreaVoceRegistroAttivita("RECLAMO", reclamo.IdReclamo, statoCorrente, data, operatoreState.UserName!));

			foreach (var upload in nuoviAllegati)
			{
				var entity = new AllegatoReclamo
				{
					IdReclamo = reclamo.IdReclamo,
					NomeFile = NormalizzaNomeFile(upload.NomeFile),
					Sorgente = "Risposta"
				};

				db.AllegatiReclami.Add(entity);
				allegatiNuoviDaPersistire.Add(entity);
			}

			foreach (var allegato in allegatiDaEliminare)
				db.AllegatiReclami.Remove(allegato);

			await db.SaveChangesAsync();

			await GestisciFileAllegatiAsync(
				GetCartellaAllegatiReclami(),
				allegatiDaEliminare.Select(x => x.IdAllegato).ToList(),
				allegatiNuoviDaPersistire.Select(x => (x.IdAllegato, nuoviAllegati.First(a => NormalizzaNomeFile(a.NomeFile) == x.NomeFile).Contenuto)).ToList(),
				fileDaRipristinare,
				fileCreati);

			await transaction.CommitAsync();
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

	/// <summary>
	/// Conserva una copia temporanea dei file da eliminare e scrive i nuovi file usando l'ID come nome fisico.
	/// Le liste di backup e file creati consentono al chiamante di tentare il ripristino se il salvataggio fallisce:
	/// il filesystem non partecipa alla transazione del database.
	/// </summary>
	private static async Task GestisciFileAllegatiAsync(
		string cartella,
		IReadOnlyCollection<long> allegatiDaEliminareIds,
		IReadOnlyCollection<(long IdAllegato, byte[] Contenuto)> nuoviAllegati,
		List<(string PercorsoOriginale, string PercorsoBackup)> fileDaRipristinare,
		List<string> fileCreati)
	{
		Directory.CreateDirectory(cartella);

		foreach (var idAllegato in allegatiDaEliminareIds)
		{
			var percorsoOriginale = Path.Combine(cartella, idAllegato.ToString());
			if (!File.Exists(percorsoOriginale))
				continue;

			var percorsoBackup = Path.GetTempFileName();
			File.Copy(percorsoOriginale, percorsoBackup, true);
			fileDaRipristinare.Add((percorsoOriginale, percorsoBackup));
			File.Delete(percorsoOriginale);
		}

		foreach (var allegato in nuoviAllegati)
		{
			var percorso = Path.Combine(cartella, allegato.IdAllegato.ToString());
			await File.WriteAllBytesAsync(percorso, allegato.Contenuto);
			File.SetLastWriteTime(percorso, DateTime.Now);
			fileCreati.Add(percorso);
		}
	}

	/// <summary>
	/// Controlla estensione, dimensione, numero complessivo e nomi duplicati, senza distinguere maiuscole e minuscole.
	/// Il chiamante passa solo gli allegati esistenti che rimarranno attivi dopo le rimozioni.
	/// </summary>
	private void ValidaNuoviAllegati(
		IEnumerable<string?> allegatiEsistenti,
		IReadOnlyCollection<AllegatoNuovoUploadDto> nuoviAllegati)
	{
		if (nuoviAllegati.Count == 0)
			return;

		var estensioniConsentite = GetEstensioniAllegati();
		var dimensioneMassimaBytes = GetDimensioneMassimaAllegatoBytes();
		var numeroMassimoAllegati = GetNumeroMassimoAllegati();
		var nomiAttivi = new HashSet<string>(allegatiEsistenti.Where(x => !string.IsNullOrWhiteSpace(x)).Select(NormalizzaNomeFile), StringComparer.OrdinalIgnoreCase);
		var totaleAttivo = nomiAttivi.Count;

		foreach (var allegato in nuoviAllegati)
		{
			var nomeFile = NormalizzaNomeFile(allegato.NomeFile);
			var estensione = Path.GetExtension(nomeFile);

			if (!estensioniConsentite.Contains(estensione))
				throw new InvalidOperationException("Estensione allegato non consentita.");

			if (allegato.Contenuto.LongLength > dimensioneMassimaBytes)
				throw new InvalidOperationException("Dimensione allegato superiore al massimo consentito.");

			if (totaleAttivo + 1 > numeroMassimoAllegati)
				throw new InvalidOperationException("Numero massimo di allegati superato.");

			if (!nomiAttivi.Add(nomeFile))
				throw new InvalidOperationException("Esiste già un allegato con lo stesso nome file.");

			totaleAttivo++;
		}
	}

	// Conserva il solo nome del file, senza eventuali componenti di percorso fornite con l'upload.
	private static string NormalizzaNomeFile(string? nomeFile)
	{
		if (string.IsNullOrWhiteSpace(nomeFile))
			throw new InvalidOperationException("Nome file allegato non valido.");

		return Path.GetFileName(nomeFile.Trim());
	}

	// Uniforma le estensioni configurate; se l'elenco è vuoto usa i formati predefiniti.
	private HashSet<string> GetEstensioniAllegati()
	{
		var valori = configuration.GetSection("EstensioniAllegati").GetChildren()
			.Select(x => x.Value)
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Select(x => x!.Trim().StartsWith('.') ? x!.Trim().ToLowerInvariant() : "." + x!.Trim().ToLowerInvariant())
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		return valori.Count > 0
			? valori
			: new HashSet<string>([".pdf", ".png", ".jpg", ".jpeg", ".doc", ".docx"], StringComparer.OrdinalIgnoreCase);
	}

	// Le configurazioni mancanti o non positive usano i limiti predefiniti di 10 MB e 10 allegati.
	private long GetDimensioneMassimaAllegatoBytes()
		=> int.TryParse(configuration["DimensioneMassimaAllegatoMB"], out var megabyte) && megabyte > 0
			? megabyte * 1024L * 1024L
			: 10 * 1024L * 1024L;

	private int GetNumeroMassimoAllegati()
		=> int.TryParse(configuration["NumeroMassimoAllegati"], out var massimo) && massimo > 0 ? massimo : 10;

	// Un file non disponibile non impedisce la lettura dei metadati: la data viene restituita come null.
	private static DateTime? GetDataCaricamentoAllegato(string? cartella, long idAllegato)
	{
		if (string.IsNullOrWhiteSpace(cartella))
			return null;

		var percorso = Path.Combine(cartella, idAllegato.ToString());
		return File.Exists(percorso) ? File.GetLastWriteTime(percorso) : null;
	}

	// Copia i dati già validati e attribuisce l'ultima modifica all'operatore corrente.
	private void ApplicaDatiGestione(Richiesta richiesta, GestionePraticaUpdateDto data)
	{
		richiesta.UltimoAggiornamento = DateTime.Now;
		richiesta.OperatoreUltimoAggiornamento = operatoreState.UserName;
		richiesta.StatoPratica = data.StatoPratica;
		richiesta.DataPresaInCarico = data.DataPresaInCarico;
		richiesta.OperatorePresaInCarico = data.OperatorePresaInCarico;
		richiesta.IdOperatorePresaInCarico = data.IdOperatorePresaInCarico;
		richiesta.AnnotazioniPresaInCarico = data.AnnotazioniPresaInCarico;
		richiesta.DataAssegnazioneAltroOperatore = data.DataAssegnazioneAltroOperatore;
		richiesta.OperatoreAssegnatario = data.OperatoreAssegnatario;
		richiesta.IdOperatoreAssegnatario = data.IdOperatoreAssegnatario;
		richiesta.AnnotazioniAssegnazione = data.AnnotazioniAssegnazione;
		richiesta.DataInoltroAltraStruttura = data.DataInoltroAltraStruttura;
		richiesta.StrutturaDestinataria = data.StrutturaDestinataria;
		richiesta.CodiceStrutturaDestinataria = data.CodiceStrutturaDestinataria;
		richiesta.AnnotazioniInoltro = data.AnnotazioniInoltro;
		richiesta.DataRispostaStruttura = data.DataRispostaStruttura;
		richiesta.RispostaDaStruttura = data.RispostaDaStruttura;
		richiesta.DataRisposta = data.DataRisposta;
		richiesta.TestoRisposta = data.TestoRisposta;
		richiesta.AnnotazioniRisposta = data.AnnotazioniRisposta;
		richiesta.DataChiusura = data.DataChiusura;
		richiesta.AnnotazioniChiusura = data.AnnotazioniChiusura;
	}

	private static bool StatoModificato(string statoPrecedente, string statoCorrente)
		=> !string.Equals(statoPrecedente, statoCorrente, StringComparison.Ordinal);

	// I salvataggi della gestione creano un evento soltanto quando lo stato cambia.
	private static RegistroAttivita CreaVoceRegistroAttivita(
		string tipoSegnalazione,
		long idSegnalazione,
		string statoPratica,
		GestionePraticaUpdateDto data,
		string operatore)
		=> new()
		{
			TipoSegnalazione = tipoSegnalazione,
			IdSegnalazione = idSegnalazione,
			DataOraEvento = DateTime.Now,
			Operatore = operatore,
			StatoPratica = statoPratica,
			Dettagli = CreaDettagliRegistro(statoPratica, data)
		};

	// Fotografa i dati del passaggio raggiunto in testo multilinea, limitato a 4000 caratteri.
	private static string? CreaDettagliRegistro(string statoPratica, GestionePraticaUpdateDto data)
	{
		if (string.Equals(statoPratica, "Aperta", StringComparison.Ordinal))
			return null;

		string[] righe = statoPratica switch
		{
			"Presa in carico da operatore" =>
			[
				$"Data presa in carico: {FormattaData(data.DataPresaInCarico)}",
				$"Operatore presa in carico: {FormattaTesto(data.OperatorePresaInCarico)}",
				$"Annotazioni presa in carico: {FormattaTesto(data.AnnotazioniPresaInCarico)}"
			],
			"Passaggio ad altro operatore" =>
			[
				$"Data assegnazione ad altro operatore: {FormattaData(data.DataAssegnazioneAltroOperatore)}",
				$"Operatore assegnatario: {FormattaTesto(data.OperatoreAssegnatario)}",
				$"Annotazioni assegnazione: {FormattaTesto(data.AnnotazioniAssegnazione)}"
			],
			"Richiesto approfondimento ad altra struttura" =>
			[
				$"Data inoltro ad altra struttura: {FormattaData(data.DataInoltroAltraStruttura)}",
				$"Struttura destinataria: {FormattaTesto(data.StrutturaDestinataria)}",
				$"Annotazioni inoltro: {FormattaTesto(data.AnnotazioniInoltro)}"
			],
			"Ricevuta risposta da struttura" =>
			[
				$"Data risposta della struttura: {FormattaData(data.DataRispostaStruttura)}",
				$"Risposta da struttura: {FormattaTesto(data.RispostaDaStruttura)}"
			],
			"Risposta per il cittadino" =>
			[
				$"Data risposta: {FormattaData(data.DataRisposta)}",
				$"Testo risposta: {FormattaTesto(data.TestoRisposta)}",
				$"Annotazioni risposta: {FormattaTesto(data.AnnotazioniRisposta)}"
			],
			"Chiusa" =>
			[
				$"Data chiusura: {FormattaData(data.DataChiusura)}",
				$"Annotazioni chiusura: {FormattaTesto(data.AnnotazioniChiusura)}"
			],
			_ => []
		};

		if (righe.Length == 0)
			return null;

		var dettagli = string.Join(Environment.NewLine, righe);
		return dettagli.Length <= 4000 ? dettagli : dettagli[..4000];
	}

	private static string FormattaData(DateTime? value) => value?.ToString("dd/MM/yyyy") ?? string.Empty;

	private static string FormattaTesto(string? value) => value?.Trim() ?? string.Empty;

	// Aggiorna gli stessi campi di gestione per il reclamo, mantenendo la mappatura specifica dell'entità.
	private void ApplicaDatiGestione(Reclamo reclamo, GestionePraticaUpdateDto data)
	{
		reclamo.UltimoAggiornamento = DateTime.Now;
		reclamo.OperatoreUltimoAggiornamento = operatoreState.UserName;
		reclamo.StatoPratica = data.StatoPratica;
		reclamo.DataPresaInCarico = data.DataPresaInCarico;
		reclamo.OperatorePresaInCarico = data.OperatorePresaInCarico;
		reclamo.IdOperatorePresaInCarico = data.IdOperatorePresaInCarico;
		reclamo.AnnotazioniPresaInCarico = data.AnnotazioniPresaInCarico;
		reclamo.DataAssegnazioneAltroOperatore = data.DataAssegnazioneAltroOperatore;
		reclamo.OperatoreAssegnatario = data.OperatoreAssegnatario;
		reclamo.IdOperatoreAssegnatario = data.IdOperatoreAssegnatario;
		reclamo.AnnotazioniAssegnazione = data.AnnotazioniAssegnazione;
		reclamo.DataInoltroAltraStruttura = data.DataInoltroAltraStruttura;
		reclamo.StrutturaDestinataria = data.StrutturaDestinataria;
		reclamo.CodiceStrutturaDestinataria = data.CodiceStrutturaDestinataria;
		reclamo.AnnotazioniInoltro = data.AnnotazioniInoltro;
		reclamo.DataRispostaStruttura = data.DataRispostaStruttura;
		reclamo.RispostaDaStruttura = data.RispostaDaStruttura;
		reclamo.DataRisposta = data.DataRisposta;
		reclamo.TestoRisposta = data.TestoRisposta;
		reclamo.AnnotazioniRisposta = data.AnnotazioniRisposta;
		reclamo.DataChiusura = data.DataChiusura;
		reclamo.AnnotazioniChiusura = data.AnnotazioniChiusura;
	}

	// Compensa le modifiche ai file eliminando quelli creati e ricopiando gli originali dai backup disponibili.
	private static void RipristinaFileAllegati(
		IEnumerable<string> fileCreati,
		IEnumerable<(string PercorsoOriginale, string PercorsoBackup)> fileDaRipristinare)
	{
		foreach (var percorso in fileCreati)
		{
			if (File.Exists(percorso))
				File.Delete(percorso);
		}

		foreach (var item in fileDaRipristinare)
		{
			if (!File.Exists(item.PercorsoBackup))
				continue;

			Directory.CreateDirectory(Path.GetDirectoryName(item.PercorsoOriginale)!);
			File.Copy(item.PercorsoBackup, item.PercorsoOriginale, true);
		}
	}

	// I backup temporanei vengono eliminati dal finally dei flussi di salvataggio.
	private static void PulisciBackup(IEnumerable<(string PercorsoOriginale, string PercorsoBackup)> fileDaRipristinare)
	{
		foreach (var item in fileDaRipristinare)
		{
			if (File.Exists(item.PercorsoBackup))
				File.Delete(item.PercorsoBackup);
		}
	}
}
