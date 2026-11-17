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

namespace GestioneURP.Components.Pages;

public partial class DettaglioSegnalazione
{
	[Inject] private SegnalazioniService SegnalazioniService { get; set; } = default!;
	[Inject] private NavigationManager NavigationManager { get; set; } = default!;
	[Inject] private ILogger<DettaglioSegnalazione> Logger { get; set; } = default!;
	[Inject] private OperatoreState OperatoreCorrente { get; set; } = default!;

	[Parameter] public long Id { get; set; }
	[SupplyParameterFromQuery(Name = "tipo")] public string? Tipo { get; set; }

	private SegnalazioneDettaglioDto? _dettaglio;
	private Sections.GestionePratica? _gestionePratica;
	private bool _erroreBloccante;
	private string? _dettaglioErrore;

	protected override async Task OnInitializedAsync()
	{
		if (!OperatoreCorrente.SeGestore())
		{
			var messaggio = $"L'utente {OperatoreCorrente.UserName} non è autorizzato ad accedere alla pagina.";
			NavigationManager.NavigateTo($"/errore-logon?testo={Uri.EscapeDataString(messaggio)}");
		}
	}

	protected override async Task OnParametersSetAsync()
	{
		_erroreBloccante = false;
		_dettaglioErrore = null;
		_dettaglio = null;

		if (string.IsNullOrWhiteSpace(Tipo))
		{
			var message = "Parametro obbligatorio 'tipo' assente.";
			SetErroreBloccante(message);
			Logger.LogError("Errore bloccante dettaglio segnalazione {Id}: {Message}", Id, message);
			return;
		}

		try
		{
			_dettaglio = await SegnalazioniService.GetDettaglioSegnalazioneAsync(Id, Tipo);
		}
		catch (InvalidOperationException ex)
		{
			SetErroreBloccante(ex.Message);
			Logger.LogError(ex, "Errore bloccante dettaglio segnalazione {Id}, tipo {Tipo}", Id, Tipo);
		}
		catch (Exception ex)
		{
			SetErroreBloccante("Errore inatteso durante il caricamento del dettaglio.");
			Logger.LogError(ex, "Errore inatteso dettaglio segnalazione {Id}, tipo {Tipo}", Id, Tipo);
		}
	}

	private void TornaAllaLista()
	{
		if (_gestionePratica is not null)
			_gestionePratica.RichiediUscita();
		else
			NavigationManager.NavigateTo("/");
	}

	private string GetAllegatoDownloadUrl(AllegatoDettaglioDto allegato)
		=> $"/segnalazioni/allegati/download/{_dettaglio!.TipoSegnalazione}/{allegato.IdAllegato}";

	private IReadOnlyList<AllegatoDettaglioDto> GetAllegatiSegnalazione()
		=> _dettaglio?.Allegati
			.Where(x => string.Equals(x.Sorgente, "Richiesta", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(x.Sorgente, "Reclamo", StringComparison.OrdinalIgnoreCase))
			.ToList()
		?? [];

	private static string FormatDataOra(DateTime? value) => value?.ToString("dd/MM/yyyy HH:mm") ?? "-";

	private static string ValoreOrDash(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value;

	private void SetErroreBloccante(string dettaglio)
	{
		_erroreBloccante = true;
		_dettaglioErrore = dettaglio;
	}

	private async Task OnGestionePraticaSalvata(bool salvato)
	{
		if (salvato)
		{
			// Ricarica il dettaglio per avere i dati aggiornati
			try
			{
				_dettaglio = await SegnalazioniService.GetDettaglioSegnalazioneAsync(Id, Tipo!);
				Logger.LogInformation("Dettaglio segnalazione aggiornato dopo salvataggio gestione pratica");
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Errore durante il refresh del dettaglio dopo salvataggio");
			}
		}
	}
}
