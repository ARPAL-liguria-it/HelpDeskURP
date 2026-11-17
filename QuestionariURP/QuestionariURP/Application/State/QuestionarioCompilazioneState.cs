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

using QuestionariURP.Domain.Entities;

namespace QuestionariURP.Application.State;

public class QuestionarioCompilazioneState
{
	public const int LunghezzaMassimaRispostaAperta = 4000;

	public TipoQuestionario? TipoQuestionario { get; private set; }
	public string? Sorgente { get; private set; }
	public Token? Token { get; private set; }

	public bool Inizializzato => TipoQuestionario is not null;
	public bool InRiepilogo { get; private set; }

	public List<DomandaCompilazioneState> Domande { get; private set; } = new();

	public void Inizializza(TipoQuestionario tipoQuestionario, string sorgente, Token? token)
	{
		TipoQuestionario = tipoQuestionario;
		Sorgente = sorgente;
		Token = token;
		InRiepilogo = false;

		Domande = tipoQuestionario.Domande
			.OrderBy(d => d.Ordine)
			.Select(d => new DomandaCompilazioneState(d))
			.ToList();
	}

	public void SelezionaOpzione(long domandaId, long opzioneRispostaId)
	{
		var domanda = Domande.FirstOrDefault(d => d.Domanda.Id == domandaId);
		if (domanda is null)
		{
			return;
		}

		domanda.OpzioneRispostaId = opzioneRispostaId;
		domanda.Errore = null;
	}

	public void AggiornaTestoRisposta(long domandaId, string? testoRisposta)
	{
		var domanda = Domande.FirstOrDefault(d => d.Domanda.Id == domandaId);
		if (domanda is null)
		{
			return;
		}

		if (!string.IsNullOrEmpty(testoRisposta) && testoRisposta.Length > LunghezzaMassimaRispostaAperta)
		{
			testoRisposta = testoRisposta[..LunghezzaMassimaRispostaAperta];
		}

		domanda.TestoRisposta = testoRisposta;
		domanda.Errore = null;
	}

	public bool ValidaDomandeEApriRiepilogo()
	{
		var validazioneOk = true;

		foreach (var domanda in Domande)
		{
			domanda.Errore = null;

			if (domanda.Domanda is DomandaChiusa)
			{
				if (domanda.OpzioneRispostaId is null)
				{
					domanda.Errore = "È necessario selezionare un'opzione.";
					validazioneOk = false;
				}
			}
			else if (domanda.Domanda is DomandaAperta domandaAperta)
			{
				if (!string.IsNullOrEmpty(domanda.TestoRisposta) && domanda.TestoRisposta.Length > LunghezzaMassimaRispostaAperta)
				{
					domanda.Errore = $"Il testo non può superare {LunghezzaMassimaRispostaAperta} caratteri.";
					validazioneOk = false;
					continue;
				}

				if (domandaAperta.Obbligatoria && string.IsNullOrWhiteSpace(domanda.TestoRisposta))
				{
					domanda.Errore = "La compilazione del campo è richiesta.";
					validazioneOk = false;
				}
			}
		}

		InRiepilogo = validazioneOk;
		return validazioneOk;
	}

	public void TornaInModifica()
	{
		InRiepilogo = false;
	}

	public void AbbandonaCompilazione()
	{
		TipoQuestionario = null;
		Sorgente = null;
		Token = null;
		InRiepilogo = false;
		Domande = new List<DomandaCompilazioneState>();
	}
}

public class DomandaCompilazioneState
{
	public DomandaCompilazioneState(Domanda domanda)
	{
		Domanda = domanda;
	}

	public Domanda Domanda { get; }
	public long? OpzioneRispostaId { get; set; }
	public string? TestoRisposta { get; set; }
	public string? Errore { get; set; }

	public DomandaChiusa? DomandaChiusa => Domanda as DomandaChiusa;
	public DomandaAperta? DomandaAperta => Domanda as DomandaAperta;
}
