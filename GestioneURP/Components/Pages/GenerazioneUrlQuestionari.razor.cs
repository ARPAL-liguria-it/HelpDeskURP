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
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;

namespace GestioneURP.Components.Pages;

/// <summary>
/// Gestisce il percorso di inserimento, conferma e generazione dell'URL di un questionario,
/// con controllo dell'accesso dell'operatore e copia del risultato negli appunti.
/// </summary>
public partial class GenerazioneUrlQuestionari
{
	[Inject] private QuestionariService QuestionariService { get; set; } = default!;
	[Inject] private ILogger<GenerazioneUrlQuestionari> Logger { get; set; } = default!;
	[Inject] private NavigationManager NavigationManager { get; set; } = default!;
	[Inject] private IJSRuntime JsRuntime { get; set; } = default!;
	[Inject] private OperatoreState OperatoreCorrente { get; set; } = default!;

	private IReadOnlyList<QuestionarioSorgenteDto> _sorgenti = [];
	private IReadOnlyList<QuestionarioTipoDto> _tipiQuestionario = [];
	// Il modello e il suo EditContext restano gli stessi quando si passa dall'inserimento alla conferma e viceversa.
	private ParametriGenerazioneModel _parametri = new();
	private EditContext _editContext = default!;
	// 1: inserimento parametri; 2: conferma; 3: visualizzazione dell'URL generata.
	private int _step = 1;
	private string? _urlGenerata;
	private string? _messaggioCopia;
	private string? _errore;
	private bool _inGenerazione;

	/// <summary>
	/// Prepara la validazione e carica le opzioni selezionabili solo se l'operatore è autorizzato.
	/// In caso di accesso negato reindirizza alla pagina di errore senza caricare i parametri.
	/// </summary>
	protected override async Task OnInitializedAsync()
	{
		_editContext = new EditContext(_parametri);

		if (!OperatoreCorrente.SeGenerazioneUrlQuestionari())
		{
			var messaggio = $"L'utente {OperatoreCorrente.UserName} non è autorizzato ad accedere alla pagina.";
			NavigationManager.NavigateTo($"/errore-logon?testo={Uri.EscapeDataString(messaggio)}");
			return;
		}

		try
		{
			_sorgenti = await QuestionariService.GetSorgentiAbilitateAsync();
			_tipiQuestionario = await QuestionariService.GetTipiQuestionarioAsync();
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Errore nel caricamento dei parametri per la generazione URL questionari");
			_errore = "Errore durante il caricamento dei parametri iniziali.";
		}
	}

	/// <summary>
	/// Torna alla Home; la pagina espone questa azione soltanto agli operatori gestori.
	/// </summary>
	private void Annulla() => NavigationManager.NavigateTo("/");

	/// <summary>
	/// Mostra il riepilogo solo se i parametri superano la validazione del form.
	/// L'URL non viene ancora generata in questo passaggio.
	/// </summary>
	private Task VaiAConfermaAsync()
	{
		if (!_editContext.Validate())
			return Task.CompletedTask;

		_errore = null;
		_messaggioCopia = null;
		_step = 2;
		return Task.CompletedTask;
	}

	/// <summary>
	/// Riporta al form conservando i valori inseriti per consentirne la correzione.
	/// </summary>
	private void TornaAlPassaggio1()
	{
		_errore = null;
		_step = 1;
	}

	/// <summary>
	/// Delega al servizio la generazione in modalità libera o personale e mostra il risultato.
	/// Se la generazione fallisce, mantiene il riepilogo e segnala l'errore all'operatore.
	/// </summary>
	private async Task ConfermaECreaUrlAsync()
	{
		_errore = null;
		_messaggioCopia = null;
		// Disabilita Conferma e Indietro nella UI fino al termine dell'operazione.
		_inGenerazione = true;

		try
		{
			_urlGenerata = _parametri.ModalitaCompilazione switch
			{
				"Libera" => QuestionariService.GeneraUrlLibera(_parametri.Sorgente!, _parametri.CodiceTipoQuestionario!),
				"Personale" => await QuestionariService.GeneraUrlPersonaleAsync(_parametri.CodiceTipoQuestionario!, _parametri.Sorgente!, _parametri.Email!),
				_ => throw new InvalidOperationException("Modalità compilazione non valida.")
			};

			_step = 3;
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Errore durante la generazione URL questionario");
			_errore = "Impossibile generare la URL. Verificare la configurazione e riprovare.";
		}
		finally
		{
			// Riabilita i pulsanti anche quando il servizio genera un'eccezione.
			_inGenerazione = false;
		}
	}

	/// <summary>
	/// Copia l'URL negli appunti del browser tramite JavaScript e comunica l'esito nella pagina.
	/// Un errore nella copia non elimina l'URL già generata.
	/// </summary>
	private async Task CopiaUrlAsync()
	{
		if (string.IsNullOrWhiteSpace(_urlGenerata))
			return;

		try
		{
			await JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", _urlGenerata);
			_messaggioCopia = "URL copiata negli appunti.";
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Errore durante la copia in clipboard della URL questionario");
			_errore = "Copia non riuscita. Riprovare.";
		}
	}

	/// <summary>
	/// Avvia un nuovo inserimento mantenendo sorgente e tipo questionario, ma azzerando
	/// modalità, email, URL e messaggi dell'operazione precedente.
	/// </summary>
	private void NuovaGenerazione()
	{
		var sorgente = _parametri.Sorgente;
		var codiceTipo = _parametri.CodiceTipoQuestionario;
		_parametri = new ParametriGenerazioneModel
		{
			Sorgente = sorgente,
			CodiceTipoQuestionario = codiceTipo,
			ModalitaCompilazione = null,
			Email = null
		};
		// Il nuovo modello richiede un nuovo contesto, senza gli esiti della validazione precedente.
		_editContext = new EditContext(_parametri);

		_urlGenerata = null;
		_messaggioCopia = null;
		_errore = null;
		_step = 1;
	}

	/// <summary>
	/// Recupera il nome del tipo per il riepilogo, usando il codice come ripiego se non è presente nell'elenco.
	/// </summary>
	private string GetNomeTipoQuestionario()
	{
		return _tipiQuestionario.FirstOrDefault(x => x.Codice == _parametri.CodiceTipoQuestionario)?.Nome
			?? _parametri.CodiceTipoQuestionario
			?? string.Empty;
	}

	/// <summary>
	/// Definisce i dati del form e le relative regole: selezioni obbligatorie, formato dell'email
	/// quando valorizzata e obbligatorietà dell'email soltanto in modalità personale.
	/// </summary>
	private sealed class ParametriGenerazioneModel : IValidatableObject
	{
		[Required(ErrorMessage = "La sorgente è obbligatoria.")]
		public string? Sorgente { get; set; }

		[Required(ErrorMessage = "Il tipo questionario è obbligatorio.")]
		public string? CodiceTipoQuestionario { get; set; }

		[Required(ErrorMessage = "La modalità compilazione è obbligatoria.")]
		public string? ModalitaCompilazione { get; set; }

		[EmailAddress(ErrorMessage = "Inserire un indirizzo email valido.")]
		public string? Email { get; set; }

		// Completa le annotazioni con la regola che dipende dalla modalità selezionata.
		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (ModalitaCompilazione == "Personale" && string.IsNullOrWhiteSpace(Email))
				yield return new ValidationResult("L'email è obbligatoria in modalità personale.", [nameof(Email)]);
		}
	}
}
