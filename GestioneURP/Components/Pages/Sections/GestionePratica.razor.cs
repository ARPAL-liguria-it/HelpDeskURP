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
using GestioneURP.Components.State;
using GestioneURP.Infrastructure.Gso;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;

namespace GestioneURP.Components.Pages.Sections;

/// <summary>
/// Gestisce la modifica della pratica, gli avanzamenti di stato e gli allegati della risposta.
/// Mantiene separati i dati in modifica, l'ultima versione salvata e l'eventuale avanzamento
/// da confermare, proteggendo la navigazione dalla perdita di modifiche non salvate.
/// </summary>
public partial class GestionePratica
{
	[Inject] private SegnalazioniService SegnalazioniService { get; set; } = default!;
	[Inject] private IConfiguration Configuration { get; set; } = default!;
	[Inject] private HttpClient HttpClient { get; set; } = default!;
	[Inject] private NavigationManager NavigationManager { get; set; } = default!;
	[Inject] private ILogger<GestionePratica> Logger { get; set; } = default!;
	[Inject] private OperatoreState OperatoreCorrente { get; set; } = default!;

	[Parameter] public long IdSegnalazione { get; set; }
	[Parameter] public string TipoSegnalazione { get; set; } = default!;
	[Parameter] public DateTime? DataInserimentoSegnalazione { get; set; }
	[Parameter] public EventCallback<bool> OnSalvatoCallback { get; set; }

	private readonly GestionePraticaService _gestionePraticaService = new();
	private List<UtenteApplicazione> OperatoriDisponibili { get; set; } = [];
	private List<Struttura> StruttureDisponibili { get; set; } = [];
	private bool _inizializzato;

	/// <summary>
	/// Carica la pratica e gli elenchi per la selezione di operatori e strutture.
	/// Se gli elenchi non sono disponibili, segnala il problema senza bloccare le altre funzioni.
	/// </summary>
	protected override async Task OnInitializedAsync()
	{
		if (_inizializzato)
			return;

		_inizializzato = true;

		await CaricaDettaglioAsync();
		try
		{
			OperatoriDisponibili = await _gestionePraticaService.GetOperatoriDisponibiliAsync(Configuration, HttpClient, Logger);
			StruttureDisponibili = await _gestionePraticaService.GetStruttureDisponibiliAsync(Configuration, HttpClient, Logger);
			AggiungiOperatoriSeAssenti();
			AggiungiStrutturaDestinatariaSeAssente();
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Errore durante il caricamento di operatori e strutture");
			_erroreElenchi = "Impossibile caricare gli elenchi di operatori e strutture. Le altre funzioni restano disponibili.";
		}
	}

