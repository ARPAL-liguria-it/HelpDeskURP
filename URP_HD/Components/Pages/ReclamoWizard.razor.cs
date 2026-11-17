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

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using URP_HD.Data;
using URP_HD.Models;

namespace URP_HD.Components.Pages
{
	public partial class ReclamoWizard
	{
		[Inject] private ILogger<ReclamoWizard> Logger { get; set; } = default!;
		private ReclamoWizardState State = new();
		private string? descrizioneTipoRichiestaSelezionata;

		[Parameter]
		[SupplyParameterFromQuery(Name = "idtiporichiesta")]
		public int? IdTipoRichiesta { get; set; }

		Operatore? operatore = null;

		protected override async Task OnInitializedAsync()
		{
			if (IdTipoRichiesta.HasValue)
			{
				State.Reclamo.ID_TIPO_RICHIESTA = IdTipoRichiesta.Value;

				descrizioneTipoRichiestaSelezionata = await urpDbContext.TIPI_RICHIESTA
					.Where(i => i.ID_TIPO_RICHIESTA == IdTipoRichiesta.Value)
					.Select(i => i.DESCRIZIONE_TIPO_RICHIESTA)
					.FirstOrDefaultAsync();

				if (descrizioneTipoRichiestaSelezionata is null)
				{
					Logger.LogWarning("Tipo richiesta non trovato");
					// torna alla home
					NavigationManager.NavigateTo(NavigationManager.BaseUri);
				}
			}
			else
			{
				Logger.LogWarning("Tipo richiesta mancante");
				// torna alla home
				NavigationManager.NavigateTo(NavigationManager.BaseUri);
			}
			var json = HttpContextAccessor.HttpContext?.Session.GetString("Operatore");

			if (!string.IsNullOrEmpty(json))
			{
				operatore = JsonSerializer.Deserialize<Operatore>(json);
			}
		}

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				await PuliziaFileTemporaneiService.EliminaFile();
			}
		}

	}
	public class ReclamoWizardState
	{
		public RECLAMI Reclamo { get; set; } = new();

		public List<AllegatoCaricato> AllegatiCaricati { get; set; } = [];

		public class AllegatoCaricato
		{
			public string NomeFile { get; set; } = string.Empty;
			public string NomeFileTemp { get; set; } = string.Empty;
			public long DimensioneFile { get; set; }
		}
		public int Step { get; set; } = 1;
	}
}
