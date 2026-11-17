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
using QuestionariURP.Application.State;
using QuestionariURP.Domain.Entities;
using QuestionariURP.Infrastructure.Persistence;

namespace QuestionariURP.Application.Services;

public class InvioRisposteService
{
	private readonly QuestionariDbContext _dbContext;
	private readonly ILogger<InvioRisposteService> _logger;

	public InvioRisposteService(QuestionariDbContext dbContext, ILogger<InvioRisposteService> logger)
	{
		_dbContext = dbContext;
		_logger = logger;
	}

	public async Task<EsitoInvioRisposte> InviaAsync(QuestionarioCompilazioneState statoCompilazione)
	{
		if (!statoCompilazione.Inizializzato || statoCompilazione.TipoQuestionario is null || string.IsNullOrWhiteSpace(statoCompilazione.Sorgente))
		{
			return EsitoInvioRisposte.Errore("Compilazione non valida.");
		}

		var validazioneOk = statoCompilazione.ValidaDomandeEApriRiepilogo();
		if (!validazioneOk)
		{
			return EsitoInvioRisposte.Errore("La compilazione contiene errori.");
		}

		await using var transazione = await _dbContext.Database.BeginTransactionAsync();

		try
		{
			Token? tokenDb = null;

			if (statoCompilazione.Token is not null)
			{
				tokenDb = await _dbContext.Token
					.FirstOrDefaultAsync(t => t.Valore == statoCompilazione.Token.Valore);

				if (tokenDb is null || !tokenDb.EUtilizzabile)
				{
					_logger.LogWarning("Tentativo di utilizzo token non valido in invio. Token={Token}", statoCompilazione.Token.Valore);
					await transazione.RollbackAsync();
					return EsitoInvioRisposte.TokenGiaUtilizzato();
				}
			}

			var compilazione = new Compilazione
			{
				TipoQuestionarioCodice = statoCompilazione.TipoQuestionario.Codice,
				Sorgente = statoCompilazione.Sorgente,
				DataOraCompletamento = DateTime.Now,
				Risposte = CostruisciRisposte(statoCompilazione)
			};

			_dbContext.Compilazioni.Add(compilazione);

			if (tokenDb is not null)
			{
				tokenDb.DataUtilizzo = DateTime.Now;
				_logger.LogInformation("Token utilizzato. Token={Token}, CodiceTipoQuestionario={CodiceTipoQuestionario}", tokenDb.Valore, statoCompilazione.TipoQuestionario.Codice);
			}

			await _dbContext.SaveChangesAsync();
			await transazione.CommitAsync();

			_logger.LogInformation("Questionario salvato. CompilazioneId={CompilazioneId}, CodiceTipoQuestionario={CodiceTipoQuestionario}, Sorgente={Sorgente}, NumeroRisposte={NumeroRisposte}", compilazione.Id, compilazione.TipoQuestionarioCodice, compilazione.Sorgente, compilazione.Risposte.Count);
			return EsitoInvioRisposte.Ok();
		}
		catch
		{
			await transazione.RollbackAsync();
			return EsitoInvioRisposte.Errore("Si è verificato un errore durante l'invio delle risposte.");
		}
	}

	private static List<RispostaCompilata> CostruisciRisposte(QuestionarioCompilazioneState statoCompilazione)
	{
		var risposte = new List<RispostaCompilata>();

		foreach (var domanda in statoCompilazione.Domande.OrderBy(d => d.Domanda.Ordine))
		{
			if (domanda.Domanda is DomandaChiusa domandaChiusa)
			{
				var opzione = domandaChiusa.OpzioniRisposta.FirstOrDefault(o => o.Id == domanda.OpzioneRispostaId);
				if (opzione is null)
				{
					continue;
				}

				risposte.Add(new RispostaCompilata
				{
					DomandaId = domanda.Domanda.Id,
					OpzioneRispostaId = opzione.Id,
					CodiceNumericoSnapshot = opzione.CodiceNumerico,
					TestoRisposta = null
				});

				continue;
			}

			risposte.Add(new RispostaCompilata
			{
				DomandaId = domanda.Domanda.Id,
				OpzioneRispostaId = null,
				CodiceNumericoSnapshot = null,
				TestoRisposta = string.IsNullOrWhiteSpace(domanda.TestoRisposta) ? null : domanda.TestoRisposta
			});
		}

		return risposte;
	}
}

public record EsitoInvioRisposte(bool Successo, string? MessaggioErrore)
{
	public static EsitoInvioRisposte Ok() => new(true, null);

	public static EsitoInvioRisposte Errore(string messaggioErrore) => new(false, messaggioErrore);

	public static EsitoInvioRisposte TokenGiaUtilizzato() => new(false, "questionario già compilato");
}
