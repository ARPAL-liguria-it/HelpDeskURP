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
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using URP_HD.Data;
using URP_HD.Models;

namespace URP_HD.Components.Pages
{
	public partial class ReclamoInserimentoDati
	{
		[Inject]
		private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;
		[Inject] private ILogger<ReclamoInserimentoDati> Logger { get; set; } = default!;

		bool cognomePrecompilato = false;
		bool nomePrecompilato = false;
		bool emailPrecompilato = false;
		List<TIPI_AUTORE_RICHIESTA> tipiAutoreRichiesta = new();
		List<TIPI_STRUTTURA> tipiStruttura = new();
		private string tipoAutoreSelezionato
		{
			get
			{
				return State.Reclamo.ID_TIPO_AUTORE_RICHIESTA.ToString();
			}
			set
			{
				if (int.TryParse(value, out int id))
				{
					State.Reclamo.ID_TIPO_AUTORE_RICHIESTA = id;
				}
			}
		}
		private string tipoStrutturaSelezionata
		{
			get
			{
				return State.Reclamo.CODICE_STRUTTURA;
			}
			set
			{
				if (String.IsNullOrWhiteSpace(value))
				{
					State.Reclamo.CODICE_STRUTTURA = "0";
				}
				else
				{
					State.Reclamo.CODICE_STRUTTURA = value;
				}
			}
		}


		[Parameter]
		public ReclamoWizardState State { get; set; } = default!;
		[Parameter]
		public EventCallback OnAvanti { get; set; }

		[Parameter]
		public EventCallback OnIndietro { get; set; }

		protected override async Task OnInitializedAsync()
		{
			Logger.LogInformation($"IdTipoRichiesta:{State.Reclamo.ID_TIPO_RICHIESTA}");

			// usa dbfactory per creare il contesto del database
			await using var db = await DbFactory.CreateDbContextAsync();

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

			tipiStruttura.Add(new TIPI_STRUTTURA
			{
				CODICE_STRUTTURA = "0",
				DESCRIZIONE_STRUTTURA = "--- selezionare una voce ---",
				ORDINE = -1
			});

			tipiStruttura.AddRange(await db.TIPI_STRUTTURA
			.OrderBy(i => i.ORDINE)
			.ThenBy(i => i.DESCRIZIONE_STRUTTURA)
			.ToListAsync());

			var json = HttpContextAccessor.HttpContext?.Session.GetString("Operatore");

			Operatore? operatore = null;

			if (!string.IsNullOrEmpty(json))
			{
				operatore = JsonSerializer.Deserialize<Operatore>(json);
				if (!String.IsNullOrWhiteSpace(operatore?.Cognome))
				{
					cognomePrecompilato = true;
					State.Reclamo.COGNOME_AUTORE = operatore?.Cognome;
				}
				if (!String.IsNullOrWhiteSpace(operatore?.Nome))
				{
					nomePrecompilato = true;
					State.Reclamo.NOME_AUTORE = operatore?.Nome;
				}
				if (!String.IsNullOrWhiteSpace(operatore?.Email))
				{
					emailPrecompilato = true;
					State.Reclamo.EMAIL_AUTORE = operatore?.Email;
				}
			}
		}

		private List<string> elencoErrori = new();
		private bool ErroriValidazione()
		{
			// Verifica campi obbligatori
			elencoErrori.Clear();

			if (string.IsNullOrWhiteSpace(State.Reclamo.COGNOME_AUTORE))
			{
				elencoErrori.Add("Il campo Cognome è obbligatorio. ");
			}

			if (string.IsNullOrWhiteSpace(State.Reclamo.NOME_AUTORE))
			{
				elencoErrori.Add("Il campo Nome è obbligatorio. ");
			}

			if (string.IsNullOrWhiteSpace(State.Reclamo.EMAIL_AUTORE))
			{
				elencoErrori.Add("Il campo Email è obbligatorio. ");
			}
			else
			{
				var emailValidator = new EmailAddressAttribute();

				if (!emailValidator.IsValid(State.Reclamo.EMAIL_AUTORE))
				{
					elencoErrori.Add("L'indirizzo e-mail non è valido.");
				}
			}

			if (!string.IsNullOrWhiteSpace(State.Reclamo.PEC_AUTORE))
			{
				var emailValidator = new EmailAddressAttribute();

				if (!emailValidator.IsValid(State.Reclamo.PEC_AUTORE))
				{
					elencoErrori.Add("L'indirizzo PEC non è valido.");
				}
			}

			if (string.IsNullOrWhiteSpace(tipoAutoreSelezionato) || tipoAutoreSelezionato == "0")
			{
				elencoErrori.Add("Il campo Chi sono è obbligatorio. ");
			}

			if (string.IsNullOrWhiteSpace(State.Reclamo.TESTO))
			{
				elencoErrori.Add("Il campo Testo del reclamo è obbligatorio. ");
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
