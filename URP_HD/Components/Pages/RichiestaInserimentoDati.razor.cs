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
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using URP_HD.Data;
using URP_HD.Models;


namespace URP_HD.Components.Pages
{
	public partial class RichiestaInserimentoDati
	{
		[Inject] private ILogger<RichiestaInserimentoDati> Logger { get; set; } = default!;
		[Inject]
		private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

		List<TIPI_AUTORE_RICHIESTA> tipiAutoreRichiesta = new();
		List<TIPI_TEMATICA> tipiTematica = new();
		List<TIPI_LUOGO> tipiLuogo = new();
		List<V_ELENCO_COMUNI_PROV> elencoComuni = new();
		V_ELENCO_COMUNI_PROV? comuneSelezionato = null;

		bool cognomePrecompilato = false;
		bool nomePrecompilato = false;
		bool emailPrecompilato = false;
		private string tipoTematicaSelezionata
		{
			get
			{
				return State.Richiesta.ID_TIPO_TEMATICA.ToString();
			}
			set
			{
				if (int.TryParse(value, out int id))
				{
					State.Richiesta.ID_TIPO_TEMATICA = id;
				}
			}
		}

		private string tipoAutoreSelezionato
		{
			get
			{
				return State.Richiesta.ID_TIPO_AUTORE_RICHIESTA.ToString();
			}
			set
			{
				if (int.TryParse(value, out int id))
				{
					State.Richiesta.ID_TIPO_AUTORE_RICHIESTA = id;
				}
			}
		}

		private string tipoLuogoSelezionato
		{
			get
			{
				return State.Richiesta.ID_TIPO_LUOGO.ToString();
			}
			set
			{
				if (int.TryParse(value, out int id))
				{
					State.Richiesta.ID_TIPO_LUOGO = id;
				}
			}
		}

		[Parameter]
		public RichiestaWizardState State { get; set; } = default!;
		[Parameter]
		public EventCallback OnAvanti { get; set; }

		[Parameter]
		public EventCallback OnIndietro { get; set; }

		protected override async Task OnInitializedAsync()
		{
			Logger.LogInformation($"IdTipoRichiesta:{State.Richiesta.ID_TIPO_RICHIESTA}");

			// usa dbfactory per creare il contesto del database
			await using var db = await DbFactory.CreateDbContextAsync();

			tipiTematica.Add(new TIPI_TEMATICA
			{
				ID_TIPO_TEMATICA = 0,
				DESCRIZIONE_TIPO_TEMATICA = "--- selezionare una voce ---",
				ORDINE = -1
			});

			tipiTematica.AddRange(await db.TIPI_TEMATICA
				.OrderBy(i => i.ORDINE)
				.ThenBy(i => i.DESCRIZIONE_TIPO_TEMATICA)
				.ToListAsync());

			tipiLuogo.Add(new TIPI_LUOGO
			{
				ID_TIPO_LUOGO = 0,
				DESCRIZIONE_TIPO_LUOGO = "--- selezionare una voce ---",
				ORDINE = -1
			});

			tipiLuogo.AddRange(await db.TIPI_LUOGO
				.OrderBy(i => i.ORDINE)
				.ThenBy(i => i.DESCRIZIONE_TIPO_LUOGO)
				.ToListAsync());

			elencoComuni.Add(new V_ELENCO_COMUNI_PROV
			{
				DESC_COMUNE_PROVINCIA = "--- selezionare una voce ---",
			});

			elencoComuni.AddRange(await db.V_ELENCO_COMUNI_PROV
				.OrderBy(i => i.DESC_COMUNE_PROVINCIA)
				.ToListAsync());

			tipiAutoreRichiesta.Add(new TIPI_AUTORE_RICHIESTA
			{
				ID_TIPO_AUTORE_RICHIESTA = 0,
				DESCRIZIONE_TIPO_AUTORE_RICHIESTA = "--- selezionare una voce ---",
				ORDINE = -1
			});

			tipiAutoreRichiesta.AddRange(await db.TIPI_AUTORE_RICHIESTA
				.OrderBy(i => i.ORDINE)
				.ThenBy(i => i.DESCRIZIONE_TIPO_AUTORE_RICHIESTA)
				.ToListAsync());

			if (!string.IsNullOrWhiteSpace(State.Richiesta.COMUNE_LUOGO))
			{
				comuneSelezionato = await db.V_ELENCO_COMUNI_PROV
					.FirstOrDefaultAsync(x =>
						x.DESC_COMUNE_PROVINCIA == State.Richiesta.COMUNE_LUOGO);
			}
			var json = HttpContextAccessor.HttpContext?.Session.GetString("Operatore");

			Operatore? operatore = null;
			if (!string.IsNullOrEmpty(json))
			{
				operatore = JsonSerializer.Deserialize<Operatore>(json);
				if (!String.IsNullOrWhiteSpace(operatore?.Cognome))
				{
					cognomePrecompilato = true;
					State.Richiesta.COGNOME_AUTORE = operatore?.Cognome;
				}
				if (!String.IsNullOrWhiteSpace(operatore?.Nome))
				{
					nomePrecompilato = true;
					State.Richiesta.NOME_AUTORE = operatore?.Nome;
				}
				if (!String.IsNullOrWhiteSpace(operatore?.Email))
				{
					emailPrecompilato = true;
					State.Richiesta.EMAIL_AUTORE = operatore?.Email;
				}
			}
		}

