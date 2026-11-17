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
	public partial class Home
	{
		private List<TIPI_RICHIESTA> tipiRichiesta = new();
		[Inject] private ILogger<Home> Logger { get; set; } = default!;

		Operatore? operatore = null;

		string? errore;

		protected override async Task OnInitializedAsync()
		{
			Logger.LogInformation("Home page"); 
			await base.OnInitializedAsync();

			foreach (var header in HttpContextAccessor.HttpContext?.Request.Headers
					 ?? new HeaderDictionary())
			{
				Logger.LogDebug(
					"Header {HeaderName}: {HeaderValue}",
					header.Key,
					header.Value.ToString());
			}

			try
			{
				Logger.LogDebug("Caricamento tipi richiesta");
				tipiRichiesta = await urpDbContext.TIPI_RICHIESTA
					.OrderBy(i => i.ORDINE)
					.ThenBy(i => i.DESCRIZIONE_TIPO_RICHIESTA)
					.ToListAsync();


				var json = HttpContextAccessor.HttpContext?.Session.GetString("Operatore");

				Logger.LogDebug($"NavigationManager.BaseUri: {NavigationManager.BaseUri}");
				Logger.LogDebug($"SessionId: {HttpContextAccessor.HttpContext?.Session.Id}");
				Logger.LogDebug($"Caricamento operatore dalla sessione: {json}");

				if (!string.IsNullOrEmpty(json))
				{
					operatore = JsonSerializer.Deserialize<Operatore>(json);
				}
				else
				{
					Logger.LogWarning("Operatore non trovato nella sessione");
				}
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Errore durante il caricamento dei tipi richiesta");
				errore = "Si è verificato un errore. Contattare l'assistenza.";
			}
		}
	}
}