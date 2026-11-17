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

namespace GestioneURP.Application.Services;

/// <summary>
/// Conserva il contenuto preparato per la conferma dell'operatore e i dati necessari al successivo invio.
/// La creazione dell'anteprima non registra il token né chiude la pratica; la stessa anteprima
/// consente un solo avvio, salvo riabilitazione dopo un errore nella validazione iniziale.
/// </summary>
public sealed class AnteprimaEmail
{
	internal AnteprimaEmail(long id, string tipo, string to, string prefisso, string suffisso,
		string html, string testoRegistro, string token, string tipoQuestionario, string nomeQuestionario,
		string urlQuestionario, IReadOnlyList<AllegatoEmailDto> allegati, ConfigurazioneEmail smtp)
	{
		IdSegnalazione = id;
		TipoSegnalazione = tipo;
		To = to;
		PrefissoOggetto = prefisso;
		SuffissoIniziale = suffisso;
		Html = html;
		TestoRegistro = testoRegistro;
		Token = token;
		TipoQuestionario = tipoQuestionario;
		NomeQuestionario = nomeQuestionario;
		UrlQuestionario = urlQuestionario;
		Allegati = allegati;
		Smtp = smtp;
	}

	public string To { get; }
	public string PrefissoOggetto { get; }
	public string SuffissoIniziale { get; }
	public string Html { get; }
	public IReadOnlyList<AllegatoEmailDto> Allegati { get; }
	internal long IdSegnalazione { get; }
	internal string TipoSegnalazione { get; }
	internal string TestoRegistro { get; }
	internal string Token { get; }
	internal string TipoQuestionario { get; }
	internal string NomeQuestionario { get; }
	internal string UrlQuestionario { get; }
	internal ConfigurazioneEmail Smtp { get; }
	private int _invioAvviato;

	// L'avvio viene acquisito atomicamente per impedire invii concorrenti della stessa anteprima.
	internal bool ProvaAvvio() => Interlocked.CompareExchange(ref _invioAvviato, 1, 0) == 0;
	internal void RiabilitaDopoValidazione() => Interlocked.Exchange(ref _invioAvviato, 0);
	// Il prefisso con il codice pratica resta fisso; l'operatore può modificare soltanto il suffisso.
	public string OggettoFinale(string suffisso) => Tronca(PrefissoOggetto + suffisso, 80);

	// Evita di troncare a metà una coppia di surrogate UTF-16 quando applica il limite di lunghezza.
	internal static string Tronca(string testo, int massimo)
	{
		if (testo.Length <= massimo) return testo;
		if (char.IsHighSurrogate(testo[massimo - 1])) massimo--;
		return testo[..massimo];
	}
}

/// <summary>
/// Identifica un allegato selezionabile nell'anteprima e conserva internamente il percorso usato per l'invio.
/// Non contiene i byte del file: il contenuto viene letto dal flusso di invio.
/// </summary>
public sealed class AllegatoEmailDto(long id, string nomeFile, string percorso)
{
	public long Id { get; } = id;
	public string NomeFile { get; } = nomeFile;
	internal string Percorso { get; } = percorso;
}

/// <summary>
/// Conserva le impostazioni acquisite alla preparazione dell'anteprima per utilizzarle al momento dell'invio.
/// </summary>
internal sealed class ConfigurazioneEmail
{
	internal required string From { get; init; }
	internal string? Bcc { get; init; }
	internal required string Server { get; init; }
	internal int Port { get; init; }
	internal required string Username { get; init; }
	internal required string Password { get; init; }
}

/// <summary>
/// Distingue il completamento, il blocco preventivo, l'errore di invio, l'errore dopo l'accettazione SMTP e l'interruzione.
/// </summary>
public enum StatoInvioEmail { Riuscito, Bloccato, ErroreInvio, ErrorePersistenza, Interrotto }

/// <summary>
/// Comunica alla UI l'esito del flusso e il messaggio da mostrare, senza esporre dettagli tecnici delle eccezioni.
/// </summary>
public sealed record EsitoInvioEmail(StatoInvioEmail Stato, string Messaggio);

/// <summary>
/// Segnala una condizione non valida con un messaggio destinato all'operatore.
/// </summary>
internal sealed class ValidazioneEmailException(string messaggio) : Exception(messaggio);
