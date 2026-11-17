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

using Microsoft.EntityFrameworkCore;
using QuestionariURP.Domain.Entities;
using QuestionariURP.Infrastructure.Persistence;

namespace QuestionariURP.Application.Services;

/// <summary>
/// Servizio (Scoped) di risoluzione dell'accesso al questionario.
/// Mantiene lo stato della risoluzione all'interno del circuito Blazor.
/// Ogni tab/circuito ha una propria istanza (isolamento garantito da Scoped).
/// </summary>
public class QuestionarioAccessResolver
{
	private readonly QuestionariDbContext _dbContext;
	private readonly ILogger<QuestionarioAccessResolver> _logger;

	public QuestionarioAccessResolver(QuestionariDbContext dbContext, ILogger<QuestionarioAccessResolver> logger)
	{
		_dbContext = dbContext;
		_logger = logger;
	}

	public TipoQuestionario? TipoQuestionarioRisolto { get; set; }
	public string? ErroreAccesso { get; set; }
	public Token? TokenVerificato { get; set; }
	public string? SorgenteRisolta { get; set; }

	/// <summary>
	/// Risolve l'accesso tramite URL libera: ricerca il TipoQuestionario per codice.
	/// </summary>
	/// <param name="sorgente">Sorgente di somministrazione.</param>
	/// <param name="codice">Codice del tipo di questionario.</param>
	/// <returns>true se risoluzione riuscita, false altrimenti.</returns>
	public async Task<bool> RisolviAccessoLiberoAsync(string sorgente, string codice)
	{
		var tipoQuestionario = await CaricaTipoQuestionarioCompletoAsync(codice);

		if (tipoQuestionario is null)
		{
			_logger.LogWarning("Tentativo di utilizzo di tipo questionario non valido. Codice={CodiceTipoQuestionario}, Sorgente={Sorgente}", codice, sorgente);
			ErroreAccesso = "Il codice del questionario non corrisponde a un tipo esistente.";
			return false;
		}

		if (tipoQuestionario.Domande.Count == 0)
		{
			_logger.LogWarning("Tipo questionario senza domande associate. Codice={CodiceTipoQuestionario}, Sorgente={Sorgente}", tipoQuestionario.Codice, sorgente);
			ErroreAccesso = "Il questionario selezionato non è disponibile.";
			return false;
		}

		TipoQuestionarioRisolto = tipoQuestionario;
		TokenVerificato = null;
		SorgenteRisolta = sorgente;
		ErroreAccesso = null;
		_logger.LogInformation("Questionario caricato. Codice={CodiceTipoQuestionario}, Sorgente={Sorgente}, ModalitaAccesso=Libero", tipoQuestionario.Codice, sorgente);
		return true;
	}

	/// <summary>
	/// Risolve l'accesso tramite URL personale: ricerca e verifica il token.
	/// </summary>
	/// <param name="tokenValue">Valore del token.</param>
	/// <returns>true se risoluzione riuscita, false altrimenti.</returns>
	public async Task<bool> RisolviAccessoPersonaleAsync(string tokenValue)
	{
		var token = await _dbContext.Token
			.Where(t => t.Valore == tokenValue)
			.FirstOrDefaultAsync();

		if (token is null)
		{
			_logger.LogWarning("Tentativo di utilizzo token non valido. Token={Token}", tokenValue);
			ErroreAccesso = "Link non valido";
			return false;
		}

		if (!token.EUtilizzabile)
		{
			_logger.LogWarning("Tentativo di utilizzo token non valido: già utilizzato. Token={Token}", tokenValue);
			ErroreAccesso = "Questionario già compilato";
			return false;
		}

		var tipoQuestionario = await CaricaTipoQuestionarioCompletoAsync(token.TipoQuestionarioCodice);

		if (tipoQuestionario is null)
		{
			_logger.LogWarning("Tentativo di utilizzo di tipo questionario non valido da token. Token={Token}, CodiceTipoQuestionario={CodiceTipoQuestionario}", tokenValue, token.TipoQuestionarioCodice);
			ErroreAccesso = "Il tipo di questionario associato al token non esiste.";
			return false;
		}

		if (tipoQuestionario.Domande.Count == 0)
		{
			_logger.LogWarning("Tipo questionario senza domande associate da accesso token. Codice={CodiceTipoQuestionario}, Token={Token}", tipoQuestionario.Codice, tokenValue);
			ErroreAccesso = "Il questionario selezionato non è disponibile.";
			return false;
		}

		TipoQuestionarioRisolto = tipoQuestionario;
		TokenVerificato = token;
		SorgenteRisolta = token.Sorgente;
		ErroreAccesso = null;
		_logger.LogInformation("Questionario caricato. Codice={CodiceTipoQuestionario}, Sorgente={Sorgente}, ModalitaAccesso=Personale, Token={Token}", tipoQuestionario.Codice, token.Sorgente, tokenValue);
		return true;
	}

	private async Task<TipoQuestionario?> CaricaTipoQuestionarioCompletoAsync(string codice)
	{
		var tipoQuestionario = await _dbContext.TipiQuestionario
			.Where(t => t.Codice == codice)
			.FirstOrDefaultAsync();

		if (tipoQuestionario is null)
		{
			return null;
		}

		var domande = await _dbContext.Domande
			.Where(d => d.TipoQuestionarioCodice == codice)
			.OrderBy(d => d.Ordine)
			.ToListAsync();

		foreach (var domanda in domande.OfType<DomandaChiusa>())
		{
			domanda.OpzioniRisposta = await _dbContext.OpzioniRisposta
				.Where(o => o.DomandaId == domanda.Id)
				.OrderBy(o => o.Posizione)
				.ToListAsync();
		}

		tipoQuestionario.Domande = domande;
		return tipoQuestionario;
	}
}

