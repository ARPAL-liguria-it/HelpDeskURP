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
using GestioneURP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using System.Reflection;

namespace GestioneURP.Components.Pages;

public partial class Home
{
	[Inject] private SegnalazioniService SegnalazioniService { get; set; } = default!;
	[Inject] private NavigationManager NavigationManager { get; set; } = default!;
	[Inject] private HomeFiltriState HomeFiltriState { get; set; } = default!;
	[Inject] private OperatoreState OperatoreCorrente { get; set; } = default!;

	private FluentDataGrid<SegnalazioneRigaDto>? _griglia;
	private PaginationState _paginazione = new() { ItemsPerPage = 15 };
	private GridItemsProvider<SegnalazioneRigaDto>? _itemsProvider;

	private List<TipoTematica> _tipiTematica = [];

	private static readonly string[] _statiPratica =
	[
		"Aperta",
		"Presa in carico da operatore",
		"Passaggio ad altro operatore",
		"Richiesto approfondimento ad altra struttura",
		"Ricevuta risposta da struttura",
		"Risposta per il cittadino",
		"Chiusa"
	];

	private FiltroViewModel _filtroInput = new();
	private FiltroViewModel _filtroAttivo = new();
	private string _versioneApplicazione = "n/d";

	protected override async Task OnInitializedAsync()
	{
		_versioneApplicazione = Assembly.GetEntryAssembly()?
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
			.InformationalVersion
			?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
			?? "n/d";

		if (!OperatoreCorrente.SeGestore())
		{
			var messaggio = $"L'utente {OperatoreCorrente.UserName} non è autorizzato ad accedere alla pagina.";
			NavigationManager.NavigateTo($"/errore-logon?testo={Uri.EscapeDataString(messaggio)}");
			return;
		}

		_tipiTematica = await SegnalazioniService.GetTipiTematicaAsync();

		ApplicaFiltriMemorizzatiAiControlli();

		_itemsProvider = async req =>
		{
			var pagina = req.StartIndex / _paginazione.ItemsPerPage + 1;
			var (righe, totale) = await SegnalazioniService.GetSegnalazioniAsync(
				_filtroAttivo.ToFiltri(), pagina, _paginazione.ItemsPerPage);
			return GridItemsProviderResult.From(righe, totale);
		};
	}

	private void ApriNuovaSegnalazione() => NavigationManager.NavigateTo("/segnalazioni/nuova");
	private void ApriGenerazioneUrlQuestionari() => NavigationManager.NavigateTo("/questionari/generazione-url");

	private async Task ApplicaFiltriAsync()
	{
		_filtroAttivo = _filtroInput.Clone();
		_filtroInput = _filtroAttivo.Clone();
		HomeFiltriState.Testo = _filtroAttivo.Testo;
		HomeFiltriState.IdTipoTematicaStr = _filtroAttivo.IdTipoTematicaStr;
		HomeFiltriState.StatoPratica = _filtroAttivo.StatoPratica;
		await _paginazione.SetCurrentPageIndexAsync(0);
		await (_griglia?.RefreshDataAsync() ?? Task.CompletedTask);
	}

	private async Task RimuoviFiltriAsync()
	{
		_filtroInput = new();
		_filtroAttivo = new();
		HomeFiltriState.Reset();
		await _paginazione.SetCurrentPageIndexAsync(0);
		await (_griglia?.RefreshDataAsync() ?? Task.CompletedTask);
	}

	private void ApplicaFiltriMemorizzatiAiControlli()
	{
		_filtroAttivo = new FiltroViewModel
		{
			Testo = HomeFiltriState.Testo,
			IdTipoTematicaStr = HomeFiltriState.IdTipoTematicaStr,
			StatoPratica = HomeFiltriState.StatoPratica
		};

		_filtroInput = _filtroAttivo.Clone();
		_filtroInput.IdTipoTematicaStr ??= string.Empty;
		_filtroInput.StatoPratica ??= string.Empty;
	}

	private bool SelectFromEntireRow { get; set; } = true;

	private void OnRowClick(FluentDataGridRow<SegnalazioneRigaDto> row)
	{
		var item = row.Item;
		NavigationManager.NavigateTo($"/segnalazioni/dettaglio/{item.Id}?tipo={item.TipoSegnalazione.ToLowerInvariant()}");
	}

	private sealed class FiltroViewModel
	{
		public string? Testo { get; set; }
		public string? IdTipoTematicaStr { get; set; }
		public string? StatoPratica { get; set; }

		public FiltroViewModel Clone() => new()
		{
			Testo = Testo,
			IdTipoTematicaStr = IdTipoTematicaStr,
			StatoPratica = StatoPratica
		};

		public FiltriSegnalazione ToFiltri() => new()
		{
			Testo = Testo,
			IdTipoTematica = long.TryParse(IdTipoTematicaStr, out var id) ? id : null,
			StatoPratica = string.IsNullOrEmpty(StatoPratica) ? null : StatoPratica
		};
	}
}
