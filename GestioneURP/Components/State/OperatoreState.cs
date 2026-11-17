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

namespace GestioneURP.Components.State
{
	public class OperatoreState
	{
		// Nota: la gestione dei diritti di accesso è implementata al minimo dato che 
		// non esistono distinzioni tranne che per la generazione dell'URL dei report.questionari
		// e la cancellazione dei questionari.
		public string? UserName { get; private set; }
		public bool SeCaricato { get; private set; }
		public decimal? UserId { get; private set; }
		public bool SeOverride { get; private set; }
		public List<ProfiloOperatore> Profili { get; set; } = [];

		public void Set(string userName, decimal? userId, List<ProfiloOperatore>? profili, bool seOverride = false)
		{
			UserName = userName;
			UserId = userId;
			SeOverride = seOverride;
			SeCaricato = true;
			if (profili != null)
			{
				Profili = profili;
			}
		}

		// Elenco nomi dei profili dell'operatore
		public List<string> ElencoNomiProfili => [.. Profili.Select(p => p.Nome ?? string.Empty)];

		public bool SeGestore()
		{
			return Profili.Any(profilo =>
				profilo.FunzioniAbilitate.Any(funzione =>
					string.Equals(funzione, "Gestore", StringComparison.OrdinalIgnoreCase)));
		}

		public bool SeGenerazioneUrlQuestionari()
		{
			return Profili.Any(profilo =>
				profilo.FunzioniAbilitate.Any(funzione =>
					string.Equals(funzione, "GenerazioneUrlQuestionari", StringComparison.OrdinalIgnoreCase)));
		}

		public bool SeCancellazioneSegnalazioni()
		{
			return Profili.Any(profilo =>
				profilo.FunzioniAbilitate.Any(funzione =>
					string.Equals(funzione, "CancellazioneSegnalazioni", StringComparison.OrdinalIgnoreCase)));
		}
	}

	public class ProfiloOperatore
	{
		public string? Nome { get; set; }
		public List<string> FunzioniAbilitate { get; set; } = [];
	}
}