	// La copia salvata permette di rilevare le modifiche; la transizione contiene invece
	// i dati del nuovo stato finché l'operatore non conferma l'avanzamento.
	private GestionePraticaUpdateDto _gestionePratica = new();
	private GestionePraticaUpdateDto _salvata = new();
	private GestionePraticaUpdateDto? _transizione;
	private EditContext? _editContext;
	private EditContext? _editContextTransizione;
	private ValidationMessageStore? _messaggi;
	private ValidationMessageStore? _messaggiTransizione;
	private bool _occupato;
	private bool _mostraUscita;
	private bool _consentiUscita;
	private string _destinazioneUscita = "/";
	private string? _erroreCaricamento;
	private string? _erroreElenchi;
	private string? _erroreSalvataggio;
	private GestionePraticaValidationResult _erroriValidazione = new();
	private string? _erroreAllegati;
	private SegnalazioneDettaglioDto? _dettaglio;
	private List<AllegatoEsistenteStaging> _allegatiEsistenti = [];
	private List<AllegatoNuovoStaging> _nuoviAllegati = [];
	private List<RegistroAttivitaDto> _registroAttivita = [];
	private bool _mostraRegistroAttivita;
	private string? _erroreRegistroAttivita;
	private bool _mostraConfermaEliminazione;
	private bool _mostraEmail;
	// Le condizioni sono condivise dai pulsanti e dai gestori degli eventi: l'avanzamento
	// richiede dati validi e già salvati, mentre l'e-mail richiede anche una risposta compilata.
	private bool MostraPulsanteElimina => OperatoreCorrente.SeCancellazioneSegnalazioni();
	// Anche gli errori di validazione e le modifiche agli allegati richiedono attenzione prima dell'uscita.
	private bool ModificheNonSalvate => _gestionePratica != _salvata || (_editContext?.GetValidationMessages().Any() ?? false) || _nuoviAllegati.Count > 0 || _allegatiEsistenti.Any(x => x.InEliminazione);
	private bool ModaleAperto => _transizione is not null || _mostraUscita || _mostraRegistroAttivita || _mostraConfermaEliminazione || _mostraEmail;
	private bool PuoAvanzare => _dettaglio is not null && !_occupato && !ModificheNonSalvate && _gestionePraticaService.Validate(_gestionePratica, DataInserimentoSegnalazione).IsValid;
	private bool PuoAprireEmail => PuoAvanzare && !ModaleAperto && _salvata.StatoPratica == "Risposta per il cittadino"
		&& _gestionePratica.DataRisposta.HasValue && !string.IsNullOrWhiteSpace(_gestionePratica.TestoRisposta);
	private bool PuoConfermare => PuoAvanzare && _transizione is not null && !(_editContextTransizione?.GetValidationMessages().Any() ?? false) && _gestionePraticaService.Validate(_transizione, DataInserimentoSegnalazione).IsValid;
	private IReadOnlyList<string> StatiTimeline => GestionePraticaService.GetStatiAttraversati(_salvata);

	/// <summary>
	/// Espone il primo motivo che impedisce l'avanzamento, dando precedenza alle operazioni
	/// in corso, all'assenza dei dati e agli errori rispetto alle sole modifiche da salvare.
	/// </summary>
	private string? MotivoBloccoAvanzamento
	{
		get
		{
			if (_occupato)
				return "Attendi il completamento dell'operazione in corso prima di avanzare.";
			if (_dettaglio is null)
				return "I dati della pratica non sono disponibili. Riapri la pagina prima di avanzare.";
			if ((_editContext?.GetValidationMessages().Any() ?? false)
				|| !_gestionePraticaService.Validate(_gestionePratica, DataInserimentoSegnalazione).IsValid)
				return "Correggi i dati non validi e salva le modifiche prima di avanzare.";
			return ModificheNonSalvate ? "Salva le modifiche prima di avanzare." : null;
		}
	}

