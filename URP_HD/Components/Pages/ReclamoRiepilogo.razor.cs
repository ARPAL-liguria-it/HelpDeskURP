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
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;
using URP_HD.Data;
using URP_HD.Models;

namespace URP_HD.Components.Pages
{
	public partial class ReclamoRiepilogo
	{
		[Inject] private ILogger<ReclamoRiepilogo> Logger { get; set; } = default!;

		[Parameter]
		public ReclamoWizardState State { get; set; } = default!;
		[Parameter]
		public EventCallback OnAvanti { get; set; }

		[Parameter]
		public EventCallback OnIndietro { get; set; }

		private string DESCRIZIONE_TIPO_AUTORE;
		private bool checkInformativa = false;

		// utilizzato per mostrare un messaggio di caricamento durante l'invio della richiesta
		private bool isLoading = false;

		protected override async Task OnInitializedAsync()
		{
			// usa dbfactory per creare il contesto del database
			await using var db = await DbFactory.CreateDbContextAsync();

			// Legge la descrizione tematica corrispondente al tipo
			if (State.Reclamo.ID_TIPO_AUTORE_RICHIESTA != 0)
			{
				DESCRIZIONE_TIPO_AUTORE = await db.TIPI_AUTORE_RICHIESTA
					.Where(i => i.ID_TIPO_AUTORE_RICHIESTA == State.Reclamo.ID_TIPO_AUTORE_RICHIESTA)
					.Select(i => i.DESCRIZIONE_TIPO_AUTORE_RICHIESTA)
					.FirstOrDefaultAsync() ?? string.Empty;
			}
		}

		private List<string> elencoErrori = new();

