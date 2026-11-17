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

namespace GestioneURP.Application.Services;

/// <summary>
/// Raccoglie lo stato della pratica e i dati dei passaggi di gestione, condivisi da richieste e reclami.
/// Le regole di obbligatorietà e coerenza delle date sono verificate da GestionePraticaService.
/// </summary>
public record GestionePraticaUpdateDto
{
	public string? StatoPratica { get; set; }
	public DateTime? DataPresaInCarico { get; set; }
	public string? OperatorePresaInCarico { get; set; }
	public long? IdOperatorePresaInCarico { get; set; }
	public string? AnnotazioniPresaInCarico { get; set; }
	public DateTime? DataAssegnazioneAltroOperatore { get; set; }
	public string? OperatoreAssegnatario { get; set; }
	public long? IdOperatoreAssegnatario { get; set; }
	public string? AnnotazioniAssegnazione { get; set; }
	public DateTime? DataInoltroAltraStruttura { get; set; }
	public string? StrutturaDestinataria { get; set; }
	public string? CodiceStrutturaDestinataria { get; set; }
	public string? AnnotazioniInoltro { get; set; }
	public DateTime? DataRispostaStruttura { get; set; }
	public string? RispostaDaStruttura { get; set; }
	public DateTime? DataRisposta { get; set; }
	public string? TestoRisposta { get; set; }
	public string? AnnotazioniRisposta { get; set; }
	public DateTime? DataChiusura { get; set; }
	public string? AnnotazioniChiusura { get; set; }

	/// <summary>
	/// Crea una copia sostituendo soltanto lo stato e i campi del passaggio indicato.
	/// Il confronto con i dati proposti permette al servizio di rilevare modifiche ai campi di altri passaggi.
	/// </summary>
	public GestionePraticaUpdateDto ConDatiStato(GestionePraticaUpdateDto dati) => dati.StatoPratica switch
	{
		"Aperta" => this with { StatoPratica = dati.StatoPratica },
		"Presa in carico da operatore" => this with
		{
			StatoPratica = dati.StatoPratica,
			DataPresaInCarico = dati.DataPresaInCarico,
			OperatorePresaInCarico = dati.OperatorePresaInCarico,
			IdOperatorePresaInCarico = dati.IdOperatorePresaInCarico,
			AnnotazioniPresaInCarico = dati.AnnotazioniPresaInCarico
		},
		"Passaggio ad altro operatore" => this with
		{
			StatoPratica = dati.StatoPratica,
			DataAssegnazioneAltroOperatore = dati.DataAssegnazioneAltroOperatore,
			OperatoreAssegnatario = dati.OperatoreAssegnatario,
			IdOperatoreAssegnatario = dati.IdOperatoreAssegnatario,
			AnnotazioniAssegnazione = dati.AnnotazioniAssegnazione
		},
		"Richiesto approfondimento ad altra struttura" => this with
		{
			StatoPratica = dati.StatoPratica,
			DataInoltroAltraStruttura = dati.DataInoltroAltraStruttura,
			StrutturaDestinataria = dati.StrutturaDestinataria,
			CodiceStrutturaDestinataria = dati.CodiceStrutturaDestinataria,
			AnnotazioniInoltro = dati.AnnotazioniInoltro
		},
		"Ricevuta risposta da struttura" => this with
		{
			StatoPratica = dati.StatoPratica,
			DataRispostaStruttura = dati.DataRispostaStruttura,
			RispostaDaStruttura = dati.RispostaDaStruttura
		},
		"Risposta per il cittadino" => this with
		{
			StatoPratica = dati.StatoPratica,
			DataRisposta = dati.DataRisposta,
			TestoRisposta = dati.TestoRisposta,
			AnnotazioniRisposta = dati.AnnotazioniRisposta
		},
		"Chiusa" => this with
		{
			StatoPratica = dati.StatoPratica,
			DataChiusura = dati.DataChiusura,
			AnnotazioniChiusura = dati.AnnotazioniChiusura
		},
		_ => throw new InvalidOperationException("Stato della pratica non valido.")
	};
}

/// <summary>
/// Restituisce l'esito della validazione e un messaggio per ciascun campo non valido,
/// così che la UI possa mostrare gli errori accanto ai controlli interessati.
/// </summary>
public class GestionePraticaValidationResult
{
	public bool IsValid { get; set; }
	public Dictionary<string, string> ErrorsByField { get; set; } = new();

	// Un nuovo errore rende l'esito non valido e sostituisce l'eventuale messaggio dello stesso campo.
	public void AddError(string fieldName, string errorMessage)
	{
		IsValid = false;
		ErrorsByField[fieldName] = errorMessage;
	}

	public bool HasErrorFor(string fieldName)
	{
		return ErrorsByField.ContainsKey(fieldName);
	}

	public string? GetErrorFor(string fieldName)
	{
		return ErrorsByField.TryGetValue(fieldName, out var error) ? error : null;
	}
}