	/// <summary>
	/// Rilegge la pratica e ricostruisce il modulo a partire dai dati persistiti.
	/// Azzera le modifiche temporanee agli allegati e aggiorna anche il registro attività.
	/// </summary>
	private async Task CaricaDettaglioAsync()
	{
		try
		{
			_dettaglio = await SegnalazioniService.GetDettaglioSegnalazioneAsync(IdSegnalazione, TipoSegnalazione);
			_gestionePratica = new GestionePraticaUpdateDto
			{
				StatoPratica = _dettaglio.StatoPratica ?? "Aperta",
				DataPresaInCarico = _dettaglio.DataPresaInCarico,
				OperatorePresaInCarico = _dettaglio.OperatorePresaInCarico,
				IdOperatorePresaInCarico = _dettaglio.IdOperatorePresaInCarico,
				AnnotazioniPresaInCarico = _dettaglio.AnnotazioniPresaInCarico,
				DataAssegnazioneAltroOperatore = _dettaglio.DataAssegnazioneAltroOperatore,
				OperatoreAssegnatario = _dettaglio.OperatoreAssegnatario,
				IdOperatoreAssegnatario = _dettaglio.IdOperatoreAssegnatario,
				AnnotazioniAssegnazione = _dettaglio.AnnotazioniAssegnazione,
				DataInoltroAltraStruttura = _dettaglio.DataInoltroAltraStruttura,
				StrutturaDestinataria = _dettaglio.StrutturaDestinataria,
				CodiceStrutturaDestinataria = _dettaglio.CodiceStrutturaDestinataria,
				AnnotazioniInoltro = _dettaglio.AnnotazioniInoltro,
				DataRispostaStruttura = _dettaglio.DataRispostaStruttura,
				RispostaDaStruttura = _dettaglio.RispostaDaStruttura,
				DataRisposta = _dettaglio.DataRisposta,
				TestoRisposta = _dettaglio.TestoRisposta,
				AnnotazioniRisposta = _dettaglio.AnnotazioniRisposta,
				DataChiusura = _dettaglio.DataChiusura,
				AnnotazioniChiusura = _dettaglio.AnnotazioniChiusura
			};

			AggiungiOperatoriSeAssenti();
			AggiungiStrutturaDestinatariaSeAssente();

			// Qui si gestiscono solo gli allegati della risposta, non quelli della segnalazione iniziale.
			_allegatiEsistenti = _dettaglio.Allegati
				.Where(allegato => string.Equals(allegato.Sorgente, "Risposta", StringComparison.OrdinalIgnoreCase))
				.Select(allegato => new AllegatoEsistenteStaging(allegato.IdAllegato, allegato.NomeFile, allegato.DataCaricamento))
				.ToList();
			_nuoviAllegati = [];
			_erroreAllegati = null;
			_erroriValidazione = new();
			_erroreCaricamento = null;
			_salvata = _gestionePratica with { };
			_editContext = CreaContesto(_gestionePratica);
			_messaggi = new ValidationMessageStore(_editContext);
			await CaricaRegistroAttivitaAsync();
		}
		catch (Exception ex)
		{
			_erroreCaricamento = "Impossibile aggiornare la visualizzazione della pratica. Riprovare aprendo nuovamente la pagina.";
			Logger.LogError(ex, "Errore durante il caricamento della gestione della pratica");
		}
	}

	/// <summary>
	/// Collega il modello al modulo Blazor e aggiorna la UI quando cambiano i messaggi di validazione.
	/// </summary>
	private EditContext CreaContesto(GestionePraticaUpdateDto dati)
	{
		var context = new EditContext(dati);
		context.OnValidationStateChanged += (_, _) => StateHasChanged();
		return context;
	}

	/// <summary>
	/// Applica le regole del servizio e sostituisce i messaggi del modulo con gli errori correnti,
	/// associandoli ai rispettivi campi del modello.
	/// </summary>
	private void AggiornaValidazione(GestionePraticaUpdateDto dati, EditContext context, ValidationMessageStore messaggi)
	{
		_erroriValidazione = _gestionePraticaService.Validate(dati, DataInserimentoSegnalazione, Logger);
		messaggi.Clear();
		foreach (var errore in _erroriValidazione.ErrorsByField)
			messaggi.Add(new FieldIdentifier(dati, errore.Key), errore.Value);
		context.NotifyValidationStateChanged();
	}

	// I due moduli validano separatamente i dati correnti e quelli dell'avanzamento in preparazione.
	private void ModificatoStatoCorrente() => AggiornaValidazione(_gestionePratica, _editContext!, _messaggi!);
	private void ModificataTransizione() => AggiornaValidazione(_transizione!, _editContextTransizione!, _messaggiTransizione!);

	/// <summary>
	/// Salva dal modulo principale solo quando nessun pannello modale è aperto.
	/// </summary>
	private async Task SalvaGestionePratica()
	{
		if (!ModaleAperto)
			await SalvaAsync(_gestionePratica, false);
	}

