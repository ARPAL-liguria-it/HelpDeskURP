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

using GestioneURP.Components.State;

namespace GestioneURP.Infrastructure.Gso
{
	public static class GsoWsClientAdapter
	{
		public static async Task<List<UtenteApplicazione>> ElencoUtentiApplicazioneAsync(
			string identificativoApplicazione,
			string gsoWsBaseUrl,
			HttpClient httpClient,
			ILogger? logger = null)
		{
			logger?.LogInformation("Recupero elenco utenti applicazione {IdentificativoApplicazione} da GSO WS {GsoWsBaseUrl}",
				identificativoApplicazione, gsoWsBaseUrl);

			ArgumentException.ThrowIfNullOrWhiteSpace(identificativoApplicazione);
			ArgumentException.ThrowIfNullOrWhiteSpace(gsoWsBaseUrl);
			ArgumentNullException.ThrowIfNull(httpClient);

			var client = new GsoWsClient.GsoWsClient(httpClient, gsoWsBaseUrl, logger);

			var result = await client.GetApplicationUsersAsync(identificativoApplicazione);

			return result
				.Where(x => x.IdUtente.HasValue)
				.GroupBy(x => x.IdUtente!.Value)
				.Select(g => new UtenteApplicazione
				{
					IdUtente = g.Key,
					CognomeNome = string.Join(" ",
						new[] { g.First().Nome, g.First().Cognome }
							.Where(v => !string.IsNullOrWhiteSpace(v)))
				})
				.ToList();
		}

		public static async Task<List<ProfiloOperatore>> ProfiliOperatoreAsync(
			decimal userId,
			string identificativoApplicazione,
			string gsoWsBaseUrl,
			HttpClient httpClient,
			ILogger? logger = null)
		{
			logger?.LogInformation("Recupero profili GSO per utente {UserId} da GSO WS {GsoWsBaseUrl}", userId, gsoWsBaseUrl);

			ArgumentException.ThrowIfNullOrWhiteSpace(gsoWsBaseUrl);
			ArgumentNullException.ThrowIfNull(httpClient);

			var client = new GsoWsClient.GsoWsClient(httpClient, gsoWsBaseUrl, logger);
			var profili = await client.GetAppUserProfilesAsync(userId, identificativoApplicazione);

			return profili
				.Select(profilo => new ProfiloOperatore
				{
					Nome = profilo.Nome,
					FunzioniAbilitate = profilo.Funzioni
						.Where(funzione => funzione.Abilit)
						.Select(funzione => funzione.Valore)
						.Where(valore => !string.IsNullOrWhiteSpace(valore))
						.Cast<string>()
						.ToList()
				})
				.ToList();
		}

		public static async Task<List<Struttura>> ElencoStruttureAsync(
			string gsoWsBaseUrl,
			HttpClient httpClient,
			ILogger? logger = null)
		{
			logger?.LogInformation("Recupero elenco strutture da GSO WS {GsoWsBaseUrl}", gsoWsBaseUrl);

			ArgumentException.ThrowIfNullOrWhiteSpace(gsoWsBaseUrl);
			ArgumentNullException.ThrowIfNull(httpClient);

			var client = new GsoWsClient.GsoWsClient(httpClient, gsoWsBaseUrl, logger);

			var result = await client.GetAllStruttureAsync();


			return result
				.Select(s => new Struttura
				{
					CodiceStruttura = s.CodiceStruttura,
					DescrizioneStruttura = s.DescrizioneStruttura
				})
				.OrderBy(s => s.CodiceStruttura)
				.ToList();
		}
	}


	public sealed class UtenteApplicazione
	{
		public decimal IdUtente { get; init; }
		public string? CognomeNome { get; init; }
	}

	public sealed class Struttura
	{
		public string? CodiceStruttura { get; init; }
		public string? DescrizioneStruttura { get; init; }
	}
}
