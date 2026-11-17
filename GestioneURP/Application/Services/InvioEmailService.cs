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
using GestioneURP.Infrastructure.Email;
using GestioneURP.Infrastructure.Persistence;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging.Abstractions;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace GestioneURP.Application.Services;

/// <summary>
/// Prepara la risposta email al cittadino e coordina l'invio con la chiusura della pratica.
/// L'anteprima non modifica i dati: chiusura, token del questionario e registro attività
/// vengono salvati insieme solo dopo l'accettazione dell'email da parte del server SMTP.
/// L'invio SMTP non è annullabile dalla transazione del database.
/// </summary>
public sealed class InvioEmailService(DbContextOptions<UrpHdDbContext> options, IConfiguration configuration,
	IWebHostEnvironment environment, OperatoreState operatore, SmtpEmailSender smtp, ILogger<InvioEmailService> logger)
{
	// Il flusso usa contesti dedicati per rileggere la pratica nelle diverse fasi:
	// il template proviene dai file dell'applicazione, l'identità dallo stato operatore,
	// mentre l'invio effettivo è delegato al servizio SMTP.
	private const string StatoRisposta = "Risposta per il cittadino";

	// I contesti utilizzati dal flusso email non producono log EF:
	// la diagnostica viene registrata esplicitamente da LogEsito, senza messaggi
	// delle eccezioni che potrebbero esporre token o contenuti riservati.
	private UrpHdDbContext CreaContesto()
	{
		// Le eccezioni del provider possono contenere token e dettagli: qui si usa solo diagnostica filtrata.
		var emailOptions = new DbContextOptionsBuilder<UrpHdDbContext>(options)
			.UseLoggerFactory(NullLoggerFactory.Instance).EnableSensitiveDataLogging(false).Options;
		return new UrpHdDbContext(emailOptions);
	}

	/// <summary>
	/// Prepara destinatario, oggetto, contenuto, collegamento al questionario e allegati
	/// disponibili per l'anteprima, senza inviare l'email né chiudere la pratica.
	/// </summary>
	public async Task<AnteprimaEmail> PreparaAsync(long id, string tipo, CancellationToken cancellationToken)
	{
		string? to = null, bcc = null, oggetto = null;
		try
		{
			// L'anteprima è consentita solo a un operatore identificato e per una pratica
			// nello stato previsto, con dati di gestione validi.
			ValidaOperatore();
			await using var db = CreaContesto();
			var pratica = await LeggiPraticaAsync(db, id, tipo, false, cancellationToken);
			ValidaPratica(pratica, false);
			var valori = pratica.CurrentValues;
			to = valori.GetValue<string?>("EmailAutore") ?? "";
			// Il codice pratica forma il prefisso dell'oggetto; il suffisso iniziale
			// deriva dalla tematica della richiesta oppure identifica il reclamo.
			var prefisso = $"[{valori.GetValue<string?>("Codice")}] - ";
			var suffisso = pratica.Entity is Richiesta richiesta
				? await db.TipiTematica.AsNoTracking().Where(x => x.IdTipoTematica == richiesta.IdTipoTematica)
					.Select(x => x.DescrizioneTipoTematica).FirstOrDefaultAsync(cancellationToken) ?? ""
				: "Reclamo";
			suffisso = AnteprimaEmail.Tronca(suffisso, 70);
			oggetto = AnteprimaEmail.Tronca(prefisso + suffisso, 80);
			// La configurazione viene acquisita nell'anteprima, che verrà poi
			// utilizzata per l'invio. Il destinatario in copia nascosta è facoltativo.
			bcc = configuration["Email:Bcc"];
			var impostazioni = new ConfigurazioneEmail
			{
				From = Obbligatoria("Email:From"),
				Bcc = string.IsNullOrWhiteSpace(bcc) ? null : bcc,
				Server = Obbligatoria("Email:SmtpServer"),
				Username = Obbligatoria("Email:Username"),
				Password = Obbligatoria("Email:Password"),
				Port = configuration.GetValue<int>("Email:Port")
			};
			if (impostazioni.Port is < 1 or > 65535)
				throw new ValidazioneEmailException("Porta SMTP non valida. Contattare il supporto tecnico.");
			// Il collegamento personale richiede un URL base valido e un tipo
			// di questionario esistente, dotato del nome da mostrare al cittadino.
			var tipoQuestionario = Obbligatoria("Email:TipoQuestionarioCodice");
			var baseUrl = Obbligatoria("QuestionariBaseUrl").TrimEnd('/');
			if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
				(uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
				!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
				throw new ValidazioneEmailException("QuestionariBaseUrl non valido. Contattare il supporto tecnico.");
			var nomeQuestionario = await db.QuestionariTipo.AsNoTracking().Where(x => x.Codice == tipoQuestionario)
				.Select(x => x.Nome).FirstOrDefaultAsync(cancellationToken);
			if (string.IsNullOrWhiteSpace(nomeQuestionario))
				throw new ValidazioneEmailException("Tipo questionario inesistente o nome mancante. Contattare il supporto tecnico.");

			// Il token viene generato per comporre il collegamento nell'anteprima;
			// la sua registrazione nel database avverrà dopo l'accettazione SMTP.
			var template = await File.ReadAllTextAsync(Path.Combine(environment.ContentRootPath, "template_email.html"), cancellationToken);
			if (!template.Contains("{link_questionario}", StringComparison.Ordinal))
				throw new ValidazioneEmailException("Il template non contiene il collegamento al questionario. Contattare il supporto tecnico.");
			var token = await QuestionariService.GeneraTokenUnivocoAsync(db, cancellationToken);
			var url = $"{baseUrl}/questionario/personale/{token}";
			// I testi della pratica vengono codificati per mostrarli come testo,
			// evitando che eventuale HTML inserito nei campi venga interpretato.
			var placeholder = new Dictionary<string, string>
			{
				["{codice_pratica}"] = HtmlTesto(valori.GetValue<string?>("Codice")),
				["{data_inserimento}"] = HtmlTesto(valori.GetValue<DateTime?>("DataInserimento")?.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)),
				["{testo_richiesta}"] = HtmlTesto(valori.GetValue<string?>("Testo")),
				["{testo_risposta}"] = HtmlTesto(valori.GetValue<string?>("TestoRisposta")),
				["{link_questionario}"] = $"<a href=\"{WebUtility.HtmlEncode(url)}\">{WebUtility.HtmlEncode(nomeQuestionario)}</a>"
			};
			var html = ValorizzaTemplate(template, placeholder);			
			// Per il registro si prepara una versione testuale senza il collegamento:
			// nome e URL del questionario saranno aggiunti separatamente, così da
			// preservarli anche quando il corpo deve essere abbreviato.
			placeholder["{link_questionario}"] = "";
			var testoRegistro = EmailHtmlText.Converti(ValorizzaTemplate(template, placeholder));
			var tipoNormalizzato = pratica.Entity is Richiesta ? "RICHIESTA" : "RECLAMO";
			var allegati = await LeggiAllegatiAsync(db, id, tipoNormalizzato, cancellationToken);
			return new AnteprimaEmail(id, tipoNormalizzato, to, prefisso, suffisso, html, testoRegistro,
				token, tipoQuestionario, nomeQuestionario, url, allegati, impostazioni);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
		catch (Exception ex)
		{
			// Gli errori di validazione restano comprensibili all'operatore;
			// gli altri errori sono sostituiti da un messaggio privo di dettagli tecnici.
			LogEsito(to, bcc, oggetto, "Errore preparazione", ex);
			throw new InvalidOperationException(ex is ValidazioneEmailException ? ex.Message :
				"Impossibile preparare l'email. La pratica non è stata chiusa. Contattare il supporto tecnico.");
		}
	}

	/// <summary>
	/// Invia l'email confermata e, dopo l'accettazione SMTP, salva in un'unica
	/// transazione la chiusura della pratica, il token e l'evento nel registro.
	/// Distingue gli errori precedenti all'invio da quelli di persistenza successivi,
	/// perché un'email già accettata dal server non può essere revocata.
	/// </summary>
	public async Task<EsitoInvioEmail> InviaEChiudiAsync(AnteprimaEmail anteprima, string suffisso,
		IReadOnlyCollection<long> allegatiSelezionati, CancellationToken cancellationToken)
	{
		// La stessa anteprima non può avviare più invii, ad esempio in seguito
		// a conferme ripetute dell'operatore.
		if (!anteprima.ProvaAvvio())
			return new(StatoInvioEmail.Bloccato, "Invio già avviato. Non ripetere la conferma.");
		var oggetto = anteprima.OggettoFinale(suffisso);
		var accettata = false;
		var fase = "Validazione";
		try
		{
			// Si rileggono i dati correnti: la pratica potrebbe essere cambiata
			// dopo la preparazione dell'anteprima.
			if (string.IsNullOrWhiteSpace(suffisso) || suffisso.Length > 70)
				throw new ValidazioneEmailException("Inserire un suffisso oggetto non vuoto, di massimo 70 caratteri.");
			var autore = ValidaOperatore();
			await using (var db = CreaContesto())
			{
				var pratica = await LeggiPraticaAsync(db, anteprima.IdSegnalazione, anteprima.TipoSegnalazione, false, cancellationToken);
				ValidaPratica(pratica, true);
			}
			// Sono accettati soltanto identificativi presenti negli allegati
			// dell'anteprima; eventuali identificativi sconosciuti bloccano l'invio.
			var allegati = anteprima.Allegati.Where(x => allegatiSelezionati.Contains(x.Id)).ToList();
			if (allegati.Count != allegatiSelezionati.Distinct().Count())
				throw new ValidazioneEmailException("Selezione degli allegati non valida.");
			// Il testo del registro viene preparato prima dell'invio, per rilevare
			// in anticipo eventuali problemi di composizione o di dimensione.
			fase = "Preparazione registro";
			string dettagli;
			await using (var db = CreaContesto())
				dettagli = await CreaDettagliAsync(db, anteprima, allegati, cancellationToken);
			// Da questo punto l'email, una volta accettata dal server, non può
			// essere annullata da un successivo errore sul database.
			// L'accettazione SMTP non garantisce la consegna finale al destinatario.
			fase = "SMTP";
			await smtp.InviaAsync(anteprima, oggetto, allegati, cancellationToken);
			accettata = true;
			LogEsito(anteprima.To, anteprima.Smtp.Bcc, oggetto, "Accettata SMTP", token: anteprima.Token);

			fase = "Persistenza";
			await using (var db = CreaContesto())
			{
				// Chiusura, token e registro costituiscono una sola operazione
				// sul database: la transazione evita salvataggi parziali.
				await using var transazione = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
				var pratica = await LeggiPraticaAsync(db, anteprima.IdSegnalazione, anteprima.TipoSegnalazione, true, cancellationToken);
				var ora = DateTime.Now;
				// Si verifica nuovamente che la chiusura sia ancora consentita,
				// usando la data che verrà effettivamente salvata.
				ValidaPratica(pratica, true, ora.Date);
				pratica.Property("StatoPratica").CurrentValue = "Chiusa";
				pratica.Property("DataChiusura").CurrentValue = ora.Date;
				pratica.Property("AnnotazioniChiusura").CurrentValue = "Inviata email";
				pratica.Property("UltimoAggiornamento").CurrentValue = ora;
				pratica.Property("OperatoreUltimoAggiornamento").CurrentValue = autore;
				db.QuestionariToken.Add(new QuestionarioToken
				{
					Token = anteprima.Token,
					TipoQuestionarioCodice = anteprima.TipoQuestionario,
					Email = anteprima.To,
					Sorgente = "EmailURP",
					DataUtilizzo = null
				});
				db.RegistroAttivita.Add(new RegistroAttivita
				{
					TipoSegnalazione = anteprima.TipoSegnalazione,
					IdSegnalazione = anteprima.IdSegnalazione,
					DataOraEvento = ora,
					Operatore = autore,
					StatoPratica = "Chiusa",
					Dettagli = dettagli
				});
				await db.SaveChangesAsync(cancellationToken);
				await transazione.CommitAsync(cancellationToken);
			}
			LogEsito(anteprima.To, anteprima.Smtp.Bcc, oggetto, "Email inviata e pratica chiusa", token: anteprima.Token);
			return new(StatoInvioEmail.Riuscito, "Email inviata e pratica chiusa correttamente.");
		}
		catch (ValidazioneEmailException ex) when (!accettata && fase == "Validazione")
		{
			// Solo gli errori nella validazione iniziale riabilitano l'anteprima:
			// l'operatore può correggere i dati senza che sia già partita un'email.
			anteprima.RiabilitaDopoValidazione();
			LogEsito(anteprima.To, anteprima.Smtp.Bcc, oggetto, "Invio bloccato", ex, anteprima.Token);
			return new(StatoInvioEmail.Bloccato, ex.Message);
		}
		catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
		{
			// Un'interruzione non implica che l'email non sia partita:
			// prima di qualsiasi nuovo invio occorre verificare la situazione.
			LogEsito(anteprima.To, anteprima.Smtp.Bcc, oggetto, $"Interrotto durante {fase}; SMTP accettata: {accettata}", ex, anteprima.Token);
			return new(StatoInvioEmail.Interrotto,
				"Operazione interrotta. Un'email già accettata dal server non può essere revocata. Verificare la pratica; nessun reinvio automatico.");
		}
		catch (Exception ex)
		{
			// Dopo l'accettazione SMTP si segnala un errore di completamento,
			// non un errore di invio, evitando di suggerire un reinvio automatico.
			LogEsito(anteprima.To, anteprima.Smtp.Bcc, oggetto, $"Errore {fase}", ex, anteprima.Token);
			return accettata
				? new(StatoInvioEmail.ErrorePersistenza, "Email inviata, completamento della pratica non riuscito: contattare il supporto.")
				: new(StatoInvioEmail.ErroreInvio, "Errore di invio email. La pratica non è stata chiusa. Contattare il supporto tecnico.");
		}
	}

	// Una configurazione obbligatoria assente o vuota impedisce di proseguire.
	private string Obbligatoria(string chiave) => !string.IsNullOrWhiteSpace(configuration[chiave])
		? configuration[chiave]! : throw new ValidazioneEmailException($"Configurazione {chiave} mancante. Contattare il supporto tecnico.");

	// Lo UserName identifica l'operatore cui attribuire chiusura e attività.
	private string ValidaOperatore() => !string.IsNullOrWhiteSpace(operatore.UserName)
		? operatore.UserName : throw new ValidazioneEmailException("Identità dell'operatore non disponibile. Accedere nuovamente.");

	// Verifica lo stato e le regole di gestione della pratica.
	// Per l'invio controlla anche che non vi siano annotazioni di chiusura
	// e simula la chiusura per validarne i dati senza modificare l'entità.
	private static void ValidaPratica(EntityEntry pratica, bool chiusura, DateTime? oggi = null)
	{
		var dati = SegnalazioniService.LeggiDatiGestione(pratica.CurrentValues);
		if (dati.StatoPratica != StatoRisposta)
			throw new ValidazioneEmailException("La pratica non è più nello stato Risposta per il cittadino. Verificare la gestione pratica.");
		var validatore = new GestionePraticaService();
		var inserimento = pratica.CurrentValues.GetValue<DateTime?>("DataInserimento");
		var esito = validatore.Validate(dati, inserimento);
		if (!esito.IsValid) throw new ValidazioneEmailException(string.Join(" ", esito.ErrorsByField.Values));
		if (!chiusura) return;
		if (!string.IsNullOrWhiteSpace(dati.AnnotazioniChiusura))
			throw new ValidazioneEmailException("Le annotazioni di chiusura devono essere vuote prima dell'invio. Verificare la pratica.");
		esito = validatore.Validate(dati with { StatoPratica = "Chiusa", DataChiusura = oggi ?? DateTime.Today }, inserimento);
		if (!esito.IsValid) throw new ValidazioneEmailException(string.Join(" ", esito.ErrorsByField.Values));
	}

	// Richieste e reclami vengono restituiti attraverso un accesso comune ai campi.
	// Il tracking è abilitato soltanto quando occorre salvare la chiusura.
	private static async Task<EntityEntry> LeggiPraticaAsync(UrpHdDbContext db, long id, string tipo, bool tracking, CancellationToken ct)
	{
		object? entity;
		if (tipo.Equals("RICHIESTA", StringComparison.OrdinalIgnoreCase))
			entity = await (tracking ? db.Richieste.AsTracking() : db.Richieste.AsNoTracking()).FirstOrDefaultAsync(x => x.IdRichiesta == id, ct);
		else if (tipo.Equals("RECLAMO", StringComparison.OrdinalIgnoreCase))
			entity = await (tracking ? db.Reclami.AsTracking() : db.Reclami.AsNoTracking()).FirstOrDefaultAsync(x => x.IdReclamo == id, ct);
		else throw new ValidazioneEmailException("Tipo segnalazione non valido.");
		return entity is null ? throw new ValidazioneEmailException("Pratica non disponibile.") : db.Entry(entity);
	}

	// Legge i metadati degli allegati associati alla pratica e ricostruisce
	// i percorsi fisici usando la cartella configurata e l'identificativo.
	// In questa fase non legge i file né ne verifica l'esistenza.
	private async Task<IReadOnlyList<AllegatoEmailDto>> LeggiAllegatiAsync(UrpHdDbContext db, long id, string tipo, CancellationToken ct)
	{
		var richiesta = tipo == "RICHIESTA";
		var righe = richiesta
			? await db.AllegatiRichieste.AsNoTracking()
			.Where(x => x.IdRichiesta == id && x.Sorgente == "Risposta")
			.Select(x => new { Id = x.IdAllegato, x.NomeFile }).ToListAsync(ct)
			: await db.AllegatiReclami.AsNoTracking()
			.Where(x => x.IdReclamo == id && x.Sorgente == "Risposta")
			.Select(x => new { Id = x.IdAllegato, x.NomeFile }).ToListAsync(ct);
		var cartella = configuration[richiesta ? "CartellaAllegatiRichieste" : "CartellaAllegatiReclami"];
		return righe.Select(x => new AllegatoEmailDto(x.Id, x.NomeFile ?? "",
			string.IsNullOrWhiteSpace(cartella) ? "" : Path.Combine(cartella, x.Id.ToString(CultureInfo.InvariantCulture)))).ToList().AsReadOnly();
	}

	// Preserva il contenuto come testo e rende visibili gli a capo nell'email HTML.
	private static string HtmlTesto(string? testo) => WebUtility.HtmlEncode(testo ?? "")
		.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Replace("\n", "<br />", StringComparison.Ordinal);

	// Sostituisce soltanto i placeholder supportati, in un unico passaggio:
	// eventuali placeholder presenti nei testi inseriti non vengono rielaborati.
	private static string ValorizzaTemplate(string template, Dictionary<string, string> valori)
		=> Regex.Replace(template, @"\{(?:codice_pratica|data_inserimento|testo_richiesta|testo_risposta|link_questionario)\}",
			match => valori[match.Value], RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

	// Compone il registro entro il limite di 4000 byte, preservando destinatario,
	// nome e URL del questionario e nomi degli allegati. Solo il corpo dell'email
	// può essere abbreviato; se le parti da preservare superano già il limite,
	// la preparazione fallisce prima dell'invio SMTP.
	private static async Task<string> CreaDettagliAsync(UrpHdDbContext db, AnteprimaEmail anteprima,
		IReadOnlyList<AllegatoEmailDto> allegati, CancellationToken ct)
	{
		var prefisso = $"Inviata email: {anteprima.To}\n\n";
		var protetto = $"\n\n{anteprima.NomeQuestionario}\n{anteprima.UrlQuestionario}";
		if (allegati.Count > 0) protetto += "\nAllegati:\n" + string.Join("\n", allegati.Select(x => x.NomeFile));

		await db.Database.OpenConnectionAsync(ct);
		using var comando = (OracleCommand)db.Database.GetDbConnection().CreateCommand();
		comando.BindByName = true;
		// La dimensione è calcolata da Oracle nel charset effettivo del database:
		// un numero di caratteri non corrisponde necessariamente allo stesso numero
		// di byte. Il blocco restituisce il testo composto senza salvare dati.
		// LENGTHB usa il charset reale del DB; SUBSTRC non spezza caratteri Unicode completi.
		comando.CommandText = """
			DECLARE
			  testo VARCHAR2(32767);
			  prefisso VARCHAR2(32767) := :prefisso;
			  protetto VARCHAR2(32767) := :protetto;
			BEGIN
			  IF LENGTHB(prefisso) + LENGTHB(protetto) > 4000 THEN
				RAISE_APPLICATION_ERROR(-20001, 'Contenuto da preservare oltre il limite del registro');
			  END IF;

			  testo := DBMS_LOB.SUBSTR(:testo, 4000, 1);
			  WHILE NVL(LENGTHB(testo), 0) + LENGTHB(prefisso) + LENGTHB(protetto) > 4000 LOOP
				testo := SUBSTRC(testo, 1, LENGTHC(testo) - 1);
			  END LOOP;

			  :dettagli := prefisso || testo || protetto;
			END;
			""";

		comando.Parameters.Add("testo", OracleDbType.Clob).Value = anteprima.TestoRegistro;
		comando.Parameters.Add("prefisso", OracleDbType.Varchar2).Value = prefisso;
		comando.Parameters.Add("protetto", OracleDbType.Varchar2).Value = protetto;
		comando.Parameters.Add("dettagli", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;

		await comando.ExecuteNonQueryAsync(ct);
		return ((OracleString)comando.Parameters["dettagli"].Value).Value;
	}

	// Registra esito e diagnostica essenziale senza riportare messaggi o stack
	// delle eccezioni. Della causa più interna conserva tipo e codice di errore;
	// quando il token è disponibile, lo oscura nei campi email registrati.
	private void LogEsito(string? to, string? bcc, string? oggetto, string esito, Exception? errore = null, string? token = null)
	{
		var causa = errore;
		while (causa?.InnerException is not null) causa = causa.InnerException;
		var codice = causa switch
		{
			SmtpCommandException smtpError => ((int)smtpError.StatusCode).ToString(CultureInfo.InvariantCulture),
			OracleException oracleError => oracleError.Number.ToString(CultureInfo.InvariantCulture),
			_ => causa?.HResult.ToString(CultureInfo.InvariantCulture)
		};
		string? Filtra(string? valore) => token is null ? valore : valore?.Replace(token, "[token omesso]", StringComparison.Ordinal);
		logger.Log(errore is null ? LogLevel.Information : LogLevel.Warning,
			"To={To}; Bcc={Bcc}; Subject={Subject}; Esito={Esito}; CodiceErrore={CodiceErrore}; Diagnostica={Diagnostica}",
			Filtra(to), Filtra(bcc), Filtra(oggetto), esito, codice, causa?.GetType().FullName);
	}
}