	/// <summary>
	/// Percorso comune per salvataggio e avanzamento: valida il modulo interessato e invia
	/// dati, nuovi allegati e rimozioni al servizio. In caso di errore conserva le modifiche;
	/// dopo la persistenza aggiorna la pagina e notifica il componente chiamante.
	/// </summary>
	private async Task<bool> SalvaAsync(GestionePraticaUpdateDto dati, bool avanzamento)
	{
		if (_occupato || _dettaglio is null)
			return false;
		AggiornaValidazione(dati, avanzamento ? _editContextTransizione! : _editContext!, avanzamento ? _messaggiTransizione! : _messaggi!);
		if (!_erroriValidazione.IsValid || (avanzamento ? _editContextTransizione! : _editContext!).GetValidationMessages().Any())
			return false;

		_occupato = true;
		_erroreSalvataggio = null;
		try
		{
			// Lo stato salvato viene passato al servizio insieme alla modifica richiesta.
			await SegnalazioniService.UpdateGestionePraticaConAllegatiAsync(
				IdSegnalazione, TipoSegnalazione, dati,
				_nuoviAllegati.Select(x => new AllegatoNuovoUploadDto(x.NomeFile, x.Contenuto)).ToList(),
				_allegatiEsistenti.Where(x => x.InEliminazione).Select(x => x.IdAllegato).ToList(),
				_salvata.StatoPratica);
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Errore durante il salvataggio della gestione della pratica");
			_erroreSalvataggio = ex is InvalidOperationException ? ex.Message : "Errore durante il salvataggio. I dati inseriti sono conservati: riprovare.";
			_occupato = false;
			return false;
		}

		try
		{
			// La persistenza è riuscita: allinea le copie e svuota lo staging prima di rileggere i dati.
			_gestionePratica = dati with { };
			_salvata = dati with { };
			_editContext = CreaContesto(_gestionePratica);
			_messaggi = new ValidationMessageStore(_editContext);
			_nuoviAllegati = [];
			_allegatiEsistenti.RemoveAll(x => x.InEliminazione);
			_transizione = null;
			await CaricaDettaglioAsync();
			if (OnSalvatoCallback.HasDelegate)
				await OnSalvatoCallback.InvokeAsync(true);
		}
		finally
		{
			_occupato = false;
		}
		return true;
	}

	/// <summary>
	/// Prepara un avanzamento consentito copiando l'ultima versione salvata.
	/// Propone la data odierna e, per la presa in carico, l'operatore corrente se presente nell'elenco.
	/// Le modifiche restano nel modulo separato fino alla conferma.
	/// </summary>
	private void ApriAvanzamento(string stato)
	{
		if (!PuoAvanzare || ModaleAperto || !GestionePraticaService.GetStatiSuccessivi(_salvata.StatoPratica).Contains(stato))
			return;
		_transizione = _salvata with { StatoPratica = stato };
		var oggi = DateTime.Today;
		switch (stato)
		{
			case "Presa in carico da operatore":
				_transizione.DataPresaInCarico = oggi;
				var operatore = OperatoriDisponibili.FirstOrDefault(x => x.IdUtente == OperatoreCorrente.UserId);
				_transizione.OperatorePresaInCarico = operatore?.CognomeNome;
				_transizione.IdOperatorePresaInCarico = operatore is not null ? Convert.ToInt64(operatore.IdUtente) : null;
				break;
			case "Passaggio ad altro operatore":
				_transizione.DataAssegnazioneAltroOperatore = oggi;
				break;
			case "Richiesto approfondimento ad altra struttura":
				_transizione.DataInoltroAltraStruttura = oggi;
				break;
			case "Ricevuta risposta da struttura":
				_transizione.DataRispostaStruttura = oggi;
				break;
			case "Risposta per il cittadino":
				_transizione.DataRisposta = oggi;
				break;
			case "Chiusa":
				_transizione.DataChiusura = oggi;
				break;
		}
		_erroreSalvataggio = null;
		_editContextTransizione = CreaContesto(_transizione);
		_messaggiTransizione = new ValidationMessageStore(_editContextTransizione);
		ModificataTransizione();
	}

	/// <summary>
	/// Persiste il nuovo stato solo se i dati della pratica e della transizione consentono la conferma.
	/// </summary>
	private async Task ConfermaAvanzamentoAsync()
	{
		if (PuoConfermare)
			await SalvaAsync(_transizione!, true);
	}

	/// <summary>
	/// Scarta la transizione senza modificare la pratica, salvo quando il salvataggio è già in corso.
	/// </summary>
	private void AnnullaAvanzamento()
	{
		if (_occupato) return;
		_transizione = null;
		_erroreSalvataggio = null;
	}

