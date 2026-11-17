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

using GestioneURP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Cryptography;

namespace GestioneURP.Application.Services;

/// <summary>
/// Fornisce le opzioni per la generazione degli URL e crea collegamenti liberi o personali.
/// Soltanto la modalità personale registra un token associato a tipo questionario, sorgente ed email.
/// </summary>
public class QuestionariService(UrpHdDbContext db, IConfiguration configuration, ILogger<QuestionariService> logger)
{
	private const int SuffixLength = 28;

	private readonly UrpHdDbContext db = db;
	private readonly IConfiguration configuration = configuration;
	private readonly ILogger<QuestionariService> logger = logger;

	/// <summary>Legge le sorgenti abilitate, ordinate per nome, senza attivare il tracking delle entità.</summary>
	public async Task<IReadOnlyList<QuestionarioSorgenteDto>> GetSorgentiAbilitateAsync()
	{
		return await db.Set<QuestionarioSorgente>()
			.AsNoTracking()
			.Where(x => x.Abilitata == "S")
			.OrderBy(x => x.Sorgente)
			.Select(x => new QuestionarioSorgenteDto(x.Sorgente))
			.ToListAsync();
	}

	/// <summary>Legge codici e nomi dei tipi questionario ordinati per nome per alimentare la selezione nella UI.</summary>
	public async Task<IReadOnlyList<QuestionarioTipoDto>> GetTipiQuestionarioAsync()
	{
		return await db.Set<QuestionarioTipo>()
			.AsNoTracking()
			.OrderBy(x => x.Nome)
			.Select(x => new QuestionarioTipoDto(x.Codice, x.Nome))
			.ToListAsync();
	}

	/// <summary>
	/// Costruisce un collegamento libero senza salvare dati; sorgente e codice sono codificati per la querystring.
	/// </summary>
	public string GeneraUrlLibera(string sorgente, string codiceQuestionario)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(sorgente);
		ArgumentException.ThrowIfNullOrWhiteSpace(codiceQuestionario);

		var baseUrl = GetQuestionariBaseUrl();
		var sorgenteEncoded = Uri.EscapeDataString(sorgente);
		var codiceEncoded = Uri.EscapeDataString(codiceQuestionario);

		logger.LogInformation(
			"Generato URL questionario libero per sorgente {Sorgente} e codice {CodiceQuestionario}",
			sorgente,
			codiceQuestionario);

		return $"{baseUrl}/questionario/libero?sorgente={sorgenteEncoded}&codice={codiceEncoded}";
	}

	/// <summary>
	/// Genera e salva un token personale non ancora utilizzato, quindi restituisce il collegamento che lo contiene.
	/// Il log riporta solo una parte del token, non il collegamento personale completo.
	/// </summary>
	public async Task<string> GeneraUrlPersonaleAsync(string tipoQuestionarioCodice, string sorgente, string email)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(tipoQuestionarioCodice);
		ArgumentException.ThrowIfNullOrWhiteSpace(sorgente);
		ArgumentException.ThrowIfNullOrWhiteSpace(email);

		var token = await GeneraTokenUnivocoAsync(db);
		db.Set<QuestionarioToken>().Add(new QuestionarioToken
		{
			Token = token,
			TipoQuestionarioCodice = tipoQuestionarioCodice,
			Sorgente = sorgente,
			Email = email,
			DataUtilizzo = null
		});

		await db.SaveChangesAsync();

		logger.LogInformation(
			"Generato URL questionario personale per tipo {TipoQuestionarioCodice}. Token {TokenParziale}",
			tipoQuestionarioCodice,
			$"{token[..12]}***");

		return $"{GetQuestionariBaseUrl()}/questionario/personale/{token}";
	}

	// L'URL base è obbligatorio; rimuove gli slash finali per comporre correttamente i percorsi.
	private string GetQuestionariBaseUrl()
	{
		var baseUrl = configuration["QuestionariBaseUrl"];
		if (string.IsNullOrWhiteSpace(baseUrl))
			throw new InvalidOperationException("Configurazione 'QuestionariBaseUrl' mancante.");

		return baseUrl.TrimEnd('/');
	}

	/// <summary>
	/// Combina un prefisso temporale di 12 caratteri con 28 lettere casuali e ripete la generazione
	/// se il valore è già presente nel database. Restituisce il token senza registrarlo.
	/// </summary>
	internal static async Task<string> GeneraTokenUnivocoAsync(UrpHdDbContext db, CancellationToken cancellationToken = default)
	{
		while (true)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var token = string.Create(
				12 + SuffixLength,
				(DateTime.Now.ToString("yyMMddHHmmss", CultureInfo.InvariantCulture), GeneraSuffissoCasuale(SuffixLength)),
				(span, state) =>
				{
					state.Item1.AsSpan().CopyTo(span);
					state.Item2.AsSpan().CopyTo(span[state.Item1.Length..]);
				});

			var tokenEsistente = await db.Set<QuestionarioToken>()
				.AsNoTracking()
				.Where(x => x.Token == token)
				.Select(x => x.Token)
				.FirstOrDefaultAsync(cancellationToken);

			if (tokenEsistente is null)
				return token;
		}
	}

	// Ogni lettera A-Z è estratta mediante il generatore casuale crittografico.
	private static string GeneraSuffissoCasuale(int length)
	{
		return string.Create(length, 0, static (span, _) =>
		{
			for (var i = 0; i < span.Length; i++)
				span[i] = (char)('A' + RandomNumberGenerator.GetInt32(26));
		});
	}
}
