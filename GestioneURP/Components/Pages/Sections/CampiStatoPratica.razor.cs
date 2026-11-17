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

using GestioneURP.Application.Services;
using GestioneURP.Infrastructure.Gso;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace GestioneURP.Components.Pages.Sections;

/// <summary>
/// Mostra i dati relativi a uno stato della pratica, in sola lettura o in modifica.
/// Le modifiche aggiornano il DTO condiviso e vengono notificate al componente padre;
/// questo componente non esegue il salvataggio dei dati.
/// </summary>
public partial class CampiStatoPratica
{
	// Il padre fornisce i dati della pratica, lo stato da rappresentare e le opzioni disponibili.
	// DataApertura viene usata per mostrare lo stato "Aperta" in sola lettura.
	[Parameter, EditorRequired] public GestionePraticaUpdateDto Dati { get; set; } = default!;
	[Parameter, EditorRequired] public string Stato { get; set; } = default!;
	[Parameter] public bool SolaLettura { get; set; }
	[Parameter] public DateTime? DataApertura { get; set; }
	[Parameter] public IReadOnlyList<UtenteApplicazione> Operatori { get; set; } = [];
	[Parameter] public IReadOnlyList<Struttura> Strutture { get; set; } = [];
	[Parameter] public EventCallback OnModificato { get; set; }

	// L'autocompletamento richiede l'oggetto selezionato, mentre il DTO conserva codice e descrizione.
	private Struttura? _strutturaSelezionata;

	protected override void OnParametersSet()
	{
		// A ogni aggiornamento dei parametri riallinea la selezione ai dati ricevuti dal padre.
		// Cerca prima per codice e, se non trova corrispondenze, usa la descrizione.
		var struttura = !string.IsNullOrWhiteSpace(Dati.CodiceStrutturaDestinataria)
			? Strutture.FirstOrDefault(x => x.CodiceStruttura == Dati.CodiceStrutturaDestinataria)
			: null;
		struttura ??= Strutture.FirstOrDefault(x => !string.IsNullOrWhiteSpace(Dati.StrutturaDestinataria)
			&& string.Equals(x.DescrizioneStruttura, Dati.StrutturaDestinataria, StringComparison.OrdinalIgnoreCase));
		_strutturaSelezionata = struttura;
	}

	// Notifica la modifica senza stabilire come il padre debba gestirla o salvarla.
	private Task ModificatoAsync() => OnModificato.InvokeAsync();

	private Task OperatorePresaInCaricoModificatoAsync(string? valore)
	{
		// La select usa l'identificativo come valore: nome e ID vengono aggiornati insieme.
		// Una selezione vuota o non riconosciuta azzera entrambi i campi.
		var operatore = Operatori.FirstOrDefault(x => Convert.ToInt64(x.IdUtente).ToString() == valore);
		Dati.OperatorePresaInCarico = operatore?.CognomeNome;
		Dati.IdOperatorePresaInCarico = operatore is not null ? Convert.ToInt64(operatore.IdUtente) : null;
		return ModificatoAsync();
	}

	private Task AssegnatarioModificatoAsync()
	{
		// Il binding ha già aggiornato il nome dell'assegnatario; ricava il relativo ID
		// dalla prima corrispondenza nell'elenco degli operatori prima di notificare il padre.
		Dati.IdOperatoreAssegnatario = Operatori.FirstOrDefault(x => x.CognomeNome == Dati.OperatoreAssegnatario)?.IdUtente is decimal id
			? Convert.ToInt64(id) : null;
		return ModificatoAsync();
	}

	private Task OnStruttureSearch(OptionsSearchEventArgs<Struttura> e)
	{
		// Filtra l'elenco già ricevuto dal padre, senza interrogare servizi esterni.
		// Senza testo restituisce tutte le strutture; altrimenti cerca nella descrizione,
		// ignorando le differenze tra maiuscole e minuscole.
		var testo = e.Text ?? string.Empty;
		e.Items = string.IsNullOrWhiteSpace(testo)
			? Strutture
			: Strutture.Where(x => x.DescrizioneStruttura?.Contains(testo, StringComparison.OrdinalIgnoreCase) == true);
		return Task.CompletedTask;
	}

	private Task StrutturaModificataAsync(Struttura? struttura)
	{
		// Mantiene coerenti la selezione visualizzata e i due valori memorizzati nel DTO.
		// La rimozione della selezione azzera sia la descrizione sia il codice.
		_strutturaSelezionata = struttura;
		Dati.StrutturaDestinataria = struttura?.DescrizioneStruttura;
		Dati.CodiceStrutturaDestinataria = struttura?.CodiceStruttura;
		return ModificatoAsync();
	}

	// Costruisce le coppie etichetta/valore della modalità sola lettura per lo stato richiesto.
	// Uno stato non riconosciuto non produce campi da visualizzare.
	private IReadOnlyList<(string Etichetta, string Valore)> GetValori() => Stato switch
	{
		"Aperta" => [("Data apertura", Data(DataApertura))],
		"Presa in carico da operatore" =>
		[
			("Data presa in carico", Data(Dati.DataPresaInCarico)),
			("Operatore presa in carico", Testo(Dati.OperatorePresaInCarico)),
			("Annotazioni presa in carico", Testo(Dati.AnnotazioniPresaInCarico))
		],
		"Passaggio ad altro operatore" =>
		[
			("Data assegnazione ad altro operatore", Data(Dati.DataAssegnazioneAltroOperatore)),
			("Operatore assegnatario", Testo(Dati.OperatoreAssegnatario)),
			("Annotazioni assegnazione", Testo(Dati.AnnotazioniAssegnazione))
		],
		"Richiesto approfondimento ad altra struttura" =>
		[
			("Data inoltro ad altra struttura", Data(Dati.DataInoltroAltraStruttura)),
			("Struttura destinataria", Testo(Dati.StrutturaDestinataria)),
			("Annotazioni inoltro", Testo(Dati.AnnotazioniInoltro))
		],
		"Ricevuta risposta da struttura" =>
		[
			("Data risposta della struttura", Data(Dati.DataRispostaStruttura)),
			("Risposta da struttura", Testo(Dati.RispostaDaStruttura))
		],
		"Risposta per il cittadino" =>
		[
			("Data risposta", Data(Dati.DataRisposta)),
			("Testo risposta", Testo(Dati.TestoRisposta)),
			("Annotazioni risposta", Testo(Dati.AnnotazioniRisposta))
		],
		"Chiusa" => [("Data chiusura", Data(Dati.DataChiusura)), ("Annotazioni chiusura", Testo(Dati.AnnotazioniChiusura))],
		_ => []
	};

	// Uniforma la visualizzazione: date in formato giorno/mese/anno e trattino per valori assenti.
	private static string Data(DateTime? data) => data?.ToString("dd/MM/yyyy") ?? "-";
	private static string Testo(string? testo) => string.IsNullOrWhiteSpace(testo) ? "-" : testo;
}