	/// <summary>
	/// Gestisce il ritorno all'elenco: chiede cosa fare delle modifiche oppure naviga direttamente.
	/// </summary>
	public void RichiediUscita()
	{
		if (_occupato || ModaleAperto) return;
		_destinazioneUscita = "/";
		if (ModificheNonSalvate)
			_mostraUscita = true;
		else
			EsciSenzaSalvare();
		StateHasChanged();
	}

	/// <summary>
	/// Intercetta le navigazioni interne richieste tramite NavigationLock.
	/// Blocca l'uscita durante invio e-mail, operazioni o transizioni e, quando possibile,
	/// conserva la destinazione richiesta per il dialogo sulle modifiche non salvate.
	/// </summary>
	private Task PrimaDellaNavigazione(LocationChangingContext context)
	{
		if (_consentiUscita) return Task.CompletedTask;
		if (_mostraEmail)
		{
			context.PreventNavigation();
			return Task.CompletedTask;
		}
		if (_occupato || _transizione is not null || ModificheNonSalvate)
		{
			context.PreventNavigation();
			if (!_occupato && _transizione is null && !_mostraRegistroAttivita && !_mostraConfermaEliminazione)
			{
				_destinazioneUscita = context.TargetLocation;
				_mostraUscita = true;
			}
		}
		return Task.CompletedTask;
	}

	/// <summary>
	/// Apre l'invio e-mail solo quando la risposta è pronta e la pratica può avanzare.
	/// </summary>
	private void ApriEmail()
	{
		if (PuoAprireEmail) _mostraEmail = true;
	}

	/// <summary>
	/// Chiude il pannello e-mail e, se richiesto dal pannello, rilegge la pratica
	/// e notifica al componente chiamante l'aggiornamento.
	/// </summary>
	private async Task ChiudiEmailAsync(bool aggiorna)
	{
		_mostraEmail = false;
		if (!aggiorna) return;
		_occupato = true;
		try
		{
			await CaricaDettaglioAsync();
			await OnSalvatoCallback.InvokeAsync(true);
		}
		finally { _occupato = false; }
	}

	/// <summary>
	/// Consente il ritorno all'elenco richiesto dal pannello e-mail senza ulteriori blocchi di navigazione.
	/// </summary>
	private void ElencoDopoEmail()
	{
		_consentiUscita = true;
		NavigationManager.NavigateTo("/");
	}

	// La chiusura dei dialoghi equivale a restare nella pagina o ad annullare l'avanzamento.
	private void Rimani() => _mostraUscita = false;
	private void ChiusuraDialogoUscita(bool nascosto)
	{
		if (nascosto) Rimani();
	}
	private void ChiusuraDialogoAvanzamento(bool nascosto)
	{
		if (nascosto) AnnullaAvanzamento();
	}

	/// <summary>
	/// Autorizza la navigazione verso la destinazione memorizzata senza salvare le modifiche locali.
	/// </summary>
	private void EsciSenzaSalvare()
	{
		if (_occupato) return;
		_consentiUscita = true;
		NavigationManager.NavigateTo(_destinazioneUscita);
	}

	/// <summary>
	/// Esce soltanto se il salvataggio riesce; in caso contrario rimane nel modulo con i dati inseriti.
	/// </summary>
	private async Task SalvaEdEsciAsync()
	{
		_mostraUscita = false;
		if (await SalvaAsync(_gestionePratica, false))
			EsciSenzaSalvare();
	}

