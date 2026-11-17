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
    public partial class RichiestaRiepilogo
    {
        [Inject] private ILogger<RichiestaRiepilogo> Logger { get; set; } = default!;
        [Parameter]
        public RichiestaWizardState State { get; set; } = default!;
        [Parameter]
        public EventCallback OnAvanti { get; set; }

        [Parameter]
        public EventCallback OnIndietro { get; set; }

        private string DESCRIZIONE_TIPO_TEMATICA;
        private string DESCRIZIONE_TIPO_LUOGO;
        private string DESCRIZIONE_TIPO_AUTORE;
        private bool checkInformativa = false;

        // utilizzato per mostrare un messaggio di caricamento durante l'invio della richiesta
        private bool isLoading = false;
        protected override async Task OnInitializedAsync()
        {
            // usa dbfactory per creare il contesto del database
            await using var db = await DbFactory.CreateDbContextAsync();

            // Legge la descrizione tematica corrispondente al tipo
            if (State.Richiesta.ID_TIPO_TEMATICA != 0)
            {
                DESCRIZIONE_TIPO_TEMATICA = await db.TIPI_TEMATICA
                    .Where(i => i.ID_TIPO_TEMATICA == State.Richiesta.ID_TIPO_TEMATICA)
                    .Select(i => i.DESCRIZIONE_TIPO_TEMATICA)
                    .FirstOrDefaultAsync() ?? string.Empty;
            }
            if (State.Richiesta.ID_TIPO_LUOGO != 0)
            {
                DESCRIZIONE_TIPO_LUOGO = await db.TIPI_LUOGO
                    .Where(i => i.ID_TIPO_LUOGO == State.Richiesta.ID_TIPO_LUOGO)
                    .Select(i => i.DESCRIZIONE_TIPO_LUOGO)
                    .FirstOrDefaultAsync() ?? string.Empty;
            }
            if (State.Richiesta.ID_TIPO_AUTORE_RICHIESTA != 0)
            {
                DESCRIZIONE_TIPO_AUTORE = await db.TIPI_AUTORE_RICHIESTA
                    .Where(i => i.ID_TIPO_AUTORE_RICHIESTA == State.Richiesta.ID_TIPO_AUTORE_RICHIESTA)
                    .Select(i => i.DESCRIZIONE_TIPO_AUTORE_RICHIESTA)
                    .FirstOrDefaultAsync() ?? string.Empty;
            }
        }

        private List<string> elencoErrori = new();

        private async Task InviaRichiesta()
        {
            if (isLoading)
                return;

            isLoading = true;

            // Allegati per email a URP
            var fileAllegatiPerEmail = new List<EmailAttachment>();
            int idRichiestaInserita;
            await using var db = await DbFactory.CreateDbContextAsync();

            try
            {
                Logger.LogInformation("Inserimento richiesta");
                Logger.LogDebug($"Inserimento richiesta: tipo:{State.Richiesta.ID_TIPO_RICHIESTA}");


                State.Richiesta.TIPO_SORGENTE = "Web";

                // Legge da contatori
                // Recupera la connessione Oracle del DbContext
                var connection = db.Database.GetDbConnection();

                if (connection.State != System.Data.ConnectionState.Open)
                    await connection.OpenAsync();

                await using var cmd = connection.CreateCommand();

                cmd.CommandText = @"
					UPDATE CONTATORI
					SET valore = valore + 1
					WHERE contatore = 'Richieste'
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
                        "Il contatore 'Richieste' non ha restituito un valore.");
                }

                int progressivo = oracleValue.ToInt32();

                Logger.LogDebug($"Nuovo progressivo richiesta: {progressivo}");

                State.Richiesta.CODICE = $"RI{progressivo}";

                db.RICHIESTE.Add(State.Richiesta);

                await db.SaveChangesAsync();

                idRichiestaInserita = State.Richiesta.ID_RICHIESTA;

                Logger.LogInformation(
                    $"Inserita richiesta: ID={idRichiestaInserita}, " +
                    $"Codice={State.Richiesta.CODICE}");

                foreach (var allegatoCaricato in State.AllegatiCaricati)
                {
                    Logger.LogInformation("Inserimento allegati");

                    var allegato = new ALLEGATI_RICHIESTE
                    {
                        ID_RICHIESTA = idRichiestaInserita,
                        NOME_FILE = allegatoCaricato.NomeFile,
                        SORGENTE = "Richiesta"
                    };
                    db.ALLEGATI_RICHIESTE.Add(allegato);
                    await db.SaveChangesAsync();
                    int idAllegatoInserito = allegato.ID_ALLEGATO;

                    Logger.LogDebug($"Allegato inserito: {idAllegatoInserito}");

                    var percorsoSorgente = Path.Combine(
                        Configuration["CartellaTemporanea"],
                        allegatoCaricato.NomeFileTemp);

                    var percorsoDestinazione = Path.Combine(
                        Configuration["CartellaAllegatiRichieste"],
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
                Logger.LogError(ex, "Errore inserimento richiesta");
                NavigationManager.NavigateTo("errore");
            }
            finally
            {
                isLoading = false;
            }

            try
            {
                Logger.LogInformation("Invio email a cittadino");

                // Legge il template dell'email di conferma richiesta
                var percorsoTemplateConfermaRichiesta = Path.Combine(
                    HostEnvironment.ContentRootPath,
                    "Templates",
                    "EmailConfermaRichiesta.txt");

                // Legge il contenuto del template e sostituisce i segnaposto con i valori reali
                var testoEmail = await File.ReadAllTextAsync(percorsoTemplateConfermaRichiesta);
                testoEmail = testoEmail.Replace("[CODICE]", State.Richiesta.CODICE);
                testoEmail = testoEmail.Replace("[COGNOME_AUTORE]", State.Richiesta.COGNOME_AUTORE);
                testoEmail = testoEmail.Replace("[NOME_AUTORE]", State.Richiesta.NOME_AUTORE);
                testoEmail = testoEmail.Replace("[EMAIL_AUTORE]", State.Richiesta.EMAIL_AUTORE);
                testoEmail = testoEmail.Replace("[PEC_AUTORE]", State.Richiesta.PEC_AUTORE);
                testoEmail = testoEmail.Replace("[TELEFONO_AUTORE]", State.Richiesta.TELEFONO_AUTORE);
                testoEmail = testoEmail.Replace("[DESCRIZIONE_TIPO_AUTORE]", DESCRIZIONE_TIPO_AUTORE);
                testoEmail = testoEmail.Replace("[DESCRIZIONE_TIPO_TEMATICA]", DESCRIZIONE_TIPO_TEMATICA);
                if (State.Richiesta.ID_TIPO_LUOGO == 5)
                {
                    testoEmail = testoEmail.Replace("[DESCRIZIONE_TIPO_LUOGO]", DESCRIZIONE_TIPO_LUOGO + " - " + State.Richiesta.COMUNE_LUOGO);
                }
                else
                {
                    testoEmail = testoEmail.Replace("[DESCRIZIONE_TIPO_LUOGO]", DESCRIZIONE_TIPO_LUOGO);
                }
                testoEmail = testoEmail.Replace("[INDIRIZZO]", State.Richiesta.INDIRIZZO);
                testoEmail = testoEmail.Replace("[TESTO]", State.Richiesta.TESTO);
                if (State.AllegatiCaricati.Any())
                {
                    testoEmail = testoEmail.Replace("[ALLEGATI]", "Allegati: " + string.Join(", ", State.AllegatiCaricati.Select(a => a.NomeFile)));
                }
                else
                {
                    testoEmail = testoEmail.Replace("[ALLEGATI]", String.Empty);
                }

                Logger.LogDebug($"Invio e-mail a {State.Richiesta.EMAIL_AUTORE} per richiesta {State.Richiesta.CODICE}");

                await EmailService.SendAsync(
                    State.Richiesta.EMAIL_AUTORE,
                    "Segnalazione",
                    testoEmail);

                Logger.LogInformation($"E-mail inviata a {State.Richiesta.EMAIL_AUTORE}");

                if (!String.IsNullOrWhiteSpace(Configuration["Email:DestinatarioURP"]))
                {
                    Logger.LogInformation("Invio email a URP");
                    Logger.LogDebug($"Invio e-mail a {Configuration["Email:DestinatarioURP"]} per richiesta {State.Richiesta.CODICE}");

                    percorsoTemplateConfermaRichiesta = Path.Combine(
                        HostEnvironment.ContentRootPath,
                        "Templates",
                        "EmailConfermaRichiestaURP.txt");

                    testoEmail = await File.ReadAllTextAsync(percorsoTemplateConfermaRichiesta);
                    testoEmail = testoEmail.Replace("[CODICE]", State.Richiesta.CODICE);
                    testoEmail = testoEmail.Replace("[COGNOME_AUTORE]", State.Richiesta.COGNOME_AUTORE);
                    testoEmail = testoEmail.Replace("[NOME_AUTORE]", State.Richiesta.NOME_AUTORE);
                    testoEmail = testoEmail.Replace("[EMAIL_AUTORE]", State.Richiesta.EMAIL_AUTORE);
                    testoEmail = testoEmail.Replace("[PEC_AUTORE]", State.Richiesta.PEC_AUTORE);
                    testoEmail = testoEmail.Replace("[TELEFONO_AUTORE]", State.Richiesta.TELEFONO_AUTORE);
                    testoEmail = testoEmail.Replace("[DESCRIZIONE_TIPO_AUTORE]", DESCRIZIONE_TIPO_AUTORE);
                    testoEmail = testoEmail.Replace("[DESCRIZIONE_TIPO_TEMATICA]", DESCRIZIONE_TIPO_TEMATICA);
                    if (State.Richiesta.ID_TIPO_LUOGO == 5)
                    {
                        testoEmail = testoEmail.Replace("[DESCRIZIONE_TIPO_LUOGO]", DESCRIZIONE_TIPO_LUOGO + " - " + State.Richiesta.COMUNE_LUOGO);
                    }
                    else
                    {
                        testoEmail = testoEmail.Replace("[DESCRIZIONE_TIPO_LUOGO]", DESCRIZIONE_TIPO_LUOGO);
                    }
                    testoEmail = testoEmail.Replace("[INDIRIZZO]", State.Richiesta.INDIRIZZO);
                    testoEmail = testoEmail.Replace("[TESTO]", State.Richiesta.TESTO);

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
                var nuovoTesto = $"{nota}{State.Richiesta.TESTO.Substring(0, Math.Min(State.Richiesta.TESTO.Length, maxLunghezzaTesto))}";
                RICHIESTE richiesta = db.RICHIESTE.FirstOrDefault(r => r.ID_RICHIESTA == State.Richiesta.ID_RICHIESTA);
                richiesta.TESTO = nuovoTesto;
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
