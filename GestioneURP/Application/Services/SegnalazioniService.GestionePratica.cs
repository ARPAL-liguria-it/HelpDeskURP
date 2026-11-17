using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace GestioneURP.Application.Services;

public partial class SegnalazioniService
{
	/// <summary>
	/// Confronta i dati proposti con quelli correnti prima del salvataggio: richiede l'identità dell'operatore,
	/// verifica l'eventuale stato atteso e consente solo transizioni e modifiche ai campi del passaggio interessato.
	/// Infine controlla obbligatorietà e coerenza temporale tramite il validatore condiviso.
	/// </summary>
	private GestionePraticaUpdateDto ValidaGestioneDaSalvare<TEntity>(TEntity entity, GestionePraticaUpdateDto data, string? statoAtteso)
		where TEntity : class
	{
		if (string.IsNullOrWhiteSpace(operatoreState.UserName))
			throw new InvalidOperationException("Identità dell'operatore non disponibile. Accedere nuovamente prima di salvare.");

		var valori = db.Entry(entity).CurrentValues;
		var salvati = LeggiDatiGestione(valori);
		var dataInserimento = valori.GetValue<DateTime?>("DataInserimento");
		// Rileva una variazione rispetto allo stato con cui la pagina ha preparato l'operazione, quando fornito.
		if (statoAtteso is not null && statoAtteso != salvati.StatoPratica)
			throw new InvalidOperationException("Lo stato della pratica è cambiato. I dati inseriti sono conservati: verificare la pratica prima di riprovare.");

		var cambioStato = data.StatoPratica != salvati.StatoPratica;
		var validatore = new GestionePraticaService();
		if (cambioStato)
		{
			if (!GestionePraticaService.GetStatiSuccessivi(salvati.StatoPratica).Contains(data.StatoPratica))
				throw new InvalidOperationException("Il passaggio di stato richiesto non è consentito.");
			var validazioneCorrente = validatore.Validate(salvati, dataInserimento);
			if (!validazioneCorrente.IsValid)
				throw new InvalidOperationException(string.Join(" ", validazioneCorrente.ErrorsByField.Values));
		}

		// La copia ammette modifiche solo al passaggio proposto: eventuali differenze altrove bloccano il salvataggio.
		if (salvati.ConDatiStato(data) != data)
			throw new InvalidOperationException("È consentito modificare solo i dati dello stato corrente o dello stato di destinazione durante l'avanzamento.");

		var validazione = validatore.Validate(data, dataInserimento);
		if (!validazione.IsValid)
			throw new InvalidOperationException(string.Join(" ", validazione.ErrorsByField.Values));
		return data;
	}

	/// <summary>
	/// Estrae gli stessi campi da richieste e reclami attraverso i valori dell'entità EF.
	/// Uno stato non valorizzato viene interpretato come Aperta; il metodo è condiviso anche dal flusso email.
	/// </summary>
	internal static GestionePraticaUpdateDto LeggiDatiGestione(PropertyValues valori) => new()
	{
		StatoPratica = valori.GetValue<string?>("StatoPratica") ?? "Aperta",
		DataPresaInCarico = valori.GetValue<DateTime?>("DataPresaInCarico"),
		OperatorePresaInCarico = valori.GetValue<string?>("OperatorePresaInCarico"),
		IdOperatorePresaInCarico = valori.GetValue<long?>("IdOperatorePresaInCarico"),
		AnnotazioniPresaInCarico = valori.GetValue<string?>("AnnotazioniPresaInCarico"),
		DataAssegnazioneAltroOperatore = valori.GetValue<DateTime?>("DataAssegnazioneAltroOperatore"),
		OperatoreAssegnatario = valori.GetValue<string?>("OperatoreAssegnatario"),
		IdOperatoreAssegnatario = valori.GetValue<long?>("IdOperatoreAssegnatario"),
		AnnotazioniAssegnazione = valori.GetValue<string?>("AnnotazioniAssegnazione"),
		DataInoltroAltraStruttura = valori.GetValue<DateTime?>("DataInoltroAltraStruttura"),
		StrutturaDestinataria = valori.GetValue<string?>("StrutturaDestinataria"),
		CodiceStrutturaDestinataria = valori.GetValue<string?>("CodiceStrutturaDestinataria"),
		AnnotazioniInoltro = valori.GetValue<string?>("AnnotazioniInoltro"),
		DataRispostaStruttura = valori.GetValue<DateTime?>("DataRispostaStruttura"),
		RispostaDaStruttura = valori.GetValue<string?>("RispostaDaStruttura"),
		DataRisposta = valori.GetValue<DateTime?>("DataRisposta"),
		TestoRisposta = valori.GetValue<string?>("TestoRisposta"),
		AnnotazioniRisposta = valori.GetValue<string?>("AnnotazioniRisposta"),
		DataChiusura = valori.GetValue<DateTime?>("DataChiusura"),
		AnnotazioniChiusura = valori.GetValue<string?>("AnnotazioniChiusura")
	};
}