	/// <summary>
	/// Valida i file selezionati e conserva in memoria quelli accettati fino al salvataggio della pratica.
	/// Un file non valido viene saltato senza impedire l'esame dei successivi.
	/// </summary>
	private async Task AggiungiAllegati(InputFileChangeEventArgs e)
	{
		if (_occupato || ModaleAperto) return;
		_occupato = true;
		_erroreAllegati = null;
		try
		{
			foreach (var file in e.GetMultipleFiles())
			{
				var errore = ValidaNuovoAllegato(file);
				if (errore is not null)
				{
					_erroreAllegati = errore;
					continue;
				}

				await using var stream = file.OpenReadStream(GetDimensioneMassimaBytes());
				using var memory = new MemoryStream();
				await stream.CopyToAsync(memory);
				_nuoviAllegati.Add(new AllegatoNuovoStaging(Guid.NewGuid(), NormalizzaNomeFile(file.Name), file.Size, memory.ToArray()));
			}
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Errore durante il caricamento degli allegati");
			_erroreAllegati = "Impossibile caricare il file. Gli altri dati inseriti sono conservati.";
		}
		finally
		{
			_occupato = false;
		}
	}

	/// <summary>
	/// Segna un allegato esistente per la rimozione al prossimo salvataggio, senza eliminarlo subito.
	/// </summary>
	private void RimuoviAllegatoEsistente(long idAllegato)
	{
		var allegato = _allegatiEsistenti.FirstOrDefault(x => x.IdAllegato == idAllegato);
		if (allegato is not null)
			allegato.InEliminazione = true;
	}

	/// <summary>
	/// Revoca una rimozione ancora in attesa di salvataggio.
	/// </summary>
	private void AnnullaRimozioneAllegatoEsistente(long idAllegato)
	{
		var allegato = _allegatiEsistenti.FirstOrDefault(x => x.IdAllegato == idAllegato);
		if (allegato is not null)
			allegato.InEliminazione = false;
	}

	// Un nuovo file non è ancora persistito: rimuoverlo significa soltanto scartarlo dallo staging.
	private void RimuoviNuovoAllegato(Guid id) => _nuoviAllegati.RemoveAll(x => x.Id == id);

	/// <summary>
	/// Aggiunge gli operatori assegnatario e presa in carico alla lista degli operatori disponibili se non sono già presenti.
	/// Mantiene selezionabili i valori salvati anche quando non sono restituiti dall'elenco aggiornato.
	/// </summary>
	private void AggiungiOperatoriSeAssenti()
	{
		if (_gestionePratica.IdOperatoreAssegnatario.HasValue && !string.IsNullOrWhiteSpace(_gestionePratica.OperatoreAssegnatario))
		{
			var idOperatoreAssegnatario = _gestionePratica.IdOperatoreAssegnatario.Value;
			if (!OperatoriDisponibili.Any(x => Convert.ToInt64(x.IdUtente) == idOperatoreAssegnatario))
			{
				OperatoriDisponibili.Add(new UtenteApplicazione
				{
					IdUtente = Convert.ToDecimal(idOperatoreAssegnatario),
					CognomeNome = _gestionePratica.OperatoreAssegnatario
				});
			}
		}

		if (!_gestionePratica.IdOperatorePresaInCarico.HasValue || string.IsNullOrWhiteSpace(_gestionePratica.OperatorePresaInCarico))
			return;

		if (_gestionePratica.IdOperatoreAssegnatario.HasValue &&
			_gestionePratica.IdOperatorePresaInCarico.Value == _gestionePratica.IdOperatoreAssegnatario.Value)
			return;

		var idOperatorePresaInCarico = _gestionePratica.IdOperatorePresaInCarico.Value;
		if (OperatoriDisponibili.Any(x => Convert.ToInt64(x.IdUtente) == idOperatorePresaInCarico))
			return;

		OperatoriDisponibili.Add(new UtenteApplicazione
		{
			IdUtente = Convert.ToDecimal(idOperatorePresaInCarico),
			CognomeNome = _gestionePratica.OperatorePresaInCarico
		});
	}