		bool AutoHeightElencoComuni = true;

		private async Task OnSearchComuneAsync(OptionsSearchEventArgs<V_ELENCO_COMUNI_PROV> e)
		{
			// usa dbfactory per creare il contesto del database
			await using var db = await DbFactory.CreateDbContextAsync();
			var elencoComuni = await db.V_ELENCO_COMUNI_PROV.ToListAsync();
			e.Items = elencoComuni
				.Where(i => i.DESC_COMUNE_PROVINCIA.StartsWith(e.Text, StringComparison.OrdinalIgnoreCase))
				.OrderBy(i => i.DESC_COMUNE_PROVINCIA);
		}

		private void OnComuneSelezionato()
		{
			State.Richiesta.COMUNE_LUOGO = comuneSelezionato?.DESC_COMUNE_PROVINCIA;
		}

		private List<string> elencoErrori = new();
		private bool ErroriValidazione()
		{
			// Verifica campi obbligatori
			elencoErrori.Clear();

			if (string.IsNullOrWhiteSpace(State.Richiesta.EMAIL_AUTORE))
			{
				elencoErrori.Add("Il campo Email è obbligatorio. ");
			}
			else
			{
				var emailValidator = new EmailAddressAttribute();

				if (!emailValidator.IsValid(State.Richiesta.EMAIL_AUTORE))
				{
					elencoErrori.Add("L'indirizzo e-mail non è valido.");
				}
			}

			if (!string.IsNullOrWhiteSpace(State.Richiesta.PEC_AUTORE))
			{
				var emailValidator = new EmailAddressAttribute();

				if (!emailValidator.IsValid(State.Richiesta.PEC_AUTORE))
				{
					elencoErrori.Add("L'indirizzo PEC non è valido.");
				}
			}

			if (string.IsNullOrWhiteSpace(tipoAutoreSelezionato) || tipoAutoreSelezionato == "0")
			{
				elencoErrori.Add("Il campo Chi sono è obbligatorio. ");
			}

			if (string.IsNullOrWhiteSpace(tipoTematicaSelezionata) || tipoTematicaSelezionata == "0")
			{
				elencoErrori.Add("Il campo Tematica è obbligatorio. ");
			}

			if (string.IsNullOrWhiteSpace(tipoLuogoSelezionato) || tipoLuogoSelezionato == "0")
			{
				elencoErrori.Add("Il campo Luogo è obbligatorio. ");
			}

			if (tipoLuogoSelezionato == "5" &&
			string.IsNullOrWhiteSpace(State.Richiesta.COMUNE_LUOGO))
			{
				// Mostra un messaggio
				elencoErrori.Add("Il campo Comune è obbligatorio. ");
			}

			if (string.IsNullOrWhiteSpace(State.Richiesta.TESTO))
			{
				elencoErrori.Add("Il campo Testo della richiesta è obbligatorio. ");
			}

			return elencoErrori.Any();
		}
		private async Task Avanti()
		{
			if (ErroriValidazione())
			{
				await VaiInAlto();
				return;
			}

			if (State.Step < 4)
			{
				State.Step++;

				await OnAvanti.InvokeAsync();
			}
		}

		private async Task Annulla()
		{
			// torna alla home
			NavigationManager.NavigateTo(NavigationManager.BaseUri);
		}

		private async Task VaiInAlto()
		{
			await JS.InvokeVoidAsync("window.scrollTo",
			new
			{
				top = 0,
				left = 0,
				behavior = "smooth"
			});
		}

	}
}