		private async Task InviaReclamo()
		{
			if (isLoading)
				return;

			isLoading = true;

            // Allegati per email a URP
            var fileAllegatiPerEmail = new List<EmailAttachment>();
            int idReclamoInserito = State.Reclamo.ID_RECLAMO;
            await using var db = await DbFactory.CreateDbContextAsync();

            try
            {
				Logger.LogInformation("Inserimento reclamo");
				Logger.LogDebug($"Inserimento reclamo: tipo:{State.Reclamo.ID_TIPO_RICHIESTA}");


				State.Reclamo.TIPO_SORGENTE = "Web";

				var connection = db.Database.GetDbConnection();

				if (connection.State != System.Data.ConnectionState.Open)
					await connection.OpenAsync();

				await using var cmd = connection.CreateCommand();

				cmd.CommandText = @"
					UPDATE CONTATORI
					SET valore = valore + 1
					WHERE contatore = 'Reclami'
					RETURNING valore INTO :nuovovalore";

				var nuovoValore = new OracleParameter(
					"nuovovalore",
					OracleDbType.Decimal)
				{
					Direction = ParameterDirection.Output
				};

				cmd.Parameters.Add(nuovoValore);

				await cmd.ExecuteNonQueryAsync();

				var oracleValue = (OracleDecimal)nuovoValore.Value;

				if (oracleValue.IsNull)
				{
					throw new InvalidOperationException(
						"Il contatore 'Reclami' non ha restituito un valore.");
				}

				int progressivo = oracleValue.ToInt32();

				Logger.LogDebug($"Nuovo progressivo reclamo: {progressivo}");

				State.Reclamo.CODICE = $"RE{progressivo}";

				db.RECLAMI.Add(State.Reclamo);

				await db.SaveChangesAsync();

				idReclamoInserito = State.Reclamo.ID_RECLAMO;

				Logger.LogInformation(
					$"Inserito reclamo: ID={idReclamoInserito}, " +
					$"Codice={State.Reclamo.CODICE}");

				foreach (var allegatoCaricato in State.AllegatiCaricati)
				{
					Logger.LogInformation("Inserimento allegati");

					var allegato = new ALLEGATI_RECLAMI
					{
						ID_RECLAMO = idReclamoInserito,
						NOME_FILE = allegatoCaricato.NomeFile,
						SORGENTE = "Reclamo"
					};
					db.ALLEGATI_RECLAMI.Add(allegato);
					await db.SaveChangesAsync();
					int idAllegatoInserito = allegato.ID_ALLEGATO;

					Logger.LogDebug($"Allegato inserito: {idAllegatoInserito}");

					var percorsoSorgente = Path.Combine(
						Configuration["CartellaTemporanea"],
						allegatoCaricato.NomeFileTemp);

					var percorsoDestinazione = Path.Combine(
						Configuration["CartellaAllegatiReclami"],
						$"{idAllegatoInserito}");

					Logger.LogDebug($"Spostamento file da {percorsoSorgente} a {percorsoDestinazione}");

					File.Move(
						percorsoSorgente,
						percorsoDestinazione,
						true);

					// Allegato per email a URP
					fileAllegatiPerEmail.Add(new EmailAttachment
					{
						FileName = allegato.NOME_FILE,
						ContentType = "application/octet-stream",
						Content = await File.ReadAllBytesAsync(percorsoDestinazione)
					});

				}
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Errore inserimento reclamo");
                NavigationManager.NavigateTo("errore");
            }
            finally
            {
                isLoading = false;
            }

            try
            {
                Logger.LogInformation("Invio email a cittadino");

				// Legge il template dell'email di conferma reclamo
				var percorsoTemplateConfermaReclamo = Path.Combine(
					HostEnvironment.ContentRootPath,
					"Templates",
					"EmailConfermaReclamo.txt");

				// Legge il contenuto del template e sostituisce i segnaposto con i valori reali
				var testoEmail = await File.ReadAllTextAsync(percorsoTemplateConfermaReclamo);
				testoEmail = testoEmail.Replace("[CODICE]", State.Reclamo.CODICE);
				testoEmail = testoEmail.Replace("[COGNOME_AUTORE]", State.Reclamo.COGNOME_AUTORE);
				testoEmail = testoEmail.Replace("[NOME_AUTORE]", State.Reclamo.NOME_AUTORE);
				testoEmail = testoEmail.Replace("[EMAIL_AUTORE]", State.Reclamo.EMAIL_AUTORE);
				testoEmail = testoEmail.Replace("[PEC_AUTORE]", State.Reclamo.PEC_AUTORE);
				testoEmail = testoEmail.Replace("[TELEFONO_AUTORE]", State.Reclamo.TELEFONO_AUTORE);
				testoEmail = testoEmail.Replace("[DESCRIZIONE_TIPO_AUTORE]", DESCRIZIONE_TIPO_AUTORE);
				testoEmail = testoEmail.Replace("[TESTO]", State.Reclamo.TESTO);
				if (State.AllegatiCaricati.Any())
				{
					testoEmail = testoEmail.Replace("[ALLEGATI]", "Allegati: " + string.Join(", ", State.AllegatiCaricati.Select(a => a.NomeFile)));
				}
				else
				{
					testoEmail = testoEmail.Replace("[ALLEGATI]", String.Empty);
				}

				Logger.LogDebug($"Invio e-mail a {State.Reclamo.EMAIL_AUTORE} per reclamo {State.Reclamo.CODICE}");

				await EmailService.SendAsync(
					State.Reclamo.EMAIL_AUTORE,
					"Segnalazione",
					testoEmail
					);

				Logger.LogInformation($"E-mail inviata a {State.Reclamo.EMAIL_AUTORE}");

				if (!String.IsNullOrWhiteSpace(Configuration["Email:DestinatarioURP"]))
				{
					Logger.LogInformation("Invio email a URP");
					Logger.LogDebug($"Invio e-mail a {Configuration["Email:DestinatarioURP"]} per reclamo {State.Reclamo.CODICE}");

					percorsoTemplateConfermaReclamo = Path.Combine(
						HostEnvironment.ContentRootPath,
						"Templates",
						"EmailConfermaReclamoURP.txt");

					testoEmail = await File.ReadAllTextAsync(percorsoTemplateConfermaReclamo);
					testoEmail = testoEmail.Replace("[CODICE]", State.Reclamo.CODICE);
					testoEmail = testoEmail.Replace("[COGNOME_AUTORE]", State.Reclamo.COGNOME_AUTORE);
					testoEmail = testoEmail.Replace("[NOME_AUTORE]", State.Reclamo.NOME_AUTORE);
					testoEmail = testoEmail.Replace("[EMAIL_AUTORE]", State.Reclamo.EMAIL_AUTORE);
					testoEmail = testoEmail.Replace("[PEC_AUTORE]", State.Reclamo.PEC_AUTORE);
					testoEmail = testoEmail.Replace("[TELEFONO_AUTORE]", State.Reclamo.TELEFONO_AUTORE);
					testoEmail = testoEmail.Replace("[DESCRIZIONE_TIPO_AUTORE]", DESCRIZIONE_TIPO_AUTORE);
					testoEmail = testoEmail.Replace("[TESTO]", State.Reclamo.TESTO);

					await EmailService.SendAsync(
						Configuration["Email:DestinatarioURP"],
						"Segnalazione",
						testoEmail,
						fileAllegatiPerEmail
						);

					Logger.LogInformation($"E-mail inviata a {Configuration["Email:DestinatarioURP"]}");
				}
				else
				{
					Logger.LogWarning("Email destinatario URP non configurata, email a URP non inviata");
				}

				if (State.Step < 4)
				{
					State.Step++;

					await OnAvanti.InvokeAsync();
				}
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Errore invio email");
                // Se l'invio dell'email fallisce, aggiorna il testo della richiesta con un messaggio di errore
                string nota = $"*** NOTA: Email di conferma non inviata per motivi tecnici *** ";
                int maxLunghezzaTesto = 2900; // Lunghezza del testo della richiesta meno la lunghezza della nota + margine di sicurezza
                var nuovoTesto = $"{nota}{State.Reclamo.TESTO.Substring(0, Math.Min(State.Reclamo.TESTO.Length, maxLunghezzaTesto))}";
                RECLAMI reclamo = db.RECLAMI.FirstOrDefault(r => r.ID_RECLAMO == State.Reclamo.ID_RECLAMO);
                reclamo.TESTO = nuovoTesto;
                await db.SaveChangesAsync();
                NavigationManager.NavigateTo("errore");
            }
            finally
            {
                isLoading = false;
            }

        }

        private async Task Indietro()
		{
			if (State.Step > 1)
			{
				State.Step--;

				await OnIndietro.InvokeAsync();
			}
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