	/// <summary>
	/// Integra l'elenco con la struttura già salvata, se assente, e ripristina l'ordine per descrizione.
	/// </summary>
	private void AggiungiStrutturaDestinatariaSeAssente()
	{
		if (string.IsNullOrWhiteSpace(_gestionePratica.StrutturaDestinataria))
			return;

		if (StruttureDisponibili.Any(x => string.Equals(x.DescrizioneStruttura, _gestionePratica.StrutturaDestinataria, StringComparison.OrdinalIgnoreCase)))
			return;

		StruttureDisponibili.Add(new Struttura
		{
			CodiceStruttura = _gestionePratica.CodiceStrutturaDestinataria,
			DescrizioneStruttura = _gestionePratica.StrutturaDestinataria
		});

		StruttureDisponibili = StruttureDisponibili
			.OrderBy(x => x.DescrizioneStruttura)
			.ToList();
	}

	/// <summary>
	/// Verifica estensione, dimensione, numero di allegati e unicità del nome.
	/// Gli allegati in eliminazione non contano nei limiti né nel controllo dei duplicati.
	/// Restituisce il primo errore oppure null se il file può essere aggiunto.
	/// </summary>
	private string? ValidaNuovoAllegato(IBrowserFile file)
	{
		var nomeFile = NormalizzaNomeFile(file.Name);
		var estensione = Path.GetExtension(nomeFile);
		if (!GetEstensioniConsentite().Contains(estensione))
			return "L'estensione del file non è consentita.";

		if (file.Size > GetDimensioneMassimaBytes())
			return "La dimensione del file supera il massimo consentito.";

		var totaleAttivo = _allegatiEsistenti.Count(x => !x.InEliminazione) + _nuoviAllegati.Count;
		if (totaleAttivo + 1 > GetNumeroMassimoAllegati())
			return "È stato superato il numero massimo di allegati consentito.";

		if (_allegatiEsistenti.Any(x => !x.InEliminazione && string.Equals(x.NomeFile, nomeFile, StringComparison.OrdinalIgnoreCase)) ||
			_nuoviAllegati.Any(x => string.Equals(x.NomeFile, nomeFile, StringComparison.OrdinalIgnoreCase)))
			return "Esiste già un allegato con lo stesso nome file.";

		return null;
	}

	/// <summary>
	/// Normalizza le estensioni configurate e usa l'elenco predefinito se non sono presenti valori utilizzabili.
	/// </summary>
	private HashSet<string> GetEstensioniConsentite()
	{
		var valori = Configuration.GetSection("EstensioniAllegati").GetChildren()
			.Select(x => x.Value)
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Select(x => x!.Trim().StartsWith('.') ? x!.Trim().ToLowerInvariant() : "." + x!.Trim().ToLowerInvariant())
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		return valori.Count > 0
			? valori
			: new HashSet<string>([".pdf", ".png", ".jpg", ".jpeg", ".doc", ".docx"], StringComparer.OrdinalIgnoreCase);
	}

	// I limiti mancanti o non positivi ricadono sui valori predefiniti: 10 MB e 10 allegati.
	private long GetDimensioneMassimaBytes()
		=> int.TryParse(Configuration["DimensioneMassimaAllegatoMB"], out var megabyte) && megabyte > 0
			? megabyte * 1024L * 1024L
			: 10 * 1024L * 1024L;

	private int GetNumeroMassimoAllegati()
		=> int.TryParse(Configuration["NumeroMassimoAllegati"], out var massimo) && massimo > 0 ? massimo : 10;

	// Il filtro del selettore file affianca la validazione applicata ai file effettivamente selezionati.
	private string GetAcceptAttribute() => string.Join(',', GetEstensioniConsentite());

	// Conserva solo il nome del file, rimuovendo spazi esterni ed eventuali componenti di percorso.
	private static string NormalizzaNomeFile(string nomeFile) => Path.GetFileName(nomeFile.Trim());

	// Il tipo di segnalazione e l'identificativo dell'allegato individuano il download richiesto.
	private string GetAllegatoDownloadUrl(long idAllegato)
		=> $"/segnalazioni/allegati/download/{_dettaglio!.TipoSegnalazione}/{idAllegato}";

	/// <summary>
	/// Compone data e operatore dell'ultimo aggiornamento, usando un trattino per i dati assenti.
	/// </summary>
	private string GetUltimoAggiornamentoText()
	{
		var data = FormatDataOra(_dettaglio?.UltimoAggiornamento);
		var operatore = _dettaglio?.OperatoreUltimoAggiornamento ?? "-";
		return $"{data} - {operatore}";
	}

