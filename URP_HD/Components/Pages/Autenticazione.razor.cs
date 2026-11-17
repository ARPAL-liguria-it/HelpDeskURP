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
using System.Text.Json;
using URP_HD.Models;


namespace URP_HD.Components.Pages
{
	public partial class Autenticazione
	{
		[Inject] private ILogger<Autenticazione> Logger { get; set; } = default!;
		private string? valoreHeaderCognome;
		private string? valoreHeaderNome;
		private string? valoreHeaderEmail;

		[Parameter]
		[SupplyParameterFromQuery(Name = "idtiporichiesta")]
		public int? IdTipoRichiesta { get; set; }

		protected override void OnInitialized()
		{
			foreach (var header in HttpContextAccessor.HttpContext?.Request.Headers
					 ?? new HeaderDictionary())
			{
				Logger.LogInformation(
					"Header {HeaderName}: {HeaderValue}",
					header.Key,
					header.Value.ToString());
			}

			valoreHeaderCognome = HttpContextAccessor.HttpContext?
				.Request.Headers[Configuration["HeaderAutenticazione:Cognome"]]
				.FirstOrDefault();
			valoreHeaderNome = HttpContextAccessor.HttpContext?
				.Request.Headers[Configuration["HeaderAutenticazione:Nome"]]
				.FirstOrDefault();
			valoreHeaderEmail = HttpContextAccessor.HttpContext?
				.Request.Headers[Configuration["HeaderAutenticazione:Email"]]
				.FirstOrDefault();

			Logger.LogInformation($"Cognome: {valoreHeaderCognome}, Nome: {valoreHeaderNome}, Email: {valoreHeaderEmail}");

			// L'utente è autenticato se ci sono cognome e nome, altrimenti reindirizza alla home page
			if (String.IsNullOrEmpty(valoreHeaderCognome) || String.IsNullOrEmpty(valoreHeaderNome))
			{
				NavigationManager.NavigateTo(NavigationManager.BaseUri); 
				return;
			}

			Operatore operatore = new Operatore
			{
				Cognome = valoreHeaderCognome,
				Nome = valoreHeaderNome,
				Email = valoreHeaderEmail
			};

			HttpContextAccessor.HttpContext!.Session.SetString(
				"Operatore",
				JsonSerializer.Serialize(operatore)
			);

			Logger.LogDebug($"NavigationManager.BaseUri: {NavigationManager.BaseUri}");
			Logger.LogDebug($"SessionId: {HttpContextAccessor.HttpContext?.Session.Id}");
			Logger.LogInformation($"Operatore in sessione: {HttpContextAccessor.HttpContext?.Session.GetString("Operatore")}");

			if (!IdTipoRichiesta.HasValue)
			{
				Logger.LogInformation("IdTipoRichiesta mancante");
				NavigationManager.NavigateTo(NavigationManager.BaseUri);
				return;
			}

			Logger.LogInformation($"IdTipoRichiesta:{IdTipoRichiesta}");

			if (IdTipoRichiesta == 3)
			{
				NavigationManager.NavigateTo($"/ReclamoWizard?idtiporichiesta={IdTipoRichiesta}");
			}
			else
			{
				NavigationManager.NavigateTo($"/RichiestaWizard?idtiporichiesta={IdTipoRichiesta}");
			}
		}

	}
}
