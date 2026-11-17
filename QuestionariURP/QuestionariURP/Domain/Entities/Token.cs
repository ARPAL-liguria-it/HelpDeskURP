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

namespace QuestionariURP.Domain.Entities;

/// <summary>
/// Token di accesso personale a un questionario.
/// Questa applicazione legge il token e aggiorna esclusivamente DataUtilizzo.
/// Tabella fisica: URP_HD.QUESTIONARI_TOKEN
/// </summary>
public class Token
{
	public string Valore { get; set; } = string.Empty;
	public string TipoQuestionarioCodice { get; set; } = string.Empty;
	public string Sorgente { get; set; } = string.Empty;

	/// <summary>
	/// Null = token non ancora utilizzato; valorizzata = token già utilizzato.
	/// </summary>
	public DateTime? DataUtilizzo { get; set; }

	// ── Regola di dominio ──────────────────────────────────────────────────

	/// <summary>
	/// Un token è utilizzabile quando DataUtilizzo non è valorizzata.
	/// </summary>
	public bool EUtilizzabile => DataUtilizzo is null;
}