	// La UI mostra le date al minuto, il registro anche al secondo e le dimensioni in B, KB o MB.
	private static string FormatDataOra(DateTime? value) => value?.ToString("dd/MM/yyyy HH:mm") ?? "-";
	private static string FormattaDimensione(long bytes) => bytes < 1024 ? $"{bytes} B" : bytes < 1024 * 1024 ? $"{bytes / 1024d:0.#} KB" : $"{bytes / 1024d / 1024d:0.#} MB";
	private static string FormattaDataOraEvento(DateTime value) => value.ToString("dd/MM/yyyy HH:mm:ss");

	/// <summary>
	/// Carica gli eventi della pratica; un errore del registro viene segnalato separatamente
	/// e non impedisce il caricamento del modulo principale.
	/// </summary>
	private async Task CaricaRegistroAttivitaAsync()
	{
		try
		{
			_registroAttivita = await SegnalazioniService.GetRegistroAttivitaAsync(IdSegnalazione, TipoSegnalazione);
			_erroreRegistroAttivita = null;
		}
		catch (Exception ex)
		{
			_registroAttivita = [];
			_erroreRegistroAttivita = "Impossibile caricare il registro attività.";
			Logger.LogError(ex, "Errore durante il caricamento del registro attività per la segnalazione {IdSegnalazione} di tipo {TipoSegnalazione}", IdSegnalazione, TipoSegnalazione);
		}
	}

	// Il registro è un pannello locale: aprirlo o chiuderlo non naviga e non salva il modulo.
	private void ApriRegistroAttivita()
	{
		if (!_occupato && !ModaleAperto) _mostraRegistroAttivita = true;
	}
	private void ChiudiRegistroAttivita() => _mostraRegistroAttivita = false;

	// La conferma si apre solo se non ci sono operazioni in corso o altri pannelli aperti.
	private void ApriConfermaEliminazione()
	{
		if (!_occupato && !ModaleAperto) _mostraConfermaEliminazione = true;
	}

	private void AnnullaConfermaEliminazione() => _mostraConfermaEliminazione = false;

	/// <summary>
	/// Richiede l'eliminazione logica della segnalazione e torna all'elenco solo dopo il successo.
	/// Se il servizio fallisce, mantiene la pagina aperta e mostra un errore a livello di modulo.
	/// </summary>
	private async Task ConfermaEliminazioneAsync()
	{
		if (_occupato) return;
		_occupato = true;
		_mostraConfermaEliminazione = false;

		try
		{
			await SegnalazioniService.EliminaLogicamenteSegnalazioneAsync(IdSegnalazione, TipoSegnalazione);
			_consentiUscita = true;
			NavigationManager.NavigateTo("/");
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Errore durante l'eliminazione logica della segnalazione {IdSegnalazione} di tipo {TipoSegnalazione}", IdSegnalazione, TipoSegnalazione);
			_erroriValidazione.AddError("Form", "Errore durante l'eliminazione. Provare di nuovo.");
		}
		finally
		{
			_occupato = false;
		}
	}

	/// <summary>
	/// Rappresenta un allegato persistito con un indicatore locale di rimozione ancora annullabile.
	/// </summary>
	private sealed class AllegatoEsistenteStaging(long idAllegato, string nomeFile, DateTime? dataCaricamento)
	{
		public long IdAllegato { get; } = idAllegato;
		public string NomeFile { get; } = nomeFile;
		public DateTime? DataCaricamento { get; } = dataCaricamento;
		public bool InEliminazione { get; set; }
	}

	/// <summary>
	/// Conserva il contenuto di un nuovo allegato non ancora salvato; l'identificativo temporaneo
	/// permette di rimuoverlo dalla selezione indipendentemente dagli allegati persistiti.
	/// </summary>
	private sealed record AllegatoNuovoStaging(Guid Id, string NomeFile, long Dimensione, byte[] Contenuto);
}
