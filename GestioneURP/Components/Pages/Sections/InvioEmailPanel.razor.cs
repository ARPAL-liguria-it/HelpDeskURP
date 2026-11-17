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
using GestioneURP.Infrastructure.Email;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;

namespace GestioneURP.Components.Pages.Sections;

/// <summary>
/// Gestisce il dialogo di anteprima e conferma dell'email: l'operatore modifica il suffisso
/// dell'oggetto e sceglie gli allegati, mentre il servizio applicativo esegue l'invio e la chiusura della pratica.
/// Dopo un esito consente di tornare alla gestione aggiornata oppure all'elenco delle segnalazioni.
/// </summary>
public partial class InvioEmailPanel : IDisposable
{
	[Inject] private InvioEmailService EmailService { get; set; } = default!;
	[Inject] private EmailCircuitHandler Circuito { get; set; } = default!;
	[Parameter] public long IdSegnalazione { get; set; }
	[Parameter] public string TipoSegnalazione { get; set; } = default!;
	// Il valore true richiede alla pagina chiamante di ricaricare i dati della pratica;
	// false chiude soltanto il pannello, senza richiedere un aggiornamento.
	[Parameter] public EventCallback<bool> OnGestione { get; set; }
	[Parameter] public EventCallback OnElenco { get; set; }

	// La rimozione del componente annulla le operazioni ancora in corso.
	private readonly CancellationTokenSource _durataComponente = new();
	private readonly OggettoEmailModel _modello = new();
	// Gli allegati sono inizialmente non selezionati; l'insieme evita selezioni duplicate.
	private readonly HashSet<long> _selezionati = [];
	private FluentDialog? _dialogo;
	private AnteprimaEmail? _anteprima;
	private EditContext? _editContext;
	// Un esito sostituisce l'anteprima con il risultato; un avviso lascia possibile un nuovo tentativo.
	private EsitoInvioEmail? _esito;
	private string? _avviso;
	// Il pannello resta occupato già durante la preparazione iniziale, prima di qualsiasi invio.
	private bool _inCorso = true;
	private bool _disposed;
	// Abilita il pulsante solo per un suffisso compilato entro il limite previsto;
	// alla conferma viene comunque eseguita la validazione completa del form.
	private bool SuffissoValido => !string.IsNullOrWhiteSpace(_modello.Suffisso) && _modello.Suffisso.Length <= 70;

	// Prepara i contenuti da mostrare senza inviare l'email e inizializza il form dell'oggetto.
	protected override async Task OnInitializedAsync()
	{
		// L'operazione viene annullata sia alla rimozione del componente sia alla disconnessione del circuito.
		using var operazione = Circuito.CreaOperazione(_durataComponente.Token);
		try
		{
			_anteprima = await EmailService.PreparaAsync(IdSegnalazione, TipoSegnalazione, operazione.Token);
			_modello.Suffisso = _anteprima.SuffissoIniziale;
			_editContext = new EditContext(_modello);
		}
		catch (OperationCanceledException) when (operazione.IsCancellationRequested)
		{
			_esito = new(StatoInvioEmail.Interrotto, "Preparazione interrotta. Nessuna email è stata inviata.");
		}
		catch (InvalidOperationException ex)
		{
			_esito = new(StatoInvioEmail.ErroreInvio, ex.Message);
		}
		finally { _inCorso = false; }
	}

	// Delega al servizio l'invio con il suffisso corrente e i soli allegati selezionati.
	private async Task ConfermaAsync()
	{
		// Impedisce invii concorrenti, nuovi invii dopo un esito e conferme di un form non pronto o non valido.
		if (_inCorso || _esito is not null || _anteprima is null || _editContext is null || !_editContext.Validate()) return;
		_inCorso = true;
		_avviso = null;
		using var operazione = Circuito.CreaOperazione(_durataComponente.Token);
		try
		{
			var esito = await EmailService.InviaEChiudiAsync(_anteprima, _modello.Suffisso, _selezionati.ToArray(), operazione.Token);
			// Un blocco mantiene visibile il form e mostra il motivo; gli altri esiti mostrano il risultato.
			if (esito.Stato == StatoInvioEmail.Bloccato) _avviso = esito.Messaggio;
			else _esito = esito;
		}
		// Sblocca l'interfaccia anche se l'operazione termina con un'eccezione.
		finally { _inCorso = false; }
	}

	// Mantiene la scelta degli allegati stabile mentre il servizio sta lavorando.
	private void Seleziona(long id, bool selezionato)
	{
		if (_inCorso) return;
		if (selezionato) _selezionati.Add(id);
		else _selezionati.Remove(id);
	}

	// Durante un'operazione i comandi di uscita non hanno effetto.
	// Annulla non richiede un ricaricamento; il ritorno dopo un esito aggiorna invece la gestione.
	private Task Annulla() => _inCorso ? Task.CompletedTask : OnGestione.InvokeAsync(false);
	private Task TornaGestione() => _inCorso ? Task.CompletedTask : OnGestione.InvokeAsync(true);
	private Task TornaElenco() => _inCorso ? Task.CompletedTask : OnElenco.InvokeAsync();

	// Gestisce anche la chiusura del dialogo non avviata dai pulsanti del pannello.
	private async Task ChiusuraDialogo(bool nascosto)
	{
		if (!nascosto) return;
		if (_inCorso)
		{
			// Ripristina il dialogo per evitare l'uscita durante preparazione o invio.
			_dialogo?.Show();
			return;
		}
		// Richiede l'aggiornamento della pratica se è stato mostrato un esito, anche non riuscito.
		await OnGestione.InvokeAsync(_esito is not null);
	}

	// Annulla le operazioni legate al pannello e libera le risorse una sola volta.
	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;
		_durataComponente.Cancel();
		_durataComponente.Dispose();
	}

	// Il form modifica soltanto il suffisso: il prefisso dell'oggetto proviene dall'anteprima.
	// Questi vincoli vengono applicati dall'EditContext tramite il validatore del form Razor.
	private sealed class OggettoEmailModel
	{
		[Required(ErrorMessage = "L'oggetto è obbligatorio.")]
		[StringLength(70, ErrorMessage = "Il suffisso oggetto non può superare 70 caratteri.")]
		public string Suffisso { get; set; } = "";
	}
}
